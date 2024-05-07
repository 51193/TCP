using SharpPcap;
using System.Runtime.InteropServices;
using System.Text;

namespace TCP
{
    public class TCP
    {
        private WebDevice webDevice;

        public EthernetHeader ethernetHeader;
        public IPHeader ipHeader;
        public TCPHeader tcpHeader;
        private byte[]? payload;

        private byte[]? packet;
        public TCP()
        {
            webDevice = WebDevice.Instance;
        }

        public void SetHeaderFromFile(string filePath)
        {
            Dictionary<string, string> config = ReadConfiguration(filePath);
            var SrcMAC = config["SrcMAC"];
            var DestMAC = config["DestMAC"];
            var SrcIP = config["SrcIP"];
            var DestIP = config["DestIP"];
            var SrcPort = config["SrcPort"];
            var DestPort = config["DestPort"];

            Random ran = new Random();
            SrcPort = Convert.ToString(ran.Next(10000, 65535));


            string fileContent = $"SrcMAC: {SrcMAC}\n" +
                $"DestMAC: {DestMAC}\n" +
                $"SrcIP: {SrcIP}\n" +
                $"DestIP: {DestIP}\n" +
                $"SrcPort: {SrcPort}\n" +
                $"DestPort: {DestPort}\n";

            File.WriteAllText(filePath, fileContent);

            ethernetHeader = new(SrcMAC, DestMAC, 0x0800);
            ipHeader = new(SrcIP, DestIP);
            tcpHeader = new(Convert.ToUInt16(SrcPort), Convert.ToUInt16(DestPort));
        }

        public void SetPayloadFromFile(string filePath)
        {
            Dictionary<string, string> pl = ReadConfiguration(filePath);
            var content = pl["Content"];

            payload = Encoding.UTF8.GetBytes(content);
        }

        public void ClearPayload()
        {
            payload = null;
        }

        public void SetPayload(string pl)
        {
            payload = Encoding.UTF8.GetBytes(pl);
        }

        public byte[]? GetPayload()
        {
            return payload;
        }

        public void Pack()
        {
            if (payload == null)
            {
                ipHeader.SetTotalLength((ushort)40);
            }
            else
            {
                ipHeader.SetTotalLength((ushort)(40 + payload.Length));
            }

            ipHeader.SetChecksum(CalculateIPChecksum());
            tcpHeader.SetChecksum(CalculateTCPChecksum());

            byte[] ethernetHeaderBytes = StructureToByte(ethernetHeader);
            byte[] ipHeaderBytes = StructureToByte(ipHeader);
            byte[] tcpHeaderBytes = StructureToByte(tcpHeader);

            if (payload != null)
            {
                packet = new byte[ethernetHeaderBytes.Length + ipHeaderBytes.Length + tcpHeaderBytes.Length + payload.Length];
            }
            else
            {
                packet = new byte[ethernetHeaderBytes.Length + ipHeaderBytes.Length + tcpHeaderBytes.Length];
            }

            Buffer.BlockCopy(ethernetHeaderBytes, 0, packet, 0, ethernetHeaderBytes.Length);
            Buffer.BlockCopy(ipHeaderBytes, 0, packet, ethernetHeaderBytes.Length, ipHeaderBytes.Length);
            Buffer.BlockCopy(tcpHeaderBytes, 0, packet, ethernetHeaderBytes.Length + ipHeaderBytes.Length, tcpHeaderBytes.Length);

            if (payload != null)
            {
                Buffer.BlockCopy(payload, 0, packet, ethernetHeaderBytes.Length + ipHeaderBytes.Length + tcpHeaderBytes.Length, payload.Length);
            }

            /*Console.WriteLine("MAC头长度:" + ethernetHeaderBytes.Length + "，IP头长度:" + ipHeaderBytes.Length + "，TCP头长度:" + tcpHeaderBytes.Length);
            Console.WriteLine("MAC头:" + BitConverter.ToString(ethernetHeaderBytes));
            Console.WriteLine("IP头:" + BitConverter.ToString(ipHeaderBytes));
            Console.WriteLine("TCP头:" + BitConverter.ToString(tcpHeaderBytes));
            Console.WriteLine(BitConverter.ToString(packet, 0, packet.Length));*/
        }

        public void SendPacket()
        {
            if (packet != null)
            {
                webDevice.SendPacket(packet);
            }
            else
            {
                Console.WriteLine("Packet is not built or is empty.");
            }
        }

        public void SetCaputureFile(string filename)
        {
            webDevice.SetRecordFile(filename);
        }

        public void StartCapture()
        {
            webDevice.StartCapture();
        }

        public void StopCapture()
        {
            webDevice.EndCapture();
        }

        private Dictionary<string, string> ReadConfiguration(string filePath)
        {
            var config = new Dictionary<string, string>();
            foreach (var line in File.ReadAllLines(filePath))
            {
                var parts = line.Split(':');
                if (parts.Length == 2)
                {
                    config[parts[0].Trim()] = parts[1].Trim();
                }
            }
            return config;
        }

        private static byte[] StructureToByte(object? structureObject)
        {
            if (structureObject != null)
            {
                int size = Marshal.SizeOf(structureObject);
                byte[] bytes = new byte[size];

                IntPtr ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(structureObject, ptr, true);
                Marshal.Copy(ptr, bytes, 0, size);
                Marshal.FreeHGlobal(ptr);
                return bytes;
            }
            else
            {
                throw new ArgumentNullException(nameof(structureObject));
            }
        }

        private ushort CalculateChecksum(byte[] data)
        {
            long sum = 0;
            int length = data.Length;
            for (int i = 0; i < length - 1; i += 2)
            {
                sum += (data[i] << 8) + data[i + 1];
            }

            if (length % 2 != 0)
            {
                sum += data[length - 1] << 8;
            }

            sum = (sum >> 16) + (sum & 0xffff);
            sum += (sum >> 16);

            return (ushort)~sum;
        }

        private ushort CalculateIPChecksum()
        {
            ipHeader.SetChecksum(0);
            return CalculateChecksum(StructureToByte(ipHeader));
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct PseudoHead
        {
            [FieldOffset(0)] public uint SourceAddress;  // 32位源IP地址
            [FieldOffset(4)] public uint DestinationAddress;  // 32位目的IP地址
            [FieldOffset(8)] public byte Reserved;  // 8位保留字段，总是0
            [FieldOffset(9)] public byte Protocol;  // 8位协议号，对于TCP是6
            [FieldOffset(10)] public ushort TCPLength;  // 16位TCP长度（包括TCP头和数据长度）
        }
        private ushort CalculateTCPChecksum()
        {
            tcpHeader.SetChecksum(0);
            ushort payloadLength = 0;
            if(payload != null)
            {
                payloadLength = (ushort)payload.Length;
            }

            var pseudoHeader = new PseudoHead
            {
                SourceAddress = ipHeader.SrcAddr,
                DestinationAddress = ipHeader.DestAddr,
                Reserved = 0,
                Protocol = ipHeader.Protocol,
                TCPLength = htons((ushort)(20 + payloadLength))
            };

            byte[] pseudoHeaderBytes = StructureToByte(pseudoHeader);
            byte[] tcpHeaderBytes = StructureToByte(tcpHeader);
            byte[] tcpBytes = new byte[pseudoHeaderBytes.Length + tcpHeaderBytes.Length + payloadLength];

            Buffer.BlockCopy(pseudoHeaderBytes, 0, tcpBytes, 0, pseudoHeaderBytes.Length);
            Buffer.BlockCopy(tcpHeaderBytes, 0, tcpBytes, pseudoHeaderBytes.Length, tcpHeaderBytes.Length);
            if(payload != null)
            {
                Buffer.BlockCopy(payload, 0, tcpBytes, pseudoHeaderBytes.Length + tcpHeaderBytes.Length, payload.Length);
            }

            return CalculateChecksum(tcpBytes);
        }

        private static ushort htons(ushort host)
        {
            byte[] bytes = BitConverter.GetBytes(host);
            return (ushort)((bytes[0] << 8) | bytes[1]);
        }
    }
}
