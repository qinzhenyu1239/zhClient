// Synchronized by Tools/Sync-ServerProtocol.ps1. Edit New_Server contracts first.
using Framework.Core.Serializer;
using Framework.Core.Utils;
using ProtoBuf;

namespace Game.Datas.Messages {
    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.eExitLogicServerReq)]
    public class ReqExitLogicServer : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int quitReason; // 离开游戏的原因
    }

    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.eExitLogicServerRes)]
    public class ResExitLogicServer : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int status; // 返回玩家的状态
    }

    [ProtoContract]
    [MessageMeta((short)Module.SCENE, (short)Cmd.eReconnRoomRes)]
    public class ResReconnRoom : Message
    {
        [ProtoMember(1)]
        public ResEnterRoom room;

        [ProtoMember(2)]
        public ResSitdown selfSitdown;

        // 房间中的每个正在游戏玩家中基本信息;
        [ProtoMember(3)]
        public ResUserArrivedSeat[] seats;


    }

}
