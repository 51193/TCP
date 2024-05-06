using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TCP
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public partial struct EthernetHeader
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] DestMAC; // 目的MAC地址
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] SrcMAC; // 源MAC地址
        public ushort EtherType; // 以太网类型，例如0x0800表示IPv4

        public EthernetHeader(string srcMAC, string destMAC, ushort type)
        {
            DestMAC = ParseMacAddress(destMAC);
            SrcMAC = ParseMacAddress(srcMAC);
            SetEtherType(type);
        }

        public void SetEtherType(ushort value)
        {
            EtherType = htons(value);
        }

        public ushort GetEtherType()
        {
            return htons(EtherType);
        }

        private static ushort htons(ushort host)
        {
            byte[] bytes = BitConverter.GetBytes(host);
            return (ushort)((bytes[0] << 8) | bytes[1]);
        }

        private static bool IsValidMacAddress(string mac)
        {
            return MacRegex().IsMatch(mac);
        }

        [GeneratedRegex("^([0-9A-Fa-f]{2}[-]){5}([0-9A-Fa-f]{2})$")]
        private static partial Regex MacRegex();

        private static byte[] ParseMacAddress(string mac)
        {
            if (IsValidMacAddress(mac))
            {
                return Array.ConvertAll(mac.Split('-'), item => Convert.ToByte(item, 16));
            }
            else
            {
                throw new FormatException("Invalid MAC address format.");
            }
        }
    }
}
