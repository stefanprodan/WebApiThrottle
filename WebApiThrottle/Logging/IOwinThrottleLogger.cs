namespace WebApiThrottle
{
    /// <summary>
    /// Log requests that exceed the limit
    /// </summary>
    public interface IOwinThrottleLogger
    {
        void Log(OwinThrottleLogEntry entry);
    }
}