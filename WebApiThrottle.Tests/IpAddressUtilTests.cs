using WebApiThrottle.Net;
using Xunit;

namespace WebApiThrottle.Tests
{
    public class IpAddressUtilTests
    {
        [Fact]
        public void IsPrivateIpAddress_PrivateAddress_ReturnsTrue()
        {
            var result = IpAddressUtil.IsPrivateIpAddress("10.0.0.1");

            Assert.Equal(true, result);
        }

        [Fact]
        public void IsPrivateIpAddress_PrivateAddressIpv6_ReturnsFalse()
        {
            var result = IpAddressUtil.IsPrivateIpAddress("fd74:20cf:81a2::");

            Assert.Equal(true, result);
        }

        [Fact]
        public void IsPrivateIpAddress_PrivateAddressIpv6WithPort_ReturnsTrue()
        {
            var result = IpAddressUtil.IsPrivateIpAddress("[fd74:20cf:81a2::]:5555");

            Assert.Equal(true, result);
        }

        [Fact]
        public void IsPrivateIpAddress_PrivateAddressWithPort_ReturnsTrue()
        {
            var result = IpAddressUtil.IsPrivateIpAddress("10.0.0.1:5555");

            Assert.Equal(true, result);
        }

        [Fact]
        public void IsPrivateIpAddress_PublicAddress_ReturnsFalse()
        {
            var result = IpAddressUtil.IsPrivateIpAddress("8.8.8.8");

            Assert.Equal(false, result);
        }

        [Fact]
        public void IsPrivateIpAddress_PublicAddressIpv6_ReturnsFalse()
        {
            var result = IpAddressUtil.IsPrivateIpAddress("2001:4860:4860::8888");

            Assert.Equal(false, result);
        }

        [Fact]
        public void IsPrivateIpAddress_PublicIpAddressWithInitialSpace_ReturnsFalse()
        {
            var result = IpAddressUtil.IsPrivateIpAddress(" 8.8.8.8");

            Assert.Equal(false, result);
        }
    }
}