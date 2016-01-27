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

            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                var ok = IPAddress.TryParse(((HttpContextBase)request.Properties["MS_HttpContext"]).Request.UserHostAddress, out ipAddress);

                if (ok)
                {
                    return ipAddress;
                }
            }

            if (request.Properties.ContainsKey(RemoteEndpointMessageProperty.Name))
            {
                var ok = IPAddress.TryParse(((RemoteEndpointMessageProperty)request.Properties[RemoteEndpointMessageProperty.Name]).Address, out ipAddress);

                if (ok)
                {
                    return ipAddress;
                }
            }

            if (request.Properties.ContainsKey("MS_OwinContext"))
            {
                var ok = IPAddress.TryParse(((Microsoft.Owin.OwinContext)request.Properties["MS_OwinContext"]).Request.RemoteIpAddress, out ipAddress);

                if (ok)
                {
                    return ipAddress;
                }
            }


            return null;
        }

        public IPAddress ParseIp(string ipAddress)
        {
            return IpAddressUtil.ParseIp(ipAddress);
        }
    }
}
