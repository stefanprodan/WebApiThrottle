using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace WebApiThrottle
{
    /// <summary>
    /// Stors throttle metrics in runtime cache, intented for owin self host.
    /// </summary>
    public class MemoryCacheRepository : IThrottleRepository
    {
        ObjectCache memCache = MemoryCache.Default;

        /// <summary>
        /// Insert or update
        /// </summary>
        public void Save(string id, ThrottleCounter throttleCounter, TimeSpan expirationTime)
        {
            if (memCache[id] != null)
            {
                memCache[id] = throttleCounter;
            }
            else
            {
                memCache.Add(
                    id,
                    throttleCounter, new CacheItemPolicy()
                    {
                        SlidingExpiration = expirationTime
                    });
            }
        }

        public bool Any(string id)
        {
            return memCache[id] != null;
        }

        public ThrottleCounter? FirstOrDefault(string id)
        {
            return (ThrottleCounter?)memCache[id];
        }

        public void Remove(string id)
        {
            memCache.Remove(id);
        }

        public void Clear()
        {
            var cacheKeys = memCache.Where(kvp => kvp.Value is ThrottleCounter).Select(kvp => kvp.Key).ToList();
            foreach (string cacheKey in cacheKeys)
            {
                memCache.Remove(cacheKey);
            }
        }
    }
}
