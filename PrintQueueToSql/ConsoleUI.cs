using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Printing;
using System.Reflection;

namespace PrintQueueToSql
{
    class ConsoleUI
    {
        private Boolean running = true;
        private readonly Service service = new Service();
        public void Start()
        {
            Logger.AddMessage("Started in console");

            while (running)
            {
                DisplayMenu();
                Console.Write("> ");
                string input = Console.ReadLine();
                Console.Clear();
                string message = HandleInput(input);
                Console.WriteLine(message+"\n");
            }
        }

        private void DisplayMenu()
        {
            List<string> menu = new List<string>
            {
                "Options:\n"
            };
            if (service.IsInstalled())
            {
                menu.Add(" 1 - Uninstall");
            }
            else
            {
                menu.Add(" 1 - Install");
            }
            menu.Add(" 2 - List Printers");
            menu.Add(" 3 - Exit\n");
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
                        Logger.AddMessage("Removing Windows Service...");
                    }
                    else
                    {
                        Console.WriteLine("Installing Windows Service...");
                        Logger.AddMessage("Installing Windows Service...");
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
                            Logger.AddMessage(message);
                        }
                        else
                        {
                            message = "The operation failed with return code " + result.ToString();
                            Logger.AddMessage(message);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.AddMessage(ex.ToString());
                    }
                    break;
                case "2":
                    ListPrinterQueuesToConsole();
                    break;
                case "3":
                    Logger.WriteMessageNoWait("Exiting console");
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

        private static void ListPrinterQueuesToConsole()
        {
            Logger.AddMessage("LISTING_PRINTERS");

            try
            {
                using (PrintServer printServer = new LocalPrintServer())
                {
                    List<PrintQueue> printers = printServer.GetPrintQueues().ToList();
                    foreach (PrintQueue printer in printers)
                    {
                        Console.WriteLine($"{printer.Name} | {printer.NumberOfJobs} | {printer.QueueStatus}");
                        Logger.AddMessage($"{printer.Name} | {printer.NumberOfJobs} | {printer.QueueStatus}");
                    }
                }
            }
            catch (Exception ex1)
            {
                Logger.AddMessage($"EXCEPTION_USING_PRINTSERVER\n" + ex1.ToString());
            }
        }
    }
}
