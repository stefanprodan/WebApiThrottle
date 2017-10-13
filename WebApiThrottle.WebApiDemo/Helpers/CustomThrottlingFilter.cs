﻿using System.Linq;
using System.Net;
using System.Net.Http;
using WebApiThrottle.Logging;
using WebApiThrottle.Models;
using WebApiThrottle.Repositories;

namespace WebApiThrottle.WebApiDemo.Helpers
{
    public class CustomThrottlingFilter : ThrottlingFilter
    {
        public CustomThrottlingFilter(ThrottlePolicy policy, IPolicyRepository policyRepository,
            IThrottleRepository repository, IThrottleLogger logger)
            : base(policy, policyRepository, repository, logger)
        {
            QuotaExceededMessage = "API calls quota exceeded! maximum admitted {0} per {1}.";
        }

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

        protected override HttpResponseMessage QuotaExceededResponse(HttpRequestMessage request, object content,
            HttpStatusCode responseCode, string retryAfter)
        {
            var response = request.CreateResponse(responseCode, request);
            response.Headers.Add("Retry-After", new[] {retryAfter});
            return response;
        }
    }
}