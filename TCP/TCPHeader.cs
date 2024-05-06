using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TCP
{
    public enum TCP_FLAG : byte
    {
        SYN = 0x02,
        ACK = 0x10,
        FIN = 0x01,
        RST = 0x04,
        PSH = 0x08
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct TCPHeader
    {
        [FieldOffset(0)] public ushort SrcPort; // 16位源端口
        [FieldOffset(2)] public ushort DestPort; // 16位目的端口
        [FieldOffset(4)] public uint SeqNumber; // 32位序列号
        [FieldOffset(8)] public uint AckNumber; // 32位确认号
        [FieldOffset(12)] public byte DataOffsetResNS; // 数据偏移, 保留位, NS标志
        [FieldOffset(13)] public byte Flags; // 8位标志位
        [FieldOffset(14)] public ushort Window; // 16位窗口大小
        [FieldOffset(16)] public ushort Checksum; // 16位校验和
        [FieldOffset(18)] public ushort UrgPointer; // 16位紧急指针

        public TCPHeader(ushort srcPort, ushort destPort)
        {
            SetSrcPort(srcPort);
            SetDestPort(destPort);
            SetSeqNumber(0);
            SetAckNumber(0);
            DataOffsetResNS = (5 << 4); // Header Length = 5
            Flags = 0x00; // 标志位初值
            SetWindow(65535); // 通常窗口大小
            SetChecksum(0); // 后续修改
            SetUrgPointer(0);
        }

        public void SetSrcPort(ushort value)
        {
            SrcPort = htons(value);
        }
        public ushort GetSrcPort()
        {
            return htons(SrcPort);
        }

        public void SetDestPort(ushort value)
        {
            DestPort = htons(value);
        }
        public ushort GetDestPort()
        {
            return htons(DestPort);
        }

        public void SetSeqNumber(uint value)
        {
            SeqNumber = htons(value);
        }
        public uint GetSeqNumber()
        {
            return htons(SeqNumber);
        }

        public void SetAckNumber(uint value)
        {
            AckNumber = htons(value);
        }
        public uint GetAckNumber()
        {
            return htons(AckNumber);
        }

        public void SetWindow(ushort value)
        {
            Window = htons(value);
        }
        public ushort GetWindow()
        {
            return htons(Window);
        }

        public void SetChecksum(ushort value)
        {
            Checksum = htons(value);
        }
        public ushort GetChecksum()
        {
            return htons(Checksum);
        }

        public void SetUrgPointer(ushort value)
        {
            UrgPointer = htons(value);
        }
        public ushort GetUrgPointer()
        {
            return htons(UrgPointer);
        }

        public void SetFlag(TCP_FLAG flag)// 设置标志位方法
        {
            Flags |= (byte)flag;
        }

        public void ClearFlag(byte flag)// 清除标志位方法
        {
            Flags &= (byte)~flag;
        }

        public void ResetFlag()
        {
            Flags = 0x00;
        }

        private static ushort htons(ushort host)
        {
            byte[] bytes = BitConverter.GetBytes(host);
            return (ushort)((bytes[0] << 8) | bytes[1]);
        }
        private static uint htons(uint value)
        {
            return (uint)(((value & 0x000000FF) << 24) | ((value & 0x0000FF00) << 8) | ((value & 0x00FF0000) >> 8) | ((value & 0xFF000000) >> 24));
        }
    }
}
