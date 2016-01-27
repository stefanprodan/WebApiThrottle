using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace WebApiThrottle.Demo.Helpers
{
    public class CustomThrottlingFilter : ThrottlingFilter
    {
        public CustomThrottlingFilter(ThrottlePolicy policy, IPolicyRepository policyRepository, IThrottleRepository repository, IThrottleLogger logger)
            : base(policy, policyRepository, repository, logger)
        {
            this.QuotaExceededMessage = "API calls quota exceeded! maximum admitted {0} per {1}.";
        }

        protected override RequestIdentity SetIndentity(HttpRequestMessage request)
        {
            return new RequestIdentity()
            {
                ClientKey = request.Headers.Contains("Authorization-Key") ? request.Headers.GetValues("Authorization-Key").First() : "anon",
                ClientIp = base.GetClientIp(request).ToString(),
                Endpoint = request.RequestUri.AbsolutePath.ToLowerInvariant()
            };
        }

        protected override HttpResponseMessage QuotaExceededResponse(HttpRequestMessage request, object content, HttpStatusCode responseCode, string retryAfter)
        {
            var response = request.CreateResponse(responseCode, request);
            response.Headers.Add("Retry-After", new string[] { retryAfter });
            return response;
        }
    }
}