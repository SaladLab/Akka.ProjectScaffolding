﻿// ------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by Akka.Interfaced CodeGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
// ------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Akka.Interfaced;
using Akka.Actor;
using ProtoBuf;
using TypeAlias;
using System.ComponentModel;

#region Domain.Interface.IUser

namespace Domain.Interface
{
    [PayloadTableForInterfacedActor(typeof(IUser))]
    public static class IUser_PayloadTable
    {
        public static Type[,] GetPayloadTypes()
        {
            return new Type[,] {
                { typeof(AddNote_Invoke), null },
                { typeof(RemoveNote_Invoke), null },
                { typeof(SetNickname_Invoke), null },
            };
        }

        [ProtoContract, TypeAlias]
        public class AddNote_Invoke
            : IInterfacedPayload, IAsyncInvokable
        {
            [ProtoMember(1)] public System.Int32 id;
            [ProtoMember(2)] public System.String note;
            public Type GetInterfaceType() { return typeof(IUser); }
            public async Task<IValueGetable> InvokeAsync(object __target)
            {
                await ((IUser)__target).AddNote(id, note);
                return null;
            }
        }

        [ProtoContract, TypeAlias]
        public class RemoveNote_Invoke
            : IInterfacedPayload, IAsyncInvokable
        {
            [ProtoMember(1)] public System.Int32 id;
            public Type GetInterfaceType() { return typeof(IUser); }
            public async Task<IValueGetable> InvokeAsync(object __target)
            {
                await ((IUser)__target).RemoveNote(id);
                return null;
            }
        }

        [ProtoContract, TypeAlias]
        public class SetNickname_Invoke
            : IInterfacedPayload, IAsyncInvokable
        {
            [ProtoMember(1)] public System.String nickname;
            public Type GetInterfaceType() { return typeof(IUser); }
            public async Task<IValueGetable> InvokeAsync(object __target)
            {
                await ((IUser)__target).SetNickname(nickname);
                return null;
            }
        }
    }

    public interface IUser_NoReply
    {
        void AddNote(System.Int32 id, System.String note);
        void RemoveNote(System.Int32 id);
        void SetNickname(System.String nickname);
    }

    [ProtoContract, TypeAlias]
    public class UserRef : InterfacedActorRef, IUser, IUser_NoReply
    {
        [ProtoMember(1)] private ActorRefBase _actor
        {
            get { return (ActorRefBase)Actor; }
            set { Actor = value; }
        }

        private UserRef() : base(null)
        {
        }

        public UserRef(IActorRef actor) : base(actor)
        {
        }

        public UserRef(IActorRef actor, IRequestWaiter requestWaiter, TimeSpan? timeout) : base(actor, requestWaiter, timeout)
        {
        }

        public IUser_NoReply WithNoReply()
        {
            return this;
        }

        public UserRef WithRequestWaiter(IRequestWaiter requestWaiter)
        {
            return new UserRef(Actor, requestWaiter, Timeout);
        }

        public UserRef WithTimeout(TimeSpan? timeout)
        {
            return new UserRef(Actor, RequestWaiter, timeout);
        }

        public Task AddNote(System.Int32 id, System.String note)
        {
            var requestMessage = new RequestMessage {
                InvokePayload = new IUser_PayloadTable.AddNote_Invoke { id = id, note = note }
            };
            return SendRequestAndWait(requestMessage);
        }

        public Task RemoveNote(System.Int32 id)
        {
            var requestMessage = new RequestMessage {
                InvokePayload = new IUser_PayloadTable.RemoveNote_Invoke { id = id }
            };
            return SendRequestAndWait(requestMessage);
        }

        public Task SetNickname(System.String nickname)
        {
            var requestMessage = new RequestMessage {
                InvokePayload = new IUser_PayloadTable.SetNickname_Invoke { nickname = nickname }
            };
            return SendRequestAndWait(requestMessage);
        }

        void IUser_NoReply.AddNote(System.Int32 id, System.String note)
        {
            var requestMessage = new RequestMessage {
                InvokePayload = new IUser_PayloadTable.AddNote_Invoke { id = id, note = note }
            };
            SendRequest(requestMessage);
        }

        void IUser_NoReply.RemoveNote(System.Int32 id)
        {
            var requestMessage = new RequestMessage {
                InvokePayload = new IUser_PayloadTable.RemoveNote_Invoke { id = id }
            };
            SendRequest(requestMessage);
        }

        void IUser_NoReply.SetNickname(System.String nickname)
        {
            var requestMessage = new RequestMessage {
                InvokePayload = new IUser_PayloadTable.SetNickname_Invoke { nickname = nickname }
            };
            SendRequest(requestMessage);
        }
    }
}

#endregion
#region Domain.Interface.IUserLogin

namespace Domain.Interface
{
    [PayloadTableForInterfacedActor(typeof(IUserLogin))]
    public static class IUserLogin_PayloadTable
    {
        public static Type[,] GetPayloadTypes()
        {
            return new Type[,] {
                { typeof(Login_Invoke), typeof(Login_Return) },
            };
        }

        [ProtoContract, TypeAlias]
        public class Login_Invoke
            : IInterfacedPayload, IAsyncInvokable
        {
            [ProtoMember(1)] public System.Int32 observerId;
            public Type GetInterfaceType() { return typeof(IUserLogin); }
            public async Task<IValueGetable> InvokeAsync(object __target)
            {
                var __v = await ((IUserLogin)__target).Login(observerId);
                return (IValueGetable)(new Login_Return { v = __v });
            }
        }

        [ProtoContract, TypeAlias]
        public class Login_Return
            : IInterfacedPayload, IValueGetable
        {
            [ProtoMember(1)] public Domain.Interface.LoginResult v;
            public Type GetInterfaceType() { return typeof(IUserLogin); }
            public object Value { get { return v; } }
        }
    }

    public interface IUserLogin_NoReply
    {
        void Login(System.Int32 observerId);
    }

    [ProtoContract, TypeAlias]
    public class UserLoginRef : InterfacedActorRef, IUserLogin, IUserLogin_NoReply
    {
        [ProtoMember(1)] private ActorRefBase _actor
        {
            get { return (ActorRefBase)Actor; }
            set { Actor = value; }
        }

        private UserLoginRef() : base(null)
        {
        }

        public UserLoginRef(IActorRef actor) : base(actor)
        {
        }

        public UserLoginRef(IActorRef actor, IRequestWaiter requestWaiter, TimeSpan? timeout) : base(actor, requestWaiter, timeout)
        {
        }

        public IUserLogin_NoReply WithNoReply()
        {
            return this;
        }

        public UserLoginRef WithRequestWaiter(IRequestWaiter requestWaiter)
        {
            return new UserLoginRef(Actor, requestWaiter, Timeout);
        }

        public UserLoginRef WithTimeout(TimeSpan? timeout)
        {
            return new UserLoginRef(Actor, RequestWaiter, timeout);
        }

        public Task<Domain.Interface.LoginResult> Login(System.Int32 observerId)
        {
            var requestMessage = new RequestMessage {
                InvokePayload = new IUserLogin_PayloadTable.Login_Invoke { observerId = observerId }
            };
            return SendRequestAndReceive<Domain.Interface.LoginResult>(requestMessage);
        }

        void IUserLogin_NoReply.Login(System.Int32 observerId)
        {
            var requestMessage = new RequestMessage {
                InvokePayload = new IUserLogin_PayloadTable.Login_Invoke { observerId = observerId }
            };
            SendRequest(requestMessage);
        }
    }
}

#endregion
#region Domain.Interface.IUserEventObserver

namespace Domain.Interface
{
    public static class IUserEventObserver_PayloadTable
    {
        [ProtoContract, TypeAlias]
        public class UserContextChange_Invoke : IInvokable
        {
            [ProtoMember(1)] public Domain.Data.TrackableUserContextTracker userContextTracker;
            public void Invoke(object __target)
            {
                ((IUserEventObserver)__target).UserContextChange(userContextTracker);
            }
        }
    }

    [ProtoContract, TypeAlias]
    public class UserEventObserver : InterfacedObserver, IUserEventObserver
    {
        [ProtoMember(1)] private ActorRefBase _actor
        {
            get { return Channel != null ? (ActorRefBase)(((ActorNotificationChannel)Channel).Actor) : null; }
            set { Channel = new ActorNotificationChannel(value); }
        }

        [ProtoMember(2)] private int _observerId
        {
            get { return ObserverId; }
            set { ObserverId = value; }
        }

        private UserEventObserver() : base(null, 0)
        {
        }

        public UserEventObserver(IActorRef target, int observerId)
            : base(new ActorNotificationChannel(target), observerId)
        {
        }

        public UserEventObserver(INotificationChannel channel, int observerId)
            : base(channel, observerId)
        {
        }

        public void UserContextChange(Domain.Data.TrackableUserContextTracker userContextTracker)
        {
            var payload = new IUserEventObserver_PayloadTable.UserContextChange_Invoke { userContextTracker = userContextTracker };
            Notify(payload);
        }
    }
}

#endregion