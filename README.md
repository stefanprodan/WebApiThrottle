WebApiThrottle
==============

ASP.NET Web API Throttling handler, OWIN middleware and filter are designed to control the rate of requests that clients 
can make to a Web API based on IP address, client API key and request route. 
WebApiThrottle package is available on NuGet at [nuget.org/packages/WebApiThrottle](https://www.nuget.org/packages/WebApiThrottle/).

Web API throttling can be configured using the built-in ThrottlePolicy. You can set multiple limits 
for different scenarios like allowing an IP or Client to make a maximum number of calls per second, per minute, per hour per day or even per week.
You can define these limits to address all requests made to an API or you can scope the limits to each API route.  

---
If you are looking for the ASP.NET Core version please head to [AspNetCoreRateLimit](https://github.com/stefanprodan/AspNetCoreRateLimit) project.

AspNetCoreRateLimit is a full rewrite of WebApiThrottle and offers more flexibility in configuring rate limiting for Web API and MVC apps.

---

###Global throttling based on IP

The setup bellow will limit the number of requests originated from the same IP. 
If from the same IP, in same second, you'll make a call to <code>api/values</code> and <code>api/values/1</code> the last call will get blocked.

``` cs
public static class WebApiConfig
{
	public static void Register(HttpConfiguration config)
	{
		config.MessageHandlers.Add(new ThrottlingHandler()
		{
			Policy = new ThrottlePolicy(perSecond: 1, perMinute: 20, perHour: 200, perDay: 1500, perWeek: 3000)
			{
				IpThrottling = true
			},
			Repository = new CacheRepository()
		});
	}
}
```

If you are self-hosting WebApi with Owin, then you'll have to switch to <code>MemoryCacheRepository</code> that uses the runtime memory cache instead of <code>CacheRepository</code> that uses ASP.NET cache.

``` cs
public class Startup
{
    public void Configuration(IAppBuilder appBuilder)
    {
        // Configure Web API for self-host. 
        HttpConfiguration config = new HttpConfiguration();

        //Register throttling handler
        config.MessageHandlers.Add(new ThrottlingHandler()
        {
            Policy = new ThrottlePolicy(perSecond: 1, perMinute: 20, perHour: 200, perDay: 1500, perWeek: 3000)
            {
                IpThrottling = true
            },
            Repository = new MemoryCacheRepository()
        });

        appBuilder.UseWebApi(config);
    }
}
```

### Endpoint throttling based on IP

If, from the same IP, in the same second, you'll make two calls to <code>api/values</code>, the last call will get blocked.
But if in the same second you call <code>api/values/1</code> too, the request will go through because it's a different route.

``` cs
config.MessageHandlers.Add(new ThrottlingHandler()
{
	Policy = new ThrottlePolicy(perSecond: 1, perMinute: 30)
	{
		IpThrottling = true,
		EndpointThrottling = true
	},
	Repository = new CacheRepository()
});
```

### Endpoint throttling based on IP and Client Key

If a client (identified by an unique API key) from the same IP, in the same second, makes two calls to <code>api/values</code>, then the last call will get blocked. 
If you want to apply limits to clients regardless of their IPs then you should set IpThrottling to false.

``` cs
config.MessageHandlers.Add(new ThrottlingHandler()
{
	Policy = new ThrottlePolicy(perSecond: 1, perMinute: 30)
	{
		IpThrottling = true,
		ClientThrottling = true,
		EndpointThrottling = true
	},
	Repository = new CacheRepository()
});
```

### IP and/or Client Key White-listing

If requests are initiated from a white-listed IP or Client, then the throttling policy will not be applied and the requests will not get stored. The IP white-list supports IP v4 and v6 ranges like "192.168.0.0/24", "fe80::/10" and "192.168.0.0-192.168.0.255" for more information check [jsakamoto/ipaddressrange](https://github.com/jsakamoto/ipaddressrange).

``` cs
config.MessageHandlers.Add(new ThrottlingHandler()
{
	Policy = new ThrottlePolicy(perSecond: 2, perMinute: 60)
	{
		IpThrottling = true,
		IpWhitelist = new List<string> { "::1", "192.168.0.0/24" },
		
		ClientThrottling = true,
		ClientWhitelist = new List<string> { "admin-key" }
	},
	Repository = new CacheRepository()
});
```

### IP and/or Client Key custom rate limits

You can define custom limits for known IPs or Client Keys, these limits will override the default ones. Be aware that a custom limit will only work if you have defined a global counterpart.

``` cs
config.MessageHandlers.Add(new ThrottlingHandler()
{
	Policy = new ThrottlePolicy(perSecond: 1, perMinute: 20, perHour: 200, perDay: 1500)
	{
		IpThrottling = true,
		IpRules = new Dictionary<string, RateLimits>
		{ 
			{ "192.168.1.1", new RateLimits { PerSecond = 2 } },
			{ "192.168.2.0/24", new RateLimits { PerMinute = 30, PerHour = 30*60, PerDay = 30*60*24 } }
		},
		
		ClientThrottling = true,
		ClientRules = new Dictionary<string, RateLimits>
		{ 
			{ "api-client-key-1", new RateLimits { PerMinute = 40, PerHour = 400 } },
			{ "api-client-key-9", new RateLimits { PerDay = 2000 } }
		}
	},
	Repository = new CacheRepository()
});
```
### Endpoint custom rate limits

You can also define custom limits for certain routes, these limits will override the default ones. 
You can define endpoint rules by providing relative routes like <code>api/entry/1</code> or just a URL segment like <code>/entry/</code>. 
The endpoint throttling engine will search for the expression you've provided in the absolute URI, 
if the expression is contained in the request route then the rule will be applied. 
If two or more rules match the same URI then the lower limit will be applied.

``` cs
config.MessageHandlers.Add(new ThrottlingHandler()
{
	Policy = new ThrottlePolicy(perSecond: 1, perMinute: 20, perHour: 200)
	{
		IpThrottling = true,
		ClientThrottling = true,
		EndpointThrottling = true,
		EndpointRules = new Dictionary<string, RateLimits>
		{ 
			{ "api/search", new RateLimits { PerSecond = 10, PerMinute = 100, PerHour = 1000 } }
		}
	},
	Repository = new CacheRepository()
});
```

###Add additional Suspend Time after rate limit exceeded

The setup bellow will block request for 5 minutes after rate limit exceeded 

``` cs
public static class WebApiConfig
{
	public static void Register(HttpConfiguration config)
	{
		config.MessageHandlers.Add(new ThrottlingHandler()
		{
			Policy = new ThrottlePolicy(perSecond: 1, perMinute: 20, perHour: 200, perDay: 1500, perWeek: 3000)
			{
				IpThrottling = true,
				SuspendTime = 300
			},
			Repository = new CacheRepository()
		});
	}
}
```


### Stack rejected requests

By default, rejected calls are not added to the throttle counter. If a client makes 3 requests per second 
and you've set a limit of one call per second, the minute, hour and day counters will only record the first call, the one that wasn't blocked.
If you want rejected requests to count towards the other limits, you'll have to set <code>StackBlockedRequests</code> to true.

``` cs
config.MessageHandlers.Add(new ThrottlingHandler()
{
	Policy = new ThrottlePolicy(perSecond: 1, perMinute: 30)
	{
		IpThrottling = true,
		ClientThrottling = true,
		EndpointThrottling = true,
		StackBlockedRequests = true
	},
	Repository = new CacheRepository()
});
```

### Define rate limits in web.config or app.config

WebApiThrottle comes with a custom configuration section that lets you define the throttle policy as xml.

``` cs
config.MessageHandlers.Add(new ThrottlingHandler()
{
    Policy = ThrottlePolicy.FromStore(new PolicyConfigurationProvider()),
    Repository = new CacheRepository()
});
```

Config example (policyType values are 1 - IP, 2 - ClientKey, 3 - Endpoint):
``` xml
<configuration>
  
  <configSections>
    <section name="throttlePolicy" 
             type="WebApiThrottle.ThrottlePolicyConfiguration, WebApiThrottle" />
  </configSections>
  
  <throttlePolicy limitPerSecond="1"
                  limitPerMinute="10"
                  limitPerHour="30"
                  limitPerDay="300"
                  limitPerWeek ="1500"
                  ipThrottling="true"
                  clientThrottling="true"
                  endpointThrottling="true">
    <rules>
      <!--Ip rules-->
      <add policyType="1" entry="::1/10"
           limitPerSecond="2"
           limitPerMinute="15"/>
      <add policyType="1" entry="192.168.2.1"
           limitPerMinute="12" />
      <!--Client rules-->
      <add policyType="2" entry="api-client-key-1"
           limitPerHour="60" />
      <!--Endpoint rules-->
      <add policyType="3" entry="api/values"
           limitPerDay="120" />
    </rules>
    <whitelists>
      <!--Ip whitelist-->
      <add policyType="1" entry="127.0.0.1" />
      <add policyType="1" entry="192.168.0.0/24" />
      <!--Client whitelist-->
      <add policyType="2" entry="api-admin-key" />
    </whitelists>
  </throttlePolicy>

</configuration>
``` 

### Retrieving API Client Key

By default, the ThrottlingHandler retrieves the client API key from the "Authorization-Token" request header value. 
If your API key is stored differently, you can override the <code>ThrottlingHandler.SetIndentity</code> function and specify your own retrieval method.

``` cs
public class CustomThrottlingHandler : ThrottlingHandler
{
	protected override RequestIdentity SetIndentity(HttpRequestMessage request)
	{
		return new RequestIdentity()
		{
			ClientKey = request.Headers.Contains("Authorization-Key") ? request.Headers.GetValues("Authorization-Key").First() : "anon",
			ClientIp = base.GetClientIp(request).ToString(),
			Endpoint = request.RequestUri.AbsolutePath.ToLowerInvariant()
		};
	}
}
```

### Storing throttle metrics 

WebApiThrottle stores all request data in-memory using ASP.NET Cache when hosted in IIS or Runtime MemoryCache when self-hosted with Owin. If you want to change the storage to Velocity, Redis or a NoSQL database, all you have to do is create your own repository by implementing the <code>IThrottleRepository</code> interface. 

``` cs
public interface IThrottleRepository
{
	bool Any(string id);
	
	ThrottleCounter? FirstOrDefault(string id);
	
	void Save(string id, ThrottleCounter throttleCounter, TimeSpan expirationTime);
	
	void Remove(string id);
	
	void Clear();
}
```

Since version 1.2 there is an interface for storing and retrieving the policy object as well. The <code>IPolicyRepository</code> is used to update the policy object at runtime.

``` cs
public interface IPolicyRepository
{
    ThrottlePolicy FirstOrDefault(string id);
    
    void Remove(string id);
    
    void Save(string id, ThrottlePolicy policy);
}
```

### Update rate limits at runtime

In order to update the policy object at runtime you'll need to use the new <code>ThrottlingHandler</code> constructor along with <code>ThrottleManager.UpdatePolicy</code> function introduced in WebApiThrottle v1.2.  

Register the <code>ThrottlingHandler</code> providing <code>PolicyCacheRepository</code> in the constructor, if you are self-hosting the service with Owin then use <code>PolicyMemoryCacheRepository</code>:

``` cs
public static void Register(HttpConfiguration config)
{
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
            
            //scope to clients
            ClientThrottling = true,
            ClientRules = new Dictionary<string, RateLimits>
            { 
                { "api-client-key-1", new RateLimits { PerMinute = 60, PerHour = 600 } },
                { "api-client-key-2", new RateLimits { PerDay = 5000 } }
            },

            //scope to endpoints
            EndpointThrottling = true
        },
        
        //replace with PolicyMemoryCacheRepository for Owin self-host
        policyRepository: new PolicyCacheRepository(),
        
        //replace with MemoryCacheRepository for Owin self-host
        repository: new CacheRepository(),
        
        logger: new TracingThrottleLogger(traceWriter)));
}

```

When you want to update the policy object call the static method <code>ThrottleManager.UpdatePolicy</code> anywhere in you code.

``` cs
public void UpdateRateLimits()
{
    //init policy repo
    var policyRepository = new PolicyCacheRepository();

    //get policy object from cache
    var policy = policyRepository.FirstOrDefault(ThrottleManager.GetPolicyKey());

    //update client rate limits
    policy.ClientRules["api-client-key-1"] =
        new RateLimits { PerMinute = 80, PerHour = 800 };

    //add new client rate limits
    policy.ClientRules.Add("api-client-key-3",
        new RateLimits { PerMinute = 60, PerHour = 600 });

    //apply policy updates
    ThrottleManager.UpdatePolicy(policy, policyRepository);

}
```

### Logging throttled requests

If you want to log throttled requests you'll have to implement IThrottleLogger interface and provide it to the ThrottlingHandler. 

``` cs
public interface IThrottleLogger
{
	void Log(ThrottleLogEntry entry);
}
```

Logging implementation example with ITraceWriter
``` cs
public class TracingThrottleLogger : IThrottleLogger
{
    private readonly ITraceWriter traceWriter;
        
    public TracingThrottleLogger(ITraceWriter traceWriter)
    {
        this.traceWriter = traceWriter;
    }
       
    public void Log(ThrottleLogEntry entry)
    {
        if (null != traceWriter)
        {
            traceWriter.Info(entry.Request, "WebApiThrottle",
                "{0} Request {1} from {2} has been throttled (blocked), quota {3}/{4} exceeded by {5}",
                entry.LogDate, entry.RequestId, entry.ClientIp, entry.RateLimit, entry.RateLimitPeriod, entry.TotalRequests);
        }
    }
}
```

Logging usage example with SystemDiagnosticsTraceWriter and ThrottlingHandler
``` cs
var traceWriter = new SystemDiagnosticsTraceWriter()
{
    IsVerbose = true
};
config.Services.Replace(typeof(ITraceWriter), traceWriter);
config.EnableSystemDiagnosticsTracing();
            
config.MessageHandlers.Add(new ThrottlingHandler()
{
	Policy = new ThrottlePolicy(perSecond: 1, perMinute: 30)
	{
		IpThrottling = true,
		ClientThrottling = true,
		EndpointThrottling = true
	},
	Repository = new CacheRepository(),
	Logger = new TracingThrottleLogger(traceWriter)
});
```

### Attribute-based rate limiting with ThrottlingFilter and EnableThrottlingAttribute

As an alternative to the ThrottlingHandler, ThrottlingFilter does the same thing but allows custom rate limits to be specified by decorating Web API controllers and actions with EnableThrottlingAttribute. Be aware that when a request is processed, the ThrottlingHandler executes before the http controller dispatcher in the [Web API request pipeline](http://www.asp.net/posters/web-api/asp.net-web-api-poster-grayscale.pdf), therefore it is preferable that you always use the handler instead of the filter when you don't need the features that the ThrottlingFilter provides.

Setup the filter as you would the ThrottlingHandler:

``` cs
config.Filters.Add(new ThrottlingFilter()
{
    Policy = new ThrottlePolicy(perSecond: 1, perMinute: 20, 
    perHour: 200, perDay: 2000, perWeek: 10000)
    {
        //scope to IPs
        IpThrottling = true,
        IpRules = new Dictionary<string, RateLimits>
        { 
            { "::1/10", new RateLimits { PerSecond = 2 } },
            { "192.168.2.1", new RateLimits { PerMinute = 30, PerHour = 30*60, PerDay = 30*60*24 } }
        },
        //white list the "::1" IP to disable throttling on localhost
        IpWhitelist = new List<string> { "127.0.0.1", "192.168.0.0/24" },

        //scope to clients (if IP throttling is applied then the scope becomes a combination of IP and client key)
        ClientThrottling = true,
        ClientRules = new Dictionary<string, RateLimits>
        { 
            { "api-client-key-demo", new RateLimits { PerDay = 5000 } }
        },
        //white list API keys that don’t require throttling
        ClientWhitelist = new List<string> { "admin-key" },

        //Endpoint rate limits will be loaded from EnableThrottling attribute
        EndpointThrottling = true
    }
});
```

Use the attributes to toggle throttling and set rate limits:

``` cs
[EnableThrottling(PerSecond = 2)]
public class ValuesController : ApiController
{
    [EnableThrottling(PerSecond = 1, PerMinute = 30, PerHour = 100)]
    public IEnumerable<string> Get()
    {
        return new string[] { "value1", "value2" };
    }

    [DisableThrotting]
    public string Get(int id)
    {
        return "value";
    }
}
```

### Rate limiting with ThrottlingMiddleware

ThrottlingMiddleware is an OWIN middleware component that works the same as the ThrottlingHandler. With the ThrottlingMiddleware you can target endpoints outside of the WebAPI area, like OAuth middleware or SignalR endpoints.

Self-hosted configuration example:

``` cs
public class Startup
{
    public void Configuration(IAppBuilder appBuilder)
    {
        ...

        //throtting middleware with policy loaded from app.config
        appBuilder.Use(typeof(ThrottlingMiddleware),
            ThrottlePolicy.FromStore(new PolicyConfigurationProvider()),
            new PolicyMemoryCacheRepository(),
            new MemoryCacheRepository(),
            null,
            null);

        ...
    }
}
```

IIS hosted configuration example:

``` cs
public class Startup
{
    public void Configuration(IAppBuilder appBuilder)
    {
        ...

	//throtting middleware with policy loaded from web.config
	appBuilder.Use(typeof(ThrottlingMiddleware),
	    ThrottlePolicy.FromStore(new PolicyConfigurationProvider()),
	    new PolicyCacheRepository(),
	    new CacheRepository(),
        null,
        null);

        ...
    }
}
```

### Custom ip address parsing

If you need to extract client ip's from e.g. additional headers then you can plug in custom ipAddressParsers.
There is an example implementation in the WebApiThrottle.Demo project - <code>WebApiThrottle.Demo.Net.CustomIpAddressParser</code>

``` cs
config.MessageHandlers.Add(new ThrottlingHandler(
    policy: new ThrottlePolicy(perMinute: 20, perHour: 30, perDay: 35, perWeek: 3000)
    {        
        IpThrottling = true,
        ///...
    },
    policyRepository: new PolicyCacheRepository(),
    repository: new CacheRepository(),
    logger: new TracingThrottleLogger(traceWriter),
    ipAddressParser: new CustomIpAddressParser()));
```
