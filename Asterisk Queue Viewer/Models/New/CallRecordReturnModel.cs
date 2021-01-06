using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Asterisk_Queue_Viewer.Models.New
{
    public class CallRecordReturnModel
    {

        public string Client { get; set; }

        public string ANI { get; set; }

        public string CallerName { get; set; }

        public string Direction { get; set; }

        public string Queue { get; set; }

        public double TotalCallTime { get; set; }

        public double TalkTime { get; set; }

        public double TimeToAnswer { get; set; }

        public double HoldTime { get; set; }

        public string TimeStamp { get; set; }
    }
}