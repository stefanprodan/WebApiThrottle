using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using WebApiThrottle.Net;

namespace WebApiThrottle
{
    /// <summary>
    /// Common code shared between ThrottlingHandler and ThrottlingFilter
    /// </summary>
    internal class ThrottlingCore
    {
        public ThrottlingCore()
        {
            IpAddressParser = new DefaultIpAddressParser();
        }

        private static readonly object ProcessLocker = new object();

        internal ThrottlePolicy Policy { get; set; }

        internal IThrottleRepository Repository { get; set; }

        internal IIpAddressParser IpAddressParser { get; set; }

        internal bool ContainsIp(List<string> ipRules, string clientIp)
        {
            return IpAddressParser.ContainsIp(ipRules, clientIp);
        }

        internal bool ContainsIp(List<string> ipRules, string clientIp, out string rule)
        {
            return IpAddressParser.ContainsIp(ipRules, clientIp, out rule);
        }

        internal IPAddress GetClientIp(HttpRequestMessage request)
        {
            return IpAddressParser.GetClientIp(request);
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
            if (requestIdentity.ForceWhiteList)
            {
                return true;
            }

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
                    && Policy.EndpointWhitelist.Any(x => requestIdentity.Endpoint.IndexOf(x, 0, StringComparison.InvariantCultureIgnoreCase) != -1))
                {
                    return true;
                }
            }

            return false;
        }

        internal string ComputeThrottleKey(RequestIdentity requestIdentity, RateLimitPeriod period)
        {
            using (var ms = new MemoryStream())
            using (var sw = new StreamWriter(ms, Encoding.UTF8))
            {
                sw.Write(ThrottleManager.GetThrottleKey());

                if (Policy.IpThrottling)
                {
                    sw.Write(requestIdentity.ClientIp);
                }

                if (Policy.ClientThrottling)
                {
                    sw.Write(requestIdentity.ClientKey);
                }

                if (Policy.EndpointThrottling)
                {
                    sw.Write(requestIdentity.Endpoint);
                }

                sw.Write(period);

                ms.Position = 0;
                using (var algorithm = new SHA1Managed())
                {
                    var hash = algorithm.ComputeHash(ms);
                    return Convert.ToBase64String(hash);
                }
            }
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

        internal ThrottleCounter ProcessRequest(TimeSpan timeSpan, string id)
        {
            var throttleCounter = new ThrottleCounter()
            {
                Timestamp = DateTime.UtcNow,
                TotalRequests = 1
            };

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
                var rules = Policy.EndpointRules.Where(x => identity.Endpoint.IndexOf(x.Key, 0, StringComparison.InvariantCultureIgnoreCase) != -1).ToList();
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
