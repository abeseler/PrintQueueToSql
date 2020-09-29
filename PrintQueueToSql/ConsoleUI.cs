using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;

namespace PrintQueueToSql
{
    class ConsoleUI
    {
        private Boolean running = true;
        private readonly Service service = new Service();
        public void Start()
        {
            Logger.WriteMessage("Started in Console");

            while (running)
            {
                DisplayMenu();
                Console.Write("> ");
                string input = Console.ReadLine();
                Console.Clear();
                string message = HandleInput(input);
                Console.WriteLine(message);
                Console.WriteLine(" ");
            }
        }

        private void DisplayMenu()
        {
            List<string> menu = new List<string>
            {
                "Options:"
                ," "
            };
            if (service.IsInstalled())
            {
                menu.Add(" 1 - Uninstall");
            }
            else
            {
                menu.Add(" 1 - Install");
            }
            menu.Add(" 2 - Exit");
            menu.Add(" ");
            foreach (string item in menu)
            {
                Console.WriteLine(item);
            }
        }

        private string HandleInput(string input)
        {
            string message = "";
            switch (input)
            {
                case "1":
                    if (service.IsInstalled())
                    {
                        Console.WriteLine("Removing Windows Service...");
                        Logger.WriteMessage("Removing Windows Service...");
                    }
                    else
                    {
                        Console.WriteLine("Installing Windows Service...");
                        Logger.WriteMessage("Installing Windows Service...");
                    }
                    try
                    {
                        Process proc = InstallProcess(service.IsInstalled());
                        proc.Start();
                        proc.WaitForExit();
                        int result = proc.ExitCode;
                        if (result == 0)
                        {
                            message = "The operation completed successfully";
                            Logger.WriteMessage(message);
                        }
                        else
                        {
                            message = "The operation failed with return code " + result.ToString();
                            Logger.WriteMessage(message);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteMessage(ex.ToString());
                    }
                    break;
                case "2":
                    Logger.WriteMessage("Exiting console");
                    running = false;
                    break;
                default:
                    message = input + " is not a valid option.";
                    break;
            }
            return message;
        }

        private Process InstallProcess(Boolean svcExists)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = ConfigurationManager.AppSettings["dotnetDir"] + "\\InstallUtil.exe";
            proc.StartInfo.Arguments = "\"" + Assembly.GetEntryAssembly().Location + "\"";
            proc.StartInfo.Arguments += " /LogFile=PrintQueueToSql.InstallLog";
            proc.StartInfo.UseShellExecute = true;
            proc.StartInfo.Verb = "runas";
            if (svcExists) { proc.StartInfo.Arguments += " /u"; }
            return proc;
        }
    }
}
