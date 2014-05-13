using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApiThrottle
{
    /// <summary>
    /// Implement this interface if you want to load the policy rules from a persistent store
    /// </summary>
    public interface IThrottlePolicyProvider
    {
        ThrottlePolicySettings ReadSettings();

        IEnumerable<ThrottlePolicyRule> AllRules();

        IEnumerable<ThrottlePolicyWhitelist> AllWhitelists();
    }
}
