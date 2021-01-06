using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Asterisk_Queue_Viewer.Utility
{
    public class QueueConfiguration
    {

        public int WarningTimeoutInSeconds { get; set; }

        public int DangerTimeoutInSeconds { get; set; }

        public int QueueId { get; set; }

        public string GroupingName { get; set; }

    }
}