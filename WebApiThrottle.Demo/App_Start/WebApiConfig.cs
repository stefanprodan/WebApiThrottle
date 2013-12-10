using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using WebApiThrottle.Demo.Helpers;

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
                Policy = new ThrottlePolicy(perSecond: 1, perMinute: 20, perHour: 30, perDay: 35)
                {
                    //scope to IPs
                    IpThrottling = true,
                    IpRules = new Dictionary<string, RateLimits>
                    { 
                        { "::1", new RateLimits { PerSecond = 2 } },
                        { "192.168.0.1", new RateLimits { PerMinute = 30, PerHour = 30*60, PerDay = 30*60*24 } }
                    },
                    //white list the "::1" IP to disable throttling on localhost for Win8
                    IpWhitelist = new List<string> { "127.0.0.1" },

                    //scope to clients (if IP throttling is applied then the scope becomes a combination of IP and client key)
                    ClientThrottling = true,
                    ClientRules = new Dictionary<string, RateLimits>
                    { 
                        { "api-client-key-1", new RateLimits { PerMinute = 60, PerHour = 600 } },
                        { "api-client-key-9", new RateLimits { PerDay = 5000 } }
                    },
                    //white list API keys that don’t require throttling
                    ClientWhitelist = new List<string> { "admin-key" },

                    //scope to routes (IP + Client Key + Request URL)
                    EndpointThrottling = true,
                    EndpointRules = new Dictionary<string, RateLimits>
                    { 
                        { "api/values/", new RateLimits { PerSecond = 3 } },
                        { "api/values", new RateLimits { PerMinute = 4 } }
                    }
                },
                Repository = new CacheRepository(),
                Logger = new CustomThrottleLogger()
            });
        }
    }
}
