﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApiThrottle
{
    /// <summary>
    /// Stores the client IP, key and endpoint
    /// </summary>
    [Serializable]
    public class RequestIdentity
    {
        public string ClientIp { get; set; }

        public string ClientKey { get; set; }

        public string Endpoint { get; set; }

        public string HttpMethod { get; set; }

        public string EndpointWithMethod
        {
            get
            {
                return string.Format("{0}{1}", HttpMethod, Endpoint).ToLowerInvariant();
            }
        }
    }
}
