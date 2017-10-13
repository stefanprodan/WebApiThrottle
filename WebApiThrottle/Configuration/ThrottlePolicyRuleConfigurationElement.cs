using System.Configuration;

namespace WebApiThrottle.Configuration
{
    public class ThrottlePolicyRuleConfigurationElement : ConfigurationElement
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

        [ConfigurationProperty("entry", IsRequired = true)]
        public string Entry => this["entry"] as string;

        [ConfigurationProperty("policyType", IsRequired = true)]
        public int PolicyType => (int) this["policyType"];
    }
}