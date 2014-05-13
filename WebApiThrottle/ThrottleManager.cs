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
        private static string applicationName = string.Empty;

        private static string throttleKey = "throttle";

        private static string policyKey = "throttle_policy";

        /// <summary>
        /// Gets or sets the global prefix
        /// </summary>
        public static string ApplicationName
        {
            get
            {
                return applicationName;
            }

            set
            {
                applicationName = value;
            }
        }

        /// <summary>
        /// Gets or sets the key prefix for rate limits
        /// </summary>
        public static string ThrottleKey
        {
            get
            {
                return throttleKey;
            }

            set
            {
                throttleKey = value;
            }
        }

        /// <summary>
        /// Gets or sets the policy key suffix
        /// </summary>
        public static string PolicyKey
        {
            get
            {
                return policyKey;
            }

            set
            {
                policyKey = value;
            }
        }

        /// <summary>
        /// Returns key prefix for rate limits
        /// </summary>
        /// <returns>
        /// The throttle key.
        /// </returns>
        public static string GetThrottleKey()
        {
            return ApplicationName + ThrottleKey;
        }

        /// <summary>
        /// Returns the policy key (global prefix + policy key suffix)
        /// </summary>
        /// <returns>
        /// The policy key.
        /// </returns>
        public static string GetPolicyKey()
        {
            return ApplicationName + PolicyKey;
        }

        /// <summary>
        /// Updates the policy object cached value
        /// </summary>
        /// <param name="policy">
        /// The policy.
        /// </param>
        /// <param name="cacheRepository">
        /// The policy repository.
        /// </param>
        public static void UpdatePolicy(ThrottlePolicy policy, IPolicyRepository cacheRepository)
        {
            cacheRepository.Save(GetPolicyKey(), policy);
        }

        /// <summary>
        /// Reads the policy object from store and updates the cache
        /// </summary>
        /// <param name="storeProvider">
        /// The store provider.
        /// </param>
        /// <param name="cacheRepository">
        /// The cache repository.
        /// </param>
        public static void UpdatePolicy(IThrottlePolicyProvider storeProvider, IPolicyRepository cacheRepository)
        {
            var policy = ThrottlePolicy.FromStore(storeProvider);
            cacheRepository.Save(GetPolicyKey(), policy);
        }
    }
}
