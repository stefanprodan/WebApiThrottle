using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApiThrottle
{
    public class RequestIndentity
    {
        public string ClientIp { get; set; }
        public string ClientKey { get; set; }
        public string Endpoint { get; set; }


        public override string ToString()
        {
            return string.Format("throttle_{0}_{1}_{2}", ClientIp, ClientKey, Endpoint);
        }
    }
}
