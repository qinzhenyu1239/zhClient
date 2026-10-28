// Synchronized by Tools/Sync-ServerProtocol.ps1. Edit New_Server contracts first.
using Framework.Core.Serializer;
using Framework.Core.Utils;
using ProtoBuf;

namespace Game.Datas.Messages
{
    public enum PlayerProfileField { Avatar = 1, AvatarFrame = 2, Title = 3, Background = 4, Achievements = 5, Characters = 6, Signature = 7 }

    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eGetPlayerBasicInfoReq)]
    public sealed class ReqGetPlayerBasicInfo : Message { }

    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eGetPlayerBasicInfoRes)]
    public sealed class ResGetPlayerBasicInfo : Message
    {
        [ProtoMember(1)] public int status;
        [ProtoMember(2)] public string errorMessage;
        [ProtoMember(3)] public long playerId;
        [ProtoMember(4)] public int avatarId;
        [ProtoMember(5)] public string name;
        [ProtoMember(6)] public long guildId;
        [ProtoMember(7)] public long createTime;
        [ProtoMember(8)] public int titleId;
        [ProtoMember(9)] public string signature;
        [ProtoMember(10)] public int avatarFrameId;
        [ProtoMember(11)] public int backgroundId;
        [ProtoMember(12)] public string displayAchievementIds;
        [ProtoMember(13)] public string displayCharacterIds;
    }

    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eUpdatePlayerBasicInfoReq)]
    public sealed class ReqUpdatePlayerBasicInfo : Message
    {
        [ProtoMember(1)] public PlayerProfileField[] updateFields;
        [ProtoMember(2)] public int? avatarId;
        [ProtoMember(3)] public int? avatarFrameId;
        [ProtoMember(4)] public int? titleId;
        [ProtoMember(5)] public int? backgroundId;
        [ProtoMember(6)] public string displayAchievementIds;
        [ProtoMember(7)] public string displayCharacterIds;
        [ProtoMember(8)] public string signature;
    }

    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eUpdatePlayerBasicInfoRes)]
    public sealed class ResUpdatePlayerBasicInfo : Message
    {
        [ProtoMember(1)] public int status;
        [ProtoMember(2)] public string errorMessage;
        [ProtoMember(3)] public long playerId;
        [ProtoMember(4)] public int avatarId;
        [ProtoMember(5)] public string name;
        [ProtoMember(6)] public long guildId;
        [ProtoMember(7)] public long createTime;
        [ProtoMember(8)] public int titleId;
        [ProtoMember(9)] public string signature;
        [ProtoMember(10)] public int avatarFrameId;
        [ProtoMember(11)] public int backgroundId;
        [ProtoMember(12)] public string displayAchievementIds;
        [ProtoMember(13)] public string displayCharacterIds;
    }
}
