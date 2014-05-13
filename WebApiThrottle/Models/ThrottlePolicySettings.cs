using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApiThrottle
{
    [Serializable]
    public class ThrottlePolicySettings
    {
        public long LimitPerSecond { get; set; }

        public long LimitPerMinute { get; set; }

        public long LimitPerHour { get; set; }

        public long LimitPerDay { get; set; }

        public long LimitPerWeek { get; set; }

        public bool IpThrottling { get; set; }

        public bool ClientThrottling { get; set; }

        public bool EndpointThrottling { get; set; }

        public bool StackBlockedRequests { get; set; }
    }
}
