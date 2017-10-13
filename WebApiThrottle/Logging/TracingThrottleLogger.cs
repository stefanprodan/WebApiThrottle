using System.Web.Http.Tracing;

namespace WebApiThrottle.Logging
{
    public class TracingThrottleLogger : IThrottleLogger
    {
        private readonly ITraceWriter _traceWriter;

        public TracingThrottleLogger(ITraceWriter traceWriter)
        {
            _traceWriter = traceWriter;
        }

        public void Log(ThrottleLogEntry entry)
        {
            _traceWriter?.Info(
                entry.Request,
                "WebApiThrottle",
                "{0} Request {1} from {2} has been throttled (blocked), quota {3}/{4} exceeded by {5}",
                entry.LogDate,
                entry.RequestId,
                entry.ClientIp,
                entry.RateLimit,
                entry.RateLimitPeriod,
                entry.TotalRequests);
        }
    }
}