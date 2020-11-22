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
        private readonly string sqlSprocNameList = ConfigurationManager.AppSettings["sqlStoredProcedureList"];
        private readonly string sqlSprocNameUpdate = ConfigurationManager.AppSettings["sqlStoredProcedureUpdate"];
        private readonly string sqlParamPrinterName = ConfigurationManager.AppSettings["sqlParamPrinterName"];
        private readonly string sqlParamPrinterStatus = ConfigurationManager.AppSettings["sqlParamPrinterStatus"];
        private readonly string sqlParamJobsInQueue = ConfigurationManager.AppSettings["sqlParamJobsInQueue"];
        private int poll_counter = 0;

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
            Logger.AddMessage("SERVICE_STARTED");
            serviceTimer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            serviceTimer.Interval = int.Parse(ConfigurationManager.AppSettings["printerPollInterval"]);
            serviceTimer.Start();
        }

        private void OnElapsedTime(object sender, ElapsedEventArgs e)
        {
            serviceTimer.Stop();
            poll_counter = poll_counter == 999 ? 1 : poll_counter + 1;

            try
            {
                using (SqlConnection sqlConn = new SqlConnection(ConfigurationManager.ConnectionStrings["sqlConnectionStr"].ConnectionString))
                {
                    List<Printer> printerList = new List<Printer>();
                    sqlConn.Open();

                    //Get list of printers to poll
                    try
                    {
                        using (SqlCommand cmd = new SqlCommand(sqlSprocNameList, sqlConn) { CommandType = CommandType.StoredProcedure })
                        {
                            using (SqlDataReader rdr = cmd.ExecuteReader())
                            {
                                if (rdr.HasRows)
                                {
                                    while (rdr.Read())
                                    {
                                        printerList.Add(
                                            new Printer(
                                                Convert.ToString(rdr.GetValue(0))           // Printer Name
                                                ,rdr.IsDBNull(1) ? -1 : rdr.GetInt32(1)     // Number of Jobs
                                                ,Convert.ToString(rdr.GetValue(2))          // Status message
                                            )
                                        ) ;
                                    }
                                }
                                else
                                {
                                    Logger.AddMessage($"POLLING_CANCELLED_{poll_counter}: {sqlSprocNameList} returned no rows");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.AddMessage("EXCEPTION_GET_PRINTER_LIST_FAILED\n" + ex.ToString());
                    }

                    //Poll printer queue for status and jobs and run update sproc for each one if changed
                    if (printerList.Count > 0)
                    {
                        try
                        {
                            using (PrintServer printServer = new LocalPrintServer())
                            {
                                Logger.AddMessage($"POLLING_OPERATION_STARTING_{poll_counter}");

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
                                        status = $"Error: {ex.Message}";
                                        jobs = -1;
                                    }

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

                                        Logger.AddMessage($"[UPDT] {printer.name}: {jobs}: {status}");
                                    }
                                    else
                                    {
                                        Logger.AddMessage($"[POLL] {printer.name}: {jobs}: {status}");
                                    }
                                }

                                Logger.AddMessage($"POLLING_OPERATION_COMPLETE_{poll_counter}");
                            }
                        }
                        catch (Exception ex1)
                        {
                            Logger.AddMessage($"EXCEPTION_USING_PRINTSERVER_{poll_counter}\n" + ex1.ToString());
                        }
                    }

                    sqlConn.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.AddMessage("EXCEPTION_SERVICE_ONELAPSEDTIME\n" + ex.ToString());
            }

            serviceTimer.Start();
        }

        protected override void OnStop()
        {
            Logger.WriteMessageNoWait("SERVICE_STOPPED");
        }

        public Boolean IsInstalled()
        {
            Boolean installed = ServiceController.GetServices().Any(s => s.ServiceName == ConfigurationManager.AppSettings["serviceName"]);
            return installed;
        }
    }
}
