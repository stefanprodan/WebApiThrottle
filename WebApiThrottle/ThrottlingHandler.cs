using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace WebApiThrottle
{
    public class ThrottlingHandler : DelegatingHandler
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ThrottlingHandler"/> class.
        /// By default, the <see cref="QuotaExceededResponseCode"/> property 
        /// is set to 409 (Conflict).
        /// </summary>
        public ThrottlingHandler()
        {
            QuotaExceededResponseCode = HttpStatusCode.Conflict;
        }

        /// <summary>
        /// Throttling rate limits policy
        /// </summary>
        public ThrottlePolicy Policy { get; set; }

        /// <summary>
        /// Throttle metrics storage
        /// </summary>
        public IThrottleRepository Repository { get; set; }

        /// <summary>
        /// Log traffic and blocked requests
        /// </summary>
        public IThrottleLogger Logger { get; set; }

        /// <summary>
        /// If none specifed the default will be: 
        /// API calls quota exceeded! maximum admitted {0} per {1}
        /// </summary>
        public string QuotaExceededMessage { get; set; }

        /// <summary>
        /// Gets or sets the value to return as the HTTP status 
        /// code when a request is rejected because of the
        /// throttling policy.  The default value is 409 (Conflict)
        /// </summary>
        public HttpStatusCode QuotaExceededResponseCode { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            //apply policy
            //the IP rules are applied last and will overwrite any client rule you might defined
            if (!Policy.IpThrottling && !Policy.ClientThrottling && Policy.EndpointThrottling)
                return base.SendAsync(request, cancellationToken);

            var identity = SetIndentity(request);
            if (IsWhitelisted(Policy, identity))
                return base.SendAsync(request, cancellationToken);

            TimeSpan timeSpan = TimeSpan.FromSeconds(1);

            var rates = Policy.Rates.AsEnumerable();
            if (Policy.StackBlockedRequests)
            {
                //all requests including the rejected ones will stack in this order: day, hour, min, sec
                //if a client hits the hour limit then the minutes and seconds counters will expire and will eventually get erased from cache
                rates = Policy.Rates.Reverse();
            }

            foreach (var rate in rates)
            {
                var rateLimitPeriod = rate.Key;
                var rateLimit = rate.Value;

                switch (rateLimitPeriod)
                {
                    case RateLimitPeriod.Second:
                        timeSpan = TimeSpan.FromSeconds(1);
                        break;
                    case RateLimitPeriod.Minute:
                        timeSpan = TimeSpan.FromMinutes(1);
                        break;
                    case RateLimitPeriod.Hour:
                        timeSpan = TimeSpan.FromHours(1);
                        break;
                    case RateLimitPeriod.Day:
                        timeSpan = TimeSpan.FromDays(1);
                        break;
                }

                //increment counter
                string requestId;
                var throttleCounter = ProcessRequest(Policy, identity, timeSpan, rateLimitPeriod, out requestId);

                if (throttleCounter.Timestamp + timeSpan < DateTime.UtcNow)
                    continue;

                //apply endpoint rate limits
                if (Policy.EndpointRules != null)
                {
                    var rules = Policy.EndpointRules.Where(x => identity.Endpoint.Contains(x.Key)).ToList();
                    if (rules.Any())
                    {
                        //get the lower limit from all applying rules
                        var customRate = (from r in rules let rateValue = r.Value.GetLimit(rateLimitPeriod) select rateValue).Min();

                        if (customRate > 0)
                        {
                            rateLimit = customRate;
                        }
                    }
                }

                //apply custom rate limit for clients that will override endpoint limits
                if (Policy.ClientRules != null && Policy.ClientRules.Keys.Contains(identity.ClientKey))
                {
                    var limit = Policy.ClientRules[identity.ClientKey].GetLimit(rateLimitPeriod);
                    if (limit > 0) rateLimit = limit;
                }

                //enforce ip rate limit as is most specific 
                string ipRule = null;
                if (Policy.IpRules != null && ContainsIp(Policy.IpRules.Keys.ToList(), identity.ClientIp, out ipRule))
                {
                    var limit = Policy.IpRules[ipRule].GetLimit(rateLimitPeriod);
                    if (limit > 0) rateLimit = limit;
                }

                //check if limit is reached
                if (rateLimit > 0 && throttleCounter.TotalRequests > rateLimit)
                {
                    //log blocked request
                    if (Logger != null) Logger.Log(ComputeLogEntry(requestId, identity, throttleCounter, rateLimitPeriod.ToString(), rateLimit, request));

                    //break execution and return 409 
                    string message;
                    if (!string.IsNullOrEmpty(QuotaExceededMessage))
                        message = QuotaExceededMessage;
                    else
                        message = "API calls quota exceeded! maximum admitted {0} per {1}.";

                    return QuotaExceededResponse(request, string.Format(message, rateLimit, rateLimitPeriod), QuotaExceededResponseCode);
                }
            }

            //no throttling required
            return base.SendAsync(request, cancellationToken);
        }

        protected virtual RequestIdentity SetIndentity(HttpRequestMessage request)
        {
            var entry = new RequestIdentity();
            entry.ClientIp = GetClientIp(request).ToString();
            entry.Endpoint = request.RequestUri.AbsolutePath;
            entry.ClientKey = request.Headers.Contains("Authorization-Token") ? request.Headers.GetValues("Authorization-Token").First() : "anon";

            return entry;
        }

        static readonly object _processLocker = new object();
        private ThrottleCounter ProcessRequest(ThrottlePolicy throttlePolicy, RequestIdentity requestIdentity, TimeSpan timeSpan, RateLimitPeriod period, out string id)
        {
            var throttleCounter = new ThrottleCounter()
                {
                    Timestamp = DateTime.UtcNow,
                    TotalRequests = 1
                };

            id = ComputeStoreKey(throttlePolicy, requestIdentity, period);

            //get the hash value of the computed id
            var hashId = ComputeHash(id);

            //serial reads and writes
            lock (_processLocker)
            {
                var entry = Repository.FirstOrDefault(hashId);
                if (entry.HasValue)
                {
                    //entry has not expired
                    if (entry.Value.Timestamp + timeSpan >= DateTime.UtcNow)
                    {
                        //increment request count
                        var totalRequests = entry.Value.TotalRequests + 1;

                        //deep copy
                        throttleCounter = new ThrottleCounter
                        {
                            Timestamp = entry.Value.Timestamp,
                            TotalRequests = totalRequests
                        };

                    }
                }

                //stores: id (string) - timestamp (datetime) - total (long)
                Repository.Save(hashId, throttleCounter, timeSpan);
            }

            return throttleCounter;
        }

        protected virtual string ComputeHash(string s)
        {
            return BitConverter.ToString(new System.Security.Cryptography.SHA1Managed().ComputeHash(System.Text.Encoding.UTF8.GetBytes(s))).Replace("-", "");
        }

        protected virtual string ComputeStoreKey(ThrottlePolicy throttlePolicy, RequestIdentity requestIdentity, RateLimitPeriod period)
        {
            var keyValues = new List<string>()
                {
                    "throttle"
                };

            if (throttlePolicy.IpThrottling)
                keyValues.Add(requestIdentity.ClientIp);

            if (throttlePolicy.ClientThrottling)
                keyValues.Add(requestIdentity.ClientKey);

            if (throttlePolicy.EndpointThrottling)
                keyValues.Add(requestIdentity.Endpoint);

            keyValues.Add(period.ToString());

            var id = string.Join("_", keyValues);
            return id;
        }

        protected IPAddress GetClientIp(HttpRequestMessage request)
        {
            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                return IPAddress.Parse(((HttpContextBase)request.Properties["MS_HttpContext"]).Request.UserHostAddress);
            }

            if (request.Properties.ContainsKey(RemoteEndpointMessageProperty.Name))
            {
                return IPAddress.Parse(((RemoteEndpointMessageProperty)request.Properties[RemoteEndpointMessageProperty.Name]).Address);
            }

            return null;
        }

        private bool IsWhitelisted(ThrottlePolicy throttlePolicy, RequestIdentity requestIdentity)
        {
            if (throttlePolicy.IpThrottling)
                if (throttlePolicy.IpWhitelist != null && ContainsIp(throttlePolicy.IpWhitelist, requestIdentity.ClientIp))
                    return true;

            if (throttlePolicy.ClientThrottling)
                if (throttlePolicy.ClientWhitelist != null && throttlePolicy.ClientWhitelist.Contains(requestIdentity.ClientKey))
                    return true;

            if (throttlePolicy.EndpointThrottling)
                if (throttlePolicy.EndpointWhitelist != null && Policy.EndpointWhitelist.Any(x => requestIdentity.Endpoint.Contains(x)))
                    return true;

            return false;
        }

        private bool ContainsIp(List<string> ipRules, string clientIp)
        {
            var ip = IPAddress.Parse(clientIp);
            if (ipRules != null && ipRules.Any())
            {
                foreach (var rule in ipRules)
                {
                    var range = new IPAddressRange(rule);
                    if (range.Contains(ip)) return true;
                }

            }

            return false;
        }

        private bool ContainsIp(List<string> ipRules, string clientIp, out string rule)
        {
            rule = null;
            var ip = IPAddress.Parse(clientIp);
            if (ipRules != null && ipRules.Any())
            {
                foreach (var r in ipRules)
                {
                    var range = new IPAddressRange(r);
                    if (range.Contains(ip))
                    {
                        rule = r;
                        return true;
                    }
                }
            }

            return false;
        }

        private Task<HttpResponseMessage> QuotaExceededResponse(HttpRequestMessage request, string message, HttpStatusCode responseCode)
        {
            return Task.FromResult(request.CreateResponse(responseCode, message));
        }

        private ThrottleLogEntry ComputeLogEntry(string requestId, RequestIdentity identity, ThrottleCounter throttleCounter, string rateLimitPeriod, long rateLimit, HttpRequestMessage request)
        {
            return new ThrottleLogEntry
                    {
                        ClientIp = identity.ClientIp,
                        ClientKey = identity.ClientKey,
                        Endpoint = identity.Endpoint,
                        LogDate = DateTime.UtcNow,
                        RateLimit = rateLimit,
                        RateLimitPeriod = rateLimitPeriod,
                        RequestId = requestId,
                        StartPeriod = throttleCounter.Timestamp,
                        TotalRequests = throttleCounter.TotalRequests,
                        Request = request
                    };
        }
    }
}
