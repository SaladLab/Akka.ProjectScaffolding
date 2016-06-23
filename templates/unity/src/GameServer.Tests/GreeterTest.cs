using System.Threading.Tasks;
using System.Net;
using Akka.Actor;
using Akka.Interfaced;
using Akka.TestKit.Xunit2;
using Domain;
using Xunit;

namespace GameServer.Tests
{
    public class GreeterTest : TestKit
    {
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
