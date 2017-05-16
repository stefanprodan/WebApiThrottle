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
            return ParseIp(request.GetClientIpAddress());
        }

        public IPAddress ParseIp(string ipAddress)
        {
            return IpAddressUtil.ParseIp(ipAddress);
        }

    }
}
