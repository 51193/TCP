using PacketDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace TCP
{
    public class TCPController
    {
        private static Dictionary<TCPIdentifier, TCPControlBlock>? tcpList;
        private WebDevice webDevice;

        private static TCPController? instance;

        public static TCPController Instance
        {
            get
            {
                if(instance == null)
                {
                    instance = new TCPController();
                }
                return instance;
            }
        }

        private TCPController()
        {
            webDevice = WebDevice.Instance;
            tcpList = new Dictionary<TCPIdentifier, TCPControlBlock>();
            PacketParser.tcpList = tcpList;
        }

        public void StartNewConnection(string filename)
        {
            if (tcpList != null)
            {
                TCP tcp = new();
                tcp.SetHeaderFromFile(filename);
                //tcp.SetPayloadFromFile("payload.txt");
                tcp.tcpHeader.SetSeqNumber(99999999);
                tcp.tcpHeader.SetFlag(TCP_FLAG.SYN);
                tcp.Pack();
                tcpList.Add(new TCPIdentifier(tcp), new TCPControlBlock { tcpPacket = tcp, state = TCP_STATE.SYN_SENT });
                tcp.SendPacket();
            }
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

        public void SendDataPacket(string tcpIdentifierFilename, string resourceFile)
        {
            Dictionary<string, string> config = ReadConfiguration(tcpIdentifierFilename);
            var SrcIP = config["SrcIP"];
            var DestIP = config["DestIP"];
            var SrcPort = config["SrcPort"];
            var DestPort = config["DestPort"];

            Dictionary<string, string> resource = ReadConfiguration(resourceFile);
            var url = resource["Resource"];

            var tcpIdentifier = new TCPIdentifier(SrcIP, DestIP, Convert.ToUInt16(SrcPort), Convert.ToUInt16(DestPort));

            if (tcpList != null && tcpList.ContainsKey(tcpIdentifier))
            {
                var tcpCB = tcpList[tcpIdentifier];
                if (tcpCB.state == TCP_STATE.ESTABLISHED)
                {
                    tcpCB.tcpPacket.tcpHeader.ResetFlag();
                    tcpCB.tcpPacket.tcpHeader.SetFlag(TCP_FLAG.PSH | TCP_FLAG.ACK);
                    byte[]? payload = tcpCB.tcpPacket.GetPayload();
                    tcpCB.tcpPacket.SetPayload(CreateHttpRequestBytes(tcpIdentifier.IPAddress_2, tcpIdentifier.Port_2, url));
                    if (payload != null)
                    {
                        tcpCB.tcpPacket.tcpHeader.SetSeqNumber(tcpCB.tcpPacket.tcpHeader.GetSeqNumber() + (uint)payload.Length);
                    }
                    else
                    {
                        tcpCB.tcpPacket.tcpHeader.SetSeqNumber(tcpCB.tcpPacket.tcpHeader.GetSeqNumber());
                    }
                    tcpCB.tcpPacket.Pack();
                    tcpCB.tcpPacket.SendPacket();
                    tcpList[tcpIdentifier] = tcpCB;
                }
            }
        }

        public static string CreateHttpRequestBytes(string ip, int port, string resource = "/")
        {
            string httpRequest = $"GET {resource} HTTP/1.1\r\n" +
                                 $"Host: {ip}:{port}\r\n" +
                                 $"Connection: close\r\n" +
                                 $"User-Agent: CustomClient/1.0\r\n\r\n";

            return httpRequest;
        }


        public void ConnectionStateMove(TCPIdentifier tcpIdentifier, IPHeader ipHeader, TCPHeader tcpHeader, byte[]? payload = null)
        {
            if (tcpList != null)
            {
                var tcpCB = tcpList[tcpIdentifier];
                string fileName;
                string fileContent;

                if (tcpList[tcpIdentifier].tcpPacket.ipHeader.GetSrcAddr() == ipHeader.GetDestAddr())
                {
                    fileContent = $"Received Packet Details:\n" +
                          $"Source IP: {ipHeader.GetSrcAddr()}, Port: {tcpIdentifier.Port_1}\n" +
                          $"Destination IP: {ipHeader.GetDestAddr()}, Port: {tcpIdentifier.Port_2}\n" +
                          $"TCP Flags: {tcpHeader.Flags}\n" +
                          $"Sequence Number: {tcpHeader.GetSeqNumber()}\n" +
                          $"Acknowledgment Number: {tcpHeader.GetAckNumber()}\n";

                    fileName = $"Received_{tcpHeader.GetAckNumber()}_{tcpHeader.GetSeqNumber()}.txt";

                    if (payload != null)
                    {
                        fileContent += $"Payload Length: {payload.Length}\n";
                    }

                    File.WriteAllText(fileName, fileContent);

                    switch (tcpCB.state)
                    {
                        case TCP_STATE.SYN_SENT:
                            if (tcpHeader.Flags == ((byte)TCP_FLAG.SYN | (byte)TCP_FLAG.ACK))
                            {
                                Console.WriteLine("SYN-ACK Packet received, ACK number: " + tcpHeader.GetAckNumber());
                                tcpCB.tcpPacket.tcpHeader.ResetFlag();
                                tcpCB.tcpPacket.tcpHeader.SetFlag(TCP_FLAG.ACK);
                                tcpCB.tcpPacket.tcpHeader.SetSeqNumber(tcpCB.tcpPacket.tcpHeader.GetSeqNumber() + 1);
                                tcpCB.tcpPacket.tcpHeader.SetAckNumber(tcpHeader.GetSeqNumber() + 1);
                                tcpCB.tcpPacket.Pack();
                                tcpCB.tcpPacket.SendPacket();
                                tcpCB.state = TCP_STATE.ESTABLISHED;
                                Console.WriteLine($"Connection to {tcpIdentifier.IPAddress_1}:{tcpIdentifier.Port_1} established");
                                tcpList[tcpIdentifier] = tcpCB;
                            }
                            break;

                        case TCP_STATE.ESTABLISHED:
                            if (tcpHeader.Flags == (byte)TCP_FLAG.ACK)
                            {
                                Console.WriteLine("ACK Packet received, ACK number: " + tcpHeader.GetAckNumber());
                            }

                            if (tcpHeader.Flags == ((byte)TCP_FLAG.PSH | (byte)TCP_FLAG.ACK) && payload != null)
                            {
                                Console.WriteLine("PSH-ACK Packet received, ACK number: " + tcpHeader.GetAckNumber());
                                tcpCB.tcpPacket.tcpHeader.ResetFlag();
                                tcpCB.tcpPacket.tcpHeader.SetFlag(TCP_FLAG.ACK);
                                tcpCB.tcpPacket.tcpHeader.SetSeqNumber(tcpHeader.GetAckNumber());
                                tcpCB.tcpPacket.tcpHeader.SetAckNumber(tcpHeader.GetSeqNumber() + (uint)payload.Length);
                                tcpCB.tcpPacket.ClearPayload();
                                tcpCB.tcpPacket.Pack();
                                tcpCB.tcpPacket.SendPacket();

                                string filePath = $"ReceivedData_{tcpIdentifier.IPAddress_1}_{tcpIdentifier.Port_1}.bin";
                                File.WriteAllBytes(filePath, payload);
                                Console.WriteLine($"Data is saved in {filePath}.");
                                tcpList[tcpIdentifier] = tcpCB;
                            }

                            if (tcpHeader.Flags == (byte)TCP_FLAG.FIN)
                            {
                                Console.WriteLine("FIN Packet received, ACK number: " + tcpHeader.GetAckNumber());
                                tcpCB.tcpPacket.tcpHeader.ResetFlag();
                                tcpCB.tcpPacket.tcpHeader.SetFlag(TCP_FLAG.ACK);
                                tcpCB.tcpPacket.tcpHeader.SetSeqNumber(tcpHeader.GetAckNumber());
                                tcpCB.tcpPacket.tcpHeader.SetAckNumber(tcpHeader.GetSeqNumber() + 1);
                                tcpCB.tcpPacket.Pack();
                                tcpCB.tcpPacket.SendPacket();
                                tcpCB.state = TCP_STATE.CLOSE_WAIT;
                                tcpList[tcpIdentifier] = tcpCB;
                            }

                            if (tcpHeader.Flags == ((byte)TCP_FLAG.FIN | (byte)TCP_FLAG.ACK))
                            {
                                Console.WriteLine("FIN-ACK Packet received, ACK number: " + tcpHeader.GetAckNumber());
                                tcpCB.tcpPacket.tcpHeader.ResetFlag();
                                tcpCB.tcpPacket.tcpHeader.SetFlag(TCP_FLAG.ACK);
                                tcpCB.tcpPacket.tcpHeader.SetSeqNumber(tcpHeader.GetAckNumber());
                                tcpCB.tcpPacket.tcpHeader.SetAckNumber(tcpHeader.GetSeqNumber() + (uint)1);
                                tcpCB.tcpPacket.Pack();
                                tcpCB.tcpPacket.SendPacket();
                                tcpCB.state = TCP_STATE.LAST_ACK;
                                tcpCB.closeSeq = tcpHeader.GetAckNumber();
                                tcpCB.closeAck = tcpHeader.GetSeqNumber() + (uint)1;
                                tcpList[tcpIdentifier] = tcpCB;
                            }
                            break;

                        case TCP_STATE.CLOSE_WAIT:
                            if (tcpHeader.Flags == ((byte)TCP_FLAG.FIN | (byte)TCP_FLAG.ACK))
                            {
                                tcpCB.tcpPacket.tcpHeader.ResetFlag();
                                tcpCB.tcpPacket.tcpHeader.SetFlag(TCP_FLAG.FIN);
                                tcpCB.tcpPacket.tcpHeader.SetSeqNumber(tcpCB.tcpPacket.tcpHeader.GetSeqNumber() + (uint)1);
                                tcpCB.tcpPacket.tcpHeader.SetAckNumber(tcpHeader.GetSeqNumber() + (uint)1);
                                tcpCB.tcpPacket.Pack();
                                tcpCB.tcpPacket.SendPacket();
                                tcpCB.state = TCP_STATE.LAST_ACK;
                                Console.WriteLine("FIN Packet sent, entering LAST_ACK state");
                                tcpList[tcpIdentifier] = tcpCB;
                            }
                            break;

                        case TCP_STATE.LAST_ACK:
                            if (tcpHeader.Flags == (byte)TCP_FLAG.ACK)
                            {
                                Console.WriteLine("Final ACK received. Connection is now closed.");
                                tcpCB.state = TCP_STATE.CLOSE;
                                tcpList[tcpIdentifier] = tcpCB;
                            }
                            break;
                    }
                }
                else
                {
                    //此处是发出去的包
                    fileContent = $"Sent Packet Details:\n" +
                              $"Source IP: {ipHeader.GetSrcAddr()}, Port: {tcpHeader.GetSrcPort()}\n" +
                              $"Destination IP: {ipHeader.GetDestAddr()}, Port: {tcpHeader.GetDestPort()}\n" +
                              $"TCP Flags: {tcpHeader.Flags}\n" +
                              $"Sequence Number: {tcpHeader.GetSeqNumber()}\n";

                    fileName = $"Send_{tcpHeader.GetSeqNumber()}_{tcpHeader.GetAckNumber()}.txt";

                    File.WriteAllText(fileName, fileContent);

                    switch (tcpHeader.Flags)
                    {
                        case (byte)TCP_FLAG.PSH | (byte)TCP_FLAG.ACK:
                            Console.WriteLine("PSH-ACK Packet sent, SEQ number: " + tcpHeader.GetSeqNumber());
                            break;

                        case (byte)TCP_FLAG.SYN:
                            Console.WriteLine("SYN Packet sent, SEQ number: " + tcpHeader.GetSeqNumber());
                            break;

                        case (byte)TCP_FLAG.ACK:
                            Console.WriteLine("ACK Packet sent, SEQ number: " + tcpHeader.GetSeqNumber());
                            if (tcpCB.closeSeq != null && tcpCB.closeSeq == tcpHeader.GetSeqNumber()
                                && tcpCB.closeAck != null && tcpCB.closeAck == tcpHeader.GetAckNumber())
                            {
                                tcpCB.state = TCP_STATE.CLOSE;
                                tcpList[tcpIdentifier] = tcpCB;
                                Console.WriteLine($"Connection to {tcpIdentifier.IPAddress_2}:{tcpIdentifier.Port_2} finished");
                            }
                            break;
                    }
                }
            }
        }
    }
}
