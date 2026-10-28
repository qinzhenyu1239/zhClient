# UnityClient 与 New_Server 框架适配

本次只适配传输、协议和启动框架，没有实现新的登录、选服、票据鉴权、重连恢复或其他业务功能。服务端文件未修改。

## 启动与接入

- `Boot` 继续初始化现有资源、事件、场景和网络管理器；`connectOnStartup` 默认关闭。
- `GameApp.EnterGame()` 停在框架就绪，不再自动打开旧游客登录演示 UI。原演示脚本保留，未改写为新登录流程。
- 后续业务层取得目标地址后，在主线程调用 `TcpSocketMgr.Instance.Init(host, port)` 和 `ConnectToServer()`。
- New_Server 的游戏连接要求先发送 `ReqGameAuthenticate`。获取票据、发送鉴权、鉴权成功后启动心跳均由后续业务层接入。仅建立 TCP 连接不代表鉴权完成。
- `TrySendMsg(message, routeTag, logicServerId, requestId)` 返回发送是否成功；`SendMsg(message)` 保留原调用接口。
- `NetRecvCmdEvent` 的第一个数据参数仍是 `Message`，第二个数据参数是 `ProtocolPacket`，可读取响应路由和请求编号。网络事件统一从 Unity 主线程的 `Update()` 派发。
- 主动关闭和断线均停止连接，不自动重连。调用 `Close()` 后可显式重新连接。
- `Boot.remoteURL` 是资源热更新地址；`serverIp/port` 是 TCP 地址。需按实际部署分别配置。

## 协议

TCP 帧使用小端序：`总长度 ushort（含自身 2 字节）+ 业务头 22 字节 + protobuf body`。

| 业务头偏移 | 字段 | 字节 |
| --- | --- | --- |
| 0 | module | 2 |
| 2 | command | 2 |
| 4 | routeTag | 8 |
| 12 | logicServerId | 2 |
| 14 | requestId | 8 |

协议数据结构、命令号、状态码和业务头布局以 New_Server 的 C# 文件为准。本项目不是 ET，不使用 ET Proto2CS。客户端保留现有 protobuf-net，使用服务端相同字段号和线格式。

更新协议时在工作区根目录运行：

```powershell
& ./UnityClient/Tools/Sync-ServerProtocol.ps1
```

脚本同步服务端消息目录、Constants 和 ProtocolPacketLayout，移除无用的服务端专用 using，并保留已有 Unity `.meta` GUID。旧拆分协议文件变为空占位，类型统一由同步文件提供。不要在同步输出中单独修改协议。若服务端删除或重命名整个消息文件，需同时整理对应的客户端旧文件。

## 验证

```powershell
dotnet run --project UnityClient/Tools/FrameworkChecks/FrameworkChecks.csproj
```

验证器位于 Assets 之外，不进入 Unity Player。它直接编译服务端 GoogleProtocolCodec 源码，与客户端 protobuf-net 交叉编解码，并通过本地 TCP 检查拆包、粘包、完整收发、64 位元数据、EOF、主动关闭、再次连接和主线程事件。Google.Protobuf 引用来自 `New_Server/GameServer/bin/Debug/net8.0/Google.Protobuf.dll`；若不存在，先构建服务端。

当前通过 111 个服务端已支持消息的双向编解码。以下 10 个消息在服务端 GoogleProtocolCodec 中未注册，本次仅验证客户端自身编解码，实际联调受服务端限制：

`ReqUpdatePlayerBasicInfo`、`ResPullingBonuesList`、`ResPullingPackData`、`ResPullingRank`、`ResPullingTaskList`、`ResFrameSyncData`、`ResReconnRoom`、`ResSyncCharactorStatus`、`ResEnterAOI`、`ResLeaveAOI`。

全部客户端运行时脚本已使用本机可用的 Unity 2022.3.55 程序集做编译检查。项目指定的 2022.3.42 编辑器可执行文件缺失，因此未执行该版本 Unity 的导入、Play Mode、IL2CPP 打包或真实服务端登录联调。打开匹配版本 Unity 后应完成这些检查。
