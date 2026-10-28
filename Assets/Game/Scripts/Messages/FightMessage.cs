// Synchronized by Tools/Sync-ServerProtocol.ps1. Edit New_Server contracts first.
using Framework.Core.Serializer;
using Framework.Core.Utils;
using ProtoBuf;

namespace Game.Datas.Messages {
    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.eStartSkillReq)]
    public class ReqStartSkill : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int skillId; // 玩家请求放的技能Id
    }

    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.eStartSkillRes)]
    public class ResStartSkill : Message
    {
        
        [ProtoMember(1)]
        public int seatOrWorldId; // 玩家在世界中对应的Id号;

        [ProtoMember(2)]
        public int skillId; // 玩家请求放的技能Id号;
    }

    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.eStartBuffReq)]
    public class ReqStartBuff : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int buffId; // 玩家请求放的技能Id
    }

    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.eStartBuffRes)]
    public class ResStartBuff : Message
    {

        [ProtoMember(1)]
        public int seatOrWorldId; // 玩家在世界中对应的Id号;

        [ProtoMember(2)]
        public int buffId; // 玩家请求放的技能Id号;


    }

    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.eLostHpRes)]
    public class ResLostHp : Message
    {

        [ProtoMember(1)]
        public int seatOrWorldId; // 玩家在世界中对应的Id号;

        [ProtoMember(2)]
        public int lostHp; // 玩家掉了多少血，如果有其它，再加其它数据;


    }
}

