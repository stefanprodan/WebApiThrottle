using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Owin;
using Moq;
using Xunit;

namespace WebApiThrottle.Tests
{
    public class ThrottlingMiddlewareTests
    {
        private static IOwinContext CreateMockContext()
        {
            var context = Mock.Of<IOwinContext>();

            Mock.Get(context).SetupGet(x => x.Request).Returns(Mock.Of<IOwinRequest>());
            Mock.Get(context.Request).SetupAllProperties();
            Mock.Get(context.Request).SetupGet(x => x.Headers).Returns(Mock.Of<IHeaderDictionary>());
            Mock.Get(context.Request.Headers).SetupGet(x => x.Keys).Returns(new List<string>());
            context.Request.RemoteIpAddress = "127.0.0.1";
            Mock.Get(context.Request).SetupGet(x => x.Uri).Returns(new Uri($"http://{context.Request.RemoteIpAddress}"));

            Mock.Get(context).SetupGet(x => x.Response).Returns(Mock.Of<IOwinResponse>());
            Mock.Get(context.Response).SetupAllProperties();
            Mock.Get(context.Response).SetupGet(x => x.Headers).Returns(Mock.Of<IHeaderDictionary>());
            Mock.Get(context.Response.Headers).Setup(x => x.Add("Retry-After", It.IsAny<string[]>()));
            context.Response.StatusCode = 200;

            return context;
        }

        private static ThrottlingMiddleware CreateThrottlingMiddleware()
        {
            return new ThrottlingMiddleware(
                new DummyMiddleware(null),
                new ThrottlePolicy(1) {IpThrottling = true},
                new PolicyMemoryCacheRepository(),
                new MemoryCacheRepository(),
                null,
                null);
        }


        [Fact]
        public void When_RateIsExceeded_Should_SetStatusCodeSoItsAvailableToMiddlewareFurtherDownTheStack()
        {
            var context = CreateMockContext();

            var throttlingMiddleware = CreateThrottlingMiddleware();

            throttlingMiddleware.Invoke(context).Wait();
            throttlingMiddleware.Invoke(context).Wait();

            Assert.Equal(429, context.Response.StatusCode);
        }

        [Fact]
        public void When_RateIsNotExceeded_Should_NotSetStatusCode()
        {
            var context = CreateMockContext();

            CreateThrottlingMiddleware().Invoke(context).Wait();

            Assert.Equal(200, context.Response.StatusCode);
        }
    }

    internal class DummyMiddleware : OwinMiddleware
    {
        public DummyMiddleware(OwinMiddleware next) : base(next)
        {
        }

        public override async Task Invoke(IOwinContext context)
        {
        }
    }
}