using System.Web.Http;
using Owin;
using WebApiThrottle;
using WebApiThrottle.Providers;
using WebApiThrottle.Repositories;

namespace WebApiThrottler.SelfHostOwinDemo
{
    public class Startup
    {
        // This code configures Web API. The Startup class is specified as a type
        // parameter in the WebApp.Start method.
        public void Configuration(IAppBuilder appBuilder)
        {
            // Configure Web API for self-host. 
            var config = new HttpConfiguration();
            config.Routes.MapHttpRoute(
                "DefaultApi",
                "api/{controller}/{id}",
                new {id = RouteParameter.Optional}
            );

            //middleware with policy loaded from app.config
            //appBuilder.Use(typeof(ThrottlingMiddleware),
            //    ThrottlePolicy.FromStore(new PolicyConfigurationProvider()),
            //    new PolicyMemoryCacheRepository(),
            //    new MemoryCacheRepository(),
            //    null,
            //    null);

            //Web API throttling load policy from app.config
            config.MessageHandlers.Add(new ThrottlingHandler
            {
                Policy = ThrottlePolicy.FromStore(new PolicyConfigurationProvider()),
                Repository = new MemoryCacheRepository()
            });

            //Web API throttling hardcoded policy
            //config.MessageHandlers.Add(new ThrottlingHandler()
            //{
            //    Policy = new ThrottlePolicy(perSecond: 1, perMinute: 20, perHour: 30, perDay: 35, perWeek: 3000)
            //    {
            //        //scope to IPs
            //        IpThrottling = true,
            //        IpRules = new Dictionary<string, RateLimits>
            //        { 
            //            { "::1/10", new RateLimits { PerSecond = 2 } },
            //            { "192.168.2.1", new RateLimits { PerMinute = 30, PerHour = 30*60, PerDay = 30*60*24 } }
            //        },
            //        //white list the "::1" IP to disable throttling on localhost for Win8
            //        IpWhitelist = new List<string> { "127.0.0.1", "192.168.0.0/24" },

            //        //scope to clients (if IP throttling is applied then the scope becomes a combination of IP and client key)
            //        ClientThrottling = true,
            //        ClientRules = new Dictionary<string, RateLimits>
            //        { 
            //            { "api-client-key-1", new RateLimits { PerMinute = 60, PerHour = 600 } },
            //            { "api-client-key-9", new RateLimits { PerDay = 5000 } }
            //        },
            //        //white list API keys that don’t require throttling
            //        ClientWhitelist = new List<string> { "admin-key" },

            //        //scope to routes (IP + Client Key + Request URL)
            //        EndpointThrottling = true,
            //        EndpointRules = new Dictionary<string, RateLimits>
            //        { 
            //            { "api/values/", new RateLimits { PerSecond = 3 } },
            //            { "api/values", new RateLimits { PerSecond = 4 } }
            //        }
            //    },
            //    Repository = new MemoryCacheRepository()
            //});

            appBuilder.UseWebApi(config);
        }
    }
}