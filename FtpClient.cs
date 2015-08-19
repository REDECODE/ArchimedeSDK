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

        SocketStable SocketCommands;
        SocketStable SocketData;

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

                SocketCommands = new SocketStable(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);                    
                SocketData = null;
                //int passivePort = -1; // Used with socketB when using the data connection

                if (!SocketCommands.Connect(new IPEndPoint(IPAddress.Parse(IP), Port)))
                {
                    SocketCommands.Close();
                    SocketCommands = null;
                    return false;
                }
                    
                rString = readTextFromSocket(SocketCommands);
                sendTextToSocket(SocketCommands, "USER " + Username + "\r\n");
                rString = readTextFromSocket(SocketCommands);
                sendTextToSocket(SocketCommands, "PASS " + Password + "\r\n");
                rString = readTextFromSocket(SocketCommands);

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

        public bool DownloadFile(string remote_path, string local_path)
        {
            try
            {
                byte[] fileContent = ReadBinaryFile(remote_path);
                if (fileContent == null)
                {
                    return false;
                }

                FileStream FileHandle = new FileStream(local_path, FileMode.OpenOrCreate);
                FileHandle.Write(fileContent, 0, fileContent.Length);
                FileHandle.Close();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public string ReadTextFile(string remote_path)
        {
            try
            {
                string data;
                string rString = "";           
                sendTextToSocket(SocketCommands, "TYPE A\r\n"); // Not sure if necessary, but FileZilla client does this
                rString = readTextFromSocket(SocketCommands);
                using (SocketData = SocketPassiveMode())
                {
                    sendTextToSocket(SocketCommands, "RETR " + remote_path + "\r\n");
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

        public byte[] ReadBinaryFile(string remote_path)
        {
            try
            {
                string rString = "";
                byte[] data;
                sendTextToSocket(SocketCommands, "TYPE I\r\n"); // Not sure if necessary, but FileZilla client does this
                rString = readTextFromSocket(SocketCommands);
                using (SocketData = SocketPassiveMode())
                {                    
                    sendTextToSocket(SocketCommands, "RETR " + remote_path + "\r\n");
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

        public bool UploadFile(string local_path, string remote_path)
        {
            try
            {                
                byte[] fileContent = File.ReadAllBytes(local_path);

                WriteBinaryFile(remote_path, fileContent);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool WriteTextFile(string remote_path, string data)
        {
            try
            {
                string rString;
                sendTextToSocket(SocketCommands, "TYPE A\r\n"); // Not sure if necessary, but FileZilla client does this
                rString = readTextFromSocket(SocketCommands);
                using (SocketData = SocketPassiveMode())
                {
                    sendTextToSocket(SocketCommands, "STOR " + remote_path + ".txt\r\n");
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

        public bool WriteBinaryFile(string remote_path, byte[] data)
        {
            try
            {
                string rString;
                sendTextToSocket(SocketCommands, "TYPE I\r\n"); // Not sure if necessary, but FileZilla client does this
                rString = readTextFromSocket(SocketCommands);

                using (SocketData = SocketPassiveMode())
                {
                    sendTextToSocket(SocketCommands, "STOR " + remote_path + "\r\n");
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

        private static void sendTextToSocket(SocketStable sock, string data)
        {
            sendBytesToSocket(sock, Encoding.UTF8.GetBytes(data));
        }

        private static void sendBytesToSocket(SocketStable sock, byte[] data)
        {
            if (sock.Poll(1000, SelectMode.SelectWrite))
            {
                int numSent = sock.Send(data);
            }
        }

        private SocketStable SocketPassiveMode()
        {
            string rString;
            sendTextToSocket(SocketCommands, "PASV\r\n");
            rString = readTextFromSocket(SocketCommands);
            int passivePort = parseForPasvPort(rString);
            SocketStable socket = new SocketStable(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
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

    }
}
