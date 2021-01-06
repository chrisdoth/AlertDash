using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Asterisk_Queue_Viewer.Models.New
{
    public class DayStatisticsReturnModel
    {

        public string Utilization { get; set; }

        public string TotalWork { get; set; }

        public string TotalLogin { get; set; }

        public string AverageIdleTime { get; set; }

        public string TimeBetweenCalls { get; set; }

        public string SLA { get; set; }

		public int ActionsDue { get; set; }

		public int ActionsPickedUp { get; set; }

		public string ActionsAvgPast { get; set; }

		public string ActionsLongPast { get; set; }

		public string ActionsSLA { get; set; }

        public int TotalCalls { get; set; }

        public int TotalCallsQueued { get; set; }

        public int TotalCallsOffered { get; set; }

        public int  TotalCallsHandled { get; set; }

		public double TotalAbandonedPct { get; set; }

        public int SLAQualifiedCalls { get; set; }

        public double AverageHoldTime { get; set; }

        public double LongestHoldTime { get; set; }

        public double AverageTalkTime { get; set; }

        public double AverageTimeToAnswer { get; set; }

        public double LongestTimeToAnswer { get; set; }
    }
}