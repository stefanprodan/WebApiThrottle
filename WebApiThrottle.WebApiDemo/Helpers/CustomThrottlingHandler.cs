using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApiThrottle.WebApiDemo.Helpers
{
    public class CustomThrottlingHandler : ThrottlingHandler
    {
        protected override RequestIdentity SetIdentity(System.Net.Http.HttpRequestMessage request)
        {
            return new RequestIdentity()
            {
                ClientKey = request.Headers.Contains("Authorization-Key") ? request.Headers.GetValues("Authorization-Key").First() : "anon",
                ClientIp = base.GetClientIp(request).ToString(),
                Endpoint = request.RequestUri.AbsolutePath.ToLowerInvariant()
            };
        }
    }
}