using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Timers;

namespace PrintQueueToSql
{
    class Logger
    {
        public static string logFileName;
        public static bool enabled = int.Parse(ConfigurationManager.AppSettings["loggingEnabled"]) == 0 ? false : true;
        private static readonly int maxLogSize = int.Parse(ConfigurationManager.AppSettings["maxLogSize"]) * 1000;
        private static readonly int interval = int.Parse(ConfigurationManager.AppSettings["maintInterval"]) * 1000;
        private static readonly string path = AppDomain.CurrentDomain.BaseDirectory + "\\";
        private static readonly Timer maintTimer = new Timer();

        static Logger()
        {
            maintTimer.Elapsed += new ElapsedEventHandler(LogMaintenance);
            maintTimer.Interval = interval;
            maintTimer.Enabled = true;
            logFileName = "ServiceLog.txt";
        }

        public static void WriteMessage(string Message)
        {
            if (enabled)
            {
                Message = "[" + DateTime.Now + "] " + Message;
                string filePath = path + logFileName;
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                using (StreamWriter sw = new StreamWriter(filePath, true))
                {
                    sw.WriteLine(Message);
                }
            }
        }

        private static void LogMaintenance(object sender, ElapsedEventArgs e)
        {
            string filePath = path + logFileName;
            if (File.Exists(filePath))
            {
                long length = new FileInfo(filePath).Length;
                if (length > maxLogSize)
                {
                    List<string> lines = new List<string>();
                    using (StreamReader sr = new StreamReader(filePath))
                    {
                        string line = String.Empty;
                        long leftToRemove = length - maxLogSize;
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (leftToRemove > 0)
                            {
                                leftToRemove -= line.Length;
                            }
                            else
                            {

                                lines.Add(line);
                            }
                        }
                    }
                    using (StreamWriter sw = new StreamWriter(filePath, false))
                    {
                        foreach (string line in lines)
                        {
                            sw.WriteLine(line);
                        }
                    }
                }
            }
        }
    }
}
