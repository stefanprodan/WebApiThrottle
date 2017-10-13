using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using WebApiThrottle.Attributes;
using WebApiThrottle.Logging;
using WebApiThrottle.Models;
using WebApiThrottle.Net;
using WebApiThrottle.Repositories;

namespace WebApiThrottle
{
    /// <summary>
    ///     Throttle action filter
    /// </summary>
    // ReSharper disable once RedundantExtendsListEntry
    public class ThrottlingFilter : ActionFilterAttribute, IActionFilter
    {
        private readonly ThrottlingCore _core;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ThrottlingFilter" /> class.
        ///     By default, the <see cref="QuotaExceededResponseCode" /> property
        ///     is set to 429 (Too Many Requests).
        /// </summary>
        public ThrottlingFilter()
        {
            QuotaExceededResponseCode = (HttpStatusCode) 429;
            Repository = new CacheRepository();
            _core = new ThrottlingCore();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ThrottlingFilter" /> class.
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
        ///     The ip address provider
        /// </param>
        public ThrottlingFilter(ThrottlePolicy policy,
            IPolicyRepository policyRepository,
            IThrottleRepository repository,
            IThrottleLogger logger,
            IIpAddressParser ipAddressParser = null)
        {
            _core = new ThrottlingCore();
            _core.Repository = repository;
            Repository = repository;
            Logger = logger;
            if (ipAddressParser != null)
                _core.IpAddressParser = ipAddressParser;

            QuotaExceededResponseCode = (HttpStatusCode) 429;

            Policy = policy;
            PolicyRepository = policyRepository;

            if (policyRepository != null)
                policyRepository.Save(ThrottleManager.GetPolicyKey(), policy);
        }

        /// <summary>
        ///     Gets or sets a repository used to access throttling rate limits policy.
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
        ///     Gets or sets an instance of <see cref="IThrottleLogger" /> that will log blocked requests
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

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var applyThrottling = ApplyThrottling(actionContext, out EnableThrottlingAttribute attrPolicy);

            // get policy from repo
            if (PolicyRepository != null)
                Policy = PolicyRepository.FirstOrDefault(ThrottleManager.GetPolicyKey());

            if (Policy != null && applyThrottling)
            {
                _core.Repository = Repository;
                _core.Policy = Policy;

                var identity = SetIdentity(actionContext.Request);

                if (!_core.IsWhitelisted(identity))
                {
                    // get default rates
                    var defRates = _core.RatesWithDefaults(Policy.Rates.ToList());
                    if (Policy.StackBlockedRequests)
                        defRates.Reverse();

                    // apply policy
                    foreach (var rate in defRates)
                    {
                        var rateLimitPeriod = rate.Key;
                        var rateLimit = rate.Value;

                        var timeSpan = _core.GetTimeSpanFromPeriod(rateLimitPeriod);

                        // apply EnableThrottlingAttribute policy
                        var attrLimit = attrPolicy.GetLimit(rateLimitPeriod);
                        if (attrLimit > 0)
                            rateLimit = attrLimit;

                        // apply global rules
                        _core.ApplyRules(identity, timeSpan, rateLimitPeriod, ref rateLimit);

                        if (rateLimit <= 0) continue;

                        // increment counter
                        var requestId = ComputeThrottleKey(identity, rateLimitPeriod);
                        var throttleCounter = _core.ProcessRequest(timeSpan, requestId);

                        // check if key expired
                        if (throttleCounter.Timestamp + timeSpan < DateTime.UtcNow)
                            continue;

                        // check if limit is reached
                        if (throttleCounter.TotalRequests <= rateLimit) continue;
                        // log blocked request
                        Logger?.Log(_core.ComputeLogEntry(requestId, identity, throttleCounter,
                            rateLimitPeriod.ToString(), rateLimit, actionContext.Request));

                        var message = !string.IsNullOrEmpty(QuotaExceededMessage)
                            ? QuotaExceededMessage
                            : "API calls quota exceeded! maximum admitted {0} per {1}.";

                        var content = QuotaExceededContent != null
                            ? QuotaExceededContent(rateLimit, rateLimitPeriod)
                            : string.Format(message, rateLimit, rateLimitPeriod);

                        // add status code and retry after x seconds to response
                        actionContext.Response = QuotaExceededResponse(
                            actionContext.Request,
                            content,
                            QuotaExceededResponseCode,
                            _core.RetryAfterFrom(throttleCounter.Timestamp, rateLimitPeriod));
                    }
                }
            }

            base.OnActionExecuting(actionContext);
        }

        protected virtual RequestIdentity SetIdentity(HttpRequestMessage request)
        {
            var entry = new RequestIdentity
            {
                ClientIp = _core.GetClientIp(request).ToString(),
                Endpoint = request.RequestUri.AbsolutePath.ToLowerInvariant(),
                ClientKey = request.Headers.Contains("Authorization-Token")
                    ? request.Headers.GetValues("Authorization-Token").First()
                    : "anon"
            };

            return entry;
        }

        protected virtual string ComputeThrottleKey(RequestIdentity requestIdentity, RateLimitPeriod period)
        {
            return _core.ComputeThrottleKey(requestIdentity, period);
        }

        protected IPAddress GetClientIp(HttpRequestMessage request)
        {
            return _core.GetClientIp(request);
        }

        protected virtual HttpResponseMessage QuotaExceededResponse(HttpRequestMessage request, object content,
            HttpStatusCode responseCode, string retryAfter)
        {
            var response = request.CreateResponse(responseCode, content);
            response.Headers.Add("Retry-After", new[] {retryAfter});
            return response;
        }

        private bool ApplyThrottling(HttpActionContext filterContext, out EnableThrottlingAttribute attr)
        {
            var applyThrottling = false;
            attr = null;

            if (filterContext.ActionDescriptor.ControllerDescriptor.GetCustomAttributes<EnableThrottlingAttribute>(true)
                .Any())
            {
                attr = filterContext.ActionDescriptor.ControllerDescriptor
                    .GetCustomAttributes<EnableThrottlingAttribute>(true).First();
                applyThrottling = true;
            }

            if (filterContext.ActionDescriptor.GetCustomAttributes<EnableThrottlingAttribute>(true).Any())
            {
                attr = filterContext.ActionDescriptor.GetCustomAttributes<EnableThrottlingAttribute>(true).First();
                applyThrottling = true;
            }

            // explicit disabled
            if (filterContext.ActionDescriptor.GetCustomAttributes<DisableThrottingAttribute>(true).Any())
                applyThrottling = false;

            return applyThrottling;
        }
    }
}