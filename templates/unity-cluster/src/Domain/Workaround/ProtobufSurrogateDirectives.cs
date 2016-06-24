using ProtoBuf;
using TrackableData.Protobuf;

namespace Domain
{
    [ProtoContract]
    public class ProtobufSurrogateDirectives
    {
        public TrackableDictionaryTrackerSurrogate<int, string> T1;
    }
}
