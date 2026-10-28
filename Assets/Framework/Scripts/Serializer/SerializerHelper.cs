using Framework.Core.Serializer;
using System;
using System.IO;
using UnityEngine;

public class SerializerHelper
{
    // protobuf-net uses the same field numbers and wire format as the server Google codec.
    public static byte[] PBEncoder(Message msg)
    {
        if (msg == null) throw new ArgumentNullException(nameof(msg));
        try
        {
            using (var stream = new MemoryStream())
            {
                ProtoBuf.Serializer.NonGeneric.Serialize(stream, msg);
                return stream.ToArray();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Encode {msg.GetType().Name}: {e.Message}");
            return null;
        }
    }

    public static Message PbDecode(short module, short cmd, byte[] body, int offset, int count)
    {
        if (!MessageFactory.Instance.GetMessage(module, cmd, out Type type))
        {
            Debug.LogWarning($"Unknown protocol: {module}_{cmd}");
            return null;
        }
        if (body == null || offset < 0 || count < 0 || offset > body.Length - count)
            throw new ArgumentException("Invalid protobuf body range.");
        try
        {
            using (var stream = new MemoryStream(body, offset, count))
                return (Message)ProtoBuf.Serializer.Deserialize(type, stream);
        }
        catch (Exception e)
        {
            Debug.LogError($"Decode {module}_{cmd}: {e.Message}");
            return null;
        }
    }
}
