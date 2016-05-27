using System.Collections;
using System.Net;
using Akka.Interfaced.SlimSocket.Client;
using Common.Logging;
using Domain;
using UnityEngine;
using UnityEngine.UI;

public class TestScene : MonoBehaviour
{
    public Text LogText;

    private readonly ILog _logger;
    private Communicator _comm;

    public TestScene()
    {
        _logger = LogManager.GetLogger("Test");
    }

    private void Start()
    {
        LogText.text = "";

        _comm = CommunicatorHelper.CreateCommunicator<DomainProtobufSerializer>(
            _logger, new IPEndPoint(IPAddress.Loopback, 5000));
        _comm.Start();

        StartCoroutine(ProcessTest());
    }

    IEnumerator ProcessTest()
    {
        yield return new WaitForSeconds(1);

        WriteLine("Start ProcessTest");
        WriteLine("");

        var greeter = _comm.CreateRef<GreeterRef>();

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
    }

    void WriteLine(string text)
    {
        LogText.text = LogText.text + text + "\n";
    }
}
