using System.Web.Http.Tracing;

namespace WebApiThrottle
{
    public class TracingThrottleLogger : IThrottleLogger
    {
        private readonly ITraceWriter traceWriter;
        
        public TracingThrottleLogger(ITraceWriter traceWriter)
        {
            this.traceWriter = traceWriter;
        }
       
        public void Log(ThrottleLogEntry entry)
        {
            if (null != traceWriter)
            {
                traceWriter.Info(entry.Request, "WebApiThrottle", "{0} Request {1} to endpoint {2} from client {3} has been throttled (blocked), quota {4}/{5} exceeded by {6}",
                    entry.LogDate, entry.RequestId, entry.ClientIp, entry.RateLimit,
                    entry.Endpoint, entry.RateLimitPeriod, entry.TotalRequests);
            }
        }
    }
}