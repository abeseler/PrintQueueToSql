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
        private static bool changed = false;
        private static readonly bool enabled = int.Parse(ConfigurationManager.AppSettings["loggingEnabled"]) == 0 ? false : true;
        private static readonly int maxLogSize = int.Parse(ConfigurationManager.AppSettings["maxLogSize"]) * 1000;
        private static readonly string filePath = Environment.UserInteractive ? AppDomain.CurrentDomain.BaseDirectory + "\\Log_Console.txt" : AppDomain.CurrentDomain.BaseDirectory + "\\Log_Service.txt";
        private static readonly Timer saveLogTimer = new Timer();

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
            saveLogTimer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            saveLogTimer.Interval = int.Parse(ConfigurationManager.AppSettings["logWriteInterval"]);
            saveLogTimer.Start();
        }

        public static void AddMessage(string Message)
        {
            if (enabled)
            {
                try
                {
                    Message = "[" + DateTime.Now + "] " + Message;
                    messages.Enqueue(Message);
                    logSize += Message.Length;

                    while (logSize > maxLogSize)
                    {
                        Message = messages.Dequeue();
                        logSize -= Message.Length;
                    }

                    changed = true;
                }
                catch(Exception ex)
                {
                    messages.Enqueue("EXCEPTION_LOGGER_ADDMESSAGE\n" + ex.ToString());
                }
            }
        }

        public static void WriteMessageNoWait(string Message)
        {
            if (enabled)
            {
                saveLogTimer.Stop();

                try
                {
                    Message = "[" + DateTime.Now + "] " + Message;
                    messages.Enqueue(Message);
                    logSize += Message.Length;

                    using (StreamWriter sw = new StreamWriter(filePath, true))
                    {
                        sw.WriteLine(Message);
                    }
                }
                catch (Exception ex)
                {
                    messages.Enqueue("EXCEPTION_LOGGER_WRITEMESSAGE\n" + ex.ToString());
                }
            }
        }

        private static void OnElapsedTime(object sender, ElapsedEventArgs e)
        {
            saveLogTimer.Stop();

            try
            {
                if (changed)
                {
                    changed = false;
                    
                    using (StreamWriter sw = new StreamWriter(filePath, false))
                    {
                        string[] logMessages = new string[messages.Count];
                        messages.CopyTo(logMessages, 0);

                        foreach (string line in logMessages)
                        {
                            sw.WriteLine(line);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                messages.Enqueue("EXCEPTION_LOGGER_ONELAPSEDTIME\n" + ex.ToString());
            }

            saveLogTimer.Start();
        }
    }
}