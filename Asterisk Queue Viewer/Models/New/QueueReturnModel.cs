using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Asterisk_Queue_Viewer.Models.New
{
    public class QueueReturnModel
    {
        public string Site { get; set; }

        public int Id { get; set; }

        public string AffinityName { get; set; }

        public int Calls { get; set; }

        public int Agents { get; set; }

        public int InMuteAgents { get; set; }

        public int OutMuteAgents { get; set; }

        public int InTalkAgents { get; set; }

        public int OutTalkAgents { get; set; }
    }
}