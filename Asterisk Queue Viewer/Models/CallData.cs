using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Asterisk_Queue_Viewer.Models
{
    public class CallData
    {
        public int CallId { get; set; }
        public int Affinity { get; set; }
        public double TalkTime { get; set; }
        public double TimeToAnswer { get; set; }
        public double TotalHoldTime { get; set; }
        public double RingTime { get; set; }
        public double TotalCallTime { get; set; }
        public int AgentId { get; set; }
        public DateTime TimeStamp { get; set; }
        public string ClientID { get; set; }
    }
}