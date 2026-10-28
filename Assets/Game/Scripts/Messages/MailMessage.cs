// Synchronized by Tools/Sync-ServerProtocol.ps1. Edit New_Server contracts first.
using Framework.Core.Serializer;
using Framework.Core.Utils;
using ProtoBuf;
using System.Collections.Generic;
namespace Game.Datas.Messages
{
    [ProtoContract]
    public sealed class MailAttachmentMessage
    {
        [ProtoMember(1)] public int sortOrder;
        [ProtoMember(2)] public int itemId;
        [ProtoMember(3)] public long quantity;
    }
    [ProtoContract]
    public sealed class MailEntryMessage
    {
        [ProtoMember(1)] public long id;
        [ProtoMember(2)] public int mailType;
        [ProtoMember(3)] public string title;
        [ProtoMember(4)] public string body;
        [ProtoMember(5)] public long sendTime;
        [ProtoMember(6)] public long expireTime;
        [ProtoMember(7)] public long readTime;
        [ProtoMember(8)] public long claimTime;
        [ProtoMember(9)] public List<MailAttachmentMessage> attachments = new List<MailAttachmentMessage>();
    }
    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eMailSummaryReq)]
    public sealed class ReqMailSummary : Message {    }
    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eMailSummaryRes)]
    public sealed class ResMailSummary : Message
    {
        [ProtoMember(1)] public int status;
        [ProtoMember(2)] public string errorMessage;
        [ProtoMember(3)] public long mailboxVersion;
        [ProtoMember(4)] public long serverTime;
        [ProtoMember(5)] public int count;
        [ProtoMember(6)] public int unreadCount;
        [ProtoMember(7)] public int capacity;
        [ProtoMember(8)] public List<MailEntryMessage> items = new List<MailEntryMessage>();
        [ProtoMember(9)] public MailEntryMessage detail;
        [ProtoMember(10)] public List<PlayerItemMessage> itemQuantities = new List<PlayerItemMessage>();
        [ProtoMember(11)] public long mailId;
        [ProtoMember(12)] public bool hasMore;
        [ProtoMember(13)] public int nextOffset;
    }
    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eMailListReq)]
    public sealed class ReqMailList : Message {
        [ProtoMember(1)] public int offset;
        [ProtoMember(2)] public int pageSize = 50;
    }
    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eMailListRes)]
    public sealed class ResMailList : Message
    {
        [ProtoMember(1)] public int status;
        [ProtoMember(2)] public string errorMessage;
        [ProtoMember(3)] public long mailboxVersion;
        [ProtoMember(4)] public long serverTime;
        [ProtoMember(5)] public int count;
        [ProtoMember(6)] public int unreadCount;
        [ProtoMember(7)] public int capacity;
        [ProtoMember(8)] public List<MailEntryMessage> items = new List<MailEntryMessage>();
        [ProtoMember(9)] public MailEntryMessage detail;
        [ProtoMember(10)] public List<PlayerItemMessage> itemQuantities = new List<PlayerItemMessage>();
        [ProtoMember(11)] public long mailId;
        [ProtoMember(12)] public bool hasMore;
        [ProtoMember(13)] public int nextOffset;
    }
    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eMailReadReq)]
    public sealed class ReqMailRead : Message {
        [ProtoMember(1)] public long mailId;
    }
    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eMailReadRes)]
    public sealed class ResMailRead : Message
    {
        [ProtoMember(1)] public int status;
        [ProtoMember(2)] public string errorMessage;
        [ProtoMember(3)] public long mailboxVersion;
        [ProtoMember(4)] public long serverTime;
        [ProtoMember(5)] public int count;
        [ProtoMember(6)] public int unreadCount;
        [ProtoMember(7)] public int capacity;
        [ProtoMember(8)] public List<MailEntryMessage> items = new List<MailEntryMessage>();
        [ProtoMember(9)] public MailEntryMessage detail;
        [ProtoMember(10)] public List<PlayerItemMessage> itemQuantities = new List<PlayerItemMessage>();
        [ProtoMember(11)] public long mailId;
        [ProtoMember(12)] public bool hasMore;
        [ProtoMember(13)] public int nextOffset;
    }
    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eMailClaimReq)]
    public sealed class ReqMailClaim : Message {
        [ProtoMember(1)] public long mailId;
    }
    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eMailClaimRes)]
    public sealed class ResMailClaim : Message
    {
        [ProtoMember(1)] public int status;
        [ProtoMember(2)] public string errorMessage;
        [ProtoMember(3)] public long mailboxVersion;
        [ProtoMember(4)] public long serverTime;
        [ProtoMember(5)] public int count;
        [ProtoMember(6)] public int unreadCount;
        [ProtoMember(7)] public int capacity;
        [ProtoMember(8)] public List<MailEntryMessage> items = new List<MailEntryMessage>();
        [ProtoMember(9)] public MailEntryMessage detail;
        [ProtoMember(10)] public List<PlayerItemMessage> itemQuantities = new List<PlayerItemMessage>();
        [ProtoMember(11)] public long mailId;
        [ProtoMember(12)] public bool hasMore;
        [ProtoMember(13)] public int nextOffset;
    }
    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eMailClaimAllReq)]
    public sealed class ReqMailClaimAll : Message {    }
    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eMailClaimAllRes)]
    public sealed class ResMailClaimAll : Message
    {
        [ProtoMember(1)] public int status;
        [ProtoMember(2)] public string errorMessage;
        [ProtoMember(3)] public long mailboxVersion;
        [ProtoMember(4)] public long serverTime;
        [ProtoMember(5)] public int count;
        [ProtoMember(6)] public int unreadCount;
        [ProtoMember(7)] public int capacity;
        [ProtoMember(8)] public List<MailEntryMessage> items = new List<MailEntryMessage>();
        [ProtoMember(9)] public MailEntryMessage detail;
        [ProtoMember(10)] public List<PlayerItemMessage> itemQuantities = new List<PlayerItemMessage>();
        [ProtoMember(11)] public long mailId;
        [ProtoMember(12)] public bool hasMore;
        [ProtoMember(13)] public int nextOffset;
    }
    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eMailDeleteReq)]
    public sealed class ReqMailDelete : Message {
        [ProtoMember(1)] public long mailId;
    }
    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eMailDeleteRes)]
    public sealed class ResMailDelete : Message
    {
        [ProtoMember(1)] public int status;
        [ProtoMember(2)] public string errorMessage;
        [ProtoMember(3)] public long mailboxVersion;
        [ProtoMember(4)] public long serverTime;
        [ProtoMember(5)] public int count;
        [ProtoMember(6)] public int unreadCount;
        [ProtoMember(7)] public int capacity;
        [ProtoMember(8)] public List<MailEntryMessage> items = new List<MailEntryMessage>();
        [ProtoMember(9)] public MailEntryMessage detail;
        [ProtoMember(10)] public List<PlayerItemMessage> itemQuantities = new List<PlayerItemMessage>();
        [ProtoMember(11)] public long mailId;
        [ProtoMember(12)] public bool hasMore;
        [ProtoMember(13)] public int nextOffset;
    }
    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eMailDeleteReadReq)]
    public sealed class ReqMailDeleteRead : Message {    }
    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eMailDeleteReadRes)]
    public sealed class ResMailDeleteRead : Message
    {
        [ProtoMember(1)] public int status;
        [ProtoMember(2)] public string errorMessage;
        [ProtoMember(3)] public long mailboxVersion;
        [ProtoMember(4)] public long serverTime;
        [ProtoMember(5)] public int count;
        [ProtoMember(6)] public int unreadCount;
        [ProtoMember(7)] public int capacity;
        [ProtoMember(8)] public List<MailEntryMessage> items = new List<MailEntryMessage>();
        [ProtoMember(9)] public MailEntryMessage detail;
        [ProtoMember(10)] public List<PlayerItemMessage> itemQuantities = new List<PlayerItemMessage>();
        [ProtoMember(11)] public long mailId;
        [ProtoMember(12)] public bool hasMore;
        [ProtoMember(13)] public int nextOffset;
    }
    [ProtoContract]
    [MessageMeta((short)Module.PLAYER, (short)Cmd.eMailChangedRes)]
    public sealed class ResMailChanged : Message
    {
        [ProtoMember(1)]
        public long mailboxVersion;

        [ProtoMember(2)]
        public long serverTime;

        [ProtoMember(3)]
        public int count;

        [ProtoMember(4)]
        public int unreadCount;

        [ProtoMember(5)]
        public int capacity;

        [ProtoMember(6)]
        public List<long> addedIds = new List<long>();

        [ProtoMember(7)]
        public List<long> changedIds = new List<long>();

        [ProtoMember(8)]
        public List<long> deletedIds = new List<long>();
    }
}
