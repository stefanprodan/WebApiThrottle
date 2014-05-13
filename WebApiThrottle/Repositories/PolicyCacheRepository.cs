using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;

namespace WebApiThrottle
{
    /// <summary>
    /// Stores policy in asp.net cache
    /// </summary>
    public class PolicyCacheRepository : IPolicyRepository
    {
        public void Save(string id, ThrottlePolicy policy)
        {
            if (HttpContext.Current.Cache[id] != null)
            {
                HttpContext.Current.Cache[id] = policy;
            }
            else
            {
                HttpContext.Current.Cache.Add(
                    id,
                    policy,
                    null,
                    Cache.NoAbsoluteExpiration,
                    Cache.NoSlidingExpiration,
                    CacheItemPriority.High,
                    null);
            }
        }

        public ThrottlePolicy FirstOrDefault(string id)
        {
            var policy = (ThrottlePolicy)HttpContext.Current.Cache[id];
            return policy;
        }

        public void Remove(string id)
        {
            HttpContext.Current.Cache.Remove(id);
        }
    }
}
