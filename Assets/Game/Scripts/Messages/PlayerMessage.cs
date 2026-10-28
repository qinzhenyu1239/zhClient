// Synchronized by Tools/Sync-ServerProtocol.ps1. Edit New_Server contracts first.
using Framework.Core.Serializer;
using Framework.Core.Utils;
using ProtoBuf;
using System.Collections.Generic;

namespace Game.Datas.Messages {
    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eEnterLogicServerReq)]
    public class ReqEnterLogicServer : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int typeId; // 请求是那种游戏逻辑实例


        [ProtoMember(2, IsRequired = true)]
        public int zoneId; // 请求是哪个分区的游戏逻辑实例;

        [ProtoMember(3, IsRequired = true)]
        public int instId; // 请求是哪个游戏逻辑服的实例Id, -1,服务端自动分配;

        [ProtoMember(4)]
        public long openId = 0; // 玩家登录时的OpenId,如果有的化;

    }

    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eExchangeProductReq)]
    public class ReqExchangeProduct : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int productId; // 产品id;
    }

    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.ePullingBonuesListReq)]
    public class ReqPullingBonuesList : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int typeId; // 奖励的主类型

    }

    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.ePullingMailMsgReq)]
    public class ReqPullingMailMsg : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int typeId; // 拉取的任务类型，-1所有任务，默认-1;
    }

    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.ePullingPackDataReq)]
    public class ReqPullingPackData : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int typeId; // 拉取的背包数据的类型, -1标识拉取所有的背包数据;

        /*
        [ProtoMember(1, IsRequired = true)]
        public int startIndex; // 开始的索引 

        [ProtoMember(2, IsRequired = true)]
        public int num; // -1 表示拉取全部
        */

    }

    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.ePullingPlayerDataReq)]
    public class ReqPullingPlayerData : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int job;// 玩家的职业，具体的根据游戏需求来制定; -1

        // ...
        [ProtoMember(2)]
        public long openId; // 玩家的openId;
    }

    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.ePullingRankReq)]
    public class ReqPullingRank : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int typeId; // 拉取的排行榜类型;
    }

    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.ePullingTaskListReq)]
    public class ReqPullingTaskList : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int typeId; // 拉取的任务类型，-1所有任务，默认-1;
    }

    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eRecvBonuesReq)]
    public class ReqRecvBonues : Message
    {
        [ProtoMember(1)]
        public long bonuesId;
    }


    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eRecvLoginBonuesReq)]
    public class ReqRecvLoginBonues : Message
    {
        [ProtoMember(1)]
        public int type;
    }

    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eSelectPlayerReq)]
    public class ReqSelectPlayer : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int job;// 玩家的职业，具体的根据游戏需求来制定; -1

        [ProtoMember(2)]
        public string uname; // 玩家在游戏中的名字

        [ProtoMember(3)]
        public int usex; // 玩家在游戏中的性别;

        [ProtoMember(4)]
        public int charactorId; // 玩家的角色Id;

        [ProtoMember(5)]
        public long openId; // 玩家openId,可选;
        // ... 注意:我们这里是通用模板，如果游戏设定中没有，可以自行修改改;
    }

    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eUpdateMailMsgReq)]
    public class ReqUpdateMailMsg : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public long mailMsgId; // 邮件ID;

        [ProtoMember(2, IsRequired = true)]
        public int status; // 状态
    }

    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eEnterLogicServerRes)]
    public class ResEnterLogicServer : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int status; // 返回状态码;

    }

    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eExchangeProductRes)]
    public class ResExchangeProduct : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int status;
    }

    [ProtoContract]
    public class BonuesItem
    {
        [ProtoMember(1, IsRequired = true)]
        public long bonuesId;

        [ProtoMember(2, IsRequired = true)]
        public string bonuesDesic;

        [ProtoMember(3, IsRequired = true)]
        public int status;

        [ProtoMember(4)]
        public int typeId;

        // 其它的，你可以自己再添加，比如需要开始时间与结束时间
    }


    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.ePullingBonuesListRes)]
    public class ResPullingBonuesList : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int status;


        [ProtoMember(2)]
        public BonuesItem[] bonues = null;
    }

    [ProtoContract]
    public class MailMsgItem
    {
        [ProtoMember(1, IsRequired = true)]
        public int status;

        [ProtoMember(2, IsRequired = true)]
        public string msgBody;

        [ProtoMember(3)]
        public int sendTime;

        [ProtoMember(4)]
        public long msgId;

        // 其它的，你可以自己再添加，比如需要开始时间与结束时间
    }


    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.ePullingMailMsgRes)]
    public class ResPullingMailMsg : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int status;


        [ProtoMember(2)]
        public List<MailMsgItem> mailMessages = new List<MailMsgItem>();
    }

    [ProtoContract]
    public class GoodsItem
    {
        [ProtoMember(1, IsRequired = true)]
        public int typeId;

        [ProtoMember(2, IsRequired = true)]
        public int num;

        [ProtoMember(3)]
        public byte[] strengData = null;

    }

    [ProtoContract]
    public class DicGoodsItem
    {
        [ProtoMember(1)]
        public int mainType;

        [ProtoMember(2)]
        // public List<GoodsItem> Value;
        public GoodsItem[] Value;
    }

    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.ePullingPackDataRes)]
    public class ResPullingPackData : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int status;


        [ProtoMember(2)]
        // public List<DicGoodsItem> packGoods = null;
        public DicGoodsItem[] packGoods = null;
    }

    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.ePullingPlayerDataRes)]
    public class ResPullingPlayerData : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int status = 0;


        // ... PlayerInfo后面在定义;
        [ProtoMember(2)]
        public PlayerInfo pInfo = null;

        [ProtoMember(3)]
        public int isReConnectGame;// 玩家的职业，具体的根据游戏需求来制定; -1

        [ProtoMember(4)]
        public long playerId; // 网关模式需要playerId
    }

    [ProtoContract]
    public class RankItem
    {
        [ProtoMember(1, IsRequired = true)]
        public string unick;

        [ProtoMember(2, IsRequired = true)]
        public int value;


        [ProtoMember(3)]
        public int uface;

        // 其它的，你可以自己再添加，比如需要开始时间与结束时间
    }


    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.ePullingRankRes)]
    public class ResPullingRank : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int status;

        [ProtoMember(2, IsRequired = true)]
        public int selfIndex; // 自己的排名，-1为未上榜

        [ProtoMember(3)]
        public RankItem[] ranks = null;


    }

    [ProtoContract]
    public class TaskItem
    {
        [ProtoMember(1, IsRequired = true)]
        public int status;

        [ProtoMember(2, IsRequired = true)]
        public string taskDesic;

        [ProtoMember(3)]
        public int typeId;

        // 其它的，你可以自己再添加，比如需要开始时间与结束时间
    }


    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.ePullingTaskListRes)]
    public class ResPullingTaskList : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int status;


        [ProtoMember(2)]
        public TaskItem[] tasks = null;
    }

    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eRecvBonuesRes)]
    public class ResRecvBonues : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int status;

        [ProtoMember(2)]
        public int typeId;


        [ProtoMember(3)]
        public int b1;

        [ProtoMember(4)]
        public int b2;

        [ProtoMember(5)]
        public int b3;

        [ProtoMember(6)]
        public int b4;

        [ProtoMember(7)]
        public int b5;

    }

    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eRecvLoginBonuesRes)]
    public class ResRecvLoginBonues : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int status;


        [ProtoMember(2, IsRequired = true)]
        public int num;

    }

    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eSelectPlayerRes)]
    public class ResSelectPlayer : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int status = 0;


        // ... PlayerInfo后面在定义;
        [ProtoMember(2)]
        public PlayerInfo pInfo = null;

        [ProtoMember(3, IsRequired = true)]
        public int isReConnectGame;// 玩家的职业，具体的根据游戏需求来制定; -1

        [ProtoMember(4)]
        public long playerId;
    }

    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eUpdategMailMsgRes)]
    public class ResUpdateMailMsg : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int status;
    }


}