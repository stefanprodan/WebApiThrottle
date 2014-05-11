using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace WebApiThrottle
{
    /// <summary>
    /// Throttle action filter
    /// </summary>
    public class ThrottlingFilter : ActionFilterAttribute, IActionFilter
    {
        private ThrottlingCore core;

        /// <summary>
        /// Creates a new instance of the <see cref="ThrottlingFilter"/> class.
        /// By default, the <see cref="QuotaExceededResponseCode"/> property 
        /// is set to 429 (Too Many Requests).
        /// </summary>
        public ThrottlingFilter()
        {
            QuotaExceededResponseCode = (HttpStatusCode)429;
            Repository = new CacheRepository();
            core = new ThrottlingCore();
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ThrottlingFilter"/> class.
        /// Presists the policy object in cache using <see cref="IPolicyRepository"/> implementation.
        /// The policy object can be updated by <see cref="ThrottleManager"/> at runtime. 
        /// </summary>
        public ThrottlingFilter(ThrottlePolicy policy, IPolicyRepository policyRepository, IThrottleRepository repository, IThrottleLogger logger)
        {
            core = new ThrottlingCore();
            core.Repository = repository;
            Repository = repository;
            Logger = logger;

            QuotaExceededResponseCode = (HttpStatusCode)429;

            this.policy = policy;
            this.policyRepository = policyRepository;

            if (policyRepository != null)
            {
                policyRepository.Save(ThrottleManager.GetPolicyKey(), policy);
            }
        }

        private IPolicyRepository policyRepository;

        /// <summary>
        ///  Throttling rate limits policy repository
        /// </summary>
        public IPolicyRepository PolicyRepository
        {
            get { return policyRepository; }
            set { policyRepository = value; }
        }

        private ThrottlePolicy policy;

        /// <summary>
        /// Throttling rate limits policy
        /// </summary>
        public ThrottlePolicy Policy
        {
            get { return policy; }
            set { policy = value; }
        }

        /// <summary>
        /// Throttle metrics storage
        /// </summary>
        public IThrottleRepository Repository { get; set; }

        /// <summary>
        /// Log blocked requests
        /// </summary>
        public IThrottleLogger Logger { get; set; }

        /// <summary>
        /// If none specifed the default will be: 
        /// HTTP request quota exceeded! maximum admitted {0} per {1}
        /// </summary>
        public string QuotaExceededMessage { get; set; }

        /// <summary>
        /// Gets or sets the value to return as the HTTP status 
        /// code when a request is rejected because of the
        /// throttling policy. The default value is 429 (Too Many Requests).
        /// </summary>
        public HttpStatusCode QuotaExceededResponseCode { get; set; }

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            EnableThrottlingAttribute attrPolicy = null;
            var applyThrottling = ApplyThrottling(actionContext, out attrPolicy);

            //get policy from repo
            if(policyRepository != null)
            {
                policy = policyRepository.FirstOrDefault(ThrottleManager.GetPolicyKey());
            }

            if (Policy != null && applyThrottling)
            {
                core.Repository = Repository;
                core.Policy = Policy;

                var identity = SetIndentity(actionContext.Request);

                if (!core.IsWhitelisted(identity))
                {
                    TimeSpan timeSpan = TimeSpan.FromSeconds(1);

                    //get default rates
                    var defRates = core.RatesWithDefaults(Policy.Rates.ToList());
                    if (Policy.StackBlockedRequests)
                    {
                        //all requests including the rejected ones will stack in this order: week, day, hour, min, sec
                        //if a client hits the hour limit then the minutes and seconds counters will expire and will eventually get erased from cache
                        defRates.Reverse();
                    }

                    //apply policy
                    foreach (var rate in defRates)
                    {
                        var rateLimitPeriod = rate.Key;
                        var rateLimit = rate.Value;

                        timeSpan = core.GetTimeSpanFromPeriod(rateLimitPeriod);

                        //apply EnableThrottlingAttribute policy
                        var attrLimit = attrPolicy.GetLimit(rateLimitPeriod);
                        if (attrLimit > 0)
                        {
                            rateLimit = attrLimit;
                        }

                        //apply global rules
                        core.ApplyRules(identity, timeSpan, rateLimitPeriod, ref rateLimit);

                        if (rateLimit > 0)
                        {
                            //increment counter
                            string requestId;
                            var throttleCounter = core.ProcessRequest(identity, timeSpan, rateLimitPeriod, out requestId);

                            //check if key expired
                            if (throttleCounter.Timestamp + timeSpan < DateTime.UtcNow)
                                continue;

                            //check if limit is reached
                            if (throttleCounter.TotalRequests > rateLimit)
                            {
                                //log blocked request
                                if (Logger != null) Logger.Log(core.ComputeLogEntry(requestId, identity, throttleCounter, rateLimitPeriod.ToString(), rateLimit, actionContext.Request));

                                string message;
                                if (!string.IsNullOrEmpty(QuotaExceededMessage))
                                    message = QuotaExceededMessage;
                                else
                                    message = "API calls quota exceeded! maximum admitted {0} per {1}.";

                                //add status code and retry after x seconds to response
                                actionContext.Response = QuotaExceededResponse(actionContext.Request,
                                    string.Format(message, rateLimit, rateLimitPeriod),
                                    QuotaExceededResponseCode,
                                    core.RetryAfterFrom(throttleCounter.Timestamp, rateLimitPeriod));
                            }
                        }
                    }
                }
            }

            base.OnActionExecuting(actionContext);
        }

        protected virtual RequestIdentity SetIndentity(HttpRequestMessage request)
        {
            var entry = new RequestIdentity();
            entry.ClientIp = core.GetClientIp(request).ToString();
            entry.Endpoint = request.RequestUri.AbsolutePath.ToLowerInvariant();
            entry.ClientKey = request.Headers.Contains("Authorization-Token") ? request.Headers.GetValues("Authorization-Token").First() : "anon";

            return entry;
        }

        protected virtual string ComputeThrottleKey(RequestIdentity requestIdentity, RateLimitPeriod period)
        {
            return core.ComputeThrottleKey(requestIdentity, period);
        }

        protected IPAddress GetClientIp(HttpRequestMessage request)
        {
            return core.GetClientIp(request);
        }

        private bool ApplyThrottling(HttpActionContext filterContext, out EnableThrottlingAttribute attr)
        {
            var applyThrottling = false;
            attr = null;

            if (filterContext.ActionDescriptor.ControllerDescriptor.GetCustomAttributes<EnableThrottlingAttribute>(true).Any())
            {
                attr = (EnableThrottlingAttribute)filterContext.ActionDescriptor.ControllerDescriptor.GetCustomAttributes<EnableThrottlingAttribute>(true).First();
                applyThrottling = true;
            }

            if (filterContext.ActionDescriptor.GetCustomAttributes<EnableThrottlingAttribute>(true).Any())
            {
                attr = (EnableThrottlingAttribute)filterContext.ActionDescriptor.GetCustomAttributes<EnableThrottlingAttribute>(true).First();
                applyThrottling = true;
            }

            //explicit disabled
            if (filterContext.ActionDescriptor.GetCustomAttributes<DisableThrottingAttribute>(true).Any())
            {
                applyThrottling = false;
            }

            return applyThrottling;
        }

        protected virtual HttpResponseMessage QuotaExceededResponse(HttpRequestMessage request, string message, HttpStatusCode responseCode, string retryAfter)
        {
            var response = request.CreateResponse(responseCode, message);
            response.Headers.Add("Retry-After", new string[] { retryAfter });
            return response;
        }
    }
}
