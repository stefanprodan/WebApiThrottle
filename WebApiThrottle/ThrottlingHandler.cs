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
        public ThrottlePolicy Policy { get; set; }
        public IThrottleRepository Repository { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var identity = SetIndentity(request);
            TimeSpan timeSpan = TimeSpan.FromSeconds(1);

            //apply policy
            if (Policy.IpThrottling || Policy.ClientThrottling || Policy.EndpointThrottling)
                foreach (var rate in Policy.Rates)
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

                    var throttleCounter = ProcessRequest(Policy, identity, timeSpan, rateLimitPeriod);

                    if (throttleCounter.Timestamp + timeSpan > DateTime.UtcNow)
                    {
                        //get custom rate limit
                        if (Policy.IpRules != null && Policy.IpRules.Keys.Contains(identity.ClientIp))
                        {
                            rateLimit = Policy.IpRules[identity.ClientIp].GetLimit(rateLimitPeriod);
                        }

                        if (Policy.ClientRules != null && Policy.ClientRules.Keys.Contains(identity.ClientKey))
                        {
                            rateLimit = Policy.ClientRules[identity.ClientKey].GetLimit(rateLimitPeriod);
                        }

                        //check limit
                        if (rateLimit > 0 && throttleCounter.TotalRequests > rateLimit)
                        {
                            var id = identity.ToString() + "-" + rateLimitPeriod;
                            return QuotaExceededResponse(request, string.Format("API calls quota exceeded! maximum admitted {0} per {1} ID {2}", rateLimit, rateLimitPeriod, id));
                        }
                    }
                }

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

        private ThrottleCounter ProcessRequest(ThrottlePolicy throttlePolicy, RequestIndentity throttleEntry, TimeSpan timeSpan, RateLimitPeriod period)
        {
            ThrottleCounter throttleCounter = new ThrottleCounter();

            var key = "throttle";

            if (throttlePolicy.IpThrottling)
            {
                if (throttlePolicy.IpWhitelist != null && throttlePolicy.IpWhitelist.Contains(throttleEntry.ClientIp))
                {
                    return throttleCounter;
                }

                key += "_" + throttleEntry.ClientIp;
            }

            if (throttlePolicy.ClientThrottling)
            {
                if (throttlePolicy.ClientWhitelist != null && throttlePolicy.ClientWhitelist.Contains(throttleEntry.ClientKey))
                {
                    return throttleCounter;
                }

                key += "_" + throttleEntry.ClientKey;
            }

            if (throttlePolicy.EndpointThrottling)
            {
                if (throttlePolicy.EndpointWhitelist != null && throttlePolicy.EndpointWhitelist.Contains(throttleEntry.Endpoint))
                {
                    return throttleCounter;
                }

                key += "_" + throttleEntry.Endpoint;
            }

            key += "_" + period;

            var cacheKey = ComputeHash(key);
            var entry = Repository.FirstOrDefault(cacheKey);
            if (entry != null)
            {
                throttleCounter = entry;
                throttleCounter.TotalRequests++;

                if (entry.Timestamp + timeSpan < DateTime.UtcNow)
                {
                    throttleCounter = new ThrottleCounter();
                }
            }

            Repository.Save(cacheKey, throttleCounter, timeSpan);

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
    }

}
