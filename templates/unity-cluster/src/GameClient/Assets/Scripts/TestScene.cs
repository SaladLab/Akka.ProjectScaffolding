using System.Collections;
using System.Net;
using System.Linq;
using Akka.Interfaced;
using Akka.Interfaced.SlimSocket;
using Akka.Interfaced.SlimSocket.Client;
using Common.Logging;
using Domain;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using System;

public class TestScene : MonoBehaviour, IUserEventObserver
{
    public Text LogText;

    private Communicator _communicator;
    private UserRef _user;
    private IUserEventObserver _userObserver;
    private TrackableUserContext _userContext;

    private void Start()
    {
        StartCoroutine(ProcessTest());
    }

    private IEnumerator ProcessTest()
    {
        LogText.text = "";

        yield return StartCoroutine(ProcessLogin(ChannelType.Tcp, "C123"));
        if (_user != null)
            yield return StartCoroutine(ProcessUserInteraction());

        if (_communicator != null)
        {
            _communicator.ObserverRegistry.Remove(_userObserver);
            _communicator.Channels.ToList().ForEach(c => c.Close());
        }

        WriteLine("End");

    }

    private IEnumerator ProcessLogin(ChannelType channelType, string credential)
    {
        WriteLine(string.Format("ProcessLogin({0}, {1})", channelType, credential));

        // create communicator

        var communicator = UnityCommunicatorFactory.Create();
        {
            var channelFactory = communicator.ChannelFactory;
            channelFactory.Type = channelType;
            channelFactory.ConnectEndPoint = new IPEndPoint(IPAddress.Loopback, 9001);
            channelFactory.CreateChannelLogger = () => LogManager.GetLogger("Channel");
            channelFactory.PacketSerializer = PacketSerializer.CreatePacketSerializer<DomainProtobufSerializer>();
        }

        var channel = communicator.CreateChannel();

        // connect to gateway

        var t0 = channel.ConnectAsync();
        yield return t0.WaitHandle;
        if (t0.Exception != null)
        {
            WriteLine("Connection Failed: " + t0.Exception);
            yield break;
        }

        // login with an user-login actor

        var userLogin = channel.CreateRef<UserLoginRef>();
        var t1 = userLogin.Login("C123");
        yield return t1.WaitHandle;
        if (t1.Exception != null)
        {
            WriteLine("Login Failed: " + t1.Exception);
            yield break;
        }

        // initiate user from user-initiator

        var userInitiator = (UserInitiatorRef)t1.Result.Item2;
        if (userInitiator.IsChannelConnected() == false)
        {
            yield return userInitiator.ConnectChannelAsync().WaitHandle;
        }

        var observer = communicator.ObserverRegistry.Create<IUserEventObserver>(this);
        var t2 = userInitiator.Load(observer);
        yield return t2.WaitHandle;
        if (t2.Exception != null)
        {
            WriteLine("Load Failed: " + t2.Exception);
            if (t2.Exception is ResultException && ((ResultException)t2.Exception).ResultCode == ResultCodeType.UserNeedToBeCreated)
            {
                var t3 = userInitiator.Create(observer, "Unity");
                yield return t3.WaitHandle;
                if (t3.Exception != null)
                {
                    WriteLine("Create Failed: " + t3.Exception);
                    yield break;
                }
                _userContext = t3.Result;
            }
            else
            {
                yield break;
            }
        }
        else
        {
            _userContext = t2.Result;
        }

        _communicator = communicator;
        _user = userInitiator.Cast<UserRef>();
        _userObserver = observer;

        var userJson = JsonConvert.SerializeObject(_userContext, s_jsonSerializerSettings);
        WriteLine(string.Format("UserLoaded: {0}]\n", userJson));
    }

    private IEnumerator ProcessUserInteraction()
    {
        WriteLine("ProcessUserInteraction()");

        // get an user actor from an user-login actor

        WriteLine("User.SetNickname(\"TestNickname\")");
        yield return _user.SetNickname("TestNickname").WaitHandle;
        WriteLine("");

        WriteLine("User.AddNote(1, \"One\")");
        yield return _user.AddNote(1, "One").WaitHandle;
        WriteLine("");

        WriteLine("User.AddNote(2, \"Two\")");
        yield return _user.AddNote(2, "Two").WaitHandle;
        WriteLine("");

        WriteLine("User.Remove(2)");
        yield return _user.RemoveNote(2).WaitHandle;
        WriteLine("");
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
