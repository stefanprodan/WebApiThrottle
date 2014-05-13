using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApiThrottle
{
    public class PolicyConfigurationProvider : IThrottlePolicyProvider
    {
        private readonly ThrottlePolicyConfiguration policyConfig;

        public PolicyConfigurationProvider()
        {
            this.policyConfig = ConfigurationManager.GetSection("throttlePolicy") as ThrottlePolicyConfiguration;
        }

        public ThrottlePolicySettings ReadSettings()
        {
            var settings = new ThrottlePolicySettings()
            {
                IpThrottling = policyConfig.IpThrottling,
                ClientThrottling = policyConfig.ClientThrottling,
                EndpointThrottling = policyConfig.EndpointThrottling,
                StackBlockedRequests = policyConfig.StackBlockedRequests,
                LimitPerSecond = policyConfig.LimitPerSecond,
                LimitPerMinute = policyConfig.LimitPerMinute,
                LimitPerHour = policyConfig.LimitPerHour,
                LimitPerDay = policyConfig.LimitPerDay,
                LimitPerWeek = policyConfig.LimitPerWeek
            };

            return settings;
        }

        public IEnumerable<ThrottlePolicyRule> AllRules()
        {
            var rules = new List<ThrottlePolicyRule>();
            if (policyConfig.Rules != null)
            {
                foreach (ThrottlePolicyRuleConfigurationElement rule in policyConfig.Rules)
                {
                    rules.Add(new ThrottlePolicyRule
                    {
                        Entry = rule.Entry,
                        PolicyType = (ThrottlePolicyType)rule.PolicyType,
                        LimitPerSecond = rule.LimitPerSecond,
                        LimitPerMinute = rule.LimitPerMinute,
                        LimitPerHour = rule.LimitPerHour,
                        LimitPerDay = rule.LimitPerDay,
                        LimitPerWeek = rule.LimitPerWeek
                    });
                }
            }
            return rules;
        }

        public IEnumerable<ThrottlePolicyWhitelist> AllWhitelists()
        {
            var whitelists = new List<ThrottlePolicyWhitelist>();
            if (policyConfig.Whitelists != null)
            {
                foreach (ThrottlePolicyWhitelistConfigurationElement whitelist in policyConfig.Whitelists)
                {
                    whitelists.Add(new ThrottlePolicyWhitelist
                    {
                        Entry = whitelist.Entry,
                        PolicyType = (ThrottlePolicyType)whitelist.PolicyType,
                    });
                }
            }

            return whitelists;
        }
    }
}
