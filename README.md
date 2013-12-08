WebApiThrottle
==============

ASP.NET Web API Throttling handler is designed for controlling the rate of requests that clients 
can make to an Web API based on IP address, client API key and request route. 
WebApiThrottle package is available on NuGet at [nuget.org/packages/WebApiThrottle](https://www.nuget.org/packages/WebApiThrottle/).

Web API throttling can be configured using the built-in ThrottlePolicy, you can set multiple limits 
for different scenarios like allowing an IP or Client to make a maximum number of calls per second, per minute, per hour or even per day.
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
			Policy = new ThrottlePolicy(perSecond: 1, perMinute: 20, perHour: 200, perDay: 1500)
			{
				IpThrottling = true
			},
			Repository = new CacheRepository()
		});
	}
}
```

###Endpoint throttling based on IP

If from the same IP, in same second, you'll make two calls to <code>api/values</code> the last call will get blocked.
But if in the same second you'll call <code>api/values/1</code> too, the request will get throw because it's a different route.

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

If a client (identified by an unique API key) from the same IP, in same second, makes two calls to <code>api/values</code>, then the last call will get blocked. 
If you want to apply limits to clients regarding of their IPs then you should set IpThrottling to false.

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

If requests are initiated from an white-listed IP or Client, then the throttling policy will not be applied and the requests will not get stored. 

``` cs
config.MessageHandlers.Add(new ThrottlingHandler()
{
	Policy = new ThrottlePolicy(perSecond: 2, perMinute: 60)
	{
		IpThrottling = true,
		IpWhitelist = new List<string> { "::1", "10.0.0.1" },
		
		ClientThrottling = true,
		ClientWhitelist = new List<string> { "admin-key" }
	},
	Repository = new CacheRepository()
});
```

###IP and/or Client Key custom rate limits

You can define custom limits for known IPs or Client Keys, these limits will override the default ones. Be aware that a custom limit will work only if you have defined a global counterpart.

``` cs
config.MessageHandlers.Add(new ThrottlingHandler()
{
	Policy = new ThrottlePolicy(perSecond: 1, perMinute: 20, perHour: 200, perDay: 1500)
	{
		IpThrottling = true,
		IpRules = new Dictionary<string, RateLimits>
		{ 
			{ "192.168.0.1", new RateLimits { PerSecond = 2 } },
			{ "192.168.1.2", new RateLimits { PerMinute = 30, PerHour = 30*60, PerDay = 30*60*24 } }
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

###Retrieving API Client Key

By default, the ThrottlingHandler retrieves the client API key from the "Authorization-Token" request header value, 
if you API key is stored differently you can override the <code>ThrottlingHandler.SetIndentity</code> function and specify you own retrieval method.

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

WebApiThrottle stores all requests data in memory using ASP.NET Cache, if you want to change the storage to 
Velocity, MemCache or a NoSQL database all you have to do is create your own repository by implementing the IThrottleRepository interface. 

``` cs
public interface IThrottleRepository
{
	bool Any(string id);
	
	ThrottleCounter FirstOrDefault(string id);
	
	void Save(string id, ThrottleCounter throttleCounter, TimeSpan expirationTime);
	
	void Remove(string id);
	
	void Clear();
}
```
