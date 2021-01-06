using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Asterisk_Queue_Viewer.Models
{
    public class AgentInformation
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public TimeSpan TotalWork { get; set; }
        public TimeSpan TotalLogin { get; set; }
        public TimeSpan TotalLoginRounded { get { return new TimeSpan(0, 0, (int)TotalLogin.TotalSeconds); } set { } }
        public List<LogEvent> LoginEvents { get; private set; }

        public AgentInformation()
        {
            LoginEvents = new List<LogEvent>();
            this.TotalWork = new TimeSpan();
            this.TotalLogin = new TimeSpan(0,30,0);
            Id = -1;
            FirstName = "Unknown";
            LastName = "Agent";
        }

        public string FullName 
        {
            get { return string.Format("{0} {1}", FirstName, LastName); }
        }

    }
}