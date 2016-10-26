using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.ServiceModel.Channels;
using System.Web;

namespace WebApiThrottle.Net
{
    public class DefaultIpAddressParser : IIpAddressParser
    {
        public bool ContainsIp(List<string> ipRules, string clientIp)
        {
            return IpAddressUtil.ContainsIp(ipRules, clientIp);
        }

        public bool ContainsIp(List<string> ipRules, string clientIp, out string rule)
        {
            return IpAddressUtil.ContainsIp(ipRules, clientIp, out rule);
        }

        public virtual IPAddress GetClientIp(HttpRequestMessage request)
        {
            IPAddress ipAddress;
            
            // use the extension method to get the client ip address as this will
            // handle the X-Forward-For header
            var ok = IPAddress.TryParse(request.GetClientIpAddress(), out ipAddress);

            if (ok)
            {
                return ipAddress;
            }


            return null;
        }

        public IPAddress ParseIp(string ipAddress)
        {
            return IpAddressUtil.ParseIp(ipAddress);
        }

    }
}
