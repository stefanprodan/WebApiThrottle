using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace WebApiThrottle
{
    /// <summary>
    /// Common code shared between ThrottlingHandler and ThrottlingFilter
    /// </summary>
    internal class ThrottlingCore
    {
        private static readonly object ProcessLocker = new object();

        internal ThrottlePolicy Policy { get; set; }

        internal IThrottleRepository Repository { get; set; }

        internal bool ContainsIp(List<string> ipRules, string clientIp)
        {
            var ip = IPAddress.Parse(clientIp);
            if (ipRules != null && ipRules.Any())
            {
                foreach (var rule in ipRules)
                {
                    var range = new IPAddressRange(rule);
                    if (range.Contains(ip))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal bool ContainsIp(List<string> ipRules, string clientIp, out string rule)
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

        internal IPAddress GetClientIp(HttpRequestMessage request)
        {
            IPAddress ipAddress;

            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                var ok = IPAddress.TryParse(((HttpContextBase)request.Properties["MS_HttpContext"]).Request.UserHostAddress, out ipAddress);

                if (ok)
                {
                    return ipAddress;
                }
            }

            if (request.Properties.ContainsKey(RemoteEndpointMessageProperty.Name))
            {
                var ok = IPAddress.TryParse(((RemoteEndpointMessageProperty)request.Properties[RemoteEndpointMessageProperty.Name]).Address, out ipAddress);

                if (ok)
                {
                    return ipAddress;
                }
            }

            if (request.Properties.ContainsKey("MS_OwinContext"))
            {
                var ok = IPAddress.TryParse(((Microsoft.Owin.OwinContext)request.Properties["MS_OwinContext"]).Request.RemoteIpAddress, out ipAddress);

                if (ok)
                {
                    return ipAddress;
                }
            }


            return null;
        }

        internal ThrottleLogEntry ComputeLogEntry(string requestId, RequestIdentity identity, ThrottleCounter throttleCounter, string rateLimitPeriod, long rateLimit, HttpRequestMessage request)
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

        internal string RetryAfterFrom(DateTime timestamp, RateLimitPeriod period)
        {
            var secondsPast = Convert.ToInt32((DateTime.UtcNow - timestamp).TotalSeconds);
            var retryAfter = 1;
            switch (period)
            {
                case RateLimitPeriod.Minute:
                    retryAfter = 60;
                    break;
                case RateLimitPeriod.Hour:
                    retryAfter = 60 * 60;
                    break;
                case RateLimitPeriod.Day:
                    retryAfter = 60 * 60 * 24;
                    break;
                case RateLimitPeriod.Week:
                    retryAfter = 60 * 60 * 24 * 7;
                    break;
            }
            retryAfter = retryAfter > 1 ? retryAfter - secondsPast : 1;
            return retryAfter.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        internal bool IsWhitelisted(RequestIdentity requestIdentity)
        {
            if (Policy.IpThrottling)
            {
                if (Policy.IpWhitelist != null && ContainsIp(Policy.IpWhitelist, requestIdentity.ClientIp))
                {
                    return true;
                }
            }

            if (Policy.ClientThrottling)
            {
                if (Policy.ClientWhitelist != null && Policy.ClientWhitelist.Contains(requestIdentity.ClientKey))
                {
                    return true;
                }
            }

            if (Policy.EndpointThrottling)
            {
                if (Policy.EndpointWhitelist != null
                    && Policy.EndpointWhitelist.Any(x => requestIdentity.Endpoint.Contains(x.ToLowerInvariant())))
                {
                    return true;
                }
            }

            return false;
        }

        internal string ComputeThrottleKey(RequestIdentity requestIdentity, RateLimitPeriod period)
        {
            var keyValues = new List<string>()
                {
                    ThrottleManager.GetThrottleKey()
                };

            if (Policy.IpThrottling)
            {
                keyValues.Add(requestIdentity.ClientIp);
            }

            if (Policy.ClientThrottling)
            {
                keyValues.Add(requestIdentity.ClientKey);
            }

            if (Policy.EndpointThrottling)
            {
                keyValues.Add(requestIdentity.Endpoint);
            }

            keyValues.Add(period.ToString());

            var id = string.Join("_", keyValues);
            var idBytes = Encoding.UTF8.GetBytes(id);

            byte[] hashBytes;

            using (var algorithm = System.Security.Cryptography.HashAlgorithm.Create("SHA1"))
            {
                hashBytes = algorithm.ComputeHash(idBytes);
            }
            
            var hex = BitConverter.ToString(hashBytes).Replace("-", string.Empty);
            return hex;
        }

        internal List<KeyValuePair<RateLimitPeriod, long>> RatesWithDefaults(List<KeyValuePair<RateLimitPeriod, long>> defRates)
        {
            if (!defRates.Any(x => x.Key == RateLimitPeriod.Second))
            {
                defRates.Insert(0, new KeyValuePair<RateLimitPeriod, long>(RateLimitPeriod.Second, 0));
            }

            if (!defRates.Any(x => x.Key == RateLimitPeriod.Minute))
            {
                defRates.Insert(1, new KeyValuePair<RateLimitPeriod, long>(RateLimitPeriod.Minute, 0));
            }

            if (!defRates.Any(x => x.Key == RateLimitPeriod.Hour))
            {
                defRates.Insert(2, new KeyValuePair<RateLimitPeriod, long>(RateLimitPeriod.Hour, 0));
            }

            if (!defRates.Any(x => x.Key == RateLimitPeriod.Day))
            {
                defRates.Insert(3, new KeyValuePair<RateLimitPeriod, long>(RateLimitPeriod.Day, 0));
            }

            if (!defRates.Any(x => x.Key == RateLimitPeriod.Week))
            {
                defRates.Insert(4, new KeyValuePair<RateLimitPeriod, long>(RateLimitPeriod.Week, 0));
            }

            return defRates;
        }

        internal ThrottleCounter ProcessRequest(RequestIdentity requestIdentity, TimeSpan timeSpan, RateLimitPeriod period, out string id)
        {
            var throttleCounter = new ThrottleCounter()
            {
                Timestamp = DateTime.UtcNow,
                TotalRequests = 1
            };

            id = ComputeThrottleKey(requestIdentity, period);

            // serial reads and writes
            lock (ProcessLocker)
            {
                var entry = Repository.FirstOrDefault(id);
                if (entry.HasValue)
                {
                    // entry has not expired
                    if (entry.Value.Timestamp + timeSpan >= DateTime.UtcNow)
                    {
                        // increment request count
                        var totalRequests = entry.Value.TotalRequests + 1;

                        // deep copy
                        throttleCounter = new ThrottleCounter
                        {
                            Timestamp = entry.Value.Timestamp,
                            TotalRequests = totalRequests
                        };
                    }
                }

                // stores: id (string) - timestamp (datetime) - total (long)
                Repository.Save(id, throttleCounter, timeSpan);
            }

            return throttleCounter;
        }

        internal TimeSpan GetTimeSpanFromPeriod(RateLimitPeriod rateLimitPeriod)
        {
            var timeSpan = TimeSpan.FromSeconds(1);

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
                case RateLimitPeriod.Week:
                    timeSpan = TimeSpan.FromDays(7);
                    break;
            }

            return timeSpan;
        }

        internal void ApplyRules(RequestIdentity identity, TimeSpan timeSpan, RateLimitPeriod rateLimitPeriod, ref long rateLimit)
        {
            // apply endpoint rate limits
            if (Policy.EndpointRules != null)
            {
                var rules = Policy.EndpointRules.Where(x => identity.Endpoint.Contains(x.Key.ToLowerInvariant())).ToList();
                if (rules.Any())
                {
                    // get the lower limit from all applying rules
                    var customRate = (from r in rules let rateValue = r.Value.GetLimit(rateLimitPeriod) select rateValue).Min();

                    if (customRate > 0)
                    {
                        rateLimit = customRate;
                    }
                }
            }

            // apply custom rate limit for clients that will override endpoint limits
            if (Policy.ClientRules != null && Policy.ClientRules.Keys.Contains(identity.ClientKey))
            {
                var limit = Policy.ClientRules[identity.ClientKey].GetLimit(rateLimitPeriod);
                if (limit > 0)
                {
                    rateLimit = limit;
                }
            }

            // enforce ip rate limit as is most specific 
            string ipRule = null;
            if (Policy.IpRules != null && ContainsIp(Policy.IpRules.Keys.ToList(), identity.ClientIp, out ipRule))
            {
                var limit = Policy.IpRules[ipRule].GetLimit(rateLimitPeriod);
                if (limit > 0)
                {
                    rateLimit = limit;
                }
            }
        }
    }
}
