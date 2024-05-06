using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCP
{
    public enum TCP_STATE : uint
    {
        CLOSE,
        LISTEN,
        SYN_SENT,
        SYN_RECEIVED,
        ESTABLISHED,
        CLOSE_WAIT,
        LAST_ACK,
        FIN_WAIT_1,
        FIN_WAIT_2,
        TIME_WAIT,
        CLOSING
    }

    public struct TCPControlBlock
    {
        public TCP tcpPacket;
        public TCP_STATE state;
        public Queue<string> messageQueue;
        public uint? closeSeq;
        public uint? closeAck;
    }
}
