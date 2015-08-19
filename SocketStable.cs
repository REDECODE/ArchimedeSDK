using System;
using Microsoft.SPOT;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace Redecode.Archimede
{
    class SocketStable : Socket
    {
        public SocketStable (AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType) 
            : base(addressFamily, socketType, protocolType) {
                base.SendTimeout = 5000;
                base.ReceiveTimeout = 5000;
        }

        new public bool Connect(EndPoint endPoint)
        {
            bool connected = false;
            int millsConnect = 0;
            int millsThread = 0;

            do
            {
                Thread tConnect = new Thread(() => Socket_Connect(endPoint));
                tConnect.Start();

                //--- tConnect.Join(1000);  <--- it does't work if it's in a Timer callback. Solved with while() below
                millsThread = 0;
                while (tConnect.ThreadState != ThreadState.Stopped && millsThread < 1000)
                {
                    Thread.Sleep(10);
                    millsThread += 10;
                }

                if (tConnect.ThreadState == ThreadState.Stopped)
                {
                    connected = true;
                }
                else
                {
                    millsConnect += 1000;
                }

                tConnect.Abort();

            } while (!connected && millsThread < base.SendTimeout);

            return connected;
        }

        private void Socket_Connect (EndPoint endPoint) {
            base.Connect(endPoint);
        }
    }
}
