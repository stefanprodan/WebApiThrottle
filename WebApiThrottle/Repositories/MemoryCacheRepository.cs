using System;
using System.Linq;
using System.Runtime.Caching;
using WebApiThrottle.Models;

namespace WebApiThrottle.Repositories
{
    /// <summary>
    ///     Stors throttle metrics in runtime cache, intented for owin self host.
    /// </summary>
    public class MemoryCacheRepository : IThrottleRepository
    {
        private readonly ObjectCache _memCache = MemoryCache.Default;

        /// <summary>
        ///     Insert or update
        /// </summary>
        public void Save(string id, ThrottleCounter throttleCounter, TimeSpan expirationTime)
        {
            if (_memCache[id] != null)
                _memCache[id] = throttleCounter;
            else
                _memCache.Add(
                    id,
                    throttleCounter, new CacheItemPolicy
                    {
                        SlidingExpiration = expirationTime
                    });
        }

        public bool Any(string id)
        {
            return _memCache[id] != null;
        }

        public ThrottleCounter? FirstOrDefault(string id)
        {
            return (ThrottleCounter?) _memCache[id];
        }

        public void Remove(string id)
        {
            _memCache.Remove(id);
        }

        public void Clear()
        {
            var cacheKeys = _memCache.Where(kvp => kvp.Value is ThrottleCounter).Select(kvp => kvp.Key).ToList();
            foreach (var cacheKey in cacheKeys)
                _memCache.Remove(cacheKey);
        }
    }
}