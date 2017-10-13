using System.Runtime.Caching;

namespace WebApiThrottle.Repositories
{
    /// <summary>
    ///     Stores policy in runtime cache, intended for OWIN self host.
    /// </summary>
    public class PolicyMemoryCacheRepository : IPolicyRepository
    {
        private readonly ObjectCache _memCache = MemoryCache.Default;

        public void Save(string id, ThrottlePolicy policy)
        {
            if (_memCache[id] != null)
                _memCache[id] = policy;
            else
                _memCache.Add(
                    id,
                    policy,
                    new CacheItemPolicy());
        }

        public ThrottlePolicy FirstOrDefault(string id)
        {
            var policy = (ThrottlePolicy) _memCache[id];
            return policy;
        }

        public void Remove(string id)
        {
            _memCache.Remove(id);
        }
    }
}