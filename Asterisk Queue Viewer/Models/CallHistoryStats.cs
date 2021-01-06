using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Asterisk_Queue_Viewer.Models
{
    public class CallHistoryStats
    {
        public int AverageHoldTime { get; set; }
        public int AverageTimeToAnswer { get; set; }
        public int CachedCalls { get; set; }
        public int TotalCalls { get; set; }
        public int TotalCallsAnswered { get; set; }
        public int CallsAbandoned3To8 { get; set; }
        public int CallsAbandoned9To16 { get; set; }
        public int CallsAbandoned17To24 { get; set; }
        public int CallsAbandoned24Plus { get; set; }
        public string SLA { get; set; }
        public int ActiveCalls { get; set; }

    }
}