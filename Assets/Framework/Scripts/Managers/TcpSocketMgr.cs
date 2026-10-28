using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Framework.Core.Serializer;
using UnityEngine;

public class TcpSocketMgr : UnitySingleton<TcpSocketMgr>
{
    public const int NetRecvCmdEvent = 0x01;
    public const int NetDisconnectEvent = 0x02;
    public const int NetConnectedEvent = 0x03;
    public const int NetConnectingEvent = 0x04;

    private string serverIp = "127.0.0.1";
    private int port = 6080;
    private readonly object connectionLock = new object();
    private readonly object sendLock = new object();
    private readonly Queue<Action> mainThreadEvents = new Queue<Action>();
    private Socket clientSocket;
    private int generation;
    private volatile bool isConnected;
    private EventMgr eventMgr;
    public bool IsConnected => isConnected;

    public void Init(string host, int serverPort)
    {
        Close();
        if (string.IsNullOrWhiteSpace(host)) throw new ArgumentException("Server host is required.", nameof(host));
        if (serverPort < 1 || serverPort > ushort.MaxValue) throw new ArgumentOutOfRangeException(nameof(serverPort));
        serverIp = host;
        port = serverPort;
        eventMgr = EventMgr.Instance;
    }

    private void OnDestroy() => Close();
    private void OnApplicationQuit() => Close();

    private void Update()
    {
        while (true)
        {
            Action action;
            lock (mainThreadEvents)
            {
                if (mainThreadEvents.Count == 0) break;
                action = mainThreadEvents.Dequeue();
            }
            try { action(); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }

    private void QueueEvent(int session, int eventType, object data, object metadata = null)
    {
        lock (mainThreadEvents)
            mainThreadEvents.Enqueue(() =>
            {
                if (session == generation && eventMgr != null)
                    eventMgr.Emit(eventType, data, metadata);
            });
    }

    // The caller chooses the endpoint after server discovery/ticket acquisition.
    // Authentication, heartbeat scheduling and reconnect policy belong to future business code.
    public void ConnectToServer()
    {
        Socket socket;
        int session;
        lock (connectionLock)
        {
            if (clientSocket != null) return;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true, SendTimeout = 5000 };
            clientSocket = socket;
            session = ++generation;
        }
        QueueEvent(session, NetConnectingEvent, null);
        new Thread(() => RunConnection(socket, session)) { IsBackground = true, Name = "Client TCP" }.Start();
    }

    private void RunConnection(Socket socket, int session)
    {
        string error = null;
        try
        {
            socket.Connect(serverIp, port);
            lock (connectionLock)
            {
                if (clientSocket != socket) return;
                isConnected = true;
            }
            QueueEvent(session, NetConnectedEvent, null);
            byte[] header = new byte[2];
            while (ReadExactly(socket, header))
            {
                int length = header[0] | header[1] << 8;
                if (length < 2 + ProtocolPacketLayout.HeaderLength) throw new InvalidDataException("TCP frame is shorter than the protocol header.");
                byte[] packet = new byte[length - 2];
                if (!ReadExactly(socket, packet)) throw new EndOfStreamException("Incomplete TCP frame.");
                ProtocolPacket decoded = ProtocolPacket.Decode(packet, 0, packet.Length);
                if (decoded != null) QueueEvent(session, NetRecvCmdEvent, decoded.Message, decoded);
            }
        }
        catch (Exception e) { error = e.Message; }
        finally { Disconnect(socket, session, error); }
    }

    private static bool ReadExactly(Socket socket, byte[] buffer)
    {
        int offset = 0;
        while (offset < buffer.Length)
        {
            int count = socket.Receive(buffer, offset, buffer.Length - offset, SocketFlags.None);
            if (count == 0)
            {
                if (offset != 0) throw new EndOfStreamException("Connection closed during TCP frame.");
                return false;
            }
            offset += count;
        }
        return true;
    }

    private void Disconnect(Socket socket, int session, object reason)
    {
        lock (connectionLock)
        {
            if (clientSocket != socket) return;
            clientSocket = null;
            isConnected = false;
        }
        socket.Dispose();
        QueueEvent(session, NetDisconnectEvent, reason);
    }

    // Closing the socket unblocks Receive/Connect; never abort a Unity worker thread.
    public void Close()
    {
        Socket socket;
        int session;
        lock (connectionLock)
        {
            socket = clientSocket;
            clientSocket = null;
            isConnected = false;
            session = ++generation;
        }
        lock (mainThreadEvents) mainThreadEvents.Clear();
        if (socket == null) return;
        socket.Dispose();
        QueueEvent(session, NetDisconnectEvent, 1);
    }

    public void SendMsg(Message message) => TrySendMsg(message);

    public bool TrySendMsg(Message message, long routeTag = 0, short logicServerId = 0, long requestId = 0)
    {
        Socket socket;
        int session;
        lock (connectionLock)
        {
            if (!isConnected || clientSocket == null) return false;
            socket = clientSocket;
            session = generation;
        }
        byte[] frame = new ProtocolPacket(message, routeTag, logicServerId, requestId).Encode();
        if (frame == null) return false;
        try
        {
            lock (sendLock)
            {
                int offset = 0;
                while (offset < frame.Length)
                {
                    int sent = socket.Send(frame, offset, frame.Length - offset, SocketFlags.None);
                    if (sent == 0) throw new IOException("Socket did not send data.");
                    offset += sent;
                }
            }
            return true;
        }
        catch (Exception e)
        {
            Disconnect(socket, session, e.Message);
            return false;
        }
    }
}
