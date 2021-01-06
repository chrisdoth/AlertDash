using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Asterisk_Queue_Viewer.Models
{
    public class LogEvent
    {

        public DateTime TimeStamp { get; set; }

        public LogEventType EventType { get; set; }

        public int AgentId { get; set; }

        public enum LogEventType
        {
            login,
            Logout
        }

    }
}