using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Asterisk_Queue_Viewer.Models.New
{
    public class DayPeriodReturnModel
    {

        public DateTime PeriodStart { get; set; }

        public DateTime PeriodEnd { get; set; }

        public string PeriodStr { get { return PeriodStart.ToString("hh:mm tt"); } set { } }

        public string SLA { get; set; }

        public int TotalCalls { get; set; }

        public int CallsQueued { get; set; }

        public int CallsOffered { get; set; }

        public int CallsHandled { get; set; }

        public int SLAQualifiedCalls { get; set; }

        public double AverageHold { get; set; }

        public double AverageTimeToAnswer { get; set; }

        public double AverageTalkTime { get; set; }

        public double LongestHold { get; set; }

        public double LongestTimeToAnswer { get; set; }

        public string Utilization { get; set; }

        public string TotalWork { get; set; }

        public string TotalLogin { get; set; }

        public string TotalRotation { get; set; }

        public int TotalLoginSeconds { get; set; }

        public int TotalRotationInSeconds { get; set; }

        public string IdleTime { get; set; }

        public int IdleTimeInSeconds { get; set; }

        public string TimeBetweenCalls { get; set; }

        public int TimeBetweenCallsInSeconds { get; set; }

        public int TotalWorkSeconds { get; set; }

		public int ActionsDue { get; set; }

		public int ActionsPickedUp { get; set; }

		public string ActionSLA { get; set; }

		public string ActionAPD { get; set; }

		public string ActionLPD { get; set; }

        public bool IsCurrentPeriod()
        {
            var now = DateTime.Now;
            return PeriodStart < now && PeriodEnd > now;
        }

        public bool IsPreviousPeriod()
        {
            var now = DateTime.Now.AddMinutes(-30);
            return PeriodStart < now && PeriodEnd > now;
        }

        public bool IsPeriodInFuture()
        {
            return PeriodStart > DateTime.Now;
        }

        public void Reset()
        {
            this.SLA = "0%";
            this.TotalCalls = 0;
            this.CallsQueued = 0;
            this.CallsOffered = 0;
            this.CallsHandled = 0;
            this.AverageTimeToAnswer = 0;
            this.AverageHold = 0;
            this.AverageTalkTime = 0;
            this.LongestHold = 0;
            this.LongestTimeToAnswer = 0;
            this.Utilization = "0%";
            this.TotalWork = "00:00:00";
            this.TotalLogin = "00:00:00";
            this.TotalLoginSeconds = 0;
            this.TotalWorkSeconds = 0;
        }

    }
}