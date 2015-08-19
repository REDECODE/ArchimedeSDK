using System;
using Microsoft.SPOT;
using System.Net.Sockets;
using System.Net;
using Microsoft.SPOT.Net.NetworkInformation;
using System.Text;
using System.Collections;
using System.IO;
using System.Threading;


namespace Redecode.Archimede
{
    //-- For Raw FTP command see http://www.nsftools.com/tips/RawFTP.htm
    public class FtpClient
    {
        public string IP;
        public int Port;

        public string Username;
        public string Password;

        Socket SocketCommands;
        Socket SocketData;

        public FtpClient()
        {
        }

        public FtpClient(string ip, int port)
        {
            IP = ip;
            Port = port;
        }

        public FtpClient(string ip, int port, string username, string password)
        {
            IP = ip;
            Port = port;
            Username = username;
            Password = password;
        }

        public bool Connect()
        {
            try
            {
                bool connected = false;
                string rString = "";

                Ethernet.Connect();


                do
                {
                    SocketCommands = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);                    
                    SocketData = null;
                    //int passivePort = -1; // Used with socketB when using the data connection

                    IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(IP), Port);
                    Thread tConnect = new Thread(() => Socket_Connect(SocketCommands, ipep));
                    tConnect.Start();
                    tConnect.Join(1000);

                    if (tConnect.ThreadState == ThreadState.Stopped)
                    {
                        connected = true;                        
                        // Read welcome message and return
                        rString = readTextFromSocket(SocketCommands);

                        // Login to the server
                        sendTextToSocket(SocketCommands, "USER " + Username + "\r\n");
                        rString = readTextFromSocket(SocketCommands);
                        sendTextToSocket(SocketCommands, "PASS " + Password + "\r\n");
                        rString = readTextFromSocket(SocketCommands);

                        // Send command Passive Mode
                        //sendTextToSocket(SocketCommands, "PASV\r\n");
                        //rString = readTextFromSocket(SocketCommands);
                        //passivePort = parseForPasvPort(rString); // Server sends port to connect on
                    }

                    tConnect.Abort();
                    
                } while (!connected);

                return connected;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public void Disconnect()
        {
            string rString;
            try
            {
                // All done so send the QUIT command to log out
                sendTextToSocket(SocketCommands, "QUIT\r\n");
                rString = readTextFromSocket(SocketCommands);
            }
            catch
            {
            }
            if (SocketCommands != null)
            {
                SocketCommands.Close();
            }
            if (SocketData != null)
            {
                SocketData.Close();
            }
        }

        public string[] ListDirectory()
        {
            string rString;
            sendTextToSocket(SocketCommands, "NLST\r\n");
            using (SocketData = SocketPassiveMode())
            {
                rString = readTextFromSocket(SocketData); // READING FROM SOCKET B NOW!
                SocketData.Close();
            }

            string[] list = rString.Split('\n');
            for (int i = 0; i < list.Length; i++)
            {
                list[i] = list[i].Trim();
            }

            

            return list;
            //throw new Exception("Not implemented");
        }

        public bool DownloadFile(string path)
        {
            string fileContent = ReadTextFile(path);
            if (fileContent != null)
            {

            }

            return true;
        }

        public string ReadTextFile(string path)
        {
            try
            {
                string data;
                string rString = "";           
                sendTextToSocket(SocketCommands, "TYPE A\r\n"); // Not sure if necessary, but FileZilla client does this
                rString = readTextFromSocket(SocketCommands);
                using (SocketData = SocketPassiveMode())
                {
                    sendTextToSocket(SocketCommands, "RETR " + path + "\r\n");
                    rString = readTextFromSocket(SocketCommands);
                    data = readTextFromSocket(SocketData);
                    SocketData.Close();
                }
                return data;
            }
            catch
            {
                return null;
            }

        }

        public byte[] ReadBinaryFile(string path)
        {
            try
            {
                string rString = "";
                byte[] data;
                sendTextToSocket(SocketCommands, "TYPE I\r\n"); // Not sure if necessary, but FileZilla client does this
                rString = readTextFromSocket(SocketCommands);
                using (SocketData = SocketPassiveMode())
                {                    
                    sendTextToSocket(SocketCommands, "RETR " + path + "\r\n");
                    rString = readTextFromSocket(SocketCommands);
                    data = readBytesFromSocket(SocketData);
                    SocketData.Close();
                }
                return data;
            }
            catch
            {
                return null;
            }
        }

        public bool UploadFile(string path)
        {
            try
            {
                string rString;
                sendTextToSocket(SocketCommands, "TYPE A\r\n"); // Not sure if necessary, but FileZilla client does this
                rString = readTextFromSocket(SocketCommands);
                using (SocketData = SocketPassiveMode())
                {
                    sendTextToSocket(SocketCommands, "STOR madeOnMCU.txt\r\n");
                    rString = readTextFromSocket(SocketCommands);
                    sendTextToSocket(SocketData, "This was made on the MCU.");
                    SocketData.Close();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool WriteTextFile(string path, string data)
        {
            try
            {
                string rString;
                sendTextToSocket(SocketCommands, "TYPE A\r\n"); // Not sure if necessary, but FileZilla client does this
                rString = readTextFromSocket(SocketCommands);
                using (SocketData = SocketPassiveMode())
                {
                    sendTextToSocket(SocketCommands, "STOR " + path + ".txt\r\n");
                    rString = readTextFromSocket(SocketCommands);
                    sendTextToSocket(SocketData, data);
                    SocketData.Close();
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool WriteBinaryFile(string path, byte[] data)
        {
            try
            {
                string rString;
                sendTextToSocket(SocketCommands, "TYPE I\r\n"); // Not sure if necessary, but FileZilla client does this
                rString = readTextFromSocket(SocketCommands);
                using (SocketData = SocketPassiveMode())
                {
                    sendTextToSocket(SocketCommands, "STOR " + path + "\r\n");
                    rString = readTextFromSocket(SocketCommands);
                    sendBytesToSocket(SocketData, data);
                    SocketData.Close();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string readTextFromSocket(Socket sock)
        {
            return new string(Encoding.UTF8.GetChars(readBytesFromSocket(sock)));
        }

        private static byte[] readBytesFromSocket(Socket sock)
        {
            //string receivedString = "";
            int bytesAvailToRead = 0;
            byte[] buffer = null;
            var receivedBytes = new MemoryStream();

            int waitCount = 0;
            // The socket did not always have bytes immediately
            // available to read, so I wait a bit until it is
            // ready. This could be changed/shortened. Almost no
            // testing was done here. to optimize.
            while (waitCount < 10 && sock.Available == 0)
            {
                System.Threading.Thread.Sleep(100);
                waitCount++;
            }

            while (sock.Available > 0)
            {
                if (sock.Poll(1000, SelectMode.SelectRead))
                {


                    bytesAvailToRead = sock.Available;
                    buffer = new byte[bytesAvailToRead];
                    sock.Receive(buffer, bytesAvailToRead, SocketFlags.None);
                    receivedBytes.Write(buffer, 0, buffer.Length);
                }
            }

            return receivedBytes.ToArray();
        }

        private static void sendTextToSocket(Socket sock, string data)
        {
            sendBytesToSocket(sock, Encoding.UTF8.GetBytes(data));
        }

        private static void sendBytesToSocket(Socket sock, byte[] data)
        {
            if (sock.Poll(1000, SelectMode.SelectWrite))
            {
                int numSent = sock.Send(data);
            }
        }

        private Socket SocketPassiveMode()
        {
            string rString;
            sendTextToSocket(SocketCommands, "PASV\r\n");
            rString = readTextFromSocket(SocketCommands);
            int passivePort = parseForPasvPort(rString);
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(new IPEndPoint(IPAddress.Parse(IP), passivePort));
            return socket;
        }

        private static int parseForPasvPort(string s)
        {
            // Probably only works for FileZilla servers.
            // There are suggestions online for how to read/parse
            // for a passive port number online. For example,
            // here: <a href="http://cr.yp.to/ftp/retr.html" target="_blank" rel="nofollow">http://cr.yp.to/ftp/retr.html</a>
            int i, j;
            i = s.IndexOf('(');
            j = s.IndexOf(')');
            string ip = s.Substring(i + 1, j - i - 1);
            string[] pieces = ip.Split(',');

            return System.Convert.ToInt32(pieces[pieces.Length - 2]) * 256 + System.Convert.ToInt32(pieces[pieces.Length - 1]);
        }

        private static void Socket_Connect(Socket socket, IPEndPoint ipep)
        {
            //Thread.Sleep(10);
            socket.Connect(ipep);
        }

    }
}
