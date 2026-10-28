using System;

namespace Framework.Core.Serializer
{
    // Metadata belongs to the transport envelope, never the protobuf body.
    public sealed class ProtocolPacket
    {
        public Message Message { get; }
        public long RouteTag { get; }
        public short LogicServerId { get; }
        public long RequestId { get; }

        public ProtocolPacket(Message message, long routeTag = 0, short logicServerId = 0, long requestId = 0)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            RouteTag = routeTag;
            LogicServerId = logicServerId;
            RequestId = requestId;
        }

        public byte[] Encode()
        {
            byte[] payload = SerializerHelper.PBEncoder(Message);
            if (payload == null) return null;
            int length = ProtocolPacketLayout.HeaderLength + payload.Length;
            if (length > ushort.MaxValue - 2) throw new ArgumentOutOfRangeException(nameof(Message), "TCP frame exceeds 65535 bytes.");
            byte[] frame = new byte[length + 2];
            Write(frame, 0, frame.Length, 2);
            Write(frame, 2 + ProtocolPacketLayout.ModuleOffset, Message.GetModule(), 2);
            Write(frame, 2 + ProtocolPacketLayout.CommandOffset, Message.GetCmd(), 2);
            Write(frame, 2 + ProtocolPacketLayout.RouteTagOffset, RouteTag, 8);
            Write(frame, 2 + ProtocolPacketLayout.LogicServerIdOffset, LogicServerId, 2);
            Write(frame, 2 + ProtocolPacketLayout.RequestIdOffset, RequestId, 8);
            Buffer.BlockCopy(payload, 0, frame, 2 + ProtocolPacketLayout.HeaderLength, payload.Length);
            return frame;
        }

        // Input excludes the TCP length prefix, like New_Server's frame decoder.
        public static ProtocolPacket Decode(byte[] data, int offset, int count)
        {
            if (data == null || offset < 0 || count < ProtocolPacketLayout.HeaderLength || offset > data.Length - count)
                throw new ArgumentException("Invalid protocol packet.");
            short module = (short)Read(data, offset + ProtocolPacketLayout.ModuleOffset, 2);
            short cmd = (short)Read(data, offset + ProtocolPacketLayout.CommandOffset, 2);
            Message message = SerializerHelper.PbDecode(module, cmd, data, offset + ProtocolPacketLayout.HeaderLength, count - ProtocolPacketLayout.HeaderLength);
            if (message == null) return null;
            return new ProtocolPacket(message,
                Read(data, offset + ProtocolPacketLayout.RouteTagOffset, 8),
                (short)Read(data, offset + ProtocolPacketLayout.LogicServerIdOffset, 2),
                Read(data, offset + ProtocolPacketLayout.RequestIdOffset, 8));
        }

        private static void Write(byte[] data, int offset, long value, int size)
        {
            for (int i = 0; i < size; i++) data[offset + i] = (byte)(value >> (8 * i));
        }

        private static long Read(byte[] data, int offset, int size)
        {
            ulong value = 0;
            for (int i = 0; i < size; i++) value |= (ulong)data[offset + i] << (8 * i);
            return unchecked((long)value);
        }
    }
}
