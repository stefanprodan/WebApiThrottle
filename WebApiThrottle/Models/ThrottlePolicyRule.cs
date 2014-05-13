using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApiThrottle
{
    [Serializable]
    public class ThrottlePolicyRule
    {
        public long LimitPerSecond { get; set; }

        public long LimitPerMinute { get; set; }

        public long LimitPerHour { get; set; }

        public long LimitPerDay { get; set; }

        public long LimitPerWeek { get; set; }

        public string Entry { get; set; }

        public ThrottlePolicyType PolicyType { get; set; }
    }
}
