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
        [ConfigurationProperty("LimitPerSecond", DefaultValue = "0", IsRequired = false)]
        [LongValidator(ExcludeRange = false, MinValue = 0)]
        public long LimitPerSecond
        {
            get
            {
                return (long)this["LimitPerSecond"];
            }
        }

        [ConfigurationProperty("LimitPerMinute", DefaultValue = "0", IsRequired = false)]
        [LongValidator(ExcludeRange = false, MinValue = 0)]
        public long LimitPerMinute
        {
            get
            {
                return (long)this["LimitPerMinute"];
            }
        }

        [ConfigurationProperty("LimitPerHour", DefaultValue = "0", IsRequired = false)]
        [LongValidator(ExcludeRange = false, MinValue = 0)]
        public long LimitPerHour
        {
            get
            {
                return (long)this["LimitPerHour"];
            }
        }

        [ConfigurationProperty("LimitPerDay", DefaultValue = "0", IsRequired = false)]
        [LongValidator(ExcludeRange = false, MinValue = 0)]
        public long LimitPerDay
        {
            get
            {
                return (long)this["LimitPerDay"];
            }
        }

        [ConfigurationProperty("LimitPerWeek", DefaultValue = "0", IsRequired = false)]
        [LongValidator(ExcludeRange = false, MinValue = 0)]
        public long LimitPerWeek
        {
            get
            {
                return (long)this["LimitPerWeek"];
            }
        }

        [ConfigurationProperty("IpThrottling", DefaultValue = "false", IsRequired = false)]
        public bool IpThrottling
        {
            get
            {
                return (bool)this["IpThrottling"];
            }
        }

        [ConfigurationProperty("ClientThrottling", DefaultValue = "false", IsRequired = false)]
        public bool ClientThrottling
        {
            get
            {
                return (bool)this["ClientThrottling"];
            }
        }

        [ConfigurationProperty("EndpointThrottling", DefaultValue = "false", IsRequired = false)]
        public bool EndpointThrottling
        {
            get
            {
                return (bool)this["EndpointThrottling"];
            }
        }

        [ConfigurationProperty("StackBlockedRequests", DefaultValue = "false", IsRequired = false)]
        public bool StackBlockedRequests
        {
            get
            {
                return (bool)this["StackBlockedRequests"];
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
        [ConfigurationProperty("LimitPerSecond", DefaultValue = "0", IsRequired = false)]
        [LongValidator(ExcludeRange = false, MinValue = 0)]
        public long LimitPerSecond
        {
            get
            {
                return (long)this["LimitPerSecond"];
            }
        }

        [ConfigurationProperty("LimitPerMinute", DefaultValue = "0", IsRequired = false)]
        [LongValidator(ExcludeRange = false, MinValue = 0)]
        public long LimitPerMinute
        {
            get
            {
                return (long)this["LimitPerMinute"];
            }
        }

        [ConfigurationProperty("LimitPerHour", DefaultValue = "0", IsRequired = false)]
        [LongValidator(ExcludeRange = false, MinValue = 0)]
        public long LimitPerHour
        {
            get
            {
                return (long)this["LimitPerHour"];
            }
        }

        [ConfigurationProperty("LimitPerDay", DefaultValue = "0", IsRequired = false)]
        [LongValidator(ExcludeRange = false, MinValue = 0)]
        public long LimitPerDay
        {
            get
            {
                return (long)this["LimitPerDay"];
            }
        }

        [ConfigurationProperty("LimitPerWeek", DefaultValue = "0", IsRequired = false)]
        [LongValidator(ExcludeRange = false, MinValue = 0)]
        public long LimitPerWeek
        {
            get
            {
                return (long)this["LimitPerWeek"];
            }
        }

        [ConfigurationProperty("Entry", IsRequired = true)]
        public string Entry
        {
            get
            {
                return this["Entry"] as string;
            }
        }

        [ConfigurationProperty("PolicyType", IsRequired = true)]
        public int PolicyType
        {
            get
            {
                return (int)this["PolicyType"];
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
        [ConfigurationProperty("Entry", IsRequired = true)]
        public string Entry
        {
            get
            {
                return this["Entry"] as string;
            }
        }

        [ConfigurationProperty("PolicyType", IsRequired = true)]
        public int PolicyType
        {
            get
            {
                return (int)this["PolicyType"];
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
