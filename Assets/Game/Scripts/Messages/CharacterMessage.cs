// Synchronized by Tools/Sync-ServerProtocol.ps1. Edit New_Server contracts first.
using Framework.Core.Serializer;
using Framework.Core.Utils;
using ProtoBuf;

namespace Game.Datas.Messages
{
    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eGetCharacterInfoReq)]
    public class ReqGetCharacterInfo : Message
    {
        [ProtoMember(1, IsRequired = true)] public int characterId;
    }

    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eGetCharacterInfoRes)]
    public class ResGetCharacterInfo : Message
    {
        [ProtoMember(1, IsRequired = true)] public int status;
        [ProtoMember(2)] public string errorMessage;
        [ProtoMember(3)] public int characterId;
        [ProtoMember(4)] public int star;
        [ProtoMember(5)] public int pieceItemId;
        [ProtoMember(6)] public long pieceQuantity;
        [ProtoMember(7)] public int nextStar;
        [ProtoMember(8)] public int nextPieceCount;
        [ProtoMember(9)] public bool isMaxStar;
    }

    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eCharacterStarUpReq)]
    public class ReqCharacterStarUp : Message
    {
        [ProtoMember(1, IsRequired = true)] public int characterId;
        [ProtoMember(2, IsRequired = true)] public int targetStar;
        [ProtoMember(3)] public string operationId;
    }

    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eCharacterStarUpRes)]
    public class ResCharacterStarUp : Message
    {
        [ProtoMember(1, IsRequired = true)] public int status;
        [ProtoMember(2)] public string errorMessage;
        [ProtoMember(3)] public int characterId;
        [ProtoMember(4)] public int requestedStar;
        [ProtoMember(5)] public int previousStar;
        [ProtoMember(6)] public int currentStar;
        [ProtoMember(7)] public bool didUpgrade;
        [ProtoMember(8)] public int consumedPieceCount;
        [ProtoMember(9)] public int pieceItemId;
        [ProtoMember(10)] public long remainingPieceQuantity;
        [ProtoMember(11)] public bool isMaxStar;
        [ProtoMember(12)] public int nextStar;
        [ProtoMember(13)] public int nextPieceCount;
        [ProtoMember(14)] public string operationId;
    }
}
