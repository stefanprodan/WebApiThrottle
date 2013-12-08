using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace WebApiThrottle.Demo
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            //Web API throttling
            config.MessageHandlers.Add(new ThrottlingHandler()
            {
                Policy = new ThrottlePolicy(perSecond: 1, perMinute: 20, perHour: 200, perDay: 1500)
                {
                    IpThrottling = true,
                    IpRules = new Dictionary<string, RateLimits>
                    { 
                        { "::1", new RateLimits { PerSecond = 2 } },
                        { "192.168.1.2", new RateLimits { PerMinute = 30, PerHour = 30*60, PerDay = 30*60*24 } }
                    },
                    IpWhitelist = new List<string> { "10.0.0.1" },
                    ClientThrottling = true,
                    ClientRules = new Dictionary<string, RateLimits>
                    { 
                        { "api-client-key-1", new RateLimits { PerMinute = 60, PerHour = 600 } },
                        { "api-client-key-9", new RateLimits { PerDay = 5000 } }
                    },
                    ClientWhitelist = new List<string> { "admin-key" },
                    EndpointThrottling = true
                },
                Repository = new CacheRepository()
            });
        }
    }
}
