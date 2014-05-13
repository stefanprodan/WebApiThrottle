using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Filters;

namespace WebApiThrottle
{
    public class EnableThrottlingAttribute : ActionFilterAttribute, IActionFilter
    {
        public long PerSecond { get; set; }

        public long PerMinute { get; set; }

        public long PerHour { get; set; }

        public long PerDay { get; set; }

        public long PerWeek { get; set; }

        public long GetLimit(RateLimitPeriod period)
        {
            switch (period)
            {
                case RateLimitPeriod.Second:
                    return PerSecond;
                case RateLimitPeriod.Minute:
                    return PerMinute;
                case RateLimitPeriod.Hour:
                    return PerHour;
                case RateLimitPeriod.Day:
                    return PerDay;
                case RateLimitPeriod.Week:
                    return PerWeek;
                default:
                    return PerSecond;
            }
        }
    }
}
