// Synchronized by Tools/Sync-ServerProtocol.ps1. Edit New_Server contracts first.
using Framework.Core.Serializer;
using Framework.Core.Utils;
using ProtoBuf;
using System.Collections.Generic;

namespace Game.Datas.Messages
{
    [ProtoContract]
    [MessageMeta((short)Module.ACTIVITY, (short)Cmd.eGetGachaPoolInfoReq)]
    public class ReqGetGachaPoolInfo : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int poolId;
    }

    [ProtoContract]
    [MessageMeta((short)Module.ACTIVITY, (short)Cmd.eGetGachaPoolInfoRes)]
    public class ResGetGachaPoolInfo : Message
    {
        [ProtoMember(1, IsRequired = true)] public int status;
        [ProtoMember(2)] public string errorMessage;
        [ProtoMember(3)] public int poolId;
        [ProtoMember(4)] public int smallPityCount;
        [ProtoMember(5)] public int smallPityMax;
        [ProtoMember(6)] public int bigPityCount;
        [ProtoMember(7)] public int bigPityMax;
        [ProtoMember(8)] public long totalDrawCount;
        [ProtoMember(9)] public int ticketItemId;
        [ProtoMember(10)] public long ticketQuantity;
        [ProtoMember(11)] public int diamondItemId;
        [ProtoMember(12)] public long diamondQuantity;
        [ProtoMember(13)] public int diamondCostPerDraw;
        [ProtoMember(14)] public List<int> ownedCharacterIds = new List<int>();
    }

    [ProtoContract]
    [MessageMeta((short)Module.ACTIVITY, (short)Cmd.eGachaDrawReq)]
    public class ReqGachaDraw : Message
    {
        [ProtoMember(1, IsRequired = true)] public int poolId;
        [ProtoMember(2, IsRequired = true)] public int drawCount;
        [ProtoMember(3)] public string operationId;
    }

    [ProtoContract]
    public class GachaResultItem
    {
        [ProtoMember(1)] public int drawIndex;
        [ProtoMember(2)] public int characterId;
        [ProtoMember(3)] public int rarity;
        [ProtoMember(4)] public bool isNewCharacter;
        [ProtoMember(5)] public int fragmentItemId;
        [ProtoMember(6)] public int fragmentNum;
    }

    [ProtoContract]
    [MessageMeta((short)Module.ACTIVITY, (short)Cmd.eGachaDrawRes)]
    public class ResGachaDraw : Message
    {
        [ProtoMember(1, IsRequired = true)] public int status;
        [ProtoMember(2)] public string errorMessage;
        [ProtoMember(3)] public long batchId;
        [ProtoMember(4)] public int usedTicketCount;
        [ProtoMember(5)] public int usedDiamondCount;
        [ProtoMember(6)] public long ticketQuantity;
        [ProtoMember(7)] public long diamondQuantity;
        [ProtoMember(8)] public int smallPityCount;
        [ProtoMember(9)] public int bigPityCount;
        [ProtoMember(10)] public long totalDrawCount;
        [ProtoMember(11)] public List<GachaResultItem> results = new List<GachaResultItem>();
        [ProtoMember(12)] public string operationId;
    }
}
