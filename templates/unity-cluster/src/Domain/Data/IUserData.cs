using System;
using ProtoBuf;
using TrackableData;

namespace Domain
{
    [ProtoContract]
    public interface IUserData : ITrackablePoco<IUserData>
    {
        [ProtoMember(1)] string Nickname { get; set; }
        [ProtoMember(2)] DateTime RegisterTime { get; set; }
    }
}
