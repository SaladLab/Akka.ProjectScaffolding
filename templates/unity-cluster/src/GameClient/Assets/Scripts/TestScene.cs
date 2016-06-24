using System.Collections;
using System.Net;
using Akka.Interfaced.SlimSocket;
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

    private TrackableUserContext _userContext;

    private void Start()
    {
        StartCoroutine(ProcessTest(ChannelType.Tcp));
    }

    IEnumerator ProcessTest(ChannelType channelType)
    {
        LogText.text = "ProcessTest(" + channelType + ")\n";

        // create channel

        var channelFactory = ChannelFactoryBuilder.Build<DomainProtobufSerializer>(
            endPoint: new IPEndPoint(IPAddress.Loopback, 9001),
            createChannelLogger: () => LogManager.GetLogger("Channel"));
        channelFactory.Type = channelType;
        var channel = channelFactory.Create();

        // connect to gateway

        var t0 = channel.ConnectAsync();
        yield return t0.WaitHandle;
        if (t0.Exception != null)
        {
            WriteLine("Connection Failed: " + t0.Exception.Message);
            yield break;
        }

        // login with an user-login actor

        var userLogin = channel.CreateRef<UserLoginRef>();
        var observer = channel.CreateObserver<IUserEventObserver>(this);

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

        channel.Close();
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
