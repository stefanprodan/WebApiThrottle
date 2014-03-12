using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApiThrottle
{
    /// <summary>
    /// Stors throttle metrics in a thread safe dictionary, has no clean-up mechanism, expired counters are deleted on renewal
    /// </summary>
    public class ConcurrentDictionaryRepository : IThrottleRepository
    {
        [Serializable]
        internal struct ThrottleCounterWrapper
        {
            public DateTime Timestamp { get; set; }
            public long TotalRequests { get; set; }
            public TimeSpan ExpirationTime { get; set; }
        }

        private static ConcurrentDictionary<string, ThrottleCounterWrapper> cache = new ConcurrentDictionary<string, ThrottleCounterWrapper>();

        public bool Any(string id)
        {
            return cache.ContainsKey(id);
        }

        /// <summary>
        /// Insert or update
        /// </summary>
        public ThrottleCounter? FirstOrDefault(string id)
        {
            var entry = new ThrottleCounterWrapper();

            if (cache.TryGetValue(id, out entry))
            {
                //remove expired entry
                if (entry.Timestamp + entry.ExpirationTime < DateTime.UtcNow)
                {
                    cache.TryRemove(id, out entry);
                    return null;
                }
            }

            return new ThrottleCounter
            {
                Timestamp = entry.Timestamp,
                TotalRequests = entry.TotalRequests
            };
        }

        public void Save(string id, ThrottleCounter throttleCounter, TimeSpan expirationTime)
        {
            var entry = new ThrottleCounterWrapper
            {
                ExpirationTime = expirationTime,
                Timestamp = throttleCounter.Timestamp,
                TotalRequests = throttleCounter.TotalRequests
            };

            cache.AddOrUpdate(id, entry, (k, e) => entry);
        }

        public void Remove(string id)
        {
            var entry = new ThrottleCounterWrapper();
            cache.TryRemove(id, out entry);
        }

        public void Clear()
        {
            cache.Clear();
        }
    }
}
