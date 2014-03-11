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
    [Serializable]
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
        public Dictionary<string, RateLimits> EndpointRules { get; set; }

        /// <summary>
        /// All requests including the rejected ones will stack in this order: day, hour, min, sec
        /// </summary>
        public bool StackBlockedRequests { get; set; }

        internal Dictionary<RateLimitPeriod, long> Rates { get; set; }

        /// <summary>
        /// Configure default request limits per second, minute, hour or day
        /// </summary>
        public ThrottlePolicy(long? perSecond = null, long? perMinute = null, long? perHour = null, long? perDay = null, long? perWeek = null)
        {
            Rates = new Dictionary<RateLimitPeriod, long>();
            if (perSecond.HasValue) Rates.Add(RateLimitPeriod.Second, perSecond.Value);
            if (perMinute.HasValue) Rates.Add(RateLimitPeriod.Minute, perMinute.Value);
            if (perHour.HasValue) Rates.Add(RateLimitPeriod.Hour, perHour.Value);
            if (perDay.HasValue) Rates.Add(RateLimitPeriod.Day, perDay.Value);
            if (perWeek.HasValue) Rates.Add(RateLimitPeriod.Week, perWeek.Value);
        }

        public static ThrottlePolicy FromStore(IThrottlePolicyProvider provider)
        {
            var settings = provider.ReadSettings();
            var whitelists = provider.AllWhitelists();
            var rules = provider.AllRules();

            var policy = new ThrottlePolicy(perSecond: settings.LimitPerSecond,
               perMinute: settings.LimitPerMinute,
               perHour: settings.LimitPerHour,
               perDay: settings.LimitPerDay,
               perWeek: settings.LimitPerWeek);

            policy.IpThrottling = settings.IpThrottling;
            policy.ClientThrottling = settings.ClientThrottling;
            policy.EndpointThrottling = settings.EndpointThrottling;
            policy.StackBlockedRequests = settings.StackBlockedRequests;

            policy.IpRules = new Dictionary<string, RateLimits>();

            foreach (var item in rules.Where(r=> r.PolicyType == ThrottlePolicyType.IpThrottling))
            {
                var rateLimit = new RateLimits { PerSecond = item.LimitPerSecond, PerMinute = item.LimitPerMinute, PerHour = item.LimitPerHour, PerDay = item.LimitPerDay, PerWeek = item.LimitPerWeek };

                switch (item.PolicyType)
                {
                    case ThrottlePolicyType.IpThrottling:
                        policy.IpRules.Add(item.Entry, rateLimit);
                        break;
                    case ThrottlePolicyType.ClientThrottling:
                        policy.ClientRules.Add(item.Entry, rateLimit);
                        break;
                    case ThrottlePolicyType.EndpointThrottling:
                        policy.EndpointRules.Add(item.Entry, rateLimit);
                        break;
                }
            }

            return policy;
        }
    }
}
