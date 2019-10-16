using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace WebApiThrottle.WebApiDemo.Helpers
{
    public class CustomThrottlingHandler : ThrottlingHandler
    {
        protected override Task<RequestIdentity> SetIdentityAsync(System.Net.Http.HttpRequestMessage request)
        {
            return Task.FromResult(new RequestIdentity()
            {
                ClientKey = request.Headers.Contains("Authorization-Key") ? request.Headers.GetValues("Authorization-Key").First() : "anon",
                ClientIp = base.GetClientIp(request).ToString(),
                Endpoint = request.RequestUri.AbsolutePath.ToLowerInvariant()
            });
        }
    }
}