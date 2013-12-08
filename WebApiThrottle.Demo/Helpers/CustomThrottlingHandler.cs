using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApiThrottle.Demo.Helpers
{
    public class CustomThrottlingHandler : ThrottlingHandler
    {
        protected override RequestIndentity SetIndentity(System.Net.Http.HttpRequestMessage request)
        {
            return new RequestIndentity()
            {
                ClientKey = request.Headers.Contains("Authorization-Key") ? request.Headers.GetValues("Authorization-Key").First() : "anon",
                ClientIp = base.GetClientIp(request).ToString(),
                Endpoint = request.RequestUri.AbsolutePath
            };
        }
    }
}