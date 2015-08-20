using System;
using Microsoft.SPOT;
using System.IO;
using System.Text;
using GHI.OSHW.Hardware;

namespace Redecode.Archimede
{
    static class Log
    {
        public static string Path;

        public static void Info(string message) {
            WriteToFile(message, "DEBUG");
        }

        public static void Warning(string message)
        {
            WriteToFile(message, "WARN");
        }

        public static void Error(string message)
        {
            WriteToFile(message, "ERROR");
        }

        private static void WriteToFile(string message, string type)
        {
            using (FileStream fs = new FileStream(Path, FileMode.Append))
            {
                DateTime date = RTC.GetTime();
                byte[] bytes = Encoding.UTF8.GetBytes(type + " " + date.ToString() + " - " + message + "\r\n");
                fs.Write(bytes, 0, bytes.Length);
                fs.Close();
            }
        }
    }
}
