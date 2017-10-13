using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WebApiThrottle.Logging;
using WebApiThrottle.Models;
using WebApiThrottle.Net;
using WebApiThrottle.Repositories;

namespace WebApiThrottle
{
    /// <summary>
    ///     Throttle message handler
    /// </summary>
    public class ThrottlingHandler : DelegatingHandler
    {
        private readonly ThrottlingCore core;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ThrottlingHandler" /> class.
        ///     By default, the <see cref="QuotaExceededResponseCode" /> property
        ///     is set to 429 (Too Many Requests).
        /// </summary>
        public ThrottlingHandler()
        {
            QuotaExceededResponseCode = (HttpStatusCode) 429;
            Repository = new CacheRepository();
            core = new ThrottlingCore();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ThrottlingHandler" /> class.
        ///     Persists the policy object in cache using <see cref="IPolicyRepository" /> implementation.
        ///     The policy object can be updated by <see cref="ThrottleManager" /> at runtime.
        /// </summary>
        /// <param name="policy">
        ///     The policy.
        /// </param>
        /// <param name="policyRepository">
        ///     The policy repository.
        /// </param>
        /// <param name="repository">
        ///     The repository.
        /// </param>
        /// <param name="logger">
        ///     The logger.
        /// </param>
        /// <param name="ipAddressParser">
        ///     The IpAddressParser
        /// </param>
        public ThrottlingHandler(ThrottlePolicy policy,
            IPolicyRepository policyRepository,
            IThrottleRepository repository,
            IThrottleLogger logger,
            IIpAddressParser ipAddressParser = null)
        {
            core = new ThrottlingCore();
            core.Repository = repository;
            Repository = repository;
            Logger = logger;

            if (ipAddressParser != null)
                core.IpAddressParser = ipAddressParser;

            QuotaExceededResponseCode = (HttpStatusCode) 429;

            Policy = policy;
            PolicyRepository = policyRepository;

            if (policyRepository != null)
                policyRepository.Save(ThrottleManager.GetPolicyKey(), policy);
        }

        /// <summary>
        ///     Gets or sets the throttling rate limits policy repository
        /// </summary>
        public IPolicyRepository PolicyRepository { get; set; }

        /// <summary>
        ///     Gets or sets the throttling rate limits policy
        /// </summary>
        public ThrottlePolicy Policy { get; set; }

        /// <summary>
        ///     Gets or sets the throttle metrics storage
        /// </summary>
        public IThrottleRepository Repository { get; set; }

        /// <summary>
        ///     Gets or sets an instance of <see cref="IThrottleLogger" /> that logs traffic and blocked requests
        /// </summary>
        public IThrottleLogger Logger { get; set; }

        /// <summary>
        ///     Gets or sets a value that will be used as a formatter for the QuotaExceeded response message.
        ///     If none specified the default will be:
        ///     API calls quota exceeded! maximum admitted {0} per {1}
        /// </summary>
        public string QuotaExceededMessage { get; set; }

        /// <summary>
        ///     Gets or sets a value that will be used as a formatter for the QuotaExceeded response message.
        ///     If none specified the default will be:
        ///     API calls quota exceeded! maximum admitted {0} per {1}
        /// </summary>
        public Func<long, RateLimitPeriod, object> QuotaExceededContent { get; set; }

        /// <summary>
        ///     Gets or sets the value to return as the HTTP status
        ///     code when a request is rejected because of the
        ///     throttling policy. The default value is 429 (Too Many Requests).
        /// </summary>
        public HttpStatusCode QuotaExceededResponseCode { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // get policy from repo
            if (PolicyRepository != null)
                Policy = PolicyRepository.FirstOrDefault(ThrottleManager.GetPolicyKey());

            if (Policy == null || !Policy.IpThrottling && !Policy.ClientThrottling && !Policy.EndpointThrottling)
                return base.SendAsync(request, cancellationToken);

            core.Repository = Repository;
            core.Policy = Policy;

            var identity = SetIdentity(request);

            if (core.IsWhitelisted(identity))
                return base.SendAsync(request, cancellationToken);

            var timeSpan = TimeSpan.FromSeconds(1);

            // get default rates
            var defRates = core.RatesWithDefaults(Policy.Rates.ToList());
            if (Policy.StackBlockedRequests)
                defRates.Reverse();

            // apply policy
            foreach (var rate in defRates)
            {
                var rateLimitPeriod = rate.Key;
                var rateLimit = rate.Value;

                timeSpan = core.GetTimeSpanFromPeriod(rateLimitPeriod);

                // apply global rules
                core.ApplyRules(identity, timeSpan, rateLimitPeriod, ref rateLimit);

                if (rateLimit > 0)
                {
                    // increment counter
                    var requestId = ComputeThrottleKey(identity, rateLimitPeriod);
                    var throttleCounter = core.ProcessRequest(timeSpan, requestId);

                    // check if key expired
                    if (throttleCounter.Timestamp + timeSpan < DateTime.UtcNow)
                        continue;

                    // check if limit is reached
                    if (throttleCounter.TotalRequests > rateLimit)
                    {
                        // log blocked request
                        if (Logger != null)
                            Logger.Log(core.ComputeLogEntry(requestId, identity, throttleCounter,
                                rateLimitPeriod.ToString(), rateLimit, request));

                        var message = !string.IsNullOrEmpty(QuotaExceededMessage)
                            ? QuotaExceededMessage
                            : "API calls quota exceeded! maximum admitted {0} per {1}.";

                        var content = QuotaExceededContent != null
                            ? QuotaExceededContent(rateLimit, rateLimitPeriod)
                            : string.Format(message, rateLimit, rateLimitPeriod);

                        // break execution
                        return QuotaExceededResponse(
                            request,
                            content,
                            QuotaExceededResponseCode,
                            core.RetryAfterFrom(throttleCounter.Timestamp, rateLimitPeriod));
                    }
                }
            }

            // no throttling required
            return base.SendAsync(request, cancellationToken);
        }

        protected IPAddress GetClientIp(HttpRequestMessage request)
        {
            return core.GetClientIp(request);
        }

        protected virtual RequestIdentity SetIdentity(HttpRequestMessage request)
        {
            var entry = new RequestIdentity();
            entry.ClientIp = core.GetClientIp(request).ToString();
            entry.Endpoint = request.RequestUri.AbsolutePath.ToLowerInvariant();
            entry.ClientKey = request.Headers.Contains("Authorization-Token")
                ? request.Headers.GetValues("Authorization-Token").First()
                : "anon";

            return entry;
        }

        protected virtual string ComputeThrottleKey(RequestIdentity requestIdentity, RateLimitPeriod period)
        {
            return core.ComputeThrottleKey(requestIdentity, period);
        }

        protected virtual Task<HttpResponseMessage> QuotaExceededResponse(HttpRequestMessage request, object content,
            HttpStatusCode responseCode, string retryAfter)
        {
            var response = request.CreateResponse(responseCode, content);
            response.Headers.Add("Retry-After", new[] {retryAfter});
            return Task.FromResult(response);
        }
    }
}