namespace Game{

    enum Module {
        /*...*/

        /** 登录 */
        AUTH = 101,
        /** 玩家 */
        PLAYER = 102,
        /** 游戏场景 */
        SCENE = 103,
        /** 活动 */
        ACTIVITY = 104,
        /** 技能 */
        SKILL = 105,
        /** 聊天 */
        CHAT = 106,
        // ------------------跨服业务功能模块（107开始）---------------------
        /** 跨服天梯 */
        LADDER = 107,
    }

    public enum Cmd
    {
        // 游客登录
        eGuestLoginReq = 1,
        eGuestLoginRes = 2,

        // 用户名注册,
        eRegisterUserReq = 3,
        eRegisterUserRes = 4,

        // 用户登录
        eUserLoginReq = 1001,
        eUserLoginRes = 1002,
        eUserLogoutReq = 1008,
        eUserLogoutRes = 1009,

        // 登录服签发游戏服一次性票据
        eIssueGameTicketReq = 1003,
        eIssueGameTicketRes = 1004,

        // TCP/WebSocket 连接游戏服后的首包鉴权
        eGameAuthenticateReq = 1005,
        eGameAuthenticateRes = 1006,

        // 登录服通知游戏服淘汰旧认证会话
        eInvalidateAccountSessionReq = 1007,

        // 游客账号升级
        eGuestUpgradeReq = 7,
        eGuestUpgradeRes = 8,

        // 拉取玩家游戏数据
        ePullingPlayerDataReq = 9,
        ePullingPlayerDataRes = 10,

        // 领取每日登录奖励
        eRecvLoginBonuesReq = 11,
        eRecvLoginBonuesRes = 12,

        // 拉取玩家的奖励数据
        ePullingBonuesListReq = 13,
        ePullingBonuesListRes = 14,

        // 领取玩家的奖励
        eRecvBonuesReq = 15,
        eRecvBonuesRes = 16,

        // 玩家选择角色
        eSelectPlayerReq = 17,
        eSelectPlayerRes = 18,

        // 拉取玩家的任务数据
        ePullingTaskListReq = 19,
        ePullingTaskListRes = 20,

        // 拉取玩家的邮件消息
        ePullingMailMsgReq = 21,
        ePullingMailMsgRes = 22,

        // 玩家更新邮件消息状态
        eUpdateMailMsgReq = 23,
        eUpdategMailMsgRes = 24,

        // 玩家拉取排行
        ePullingRankReq = 25,
        ePullingRankRes = 26,

        // 玩家拉取背包数据
        ePullingPackDataReq = 27,
        ePullingPackDataRes = 28,

        // 玩家交易兑换
        eExchangeProductReq = 29,
        eExchangeProductRes = 30,

        // 玩家获取逻辑服实例Id
        eEnterLogicServerReq = 31,
        eEnterLogicServerRes = 32,

        // 玩家离开逻辑服实例
        eExitLogicServerReq = 33,
        eExitLogicServerRes = 34,

        // 玩家的坐下
        eSitdownReq = 35,
        eSitdownRes = 36,

        // 玩家站起
        eStandupReq = 37,
        eStandupRes = 38,

        // 玩家发送聊天消息
        eTalkMsgReq = 39,
        eTalkMsgRes = 40,

        // 玩家准备好
        ePlayerIsReadyReq = 41,
        ePlayerIsReadyRes = 42,

        // 玩家的游戏操作
        ePlayerOptReq = 43,
        ePlayerOptRes = 44,

        // 房间模式: 服务器主动通知
        eEnterRoomRes = 45,
        eUserArrivedSeatRes = 46,
        eUserExitSeatRes = 47,
        eRoundReadyRes = 48,
        eRoundStartedRes = 49,
        eRoundCheckOutRes = 50,
        eRoundClearRes = 51,
        eReconnRoomRes = 52,
        ePlayerEscapeRes = 53,
        eFrameSyncDataRes = 54, // 帧同步，同步所有的玩家操作到客户端;

        // 开放世界的命令
        // 玩家出生的请求;
        ePlayerSpawnReq = 55,
        ePlayerSpawnRes = 56,

        eNavToDstReq = 57,
        eNavToDstRes = 58,

        eNavInDirReq = 59,
        eNavInDirRes = 60,

        eStartSkillReq = 61,
        eStartSkillRes = 62,

        eStartBuffReq = 63,
        eStartBuffRes = 64,

        // 开放世界: 服务器主动通知
        eEnterAOIRes = 65,
        eLeaveAOIRes = 66,
        eSyncCharactorRes = 67,
        eLostHpRes = 68,

        // 测试正式项目不要直接开放出来
        eTestGetGoodReq = 69,
        eTestGetGoodRes = 70,

        // 测试更新背包物品,正式项目不要直接开放出来
        eTestUpdateGoodsReq = 71,
        eTestUpdateGoddsRes = 72,
        
        // 测试游戏逻辑服通讯命令
        eTestLogicCmdEchoReq = 73,
        eTestLogicCmdEchoRes = 74,

        // 抽奖
        eGetGachaPoolInfoReq = 75,
        eGetGachaPoolInfoRes = 76,
        eGachaDrawReq = 77,
        eGachaDrawRes = 78,

        // 玩家角色查询与升星
        eGetCharacterInfoReq = 79,
        eGetCharacterInfoRes = 80,
        eCharacterStarUpReq = 81,
        eCharacterStarUpRes = 82,

        // 已鉴权游戏连接心跳
        eHeartbeatReq = 83,
        eHeartbeatRes = 84,

        // 玩家编队查询与保存
        eGetPlayerFormationsReq = 85,
        eGetPlayerFormationsRes = 86,
        eSavePlayerFormationReq = 87,
        eSavePlayerFormationRes = 88,

        // 玩家全部物品查询
        eGetPlayerItemsReq = 89,
        eGetPlayerItemsRes = 90,
        eGetPlayerBasicInfoReq = 91,
        eGetPlayerBasicInfoRes = 92,
        eUpdatePlayerBasicInfoReq = 93,
        eUpdatePlayerBasicInfoRes = 94,
        eRedDotInitReq = 95,
        eRedDotInitRes = 96,
        eRedDotViewReq = 97,
        eRedDotViewRes = 98,
        eRedDotChanged = 99,
        eMailSummaryReq = 110,
        eMailSummaryRes = 111,
        eMailListReq = 112,
        eMailListRes = 113,
        eMailReadReq = 114,
        eMailReadRes = 115,
        eMailClaimReq = 116,
        eMailClaimRes = 117,
        eMailClaimAllReq = 118,
        eMailClaimAllRes = 119,
        eMailDeleteReq = 120,
        eMailDeleteRes = 121,
        eMailDeleteReadReq = 122,
        eMailDeleteReadRes = 123,
        eMailChangedRes = 124,

    }

    public enum QuitReason { 
        PlayerQuit = 1,
        ForceQuit = 2,
        DisconnectQuit = 3,

        // ...
    }

    public enum Respones
    {
        OK = 1,
        FrameSyncCached = 2,

        SystemErr = -100,
        UserIsFreeze = -101,
        UserIsNotGuest = -102,
        InvalidParams = -103,
        UnameIsExist = -104,
        UnameOrUpwdError = -105,
        InvalidOpt = -106,
        PlayerIsNotExist = -107,
        AccountIsNotLogin = -108,
        PlayerIsFreeze = -109,
        AccountIsNotExist = -110,
        MoneyIsNotEnough = -111,
        LogicServerIsBusy = -112,
        InOtherLogicServerInst = -113,
        LogicServerInstIsNotExist = -114,
        UserIsPlaying = -115,
        LevelIsNotEnough = -116,
        ChipIsNotEnough = -117,
        SeatIsUsing = -118,
        GachaPoolNotFound = -119,
        GachaConfigError = -120,
        GachaDrawCountInvalid = -121,
        GachaResourceNotEnough = -122,
        GachaFailed = -123,
        CharacterNotOwned = -124,
        CharacterMaxStar = -125,
        CharacterFragmentNotEnough = -126,
        CharacterTargetStarInvalid = -127,
        CharacterConfigError = -128,
        CharacterStarUpFailed = -129,
        EconomicOperationConflict = -130,
        FormationVersionConflict = -131,
        FormationSaveFailed = -132,
        RequestRateLimited = -133,
        MailNotFound = -140,
        MailNotAllowed = -141,
        MailNoRewards = -142,
        MailInvalidReward = -143,
        MailSourceConflict = -144,
        // ...
    }


    public enum Channal { 
        InvalidChannal = -1,
        SelfChannal = 0,
        DouYin,
        IosAppStore,
        // ...
    }


    public enum RankType
    {
        WorldCoin = 100001,

        // ...

    }

    public enum RuleType { 
        Task = 1,
        Trading,
        Bonues,
        Backpack,
    }

    public enum ServerType {
        RoomWithRule = 1, // 房间模式+RoomMgr服务模式,代表有棋牌,卡牌,格斗，回合制等;
        OpenWithMapWorld = 2, // 开放世界+GameWorld服务; 代表有 RPG, ARPG, MMORPG
        IndepOrRoomWithMapWorld = 3, // 副本 + 房间模式 + GameWorld服务，代表有Moba，游戏副本等
        
    }
}
