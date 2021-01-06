using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Dashboard.Code;
using System.Timers;

namespace Asterisk_Queue_Viewer.Utility
{
    internal static class DashboardWrapper
    {
        private static bool isConnected = true;
        private static bool isDone = false;
        private static Timer ReconnectTPTimer { get; set; }
        public static DashboardKernel DashboardKernal { get; set; }

        public static void Initialize() 
        { 
            if (DashboardWrapper.DashboardKernal == null)
            {
                DashboardWrapper.DashboardKernal = new Dashboard.Code.DashboardKernel();
                if (DashboardWrapper.DashboardKernal.Start())
                {
                    //ReconnectTPTimer = new Timer();
                    //ReconnectTPTimer.Interval = 300000;
                    //ReconnectTPTimer.AutoReset = true;
                    //ReconnectTPTimer.Enabled = true;
                    //ReconnectTPTimer.Elapsed += ReconnectTPTimer_Elapsed;
                    //ReconnectTPTimer.Start();
                    return;
                } 
                DashboardWrapper.DashboardKernal.Stop();
            }
            else
            {
                if (DashboardWrapper.DashboardKernal.IsRunning() || DashboardWrapper.DashboardKernal.Start()) return;
                DashboardWrapper.DashboardKernal.Stop();
            }

        }

        private static void ReconnectTPTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var test = DashboardKernal.eventsProcess.GetAgent(81);
            var test2 = DashboardKernal.eventsProcess.GetClientByDBID("3374");


            DashboardKernal.TPLogin();
        }

        public static void Dispose() 
        {
            DashboardWrapper.isDone = true;
            if (DashboardWrapper.DashboardKernal == null && DashboardWrapper.DashboardKernal.IsRunning()) DashboardWrapper.DashboardKernal.Stop();
            DashboardWrapper.DashboardKernal = (Dashboard.Code.DashboardKernel)null;
            ReconnectTPTimer = null;
        }

        private static void UpdateAppData(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            while (DashboardWrapper.isConnected && !DashboardWrapper.isDone) System.Threading.Thread.Sleep(500);
        }

        private static void WorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            System.ComponentModel.BackgroundWorker worker = sender as System.ComponentModel.BackgroundWorker;
            while (!DashboardWrapper.isConnected && !DashboardWrapper.isDone)
            {
                if (DashboardWrapper.isConnected) worker.RunWorkerAsync();
                else System.Threading.Thread.Sleep(180000);
            }
        }
    }
}