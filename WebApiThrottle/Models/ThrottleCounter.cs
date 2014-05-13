using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApiThrottle
{
    /// <summary>
    /// Stores the initial access time and the numbers of calls made from that point
    /// </summary>
    [Serializable]
    public struct ThrottleCounter
    {
        public DateTime Timestamp { get; set; }

        public long TotalRequests { get; set; }
    }
}
