using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace WebApiThrottle
{
    /// <summary>
    /// Stors policy in runtime cache, intented for owin self host.
    /// </summary>
    public class PolicyMemoryCacheRepository : IPolicyRepository
    {
        ObjectCache memCache = MemoryCache.Default;

        public void Save(string id, ThrottlePolicy policy)
        {
            if (memCache[id] != null)
            {
                memCache[id] = policy;
            }
            else
            {
                memCache.Add(
                    id,
                    policy, new CacheItemPolicy()
                    {

                    });
            }
        }

        public ThrottlePolicy FirstOrDefault(string id)
        {
            var policy = (ThrottlePolicy)memCache[id];
            return policy;
        }

        public void Remove(string id)
        {
            memCache.Remove(id);
        }
    }
}
