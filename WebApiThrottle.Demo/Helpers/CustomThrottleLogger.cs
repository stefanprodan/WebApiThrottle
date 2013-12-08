using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace WebApiThrottle.Demo.Helpers
{
    public class CustomThrottleLogger : IThrottleLogger
    {
        public void Log(ThrottleLogEntry entry)
        {
            Debug.WriteLine("{0} Request {1} has been blocked, quota {2}/{3} exceeded by {4}",
               entry.LogDate, entry.RequestId, entry.RateLimit, entry.RateLimitPeriod, entry.TotalRequests);
        }
    }
}