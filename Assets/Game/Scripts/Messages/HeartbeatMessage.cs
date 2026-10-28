// Synchronized by Tools/Sync-ServerProtocol.ps1. Edit New_Server contracts first.
using Framework.Core.Serializer;
using Framework.Core.Utils;
using ProtoBuf;

namespace Game.Datas.Messages
{
    [ProtoContract]
    [MessageMeta((short)Module.AUTH, (short)Cmd.eHeartbeatReq)]
    public sealed class ReqHeartbeat : Message
    {
        [ProtoMember(1)] public long clientUnixMilliseconds;
    }

    [ProtoContract]
    [MessageMeta((short)Module.AUTH, (short)Cmd.eHeartbeatRes)]
    public sealed class ResHeartbeat : Message
    {
        [ProtoMember(1, IsRequired = true)] public int status;
        [ProtoMember(2)] public long clientUnixMilliseconds;
        [ProtoMember(3)] public long serverUnixMilliseconds;
    }
}
