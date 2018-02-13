using System.Linq;
using System.Net.Http;
using WebApiThrottle.Models;

namespace WebApiThrottle.WebApiDemo.Helpers
{
    public class CustomThrottlingHandler : ThrottlingHandler
    {
        protected override RequestIdentity SetIdentity(HttpRequestMessage request)
        {
            return new RequestIdentity
            {
                ClientKey = request.Headers.Contains("Authorization-Key")
                    ? request.Headers.GetValues("Authorization-Key").First()
                    : "anon",
                ClientIp = GetClientIp(request).ToString(),
                Endpoint = request.RequestUri.AbsolutePath.ToLowerInvariant()
            };
        }
    }
}