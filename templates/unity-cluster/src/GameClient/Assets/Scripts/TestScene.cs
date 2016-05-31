using System.Collections;
using System.Net;
using Akka.Interfaced.SlimSocket.Client;
using Common.Logging;
using Domain.Data;
using Domain.Interface;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

public class TestScene : MonoBehaviour, IUserEventObserver
{
    public Text LogText;

    private readonly ILog _logger;
    private Communicator _comm;
    private TrackableUserContext _userContext;

    public TestScene()
    {
        _logger = LogManager.GetLogger("Test");
    }

    private void Start()
    {
        LogText.text = "";

        _comm = CommunicatorHelper.CreateCommunicator<DomainProtobufSerializer>(
            _logger, new IPEndPoint(IPAddress.Loopback, 9001));
        _comm.Start();

        StartCoroutine(ProcessTest());
    }

    IEnumerator ProcessTest()
    {
        yield return new WaitForSeconds(1);

        WriteLine("Start ProcessTest");
        WriteLine("");

        // login with an user-login actor

        var userLogin = _comm.CreateRef<UserLoginRef>();
        var observer = _comm.CreateObserver<IUserEventObserver>(this);

        var t1 = userLogin.Login(observer);
        yield return t1.WaitHandle;
        WriteLine("Login() = " + string.Format("{{ UserId:{0}, UserActorBindId:{1} }}",
                  t1.Result.UserId, t1.Result.User));
        WriteLine("");

        _userContext = new TrackableUserContext();

        // get an user actor from an user-login actor

        var user = t1.Result.User;
        WriteLine("User.SetNickname(\"TestNickname\")");
        yield return user.SetNickname("TestNickname").WaitHandle;
        WriteLine("");

        WriteLine("User.AddNote(1, \"One\")");
        yield return user.AddNote(1, "One").WaitHandle;
        WriteLine("");

        WriteLine("User.AddNote(2, \"Two\")");
        yield return user.AddNote(2, "Two").WaitHandle;
        WriteLine("");

        WriteLine("User.Remove(2)");
        yield return user.RemoveNote(2).WaitHandle;
        WriteLine("");

        WriteLine("End ProcessTest");
    }

    void WriteLine(string text)
    {
        LogText.text = LogText.text + text + "\n";
    }

    private static JsonSerializerSettings s_jsonSerializerSettings =
        new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
            DefaultValueHandling = DefaultValueHandling.Ignore
        };

    void IUserEventObserver.UserContextChange(TrackableUserContextTracker userContextTracker)
    {
        userContextTracker.ApplyTo(_userContext);

        var userContextJson = JsonConvert.SerializeObject(_userContext, s_jsonSerializerSettings);
        WriteLine("-> OnUserContextChange: " + userContextTracker.ToString() + " => " + userContextJson);
    }
}
