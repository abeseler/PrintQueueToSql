using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Printing;
using System.ServiceProcess;
using System.Timers;

namespace PrintQueueToSql
{
    public partial class Service : ServiceBase
    {
        private readonly Timer serviceTimer = new Timer();
        private readonly int interval = int.Parse(ConfigurationManager.AppSettings["printerPollInterval"]);
        private readonly string sqlTableName = ConfigurationManager.AppSettings["sqlTableName"];
        private readonly string sqlNameColumn = ConfigurationManager.AppSettings["sqlNameColumn"];
        private readonly string sqlJobsColumn = ConfigurationManager.AppSettings["sqlJobsColumn"];
        private readonly string sqlStatusColumn = ConfigurationManager.AppSettings["sqlStatusColumn"];
        private readonly string sqlSprocNameUpdate = ConfigurationManager.AppSettings["sqlStoredProcedureUpdate"];
        private readonly string sqlParamPrinterName = ConfigurationManager.AppSettings["sqlParamPrinterName"];
        private readonly string sqlParamPrinterStatus = ConfigurationManager.AppSettings["sqlParamPrinterStatus"];
        private readonly string sqlParamJobsInQueue = ConfigurationManager.AppSettings["sqlParamJobsInQueue"];

        struct Printer
        {
            public string name;
            public int jobs;
            public string status;

            public Printer(string name, int jobs, string status)
            {
                this.name = name;
                this.jobs = jobs;
                this.status = status;
            }
        }

        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Logger.WriteMessage("Service has started...");
            serviceTimer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            serviceTimer.Interval = interval;
            serviceTimer.Enabled = true;
        }

        private void OnElapsedTime(object sender, ElapsedEventArgs e)
        {
            try
            {
                using (SqlConnection sqlConn = new SqlConnection(ConfigurationManager.ConnectionStrings["sqlConnectionStr"].ConnectionString))
                {
                    List<Printer> printerList = new List<Printer>();
                    string sqlQuery = $"SELECT {sqlNameColumn},{sqlJobsColumn},{sqlStatusColumn} FROM {sqlTableName} WITH (NOLOCK)";
                    sqlConn.Open();

                    //Get list of printers to poll
                    try
                    {
                        using (SqlCommand cmd = new SqlCommand(sqlQuery, sqlConn))
                        {
                            using (SqlDataReader rdr = cmd.ExecuteReader())
                            {
                                if (rdr.HasRows)
                                {
                                    while (rdr.Read())
                                    {
                                        printerList.Add(new Printer(rdr.GetString(0), rdr.GetInt32(1), rdr.GetString(2)));
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteMessage("[ERROR] Exception occurred retrieving printer list\n" + ex.ToString());
                    }

                    //Poll printer queue for status and jobs and run update sproc for each one
                    if (printerList.Count > 0)
                    {
                        try
                        {
                            using (PrintServer printServer = new LocalPrintServer())
                            {
                                String resultsForLog = "Print queue polling results...\n";

                                foreach (Printer printer in printerList)
                                {
                                    string status;
                                    int jobs;

                                    try
                                    {
                                        PrintQueue pq = printServer.GetPrintQueue(printer.name);
                                        status = pq.QueueStatus.ToString();
                                        jobs = pq.NumberOfJobs;
                                    }
                                    catch (Exception ex)
                                    {
                                        status = "[ERROR] " + ex.Message;
                                        jobs = -1;
                                    }

                                    resultsForLog += "\t" + printer.name + ": " + jobs + ": " + status + "\n";

                                    if (printer.jobs != jobs || printer.status != status)
                                    {
                                        using (SqlCommand cmd = new SqlCommand(sqlSprocNameUpdate, sqlConn))
                                        {
                                            cmd.CommandType = CommandType.StoredProcedure;
                                            cmd.Parameters.AddWithValue(sqlParamPrinterName, printer.name);
                                            cmd.Parameters.AddWithValue(sqlParamPrinterStatus, status);
                                            cmd.Parameters.AddWithValue(sqlParamJobsInQueue, jobs);
                                            cmd.ExecuteNonQuery();
                                        }
                                    }
                                }
                                Logger.WriteMessage(resultsForLog);
                            }
                        }
                        catch (Exception ex1)
                        {
                            Logger.WriteMessage(ex1.ToString());
                        }
                    }

                    sqlConn.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteMessage(ex.ToString());
            }
        }

        protected override void OnStop()
        {
            Logger.WriteMessage("Service has stopped");
        }

        public Boolean IsInstalled()
        {
            Boolean installed = ServiceController.GetServices().Any(s => s.ServiceName == ConfigurationManager.AppSettings["serviceName"]);
            return installed;
        }
    }
}
