using System.Configuration;
using System.Configuration.Install;
using System.Reflection;
using System.ServiceProcess;

namespace PrintQueueToSql
{
    partial class Installer
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.ServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.ServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // ServiceProcessInstaller
            // 
            this.ServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.ServiceProcessInstaller.Password = null;
            this.ServiceProcessInstaller.Username = null;
            this.ServiceProcessInstaller.AfterInstall += new System.Configuration.Install.InstallEventHandler(this.ServiceProcessInstaller_AfterInstall);
            // 
            // ServiceInstaller
            // 
            this.ServiceInstaller.ServiceName = ConfigurationManager.OpenExeConfiguration(Assembly.GetAssembly(typeof(Installer)).Location).AppSettings.Settings["serviceName"].Value;
            this.ServiceInstaller.DisplayName = ConfigurationManager.OpenExeConfiguration(Assembly.GetAssembly(typeof(Installer)).Location).AppSettings.Settings["serviceDisplayName"].Value;
            this.ServiceInstaller.Description = ConfigurationManager.OpenExeConfiguration(Assembly.GetAssembly(typeof(Installer)).Location).AppSettings.Settings["serviceDescription"].Value;
            this.ServiceInstaller.StartType = ServiceStartMode.Automatic;
            this.ServiceInstaller.AfterInstall += new InstallEventHandler(ServiceInstaller_AfterInstall);
            // 
            // Installer
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.ServiceProcessInstaller,
            this.ServiceInstaller});

        }

        void ServiceInstaller_AfterInstall(object sender, InstallEventArgs e)
        {
            ServiceInstaller serviceInstaller = (ServiceInstaller)sender;

            using (ServiceController sc = new ServiceController(serviceInstaller.ServiceName))
            {
                sc.Start();
            }
        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller ServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller ServiceInstaller;
    }
}