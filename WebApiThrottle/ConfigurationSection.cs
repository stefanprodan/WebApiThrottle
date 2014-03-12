using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApiThrottle
{
    public class ThrottlePolicyConfiguration : ConfigurationSection
    {
        [ConfigurationProperty("limitPerSecond", DefaultValue = "0", IsRequired = false)]
        [LongValidator(ExcludeRange = false, MinValue = 0)]
        public long LimitPerSecond
        {
            get
            {
                return (long)this["limitPerSecond"];
            }
        }

        [ConfigurationProperty("limitPerMinute", DefaultValue = "0", IsRequired = false)]
        [LongValidator(ExcludeRange = false, MinValue = 0)]
        public long LimitPerMinute
        {
            get
            {
                return (long)this["limitPerMinute"];
            }
        }

        [ConfigurationProperty("limitPerHour", DefaultValue = "0", IsRequired = false)]
        [LongValidator(ExcludeRange = false, MinValue = 0)]
        public long LimitPerHour
        {
            get
            {
                return (long)this["limitPerHour"];
            }
        }

        [ConfigurationProperty("limitPerDay", DefaultValue = "0", IsRequired = false)]
        [LongValidator(ExcludeRange = false, MinValue = 0)]
        public long LimitPerDay
        {
            get
            {
                return (long)this["limitPerDay"];
            }
        }

        [ConfigurationProperty("limitPerWeek", DefaultValue = "0", IsRequired = false)]
        [LongValidator(ExcludeRange = false, MinValue = 0)]
        public long LimitPerWeek
        {
            get
            {
                return (long)this["limitPerWeek"];
            }
        }

        [ConfigurationProperty("ipThrottling", DefaultValue = "false", IsRequired = false)]
        public bool IpThrottling
        {
            get
            {
                return (bool)this["ipThrottling"];
            }
        }

        [ConfigurationProperty("clientThrottling", DefaultValue = "false", IsRequired = false)]
        public bool ClientThrottling
        {
            get
            {
                return (bool)this["clientThrottling"];
            }
        }

        [ConfigurationProperty("endpointThrottling", DefaultValue = "false", IsRequired = false)]
        public bool EndpointThrottling
        {
            get
            {
                return (bool)this["endpointThrottling"];
            }
        }

        [ConfigurationProperty("stackBlockedRequests", DefaultValue = "false", IsRequired = false)]
        public bool StackBlockedRequests
        {
            get
            {
                return (bool)this["stackBlockedRequests"];
            }
        }

        [ConfigurationProperty("rules")]
        public ThrottlePolicyRuleConfigurationCollection Rules
        {
            get
            {
                return this["rules"] as ThrottlePolicyRuleConfigurationCollection;
            }
        }

        [ConfigurationProperty("whitelists")]
        public ThrottlePolicyWhitelistConfigurationCollection Whitelists
        {
            get
            {
                return this["whitelists"] as ThrottlePolicyWhitelistConfigurationCollection;
            }
        }
    }

    public class ThrottlePolicyRuleConfigurationElement : ConfigurationElement
    {
        [ConfigurationProperty("limitPerSecond", DefaultValue = "0", IsRequired = false)]
        [LongValidator(ExcludeRange = false, MinValue = 0)]
        public long LimitPerSecond
        {
            get
            {
                return (long)this["limitPerSecond"];
            }
        }

        [ConfigurationProperty("limitPerMinute", DefaultValue = "0", IsRequired = false)]
        [LongValidator(ExcludeRange = false, MinValue = 0)]
        public long LimitPerMinute
        {
            get
            {
                return (long)this["limitPerMinute"];
            }
        }

        [ConfigurationProperty("limitPerHour", DefaultValue = "0", IsRequired = false)]
        [LongValidator(ExcludeRange = false, MinValue = 0)]
        public long LimitPerHour
        {
            get
            {
                return (long)this["limitPerHour"];
            }
        }

        [ConfigurationProperty("limitPerDay", DefaultValue = "0", IsRequired = false)]
        [LongValidator(ExcludeRange = false, MinValue = 0)]
        public long LimitPerDay
        {
            get
            {
                return (long)this["limitPerDay"];
            }
        }

        [ConfigurationProperty("limitPerWeek", DefaultValue = "0", IsRequired = false)]
        [LongValidator(ExcludeRange = false, MinValue = 0)]
        public long LimitPerWeek
        {
            get
            {
                return (long)this["limitPerWeek"];
            }
        }

        [ConfigurationProperty("entry", IsRequired = true)]
        public string Entry
        {
            get
            {
                return this["entry"] as string;
            }
        }

        [ConfigurationProperty("policyType", IsRequired = true)]
        public int PolicyType
        {
            get
            {
                return (int)this["policyType"];
            }
        }
    }

    public class ThrottlePolicyRuleConfigurationCollection : ConfigurationElementCollection
    {

        protected override ConfigurationElement CreateNewElement()
        {
            return new ThrottlePolicyRuleConfigurationElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ThrottlePolicyRuleConfigurationElement)element).Entry;
        }
    }

    public class ThrottlePolicyWhitelistConfigurationElement : ConfigurationElement
    {
        [ConfigurationProperty("entry", IsRequired = true)]
        public string Entry
        {
            get
            {
                return this["entry"] as string;
            }
        }

        [ConfigurationProperty("policyType", IsRequired = true)]
        public int PolicyType
        {
            get
            {
                return (int)this["policyType"];
            }
        }
    }

    public class ThrottlePolicyWhitelistConfigurationCollection : ConfigurationElementCollection
       {
           protected override ConfigurationElement CreateNewElement()
           {
               return new ThrottlePolicyWhitelistConfigurationElement();
           }

           protected override object GetElementKey(ConfigurationElement element)
           {
               return ((ThrottlePolicyWhitelistConfigurationElement)element).Entry;
           }
       }
}
