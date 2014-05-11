using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApiThrottle
{
    /// <summary>
    /// Allows changing the cache keys prefix and suffix, exposes ways to refresh the policy object at runtime.
    /// </summary>
    public static class ThrottleManager
    {
        /// <summary>
        /// GLobal prefix
        /// </summary>
        public static string ApplicationName = "";

        /// <summary>
        /// Rate limits key prefix
        /// </summary>
        public static string ThrottleKey = "throttle";

        /// <summary>
        /// Policy key suffix
        /// </summary>
        public static string PolicyKey = "throttle_policy";

        /// <summary>
        /// Retuns key prefix for rate limits
        /// </summary>
        public static string GetThrottleKey()
        {
            return ApplicationName + ThrottleKey;
        }

        /// <summary>
        /// Retuns policy key (global prefix + policy key suffix)
        /// </summary>
        public static string GetPolicyKey()
        {
            return ApplicationName + PolicyKey;
        }

        /// <summary>
        /// Updates the policy object cached value
        /// </summary>
        public static void UpdatePolicy(ThrottlePolicy policy, IPolicyRepository cacheRepository)
        {
            cacheRepository.Save(GetPolicyKey(), policy);
        }

        /// <summary>
        /// Reads the policy object from store and updates the cache
        /// </summary>
        public static void UpdatePolicy(IThrottlePolicyProvider storeProvider, IPolicyRepository cacheRepository)
        {
            var policy = ThrottlePolicy.FromStore(storeProvider);
            cacheRepository.Save(GetPolicyKey(), policy);
        }
    }
}
