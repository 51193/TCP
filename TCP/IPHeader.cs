using System.Net;
using System.Runtime.InteropServices;

namespace TCP
{
    [StructLayout(LayoutKind.Explicit)]
    public struct IPHeader
    {
        [FieldOffset(0)] public byte VerIHL; // 4位版本号 + 4位首部长度
        [FieldOffset(1)] public byte TOS; // 8位服务类型TOS
        [FieldOffset(2)] public ushort TotalLength; // 16位总长度
        [FieldOffset(4)] public ushort ID; // 16位标识符
        [FieldOffset(6)] public ushort FlagsOffset; // 3位标志位 + 13位片偏移
        [FieldOffset(8)] public byte TTL; // 8位生存时间
        [FieldOffset(9)] public byte Protocol; // 8位协议 (TCP = 6)
        [FieldOffset(10)] public ushort Checksum; // 16位首部校验和
        [FieldOffset(12)] public uint SrcAddr; // 32位源IP地址
        [FieldOffset(16)] public uint DestAddr; // 32位目的IP地址

        public IPHeader(string src, string dest)
        {
            VerIHL = 0x45;// IPv4, Header Length = 5
            TOS = 0;
            SetTotalLength(0);//后续修改
            SetID(12345);
            SetFlagsOffset(0);
            TTL = 128;
            Protocol = 6;// TCP协议
            SetChecksum(0);//后续修改
            SetSrcAddr(src);
            SetDestAddr(dest);
        }

        public void SetTotalLength(ushort value)
        {
            TotalLength = htons(value);
        }
        public ushort GetTotalLength()
        {
            return htons(TotalLength);
        }

        public void SetID(ushort value)
        {
            ID = htons(value);
        }
        public ushort GetID()
        {
            return htons(ID);
        }

        public void SetFlagsOffset(ushort value)
        {
            FlagsOffset = htons(value);
        }
        public ushort GetFlagsOffset()
        {
            return htons(FlagsOffset);
        }

        public void SetChecksum(ushort value)
        {
            Checksum = htons(value);
        }
        public ushort GetChecksum()
        {
            return htons(Checksum);
        }

        public void SetSrcAddr(string value)
        {
            SrcAddr = BitConverter.ToUInt32(IPAddress.Parse(value).GetAddressBytes());
        }
        public string GetSrcAddr()
        {
            var ip = new IPAddress(SrcAddr);
            return ip.ToString();
        }

        public void SetDestAddr(string value)
        {
            DestAddr = BitConverter.ToUInt32(IPAddress.Parse(value).GetAddressBytes());
        }
        public string GetDestAddr()
        {
            var ip = new IPAddress(DestAddr);
            return ip.ToString();
        }

        private static ushort htons(ushort value)
        {
            return (ushort)(((value & 0x00FF) << 8) | ((value & 0xFF00)) >> 8);
        }

        private static uint htons(uint value)
        {
            return (uint)(((value & 0x000000FF) << 24) | ((value & 0x0000FF00) << 8) | ((value & 0x00FF0000) >> 8) | ((value & 0xFF000000) >> 24));
        }
    }
}
