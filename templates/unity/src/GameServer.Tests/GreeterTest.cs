using System;
using System.Threading.Tasks;
using System.Net;
using Akka.Interfaced;
using Akka.TestKit.Xunit2;
using Domain;
using Xunit;
using Xunit.Abstractions;

namespace GameServer.Tests
{
    public class GreeterTest : TestKit
    {
        private readonly IDisposable _logCapture;

        public GreeterTest(ITestOutputHelper outputHelper)
        {
            // _logCapture = LoggingHelper.Capture(outputHelper);
        }

        private GreeterRef CreateGreeterActor()
        {
            return Sys.InterfacedActorOf(() => new Greeter(null, new IPEndPoint(IPAddress.Any, 0))).Cast<GreeterRef>();
        }

        [Fact]
        public async Task Test_Hello()
        {
            var greeter = CreateGreeterActor();

            var result = await greeter.Hello("Alice");

            Assert.Equal("Hello Alice!", result);
        }

        [Fact]
        public async Task Test_GetHelloCount()
        {
            var greeter = CreateGreeterActor();
            await greeter.Hello("A");
            await greeter.Hello("B");

            var result = await greeter.GetHelloCount();

            Assert.Equal(2, result);
        }
    }
}
