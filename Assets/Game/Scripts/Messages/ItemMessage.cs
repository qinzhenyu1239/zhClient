// Synchronized by Tools/Sync-ServerProtocol.ps1. Edit New_Server contracts first.
using Framework.Core.Serializer;
using Framework.Core.Utils;
using ProtoBuf;
using System.Collections.Generic;

namespace Game.Datas.Messages
{
    [ProtoContract]
    public sealed class PlayerItemMessage
    {
        [ProtoMember(1)] public int itemId;
        [ProtoMember(2)] public long quantity;
    }

    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eGetPlayerItemsReq)]
    public sealed class ReqGetPlayerItems : Message { }

    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eGetPlayerItemsRes)]
    public sealed class ResGetPlayerItems : Message
    {
        [ProtoMember(1)] public int status;
        [ProtoMember(2)] public string errorMessage;
        [ProtoMember(3)] public List<PlayerItemMessage> items = new List<PlayerItemMessage>();
    }
}
