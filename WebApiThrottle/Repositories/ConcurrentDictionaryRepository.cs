using System;
using System.Collections.Concurrent;
using WebApiThrottle.Models;

namespace WebApiThrottle.Repositories
{
    /// <summary>
    ///     Stores throttle metrics in a thread safe dictionary, has no clean-up mechanism, expired counters are deleted on
    ///     renewal
    /// </summary>
    public partial class ConcurrentDictionaryRepository : IThrottleRepository
    {
        private static readonly ConcurrentDictionary<string, ThrottleCounterWrapper> cache =
            new ConcurrentDictionary<string, ThrottleCounterWrapper>();

        public bool Any(string id)
        {
            return cache.ContainsKey(id);
        }

        /// <summary>
        ///     Insert or update
        /// </summary>
        /// <param name="id">
        ///     The id.
        /// </param>
        /// <returns>
        ///     The <see cref="ThrottleCounter" />.
        /// </returns>
        public ThrottleCounter? FirstOrDefault(string id)
        {
            if (!cache.TryGetValue(id, out var entry))
                return new ThrottleCounter
                {
                    Timestamp = entry.Timestamp,
                    TotalRequests = entry.TotalRequests
                };
            if (entry.Timestamp + entry.ExpirationTime >= DateTime.UtcNow)
                return new ThrottleCounter
                {
                    Timestamp = entry.Timestamp,
                    TotalRequests = entry.TotalRequests
                };
            cache.TryRemove(id, out entry);
            return null;
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
            cache.TryRemove(id, out ThrottleCounterWrapper entry);
        }

        public void Clear()
        {
            cache.Clear();
        }
    }
}