// Synchronized by Tools/Sync-ServerProtocol.ps1. Edit New_Server contracts first.
using Framework.Core.Serializer;
using Framework.Core.Utils;
using ProtoBuf;
using System.Collections.Generic;

namespace Game.Datas.Messages
{
    [ProtoContract]
    public sealed class RedDotNodeMessage
    {
        [ProtoMember(1)] public int id;
        [ProtoMember(2)] public string name;
        [ProtoMember(3)] public int parentId;
        [ProtoMember(4)] public int clearType;
        [ProtoMember(5)] public bool enabled;
        [ProtoMember(6)] public long tipVersion;
    }
    [ProtoContract]
    public sealed class RedDotStateMessage
    {
        [ProtoMember(1)] public int nodeId;
        [ProtoMember(2)] public bool isActive;
    }
    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eRedDotInitReq)]
    public sealed class ReqRedDotInit : Message { }
    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eRedDotInitRes)]
    public sealed class ResRedDotInit : Message
    {
        [ProtoMember(1)] public int status;
        [ProtoMember(2)] public string configVersion;
        [ProtoMember(3)] public long stateVersion;
        [ProtoMember(4)] public List<RedDotNodeMessage> nodes = new();
        [ProtoMember(5)] public List<RedDotStateMessage> states = new();
    }
    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eRedDotViewReq)]
    public sealed class ReqRedDotView : Message
    {
        [ProtoMember(1)] public int nodeId;
        [ProtoMember(2)] public long tipVersion;
    }
    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eRedDotViewRes)]
    public sealed class ResRedDotView : Message
    {
        [ProtoMember(1)] public int status;
        [ProtoMember(2)] public string configVersion;
        [ProtoMember(3)] public long stateVersion;
        [ProtoMember(4)] public List<RedDotStateMessage> states = new();
    }
    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eRedDotChanged)]
    public sealed class ResRedDotChanged : Message
    {
        [ProtoMember(1)] public string configVersion;
        [ProtoMember(2)] public long stateVersion;
        [ProtoMember(3)] public List<RedDotStateMessage> states = new();
    }
}
