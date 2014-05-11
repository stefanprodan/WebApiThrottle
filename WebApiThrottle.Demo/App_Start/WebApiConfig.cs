using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Tracing;
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

            //trace provider
            var traceWriter = new SystemDiagnosticsTraceWriter()
            {
                IsVerbose = true
            };
            config.Services.Replace(typeof(ITraceWriter), traceWriter);
            config.EnableSystemDiagnosticsTracing();

            //Web API throttling handler
            config.MessageHandlers.Add(new ThrottlingHandler(
                policy: new ThrottlePolicy(perMinute: 20, perHour: 30, perDay: 35, perWeek: 3000)
                {
                    //scope to IPs
                    IpThrottling = true,
                    IpRules = new Dictionary<string, RateLimits>
                    { 
                        { "::1/10", new RateLimits { PerSecond = 2 } },
                        { "192.168.2.1", new RateLimits { PerMinute = 30, PerHour = 30*60, PerDay = 30*60*24 } }
                    },
                    //white list the "::1" IP to disable throttling on localhost for Win8
                    IpWhitelist = new List<string> { "127.0.0.1", "192.168.0.0/24" },

                    //scope to clients (if IP throttling is applied then the scope becomes a combination of IP and client key)
                    ClientThrottling = true,
                    ClientRules = new Dictionary<string, RateLimits>
                    { 
                        { "api-client-key-1", new RateLimits { PerMinute = 60, PerHour = 600 } },
                        { "api-client-key-9", new RateLimits { PerDay = 5000 } }
                    },
                    //white list API keys that don’t require throttling
                    ClientWhitelist = new List<string> { "admin-key" },

                    //scope to endpoints
                    EndpointThrottling = true,
                    EndpointRules = new Dictionary<string, RateLimits>
                    { 
                        { "api/search", new RateLimits { PerSecond = 10, PerMinute = 100, PerHour = 1000 } }
                    }
                },
                policyRepository: new PolicyCacheRepository(),
                repository: new CacheRepository(),
                logger: new TracingThrottleLogger(traceWriter)));

            //Web API throttling handler load policy from web.config
            //config.MessageHandlers.Add(new ThrottlingHandler(
            //    policy: ThrottlePolicy.FromStore(new PolicyConfigurationProvider()),
            //    policyRepository: new PolicyCacheRepository(),
            //    repository: new CacheRepository(),
            //    logger: new TracingThrottleLogger(traceWriter)));

            //Web API throttling filter
            //config.Filters.Add(new ThrottlingFilter(
            //    policy: new ThrottlePolicy(perMinute: 20, perHour: 30, perDay: 35, perWeek: 3000)
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

            //        //Endpoint rate limits will be loaded from EnableThrottling attribute
            //        EndpointThrottling = true
            //    },
            //    policyRepository: new PolicyCacheRepository(),
            //    repository: new CacheRepository(),
            //    logger: new TracingThrottleLogger(traceWriter)));

            //Web API throttling filter load policy from web.config
            //config.Filters.Add(new ThrottlingFilter(           
            //    policy: ThrottlePolicy.FromStore(new PolicyConfigurationProvider()),
            //    policyRepository: new PolicyCacheRepository(),
            //    repository: new CacheRepository(),
            //    logger: new TracingThrottleLogger(traceWriter)));
        }
    }

    
}
