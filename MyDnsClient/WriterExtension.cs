using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MyDnsClient
{
    //internal static class WriterExtension
    //{
    //    public static void WriteInt16NetworkOrder(this BinaryWriter writer, short value)
    //    {
    //        var bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value));
    //        writer.WriteBytes(bytes, bytes.Length);
    //    }

    //    public static void WriteBytes(this BinaryWriter writer, byte[] data, int length) => writer.WriteBytes(data, 0, length);

    //    public static void WriteBytes(this BinaryWriter writer, byte[] data, int dataOffset, int length)
    //    {
    //        writer.WriteBytes((byte[])data, dataOffset, length);
    //        Buffer.BlockCopy(data, dataOffset, _buffer.Array, _buffer.Offset + Index, length);

    //        Index += length;
    //    }
    //}
}
