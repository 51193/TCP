using SharpPcap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TCP
{
    public class PacketParser
    {
        private static TCPController tcpController = TCPController.Instance;
        public static Dictionary<TCPIdentifier, TCPControlBlock>? tcpList;

        private static WebDevice webDevice = WebDevice.Instance;

        public static void AnalyzePacket(PacketCapture packet)
        {
            var bytes = packet.GetPacket().Data;

            EthernetHeader ethernetHeader = ParseEthernetHeader(bytes);
            if (ethernetHeader.GetEtherType() == 0x0800)
            {
                IPHeader ipHeader = ParseIPHeader(bytes, Marshal.SizeOf(typeof(EthernetHeader)));
                if (ipHeader.Protocol == 6)
                {
                    int ipHeaderLength = (ipHeader.VerIHL & 0x0F) * 4;
                    TCPHeader tcpHeader = ParseTCPHeader(bytes, Marshal.SizeOf(typeof(EthernetHeader)) + ipHeaderLength);

                    TCPIdentifier identifier = new(ipHeader, tcpHeader);

                    int tcpHeaderLength = (tcpHeader.DataOffsetResNS >> 4) * 4;
                    int payloadOffset = tcpHeaderLength + ipHeaderLength + Marshal.SizeOf(typeof(EthernetHeader));
                    int payloadLength = bytes.Length - payloadOffset;
                    byte[]? payload = null;
                    if (payloadLength > 0)
                    {
                        payload = new byte[payloadLength];
                        Array.Copy(bytes, payloadOffset, payload, 0, payloadLength);
                    }

                    if (tcpList != null && tcpList.ContainsKey(identifier) && tcpList[identifier].state != TCP_STATE.CLOSE)
                    {
                        webDevice.RecordPacket(packet.GetPacket());
                        tcpController.ConnectionStateMove(identifier, ipHeader, tcpHeader, payload);
                    }
                }
            }
        }

        private static EthernetHeader ParseEthernetHeader(byte[] data)
        {
            return ByteToStructure<EthernetHeader>(data, 0);
        }

        private static IPHeader ParseIPHeader(byte[] data, int offset)
        {
            return ByteToStructure<IPHeader>(data, offset);
        }

        private static TCPHeader ParseTCPHeader(byte[] data, int offset)
        {
            return ByteToStructure<TCPHeader>(data, offset);
        }

        private static T? ByteToStructure<T>(byte[] data, int offset)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            int size = Marshal.SizeOf(typeof(T));
            if (offset + size > data.Length)
            {
                throw new ArgumentException("Byte array is too small to contain the structure from the specified offset.", nameof(data));
            }

            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(data, offset, ptr, size);
                return Marshal.PtrToStructure<T>(ptr);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
    }

}
