using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApiThrottle
{
    public class ThrottlePolicy
    {
        public bool IpThrottling { get; set; }
        public List<string> IpWhitelist { get; set; }
        public Dictionary<string, RateLimits> IpRules { get; set; }

        public bool ClientThrottling { get; set; }
        public List<string> ClientWhitelist { get; set; }
        public Dictionary<string, RateLimits> ClientRules { get; set; }

        public bool EndpointThrottling { get; set; }
        public List<string> EndpointWhitelist { get; set; }

        internal Dictionary<RateLimitPeriod, long> Rates { get; set; }

        public ThrottlePolicy(long? perSecond, long? perMinute = null, long? perHour = null, long? perDay = null)
        {
            Rates = new Dictionary<RateLimitPeriod, long>();
            if (perDay.HasValue) Rates.Add(RateLimitPeriod.Day, perDay.Value);
            if (perHour.HasValue) Rates.Add(RateLimitPeriod.Hour, perHour.Value);
            if (perMinute.HasValue) Rates.Add(RateLimitPeriod.Minute, perMinute.Value);
            if (perSecond.HasValue) Rates.Add(RateLimitPeriod.Second, perSecond.Value);
        }

    }
}
