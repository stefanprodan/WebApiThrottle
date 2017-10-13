using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace WebApiThrottle.Net
{
    public interface IIpAddressParser
    {
        bool ContainsIp(List<string> ipRules, string clientIp);

        bool ContainsIp(List<string> ipRules, string clientIp, out string rule);

        IPAddress GetClientIp(HttpRequestMessage request);

        IPAddress ParseIp(string ipAddress);
    }
}