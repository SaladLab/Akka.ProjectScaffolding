using System.Collections;
using System.Net;
using Akka.Interfaced.SlimSocket.Base;
using Akka.Interfaced.SlimSocket.Client;
using Common.Logging;
using TypeAlias;
using UnityEngine;
using UnityEngine.UI;
using Domain.Interface;

public class TestScene : MonoBehaviour
{
    public Text LogText;

    private Communicator _comm;
    private readonly ILog _logger;

    public TestScene()
    {
        _logger = LogManager.GetLogger("Test");
    }

    private void Start()
    {
        LogText.text = "";

        var serializer = new PacketSerializer(
            new PacketSerializerBase.Data(
                new ProtoBufMessageSerializer(new DomainProtobufSerializer()),
                new TypeAliasTable()));

        _comm = new Communicator(_logger, new IPEndPoint(IPAddress.Loopback, 9001),
            _ => new TcpConnection(serializer, LogManager.GetLogger("Connection")));
        _comm.Start();

        StartCoroutine(ProcessTest());
    }

    IEnumerator ProcessTest()
    {
        yield return new WaitForSeconds(1);

        WriteLine("Start ProcessTest");
        WriteLine("");

        var greeter = new UserLoginRef(new SlimActorRef(1), new SlimRequestWaiter(_comm, this), null);

        var t1 = greeter.Login(1);
        yield return t1.WaitHandle;
        WriteLine("Hello(Alice) = " + t1.Result);

        WriteLine("");
        WriteLine("End ProcessTest");
    }

    void WriteLine(string text)
    {
        LogText.text = LogText.text + text + "\n";
    }
}
