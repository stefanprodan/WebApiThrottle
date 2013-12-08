using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApiThrottle
{
    /// <summary>
    /// Rate limits policy
    /// </summary>
    public class ThrottlePolicy
    {
        /// <summary>
        /// Enables IP throttling
        /// </summary>
        public bool IpThrottling { get; set; }
        public List<string> IpWhitelist { get; set; }
        public Dictionary<string, RateLimits> IpRules { get; set; }

        /// <summary>
        /// Enables Cient Key throttling
        /// </summary>
        public bool ClientThrottling { get; set; }
        public List<string> ClientWhitelist { get; set; }
        public Dictionary<string, RateLimits> ClientRules { get; set; }

        /// <summary>
        /// Enables routes throttling
        /// </summary>
        public bool EndpointThrottling { get; set; }
        public List<string> EndpointWhitelist { get; set; }

        internal Dictionary<RateLimitPeriod, long> Rates { get; set; }

        /// <summary>
        /// Configure default request limits per second, minute, hour or day
        /// </summary>
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
