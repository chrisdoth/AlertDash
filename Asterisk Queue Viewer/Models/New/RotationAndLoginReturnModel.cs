using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Asterisk_Queue_Viewer.Models.New
{
    public class RotationAndLoginReturnModel
    {

        public string AgentName { get; set; }

        public string Event { get; set; }

        public string TimeStamp { get; set; }

        public int AgentId { get; set; }

        public string Reason { get; set; }
    }
}