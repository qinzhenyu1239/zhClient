// Synchronized by Tools/Sync-ServerProtocol.ps1. Edit New_Server contracts first.
using Framework.Core.Serializer;
using Framework.Core.Utils;
using ProtoBuf;

namespace Game.Datas.Messages
{
    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.ePlayerSpawnReq)]
    public class ReqPlayerSpawn : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int SpawnPoint; // 玩家在第一个出生点，-1，由服务器决定;
    }



    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.ePlayerSpawnRes)]
    public class ResPlayerSpawn : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int status;


        [ProtoMember(2, IsRequired = true)]
        public int worldId; // 玩家在当前游戏世界中的Id;
    }

    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.eNavToDstReq)]
    public class ReqNavToDst : Message
    {
        [ProtoMember(1)]
        public float x;

        [ProtoMember(2)]
        public float y;

        [ProtoMember(3)]
        public float z;
    }



    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.eNavToDstRes)]
    public class ResNavToDst : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int worldId; // 表示你是哪个玩家

        [ProtoMember(2)]
        public float x;

        [ProtoMember(3)]
        public float y;

        [ProtoMember(4)]
        public float z;

        [ProtoMember(5)]
        public float speed;


    }

    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.eNavInDirReq)]
    public class ReqNavInDir : Message
    {
        [ProtoMember(1)]
        public int dirx;

        [ProtoMember(2)]
        public int diry;
    }

    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.eNavInDirRes)]
    public class ResNavInDir : Message
    {
        
        [ProtoMember(1, IsRequired = true)]
        public int worldId; // 表示你是哪个玩家

        [ProtoMember(2)]
        public int dirx;

        [ProtoMember(3)]
        public int diry;

        [ProtoMember(4)]
        public float speed;

    }

    [ProtoContract]
    public class CharactorInfo {
        [ProtoMember(1)]
        public string unick;

        [ProtoMember(2)]
        public int job;

        [ProtoMember(3)]
        public int sex;

        [ProtoMember(4)]
        public int charactorId; // -1, 200001, 20002,
    }

    [ProtoContract]
    public class CharactorTransform {
        [ProtoMember(1)]
        public float[] pos; // public float[] pos = new float[3];不能直接new, 解码的时候解码C#解码不到

        [ProtoMember(2)]
        public float[] eulerAngles; //  public float[] eulerAngles = new float[3];
    }

    [ProtoContract]
    public class SyncStatusData
    {
        [ProtoMember(1)]
        public int status; 

        // 其它数据，如果你需要，加入即可...
    }

    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.eSyncCharactorRes)]
    public class ResSyncCharactorStatus : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int worldId; // 玩家在世界里面的Id;

        [ProtoMember(2)]
        public CharactorTransform transform; // 玩家在世界的位置;

        [ProtoMember(3)]
        public SyncStatusData statusData; // 玩家要同步过去的状态数据与关键信息;
    }

    [ProtoContract]
    public class ArrivedCharactor {
        [ProtoMember(1, IsRequired = true)]
        public int worldId; // 玩家在世界里面的Id;

        [ProtoMember(2)]
        public CharactorInfo chInfo; // 玩家在世界里面的Id;

        [ProtoMember(3)]
        public CharactorTransform transform; // 玩家在世界的位置;

        [ProtoMember(4)]
        public int status; //玩家的状态; 
        // 战斗数据，如: 血，蓝,玩家的状态，攻击，行走, 后续再加...
    }

    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.eEnterAOIRes)]
    public class ResEnterAOI : Message
    {
        [ProtoMember(1)]
        public ArrivedCharactor[] charactors; // 开放世界可能有很多玩家一起过来了，用数组;
    }

    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.eLeaveAOIRes)]
    public class ResLeaveAOI : Message
    {
        [ProtoMember(1)]
        public int[] leavePlayers; // 开放世界可能有很多玩家一起离开了，用数组;
    }



}

