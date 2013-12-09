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

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var identity = SetIndentity(request);
            TimeSpan timeSpan = TimeSpan.FromSeconds(1);

            //apply policy
            //the IP rules are applied last and will overwrite any client rule you might defined
            if (Policy.IpThrottling || Policy.ClientThrottling || Policy.EndpointThrottling)
            {
                var rates = Policy.Rates.AsEnumerable();
                if(Policy.StackBlockedRequests)
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

                    if (throttleCounter.Timestamp + timeSpan > DateTime.UtcNow)
                    {
                        //apply endpoint rate limits
                        if (Policy.EndpointRules != null)
                        {
                            var rule = Policy.EndpointRules.Keys.FirstOrDefault(x => identity.Endpoint.Contains(x));
                            if (!string.IsNullOrEmpty(rule))
                            {
                                var limit = Policy.EndpointRules[rule].GetLimit(rateLimitPeriod);
                                if (limit > 0) rateLimit = limit;
                            }
                        }

                        //apply custom rate limit for clients that will override endpoint limits
                        if (Policy.ClientRules != null && Policy.ClientRules.Keys.Contains(identity.ClientKey))
                        {
                            var limit = Policy.ClientRules[identity.ClientKey].GetLimit(rateLimitPeriod);
                            if (limit > 0) rateLimit = limit;
                        }

                        //enforce ip rate limit as is most specific 
                        if (Policy.IpRules != null && Policy.IpRules.Keys.Contains(identity.ClientIp))
                        {
                            var limit = Policy.IpRules[identity.ClientIp].GetLimit(rateLimitPeriod);
                            if (limit > 0) rateLimit = limit;
                        }

                        //check if limit is reached
                        if (rateLimit > 0 && throttleCounter.TotalRequests > rateLimit)
                        {
                            //log blocked request
                            if (Logger != null) Logger.Log(ComputeLogEntry(requestId, identity, throttleCounter, rateLimitPeriod.ToString(), rateLimit));

                            //break execution and return 409 
                            var message = string.IsNullOrEmpty(QuotaExceededMessage) ?
                                "API calls quota exceeded! maximum admitted {0} per {1}" : QuotaExceededMessage;
                            return QuotaExceededResponse(request, string.Format(message, rateLimit, rateLimitPeriod));
                        }
                    }
                }
            }

            //no throttling required
            return base.SendAsync(request, cancellationToken);
        }

        protected virtual RequestIndentity SetIndentity(HttpRequestMessage request)
        {
            var entry = new RequestIndentity();
            entry.ClientIp = GetClientIp(request).ToString();
            entry.Endpoint = request.RequestUri.AbsolutePath;

            entry.ClientKey = request.Headers.Contains("Authorization-Token") ? request.Headers.GetValues("Authorization-Token").First() : "anon";

            return entry;
        }

        static readonly object _processLocker = new object();
        private ThrottleCounter ProcessRequest(ThrottlePolicy throttlePolicy, RequestIndentity throttleEntry, TimeSpan timeSpan, RateLimitPeriod period, out string id)
        {
            ThrottleCounter throttleCounter = new ThrottleCounter();

            //computed request unique id from IP, client key, url and period
            id = "throttle";

            if (throttlePolicy.IpThrottling)
            {
                if (throttlePolicy.IpWhitelist != null && throttlePolicy.IpWhitelist.Contains(throttleEntry.ClientIp))
                {
                    return throttleCounter;
                }

                id += "_" + throttleEntry.ClientIp;
            }

            if (throttlePolicy.ClientThrottling)
            {
                if (throttlePolicy.ClientWhitelist != null && throttlePolicy.ClientWhitelist.Contains(throttleEntry.ClientKey))
                {
                    return throttleCounter;
                }

                id += "_" + throttleEntry.ClientKey;
            }

            if (throttlePolicy.EndpointThrottling)
            {
                if (throttlePolicy.EndpointWhitelist != null && Policy.EndpointWhitelist.Any(x => throttleEntry.Endpoint.Contains(x)))
                {
                    return throttleCounter;
                }

                id += "_" + throttleEntry.Endpoint;
            }

            id += "_" + period;

            //get the hash value of the computed id
            var hashId = ComputeHash(id);

            //serial reads and writes
            lock (_processLocker)
            {
                var entry = Repository.FirstOrDefault(hashId);
                if (entry != null)
                {
                    throttleCounter = entry;
                    throttleCounter.TotalRequests++;

                    if (entry.Timestamp + timeSpan < DateTime.UtcNow)
                    {
                        throttleCounter = new ThrottleCounter();
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

        private Task<HttpResponseMessage> QuotaExceededResponse(HttpRequestMessage request, string message)
        {
            return Task.FromResult(request.CreateResponse(HttpStatusCode.Conflict, message));
        }

        private ThrottleLogEntry ComputeLogEntry(string requestId, RequestIndentity identity, ThrottleCounter throttleCounter, string rateLimitPeriod, long rateLimit)
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
                        TotalRequests = throttleCounter.TotalRequests
                    };
        }
    }

}
