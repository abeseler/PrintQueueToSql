using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Timers;

namespace PrintQueueToSql
{
    class Logger
    {
        private static Queue<string> messages = new Queue<string>();
        private static int logSize = 0;
        private static bool enabled = int.Parse(ConfigurationManager.AppSettings["loggingEnabled"]) == 0 ? false : true;
        private static readonly int maxLogSize = int.Parse(ConfigurationManager.AppSettings["maxLogSize"]) * 1000;
        private static readonly string filePath = Environment.UserInteractive ? AppDomain.CurrentDomain.BaseDirectory + "\\Log_Console.txt" : AppDomain.CurrentDomain.BaseDirectory + "\\Log_Service.txt";

        static Logger()
        {
            if (File.Exists(filePath))
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                        string line = String.Empty;
                        while ((line = sr.ReadLine()) != null)
                        {
                            messages.Enqueue(line);
                            logSize += line.Length;
                        }
                }
            }
        }

        public static void WriteMessage(string Message)
        {
            if (enabled)
            {
                Message = "[" + DateTime.Now + "] " + Message;
                messages.Enqueue(Message);
                logSize += Message.Length;

                while (logSize > maxLogSize)
                {
                    Message = messages.Dequeue();
                    logSize -= Message.Length;
                }

                using (StreamWriter sw = new StreamWriter(filePath, false))
                {
                    foreach (string line in messages)
                    {
                        sw.WriteLine(line);
                    }
                }
            }
        }
    }
}
