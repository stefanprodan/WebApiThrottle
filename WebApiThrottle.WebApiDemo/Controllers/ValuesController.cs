using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace WebApiThrottle.WebApiDemo.Controllers
{
    public class ValuesController : ApiController
    {
        [EnableThrottling(PerSecond = 2)]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        [DisableThrotting]
        public string Get(int id)
        {
            return "value";
        }

        /// <summary>
        /// Policy runtime update example
        /// </summary>
        [NonAction]
        public void UpdateRateLimits()
        {
            //init policy repo
            var policyRepository = new PolicyCacheRepository();

            //get policy object from cache
            var policy = policyRepository.FirstOrDefault(ThrottleManager.GetPolicyKey());

            //update client rate limits
            policy.ClientRules["api-client-key-1"] =
                new RateLimits { PerMinute = 50, PerHour = 500 };

            //add new client rate limits
            policy.ClientRules.Add("api-client-key-3",
                new RateLimits { PerMinute = 60, PerHour = 600 });

            //apply policy updates
            ThrottleManager.UpdatePolicy(policy, policyRepository);

        }
    }
}
