using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Framework.Core.Serializer;
using Framework.Core.Utils;
using Game.Datas.Messages;

// Small harness for the framework only; no Unity runtime or game/database services needed.
namespace UnityEngine
{
    public static class Debug
    {
        public static void LogError(object value) => throw new Exception(value.ToString());
        public static void LogWarning(object value) { }
        public static void LogException(Exception value) => throw value;
    }
}
public class UnitySingleton<T> where T : new()
{
    public static T Instance { get; } = new T();
}
class Program
{
    static void Assert(bool value, string reason) { if (!value) throw new Exception(reason); }
    static byte[] Google(Message message)
    {
        using var stream = new MemoryStream();
        Assert(GoogleProtocolCodec.TrySerialize(message, stream, out string error), message.GetType().Name + ": " + error);
        return stream.ToArray();
    }
    static object Sample(Type type, int depth = 0)
    {
        if (Nullable.GetUnderlyingType(type) != null) return Sample(Nullable.GetUnderlyingType(type), depth + 1);
        if (type == typeof(string)) return "协议-utf8";
        if (type == typeof(int)) return -123456;
        if (type == typeof(long)) return 9007199254740993L;
        if (type == typeof(float)) return -1.25f;
        if (type == typeof(double)) return 1.125;
        if (type == typeof(bool)) return true;
        if (type.IsEnum) return Enum.ToObject(type, 1);
        if (type.IsArray)
        {
            Array array = Array.CreateInstance(type.GetElementType(), 2);
            for (int i = 0; i < 2; i++) array.SetValue(Sample(type.GetElementType(), depth + 1), i);
            return array;
        }
        object value = Activator.CreateInstance(type);
        if (value is IList list)
        {
            list.Add(Sample(type.GetGenericArguments()[0], depth + 1));
            return list;
        }
        Assert(depth < 12, "Recursive protocol graph");
        foreach (FieldInfo field in type.GetFields())
            if (field.GetCustomAttribute<ProtoBuf.ProtoMemberAttribute>() != null)
                field.SetValue(value, Sample(field.FieldType, depth + 1));
        return value;
    }
    static void Pump() => typeof(TcpSocketMgr).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(TcpSocketMgr.Instance, null);
    static void Wait(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        do { Pump(); if (condition()) return; Thread.Sleep(5); } while (DateTime.UtcNow < deadline);
        throw new Exception("Timed out waiting for TCP event");
    }
    static void Main()
    {
        MessageFactory.Instance.InitMeesagePool();
        MessageFactory.Instance.InitMeesagePool();
        int count = 0;
        var unsupported = new List<string>();
        foreach (Type type in TypeScanner.ListAllSubTypes(typeof(Message)).Where(t => !t.IsAbstract))
        {
            var sample = (Message)Sample(type);
            using var probe = new MemoryStream();
            if (!GoogleProtocolCodec.TrySerialize(sample, probe, out string probeError))
            {
                Assert(string.IsNullOrEmpty(probeError), type.Name + ": " + probeError);
                unsupported.Add(type.Name);
                byte[] local = SerializerHelper.PBEncoder(sample);
                Assert(local != null && SerializerHelper.PbDecode(sample.GetModule(), sample.GetCmd(), local, 0, local.Length) != null, "Local contract roundtrip");
                continue;
            }
            byte[] canonical = probe.ToArray();
            Message client = SerializerHelper.PbDecode(sample.GetModule(), sample.GetCmd(), canonical, 0, canonical.Length);
            Assert(client != null && canonical.SequenceEqual(Google(client)), "Server -> client: " + type.Name);
            byte[] encoded = SerializerHelper.PBEncoder(sample);
            using var stream = new MemoryStream(encoded);
            Assert(GoogleProtocolCodec.TryDeserialize(type, stream, out Message server, out string error), sample.GetType().Name + ": " + error);
            Assert(canonical.SequenceEqual(Google(server)), "Client -> server: " + type.Name);
            var defaults = (Message)Activator.CreateInstance(type);
            byte[] defaultWire = Google(defaults);
            Message defaultClient = SerializerHelper.PbDecode(defaults.GetModule(), defaults.GetCmd(), defaultWire, 0, defaultWire.Length);
            Assert(defaultWire.SequenceEqual(Google(defaultClient)), "Default server -> client: " + type.Name);
            using var defaultStream = new MemoryStream(SerializerHelper.PBEncoder(defaults));
            Assert(GoogleProtocolCodec.TryDeserialize(type, defaultStream, out Message defaultServer, out string defaultError), defaultError);
            Assert(defaultWire.SequenceEqual(Google(defaultServer)), "Default client -> server: " + type.Name);
            count++;
        }
        Console.WriteLine($"PASS: {count} contracts, both directions against actual server Google codec.");
        Console.WriteLine("Server codec missing (local roundtrip only): " + string.Join(", ", unsupported));
        var packet = new ProtocolPacket(new ReqHeartbeat { clientUnixMilliseconds = 42 }, long.MinValue + 1, -321, long.MaxValue);
        byte[] frame = packet.Encode();
        Assert((frame[0] | frame[1] << 8) == frame.Length, "TCP inclusive length");
        ProtocolPacket decoded = ProtocolPacket.Decode(frame, 2, frame.Length - 2);
        Assert(decoded.RouteTag == packet.RouteTag && decoded.LogicServerId == -321 && decoded.RequestId == long.MaxValue, "64-bit envelope metadata");
        try { ProtocolPacket.Decode(new byte[21], 0, 21); throw new Exception("Accepted truncated packet"); } catch (ArgumentException) { }
        var unknown = new byte[22]; unknown[0] = 255;
        Assert(ProtocolPacket.Decode(unknown, 0, 22) == null, "Unknown protocol must be dropped");
        Console.WriteLine("PASS: frame layout, metadata, short packets, unknown commands.");

        EventMgr.Instance.Init();
        int main = Environment.CurrentManagedThreadId, received = 0, disconnected = 0;
        EventMgr.Instance.AddListener(TcpSocketMgr.NetRecvCmdEvent, (a,b,c) => {
            Assert(Environment.CurrentManagedThreadId == main, "Callback left main thread");
            Assert(c is ProtocolPacket envelope && envelope.RequestId == 123, "Envelope lost");
            received++;
        });
        EventMgr.Instance.AddListener(TcpSocketMgr.NetDisconnectEvent, (a,b,c) => disconnected++);
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        TcpSocketMgr.Instance.Init("127.0.0.1", ((IPEndPoint)listener.LocalEndpoint).Port);
        TcpSocketMgr.Instance.ConnectToServer();
        using Socket peer = listener.AcceptSocket();
        Wait(() => TcpSocketMgr.Instance.IsConnected);
        byte[] response = new ProtocolPacket(new ResHeartbeat { status = 1 }, 0, 0, 123).Encode();
        peer.Send(response, 0, 1, SocketFlags.None);
        Thread.Sleep(15);
        peer.Send(response, 1, 4, SocketFlags.None);
        Thread.Sleep(15);
        byte[] combined = response.Skip(5).Concat(response).ToArray();
        peer.Send(combined);
        Wait(() => received == 2);
        Assert(TcpSocketMgr.Instance.TrySendMsg(new ReqHeartbeat { clientUnixMilliseconds = 99 }, 88, 7, 456), "Client send failed");
        peer.ReceiveTimeout = 2000;
        byte[] prefix = Read(peer, 2);
        byte[] body = Read(peer, (prefix[0] | prefix[1] << 8) - 2);
        ProtocolPacket sent = ProtocolPacket.Decode(body, 0, body.Length);
        Assert(sent.RequestId == 456 && sent.RouteTag == 88 && sent.LogicServerId == 7, "Sent metadata mismatch");
        byte[] large = new ProtocolPacket(new ResUserLogin { accessToken = new string('x', 20000) }, 0, 0, 123).Encode();
        peer.Send(large);
        Wait(() => received == 3);
        try
        {
            new ProtocolPacket(new ResUserLogin { accessToken = new string('x', 65535) }).Encode();
            throw new Exception("Accepted oversized TCP packet");
        }
        catch (ArgumentOutOfRangeException) { }
        peer.Shutdown(SocketShutdown.Both);
        peer.Close();
        Wait(() => disconnected == 1);
        Assert(!TcpSocketMgr.Instance.IsConnected, "EOF left socket connected");
        TcpSocketMgr.Instance.ConnectToServer();
        using Socket peer2 = listener.AcceptSocket();
        Wait(() => TcpSocketMgr.Instance.IsConnected);
        TcpSocketMgr.Instance.Close();
        Wait(() => disconnected == 2);
        Assert(!TcpSocketMgr.Instance.TrySendMsg(new ReqHeartbeat()), "Send after close");
        Thread.Sleep(30); Pump();
        Assert(disconnected == 2 && !listener.Pending(), "Duplicate disconnect or automatic reconnect");
        Console.WriteLine("PASS: fragmented/coalesced TCP frames, send, EOF, reconnect, intentional close, main-thread events.");
    }
    static byte[] Read(Socket socket, int length)
    {
        byte[] data = new byte[length]; int offset = 0;
        while (offset < length) { int n = socket.Receive(data, offset, length - offset, SocketFlags.None); if (n == 0) throw new EndOfStreamException(); offset += n; }
        return data;
    }
}
