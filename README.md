WebApiThrottle
==============

ASP.NET Web API Throttling handler is designed to control the rate of requests that clients 
can make to a Web API based on IP address, client API key and request route. 
WebApiThrottle package is available on NuGet at [nuget.org/packages/WebApiThrottle](https://www.nuget.org/packages/WebApiThrottle/).

Web API throttling can be configured using the built-in ThrottlePolicy. You can set multiple limits 
for different scenarios like allowing an IP or Client to make a maximum number of calls per second, per minute, per hour per day or even per week.
You can define these limits to address all requests made to an API or you can scope the limits to each API route.  

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

###Endpoint throttling based on IP

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

###Endpoint throttling based on IP and Client Key

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

###IP and/or Client Key White-listing

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

###IP and/or Client Key custom rate limits

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
###Endpoint custom rate limits

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
			{ "api/search", new RateLimits { PerScond = 10, PerMinute = 100, PerHour = 1000 } }
		}
	},
	Repository = new CacheRepository()
});
```

###Stack rejected requests

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

###Retrieving API Client Key

By default, the ThrottlingHandler retrieves the client API key from the "Authorization-Token" request header value. 
If your API key is stored differently, you can override the <code>ThrottlingHandler.SetIndentity</code> function and specify your own retrieval method.

``` cs
public class CustomThrottlingHandler : ThrottlingHandler
{
	protected override RequestIndentity SetIndentity(HttpRequestMessage request)
	{
		return new RequestIndentity()
		{
			ClientKey = request.Headers.GetValues("Authorization-Key").First(),
			ClientIp = base.GetClientIp(request).ToString(),
			Endpoint = request.RequestUri.AbsolutePath
		};
	}
}
```
###Storing throttle metrics 

WebApiThrottle stores all request data in-memory using ASP.NET Cache. If you want to change the storage to 
Velocity, MemCache or a NoSQL database, all you have to do is create your own repository by implementing the IThrottleRepository interface. 

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

###Logging throttled requests

If you want to log throttled requests you'll have to implement IThrottleLogger interface and provide it to the ThrottlingHandler. 

``` cs
public interface IThrottleLogger
{
	void Log(ThrottleLogEntry entry);
}
```

Logging implementation example
``` cs
public class DebugThrottleLogger : IThrottleLogger
{
	public void Log(ThrottleLogEntry entry)
	{
		Debug.WriteLine("{0} Request {1} has been blocked, quota {2}/{3} exceeded by {4}",
		   entry.LogDate, entry.RequestId, entry.RateLimit, entry.RateLimitPeriod, entry.TotalRequests);
	}
}
```

Logging usage example 
``` cs
config.MessageHandlers.Add(new ThrottlingHandler()
{
	Policy = new ThrottlePolicy(perSecond: 1, perMinute: 30)
	{
		IpThrottling = true,
		ClientThrottling = true,
		EndpointThrottling = true
	},
	Repository = new CacheRepository(),
	Logger = new DebugThrottleLogger()
});
```
