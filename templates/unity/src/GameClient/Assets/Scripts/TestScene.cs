using System.Collections;
using System.Net;
using Akka.Interfaced.SlimSocket;
using Akka.Interfaced.SlimSocket.Client;
using Common.Logging;
using Domain;
using UnityEngine;
using UnityEngine.UI;

public class TestScene : MonoBehaviour
{
    public Text LogText;

    private void Start()
    {
        StartCoroutine(ProcessTest(ChannelType.Tcp));
    }

    IEnumerator ProcessTest(ChannelType channelType)
    {
        LogText.text = "ProcessTest(" + channelType + ")\n";

        // Create communicator

        var communicator = UnityCommunicatorFactory.Create();
        {
            var channelFactory = communicator.ChannelFactory;
            channelFactory.Type = ChannelType.Tcp;
            channelFactory.ConnectEndPoint = new IPEndPoint(IPAddress.Loopback, 5000);
            channelFactory.CreateChannelLogger = () => LogManager.GetLogger("Channel");
            channelFactory.PacketSerializer = PacketSerializer.CreatePacketSerializer<DomainProtobufSerializer>();
        }

        // Connect channel

        var channel = communicator.CreateChannel();
        var t0 = channel.ConnectAsync();
        yield return t0.WaitHandle;
        if (t0.Exception != null)
        {
            WriteLine("Connection Failed: " + t0.Exception.Message);
            yield break;
        }

        // Start communicating with actors via channel

        var greeter = channel.CreateRef<GreeterRef>();

        WriteLine("Start ProcessTest");
        WriteLine("");

        var t1 = greeter.Hello("Alice");
        yield return t1.WaitHandle;
        WriteLine("Hello(Alice) = " + t1.Result);

        var t2 = greeter.Hello("Bob");
        yield return t2.WaitHandle;
        WriteLine("Hello(Bob) = " + t2.Result);

        var t3 = greeter.GetHelloCount();
        yield return t3.WaitHandle;
        WriteLine("GetHelloCount = " + t3.Result);

        WriteLine("");
        WriteLine("End ProcessTest");

        channel.Close();
    }

    void WriteLine(string text)
    {
        LogText.text = LogText.text + text + "\n";
    }
}
