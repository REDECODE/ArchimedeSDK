using System;
using Microsoft.SPOT;
using System.IO;
using System.Text;
using GHI.OSHW.Hardware;
using System.Collections;

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
            Write(message, "INFO");
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

                DateTime date = RTC.GetTime();
                message = type + " " + date.ToString() + " - " + message;

                if (LogPath != null)
                {
                    FileInfo info = new FileInfo(LogPath);
                    if (info.Length > LogPathMaxSize)
                    {
                        File.Delete(LogPath);
                    }

                    using (FileStream fs = new FileStream(LogPath, FileMode.Append))
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(message + "\r\n");
                        fs.Write(bytes, 0, bytes.Length);
                        fs.Close();
                    }
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

        public static void EnableLogToMemory(int max_size = 1000)
        {
            ArrayMessagesMaxSize = max_size;
            ArrayMessages = new ArrayList();
        }
    }
}
