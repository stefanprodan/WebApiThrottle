using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Owin;
using WebApiThrottle.Logging;
using WebApiThrottle.Models;
using WebApiThrottle.Net;
using WebApiThrottle.Repositories;

namespace WebApiThrottle
{
    public class ThrottlingMiddleware : OwinMiddleware
    {
        private readonly ThrottlingCore core;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ThrottlingMiddleware" /> class.
        ///     By default, the <see cref="QuotaExceededResponseCode" /> property
        ///     is set to 429 (Too Many Requests).
        /// </summary>
        public ThrottlingMiddleware(OwinMiddleware next)
            : base(next)
        {
            QuotaExceededResponseCode = (HttpStatusCode) 429;
            Repository = new CacheRepository();
            core = new ThrottlingCore();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ThrottlingMiddleware" /> class.
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
        public ThrottlingMiddleware(OwinMiddleware next,
            ThrottlePolicy policy,
            IPolicyRepository policyRepository,
            IThrottleRepository repository,
            IThrottleLogger logger,
            IIpAddressParser ipAddressParser)
            : base(next)
        {
            core = new ThrottlingCore
            {
                Repository = repository
            };
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
        ///     Gets or sets the value to return as the HTTP status
        ///     code when a request is rejected because of the
        ///     throttling policy. The default value is 429 (Too Many Requests).
        /// </summary>
        public HttpStatusCode QuotaExceededResponseCode { get; set; }

        public override async Task Invoke(IOwinContext context)
        {
            var response = context.Response;
            var request = context.Request;

            // get policy from repo
            if (PolicyRepository != null)
                Policy = PolicyRepository.FirstOrDefault(ThrottleManager.GetPolicyKey());

            if (Policy == null || !Policy.IpThrottling && !Policy.ClientThrottling && !Policy.EndpointThrottling)
            {
                await Next.Invoke(context);
                return;
            }

            core.Repository = Repository;
            core.Policy = Policy;

            var identity = SetIdentity(request);

            if (core.IsWhitelisted(identity))
            {
                await Next.Invoke(context);
                return;
            }

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
                                rateLimitPeriod.ToString(), rateLimit, null));

                        var message = !string.IsNullOrEmpty(QuotaExceededMessage)
                            ? QuotaExceededMessage
                            : "API calls quota exceeded! maximum admitted {0} per {1}.";

                        // break execution
                        response.OnSendingHeaders(state =>
                        {
                            var resp = (OwinResponse) state;
                            resp.Headers.Add("Retry-After",
                                new[] {core.RetryAfterFrom(throttleCounter.Timestamp, rateLimitPeriod)});
                            resp.StatusCode = (int) QuotaExceededResponseCode;
                            resp.ReasonPhrase = string.Format(message, rateLimit, rateLimitPeriod);
                        }, response);

                        return;
                    }
                }
            }

            // no throttling required
            await Next.Invoke(context);
        }

        protected virtual RequestIdentity SetIdentity(IOwinRequest request)
        {
            var entry = new RequestIdentity
            {
                ClientIp = request.RemoteIpAddress,
                Endpoint = request.Uri.AbsolutePath.ToLowerInvariant(),
                ClientKey = request.Headers.Keys.Contains("Authorization-Token")
                    ? request.Headers.GetValues("Authorization-Token").First()
                    : "anon"
            };

            return entry;
        }

        protected virtual string ComputeThrottleKey(RequestIdentity requestIdentity, RateLimitPeriod period)
        {
            return core.ComputeThrottleKey(requestIdentity, period);
        }
    }
}