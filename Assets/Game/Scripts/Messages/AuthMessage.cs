// Synchronized by Tools/Sync-ServerProtocol.ps1. Edit New_Server contracts first.
using Framework.Core.Serializer;
using Framework.Core.Utils;
using ProtoBuf;

namespace Game.Datas.Messages
{
    [ProtoContract]
    public class AccountInfo
    {
        [ProtoMember(1, IsRequired = true)]
        public string unick;

        [ProtoMember(2, IsRequired = true)]
        public int uface;

        [ProtoMember(3, IsRequired = true)]
        public int uvip;

        [ProtoMember(4, IsRequired = true)]
        public int isGuest;

    }

    [ProtoContract]
    public class PlayerInfo
    {
        [ProtoMember(1, IsRequired = true)]
        public string unick;

        [ProtoMember(2, IsRequired = true)]
        public int hp;

        [ProtoMember(3, IsRequired = true)]
        public int exp;

        [ProtoMember(4, IsRequired = true)]
        public int mp;

        [ProtoMember(5, IsRequired = true)]
        public int umoney;

        [ProtoMember(6, IsRequired = true)]
        public int ucion;

        [ProtoMember(7, IsRequired = true)]
        public int usex;


        [ProtoMember(8, IsRequired = true)]
        public int hasBonues = 0;

        [ProtoMember(9, IsRequired = true)]
        public int days = 0;

        [ProtoMember(10, IsRequired = true)]
        public int loginBonues = 0;
        // ... 根据自己的游戏来加;
        // end...

    }

    [ProtoContract]
    [MessageMeta((short)Module.AUTH, (short)Cmd.eGuestLoginReq)]
    public class ReqGuestLogin : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public string guestKey;

        [ProtoMember(2, IsRequired = true)]
        public int channal;
    }

    [ProtoContract]
    [MessageMeta((short)Module.AUTH, (short)Cmd.eGuestUpgradeReq)]
    public class ReqGuestUpgrade : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public string uname;

        [ProtoMember(2, IsRequired = true)]
        public string upwd;

        [ProtoMember(3, IsRequired = true)]
        public string unick;

        [ProtoMember(4)]
        public string accessToken;
    }


    [ProtoContract]
    [MessageMeta((short)Module.AUTH, (short)Cmd.eRegisterUserReq)]
    public class ReqRegisterUser : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public string uname;

        [ProtoMember(2, IsRequired = true)]
        public string upwd;

        [ProtoMember(3, IsRequired = true)]
        public int channal;
    }
    


    [ProtoContract]
    [MessageMeta((short)Module.AUTH, (short)Cmd.eGuestLoginRes)]
    public class ResGuestLogin : Message
    {

        [ProtoMember(1, IsRequired = true)]
        public int status;

        [ProtoMember(2)]
        public AccountInfo uinfo = null;

        [ProtoMember(3)]
        public long accountIdOrOpenId = 0;

        [ProtoMember(4)]
        public string accessToken;
    }

    [ProtoContract]
    [MessageMeta((short)Module.AUTH, (short)Cmd.eGuestUpgradeRes)]
    public class ResGuestUpgrade : Message
    {

        [ProtoMember(1, IsRequired = true)]
        public int status;

        [ProtoMember(2)]
        public AccountInfo uinfo = null;
    }

    [ProtoContract]
    [MessageMeta((short)Module.AUTH, (short)Cmd.eRegisterUserRes)]
    public class ResRegisterUser : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int status;   // 成功，失败;

        [ProtoMember(2)]
        public string errorStr; // reason;
    }
    
    [ProtoContract]
    [MessageMeta((short)Module.AUTH, (short)Cmd.eUserLoginReq)]
    public class ReqUserLogin : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public string uname;

        [ProtoMember(2, IsRequired = true)]
        public string upwd;
    }
    
    [ProtoContract]
    [MessageMeta((short)Module.AUTH, (short)Cmd.eUserLoginRes)]
    public class ResUserLogin : Message
    {

        [ProtoMember(1, IsRequired = true)]
        public int status;

        [ProtoMember(2)]
        public AccountInfo uinfo = null;

        [ProtoMember(3)]
        public long accountIdOrOpenId = 0;

        [ProtoMember(4)]
        public string accessToken;
    }

    [ProtoContract]
    [MessageMeta((short)Module.AUTH, (short)Cmd.eIssueGameTicketReq)]
    public class ReqIssueGameTicket : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public string accessToken;

        [ProtoMember(2, IsRequired = true)]
        public int serverId;
    }

    [ProtoContract]
    [MessageMeta((short)Module.AUTH, (short)Cmd.eIssueGameTicketRes)]
    public class ResIssueGameTicket : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int status;

        [ProtoMember(2)]
        public string gameTicket;

        [ProtoMember(3)]
        public long expiresAtUnixSeconds;
    }

    [ProtoContract]
    [MessageMeta((short)Module.AUTH, (short)Cmd.eGameAuthenticateReq)]
    public class ReqGameAuthenticate : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public string gameTicket;

        [ProtoMember(2, IsRequired = true)]
        public int serverId;

        [ProtoMember(3)]
        public string clientVersion;
    }

    [ProtoContract]
    [MessageMeta((short)Module.AUTH, (short)Cmd.eGameAuthenticateRes)]
    public class ResGameAuthenticate : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public int status;

        [ProtoMember(2)]
        public long accountId;

        [ProtoMember(3)]
        public string errorMessage;
    }

    [ProtoContract]
    [MessageMeta((short)Module.AUTH, (short)Cmd.eInvalidateAccountSessionReq)]
    public class ReqInvalidateAccountSession : Message
    {
        [ProtoMember(1, IsRequired = true)]
        public long accountId;

        [ProtoMember(2, IsRequired = true)]
        public long sessionVersion;

        [ProtoMember(3, IsRequired = true)]
        public string sessionId;

        [ProtoMember(4, IsRequired = true)]
        public long timestamp;

        [ProtoMember(5, IsRequired = true)]
        public string nonce;

        [ProtoMember(6, IsRequired = true)]
        public string signature;
    }


    [ProtoContract]
    [MessageMeta((short)Module.AUTH, (short)Cmd.eUserLogoutReq)]
    public sealed class ReqUserLogout : Message
    {
        [ProtoMember(1, IsRequired = true)] public string accessToken;
    }

    [ProtoContract]
    [MessageMeta((short)Module.AUTH, (short)Cmd.eUserLogoutRes)]
    public sealed class ResUserLogout : Message
    {
        [ProtoMember(1, IsRequired = true)] public int status;
        [ProtoMember(2)] public string errorMessage;
    }
}
