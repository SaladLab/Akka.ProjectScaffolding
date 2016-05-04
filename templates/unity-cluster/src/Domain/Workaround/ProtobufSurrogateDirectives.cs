using ProtoBuf;
using TrackableData.Protobuf;

namespace Domain.Workaround
{
    [ProtoContract]
    public class ProtobufSurrogateDirectives
    {
        public TrackableDictionaryTrackerSurrogate<int, string> T1;
    }
}
