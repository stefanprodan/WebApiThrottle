using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;

namespace WebApiThrottle.Net
{
    public class IpAddressUtil
    {
        public static bool ContainsIp(List<string> ipRules, string clientIp)
        {
            var ip = ParseIp(clientIp);
            if (ipRules != null && ipRules.Any())
            {
                foreach (var rule in ipRules)
                {
                    var range = new IPAddressRange(rule);
                    if (range.Contains(ip))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool ContainsIp(List<string> ipRules, string clientIp, out string rule)
        {
            rule = null;
            var ip = ParseIp(clientIp);
            if (ipRules != null && ipRules.Any())
            {
                foreach (var r in ipRules)
                {
                    var range = new IPAddressRange(r);
                    if (range.Contains(ip))
                    {
                        rule = r;
                        return true;
                    }
                }
            }

            return false;
        }

        private static readonly IPAddress _defaultIPAddress = new IPAddress(new byte[] { 169, 254, 0, 0 });
        
        public static IPAddress ParseIp(string ipAddress)
        {
            ipAddress = ipAddress.Trim();
            int portDelimiterPos = ipAddress.LastIndexOf(":", StringComparison.InvariantCultureIgnoreCase);
            bool ipv6WithPortStart = ipAddress.StartsWith("[");
            int ipv6End = ipAddress.IndexOf("]");
            if (portDelimiterPos != -1
                && portDelimiterPos == ipAddress.IndexOf(":", StringComparison.InvariantCultureIgnoreCase)
                || ipv6WithPortStart && ipv6End != -1 && ipv6End < portDelimiterPos)
            {
                ipAddress = ipAddress.Substring(0, portDelimiterPos);
            }
            
            // If IPAddress can not be parsed, we return a default Link-local address.
            // Sometimes, X-Forwarded-For headers have non valid IP Address
            return (IPAddress.TryParse(ipAddress, out var address) ? address : _defaultIPAddress);
        }

        public static bool IsPrivateIpAddress(string ipAddress)
        {
            // http://en.wikipedia.org/wiki/Private_network
            // Private IP Addresses are: 
            //  24-bit block: 10.0.0.0 through 10.255.255.255
            //  20-bit block: 172.16.0.0 through 172.31.255.255
            //  16-bit block: 192.168.0.0 through 192.168.255.255
            //  Link-local addresses: 169.254.0.0 through 169.254.255.255 (http://en.wikipedia.org/wiki/Link-local_address)

            var ip = ParseIp(ipAddress);
            var octets = ip.GetAddressBytes();

            bool isIpv6 = octets.Length == 16;

            if (isIpv6)
            {
                bool isUniqueLocalAddress = octets[0] == 253;
                return isUniqueLocalAddress;
            }
            else
            {
                var is24BitBlock = octets[0] == 10;
                if (is24BitBlock) return true; // Return to prevent further processing

                var is20BitBlock = octets[0] == 172 && octets[1] >= 16 && octets[1] <= 31;
                if (is20BitBlock) return true; // Return to prevent further processing

                var is16BitBlock = octets[0] == 192 && octets[1] == 168;
                if (is16BitBlock) return true; // Return to prevent further processing

                var isLinkLocalAddress = octets[0] == 169 && octets[1] == 254;
                return isLinkLocalAddress;
            }
        }
    }
}
