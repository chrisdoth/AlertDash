using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Asterisk_Queue_Viewer.Models
{
    public class RawWorkData
    {
        public int AgentId { get; set; }

        public int Duration { get; set; }

        public DateTime TimeStamp { get; set; }
    }
}