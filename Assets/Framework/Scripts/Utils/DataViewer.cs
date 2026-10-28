using System;

public class DataViewer {
    public static void WriteUShortLE(byte[] buf, int offset, ushort value) { 
        // value ---> byte[];
        byte[] byte_value = BitConverter.GetBytes(value);
        // 小尾，还是大尾？BitConvert 系统是小尾还是大尾;
        if (!BitConverter.IsLittleEndian) {
            Array.Reverse(byte_value);
        }

        Array.Copy(byte_value, 0, buf, offset, byte_value.Length);
    }

    public static void WriteUIntLE(byte[] buf, int offset, uint value) {
        // value ---> byte[];
        byte[] byte_value = BitConverter.GetBytes(value);
        // 小尾，还是大尾？BitConvert 系统是小尾还是大尾;
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(byte_value);
        }

        Array.Copy(byte_value, 0, buf, offset, byte_value.Length);
    }

    public static void WriteBytes(byte[] dst, int offset, byte[] value) {
        Array.Copy(value, 0, dst, offset, value.Length);
    }

    public static ushort ReadUShortLE(byte[] data, int offset) {
        int ret = (data[offset] | (data[offset + 1] << 8));

        return (ushort)ret;
    }

    public static short ReadShortLE(byte[] data, int offset)
    {
        short value = (short)((data[offset + 1] << 8) | (data[offset + 0]));
        return value;
    }

    public static void WriteShortLE(byte[] data, int offset, short value)
    {
        data[offset + 0] = (byte)((value & 0x00ff));
        data[offset + 1] = (byte)((value & 0xff00) >> 8);
    }
}
