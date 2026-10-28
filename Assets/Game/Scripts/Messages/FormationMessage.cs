// Synchronized by Tools/Sync-ServerProtocol.ps1. Edit New_Server contracts first.
using Framework.Core.Serializer;
using Framework.Core.Utils;
using ProtoBuf;
using System.Collections.Generic;

namespace Game.Datas.Messages
{
    [ProtoContract]
    public sealed class FormationModuleRuleMessage
    {
        [ProtoMember(1)] public int moduleId;
        [ProtoMember(2)] public int slotCount;
        [ProtoMember(3)] public int minDeployCount;
        [ProtoMember(4)] public int maxDeployCount;
    }

    [ProtoContract]
    public sealed class FormationSlotMessage
    {
        [ProtoMember(1)] public int slotIndex;
        [ProtoMember(2)] public int characterId;
    }

    [ProtoContract]
    public sealed class PlayerFormationMessage
    {
        [ProtoMember(1)] public int moduleId;
        [ProtoMember(2)] public int formationId;
        [ProtoMember(3)] public long version;
        [ProtoMember(4)] public List<FormationSlotMessage> slots = new List<FormationSlotMessage>();
    }

    [ProtoContract]
    public sealed class OwnedCharacterBriefMessage
    {
        [ProtoMember(1)] public int characterId;
        [ProtoMember(2)] public int star;
    }

    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eGetPlayerFormationsReq)]
    public sealed class ReqGetPlayerFormations : Message { }

    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eGetPlayerFormationsRes)]
    public sealed class ResGetPlayerFormations : Message
    {
        [ProtoMember(1)] public int status;
        [ProtoMember(2)] public string errorMessage;
        [ProtoMember(3)] public List<FormationModuleRuleMessage> moduleRules = new List<FormationModuleRuleMessage>();
        [ProtoMember(4)] public List<PlayerFormationMessage> formations = new List<PlayerFormationMessage>();
        [ProtoMember(5)] public List<OwnedCharacterBriefMessage> characters = new List<OwnedCharacterBriefMessage>();
    }

    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eSavePlayerFormationReq)]
    public sealed class ReqSavePlayerFormation : Message
    {
        [ProtoMember(1)] public int moduleId;
        [ProtoMember(2)] public int formationId;
        [ProtoMember(3)] public long expectedVersion;
        [ProtoMember(4)] public List<FormationSlotMessage> slots = new List<FormationSlotMessage>();
    }

    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eSavePlayerFormationRes)]
    public sealed class ResSavePlayerFormation : Message
    {
        [ProtoMember(1)] public int status;
        [ProtoMember(2)] public string errorMessage;
        [ProtoMember(3)] public PlayerFormationMessage formation;
    }
}
