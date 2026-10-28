// Synchronized by Tools/Sync-ServerProtocol.ps1. Edit New_Server contracts first.
using Framework.Core.Serializer;
using Framework.Core.Utils;
using ProtoBuf;

namespace Game.Datas.Messages {
    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.eSitdownReq)]
    public class ReqSitdown : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int seatId; // 玩家座位号
    }

    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.eSitdownRes)]
    public class ResSitdown : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int status; // 

        [ProtoMember(2)]
        public int seatId; // 

    }

    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.eStandupReq)]
    public class ReqStandup : Message
    {
        [ProtoMember(1)]
        public int reserve; // 保留
    }

    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.eStandupRes)]
    public class ResStandup : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int status; // 
    }

    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.eTalkMsgReq)]
    public class ReqTalkMsg : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int talkType; // 默认为0，普通文字聊天, 1 语音聊天

        [ProtoMember(2, IsRequired = true)]
        public string msgBodyOrAudioUrl; // 默认为0，普通文字聊天, 1 语音聊天语音文件的url
    }

    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.eTalkMsgRes)]
    public class ResTalkMsg : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int roomInViewId; // 房间里面的谁说的，我们旁观列表;

        [ProtoMember(2, IsRequired = true)]
        public int talkType; // 默认为0，普通文字聊天, 1 语音聊天

        [ProtoMember(3, IsRequired = true)]
        public string msgBodyOrAudioUrl; // 默认为0，普通文字聊天, 1 语音文件的url
    }

    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.ePlayerIsReadyReq)]
    public class ReqPlayerIsReady : Message
    {
        [ProtoMember(1)]
        public int reserve; // 保留
    }

    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.ePlayerIsReadyRes)]
    public class ResPlayerIsReady : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int status;

        [ProtoMember(2, IsRequired = true)]
        public int seatId;


    }

    // 服务器主动通知
    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.eEnterRoomRes)]
    public class ResEnterRoom : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int roomId; //  zid, stype; 

        // 房间的状态
        [ProtoMember(2, IsRequired = true)]
        public int roomState;
        // end

        // 在房间旁观列表的id
        [ProtoMember(3, IsRequired = true)]
        public int roomInViewId;
        // end
    }

    // 服务器主动通知
    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.eUserArrivedSeatRes)]
    public class ResUserArrivedSeat : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public string unick; 

        [ProtoMember(2, IsRequired = true)]
        public int usex;

        [ProtoMember(3, IsRequired = true)]
        public int uface;

        [ProtoMember(4, IsRequired = true)]
        public int seatId;

        [ProtoMember(5, IsRequired = true)]
        public int playInRoomState;

        // ...
    }

    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.eUserExitSeatRes)]
    public class ResUserExitSeat : Message {
        [ProtoMember(1, IsRequired = true)]
        public int seatId;
    }

    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.eRoundReadyRes)]
    public class ResRoundReady : Message
    {
        [ProtoMember(1)]
        public int roundTime;  // 本局游戏的时长

        // ...
    }

    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.eRoundStartedRes)]
    public class ResRoundStarted : Message
    {
        [ProtoMember(1)]
        public int reserve; // 保留
        // ...
    }

    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.eRoundCheckOutRes)]
    public class ResRoundCheckOut : Message
    {
        [ProtoMember(1)]
        public int reserve; // 保留
        // ...
    }

    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.eRoundClearRes)]
    public class ResRoundClear : Message
    {
        [ProtoMember(1)]
        public int reserve; // 保留
        // ...
    }

    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.ePlayerEscapeRes)]
    public class ResPlayerEscape : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int seatId; // 哪个座位上玩家逃跑;
        // ...
    }

    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.ePlayerOptReq)]
    public class ReqPlayerOpt : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int optType;

        [ProtoMember(2)]
        public int v1;

        [ProtoMember(3)]
        public int v2;

        [ProtoMember(4)]
        public int v3;
        // ...
    }

    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.ePlayerOptRes)]
    public class ResPlayerOpt : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int status; // 保留


        [ProtoMember(2)]
        public int seatId;

        [ProtoMember(3)]
        public int optType;

        [ProtoMember(4)]
        public int v1;

        [ProtoMember(5)]
        public int v2;

        [ProtoMember(6)]
        public int v3;
    }

    [ProtoContract]
    public class FrameOpt {
        [ProtoMember(1, IsRequired = true)]
        public int seatId;

        [ProtoMember(2)]
        public int optType;

        [ProtoMember(3)]
        public int v1;

        [ProtoMember(4)]
        public int v2;
    }

    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.eFrameSyncDataRes)]
    public class ResFrameSyncData : Message {
        [ProtoMember(1, IsRequired = true)]
        public int frameId; // 每一帧的ID号

        [ProtoMember(2)]
        public FrameOpt[] playersOptSet;
    }
}

