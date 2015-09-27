using System;
using Microsoft.SPOT;
using System.IO;
using System.Text;
using GHI.OSHW.Hardware;
using System.Collections;
using Microsoft.SPOT.IO;

namespace Redecode.Archimede
{
    public static class Log
    {
        public static string LogPath { get; private set; }
        public static int LogPathMaxSize;
        public static ArrayList ArrayMessages { get; private set; }
        public static int ArrayMessagesMaxSize;

        public static string StringMessages
        {
            get
            {
                string str = "";
                for (int i = 0; i < ArrayMessages.Count; i++)
                {
                    str += ArrayMessages[i] + "\r\n";
                }
                return str;
            }
        }

        public static void Debug(string message)
        {
            Write(message, "DEBUG");
        }

        public static void Info(string message)
        {
            Write(message, "INFO");
        }

        public static void Warning(string message)
        {
            Write(message, "WARN");
        }

        public static void Error(string message)
        {
            Write(message, "ERROR");
        }

        private static void Write(string message, string type)
        {
            try
            {

                message = type + " " + DateTime.Now.ToString() + " - " + message;

                if (LogPath != null)
                {
                    VolumeInfo SD = VolumeInfo.GetVolumes()[0];
                    FileInfo info = new FileInfo(LogPath);
                    if (info != null && info.Exists && info.Length > LogPathMaxSize)
                    {
                        File.Delete(info.FullName);
                    }

                    using (FileStream fs = new FileStream(info.FullName, FileMode.Append))
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(message + "\r\n");
                        fs.Write(bytes, 0, bytes.Length);
                        fs.Close();                        
                    }

                    SD.FlushAll();
                }

                if (ArrayMessages != null)
                {
                    if (ArrayMessages.Count >= ArrayMessagesMaxSize)
                    {
                        ArrayMessages.RemoveAt(0);
                    }
                    ArrayMessages.Add(message);
                }
            }
            catch
            {
            }
        }

        public static void EnableLogToFile(string path, int max_size = 10000)
        {
            LogPathMaxSize = max_size;
            LogPath = path;
        }

        public static void EnableLogToMemory(int max_size = 100)
        {
            ArrayMessagesMaxSize = max_size;
            ArrayMessages = new ArrayList();
        }
    }
}
