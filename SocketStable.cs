using System;
using Microsoft.SPOT;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace Redecode.Archimede
{
    public class SocketStable
    {

        public static Socket Connect(string ip, int port, int timeout = 5000)
        {
            bool connected = false;
            int millsConnect = 0;
            int millsThread = 0;
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            Socket socket = null;

            do
            {
                try
                {
                    socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    Thread tConnect = new Thread(() =>
                    {
                        try
                        {
                            socket.Connect(endPoint);
                            connected = true;
                        }
                        catch
                        {
                            return;
                        }
                    });
                    tConnect.Start();

                    //--- tConnect.Join(1000);  <--- it does't work if it's in a Timer callback. Solved with while() below
                    millsThread = 0;
                    while (tConnect.IsAlive && millsThread < 1000)
                    {
                        Thread.Sleep(10);
                        millsThread += 10;
                    }

                    tConnect.Abort();
                }
                catch
                {

                }
                finally
                {
                    if (!connected)
                    {
                        if (socket != null)
                        {
                            socket.Close();
                        }
                        millsConnect += 1000;
                        connected = false;
                    }
                }

            } while (!connected && millsConnect < timeout);

            if (connected)
            {
                return socket;
            }
            else
            {
                return null;
            }
        }
    }
}
