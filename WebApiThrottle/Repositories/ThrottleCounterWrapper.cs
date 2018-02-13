using System;

namespace WebApiThrottle.Repositories
{
    public partial class ConcurrentDictionaryRepository
    {
        [Serializable]
        private struct ThrottleCounterWrapper
        {
            public DateTime Timestamp { get; set; }

            public long TotalRequests { get; set; }

            public TimeSpan ExpirationTime { get; set; }
        }
    }
}