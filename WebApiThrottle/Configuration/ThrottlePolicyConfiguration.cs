using System.Configuration;

namespace WebApiThrottle.Configuration
{
    public class ThrottlePolicyConfiguration : ConfigurationSection
    {
        [ConfigurationProperty("limitPerSecond", DefaultValue = "0", IsRequired = false)]
        [LongValidator(ExcludeRange = false, MinValue = 0)]
        public long LimitPerSecond => (long) this["limitPerSecond"];

        [ConfigurationProperty("limitPerMinute", DefaultValue = "0", IsRequired = false)]
        [LongValidator(ExcludeRange = false, MinValue = 0)]
        public long LimitPerMinute => (long) this["limitPerMinute"];

        [ConfigurationProperty("limitPerHour", DefaultValue = "0", IsRequired = false)]
        [LongValidator(ExcludeRange = false, MinValue = 0)]
        public long LimitPerHour => (long) this["limitPerHour"];

        [ConfigurationProperty("limitPerDay", DefaultValue = "0", IsRequired = false)]
        [LongValidator(ExcludeRange = false, MinValue = 0)]
        public long LimitPerDay => (long) this["limitPerDay"];

        [ConfigurationProperty("limitPerWeek", DefaultValue = "0", IsRequired = false)]
        [LongValidator(ExcludeRange = false, MinValue = 0)]
        public long LimitPerWeek => (long) this["limitPerWeek"];

        [ConfigurationProperty("ipThrottling", DefaultValue = "false", IsRequired = false)]
        public bool IpThrottling => (bool) this["ipThrottling"];

        [ConfigurationProperty("clientThrottling", DefaultValue = "false", IsRequired = false)]
        public bool ClientThrottling => (bool) this["clientThrottling"];

        [ConfigurationProperty("endpointThrottling", DefaultValue = "false", IsRequired = false)]
        public bool EndpointThrottling => (bool) this["endpointThrottling"];

        [ConfigurationProperty("stackBlockedRequests", DefaultValue = "false", IsRequired = false)]
        public bool StackBlockedRequests => (bool) this["stackBlockedRequests"];

        [ConfigurationProperty("rules")]
        public ThrottlePolicyRuleConfigurationCollection Rules =>
            this["rules"] as ThrottlePolicyRuleConfigurationCollection;

        [ConfigurationProperty("whitelists")]
        public ThrottlePolicyWhitelistConfigurationCollection Whitelists =>
            this["whitelists"] as ThrottlePolicyWhitelistConfigurationCollection;
    }
}