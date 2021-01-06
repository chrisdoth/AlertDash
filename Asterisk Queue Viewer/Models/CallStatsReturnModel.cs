using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Asterisk_Queue_Viewer.Models
{
    public class CallStatsReturnModel
    {
        public string SLA { get; set; }
        public double SLAInt { get; set; }
        public double SLAQualifiedCalls { get; set; }
        public double TotalCalls { get; set; }
        public double TotalCallsOffered { get; set; }
        public double TotalCallsHandled { get; set; }
        public double TotalCallsQueued { get; set; }
        public double AverageHoldTime { get; set; }
        public double AverageTimeToAnswer { get; set; }
        public double LongestHoldTime { get; set; }
        public double LongestTimeToAnswer { get; set; }
        public double AverageTalkTime { get; set; }
        public string Utilization { get; set; }
        public string WorkTime { get; set; }
    }
}