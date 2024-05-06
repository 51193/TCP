using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCP
{
    public class TCPIdentifier
    {
        public string IPAddress_1;
        public string IPAddress_2;

        public ushort Port_1;
        public ushort Port_2;

        public TCPIdentifier(string iPAddress_1, string iPAddress_2, ushort port_1, ushort port_2)
        {
            IPAddress_1 = iPAddress_1;
            IPAddress_2 = iPAddress_2;
            Port_1 = port_1;
            Port_2 = port_2;
        }

        public TCPIdentifier(TCP template)
        {
            IPAddress_1 = template.ipHeader.GetSrcAddr();
            IPAddress_2 = template.ipHeader.GetDestAddr();
            Port_1 = template.tcpHeader.GetSrcPort();
            Port_2 = template.tcpHeader.GetDestPort();
        }
        public TCPIdentifier(IPHeader ip, TCPHeader tcp)
        {
            IPAddress_1 = ip.GetSrcAddr();
            IPAddress_2 = ip.GetDestAddr();
            Port_1 = tcp.GetSrcPort();
            Port_2 = tcp.GetDestPort();
        }

        public override bool Equals(object? obj)
        {
            if (obj is TCPIdentifier other)
            {
                return (IPAddress_1 == other.IPAddress_1 && Port_1 == other.Port_1 && IPAddress_2 == other.IPAddress_2 && Port_2 == other.Port_2) ||
                       (IPAddress_1 == other.IPAddress_2 && Port_1 == other.Port_2 && IPAddress_2 == other.IPAddress_1 && Port_2 == other.Port_1);
            }
            return false;
        }

        public override int GetHashCode()
        {
            int hash1 = CombineHashCodes(IPAddress_1.GetHashCode(), Port_1.GetHashCode());
            int hash2 = CombineHashCodes(IPAddress_2.GetHashCode(), Port_2.GetHashCode());

            int sortedHash1 = Math.Min(hash1, hash2);
            int sortedHash2 = Math.Max(hash1, hash2);

            return CombineHashCodes(sortedHash1, sortedHash2);
        }

        private static int CombineHashCodes(int h1, int h2)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + h1;
                hash = hash * 31 + h2;
                return hash;
            }
        }

        public static bool operator ==(TCPIdentifier a, TCPIdentifier b)
        {
            // 如果两个对象都是null，视为相等
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            // 如果其中一个对象是null，则不相等
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
            {
                return false;
            }

            // 使用Equals方法判断内容是否相等
            return a.Equals(b);
        }

        public static bool operator !=(TCPIdentifier a, TCPIdentifier b)
        {
            return !(a == b);
        }

    }
}
