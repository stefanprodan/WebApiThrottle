using System;

namespace WebApiThrottle.Models
{
    /// <summary>
    ///     Stores the initial access time and the numbers of calls made from that point
    /// </summary>
    [Serializable]
    public struct ThrottleCounter
    {
        public DateTime Timestamp { get; set; }

        public long TotalRequests { get; set; }
    }
}