using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebApiThrottle.Net;
using Xunit;

namespace WebApiThrottle.Tests
{
    public class IpAddressUtilTests
    {
        [Fact]
        public void IsPrivateIpAddress_PrivateAddress_ReturnsTrue()
        {
            bool result = IpAddressUtil.IsPrivateIpAddress("10.0.0.1");

            Assert.Equal(true, result);
        }

        [Fact]
        public void IsPrivateIpAddress_PublicAddress_ReturnsFalse()
        {
            bool result = IpAddressUtil.IsPrivateIpAddress("8.8.8.8");

            Assert.Equal(false, result);
        }

        [Fact]
        public void IsPrivateIpAddress_PrivateAddressWithPort_ReturnsTrue()
        {
            bool result = IpAddressUtil.IsPrivateIpAddress("10.0.0.1:5555");

            Assert.Equal(true, result);
        }
    }
}
