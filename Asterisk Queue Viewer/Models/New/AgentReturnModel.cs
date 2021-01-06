using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Asterisk_Queue_Viewer.Models.New;
using Answer1APILib.Plugin.Startel;

namespace Asterisk_Queue_Viewer.Models.New
{
    public class AgentReturnModel
    {

        public int Id { get; set; }

        public string Site { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Initial { get; set; }

        public string Utilization { get; set; }

        public double AverageTalkTime { get; set; }

        public double AverageTimeToAnswer { get; set; }

        public int RefuseCount { get; set; }

        public int IgnoreCount { get; set; }

        public int AssignedCalls { get; set; }

        public int AnsweredCalls { get; set; }

        public string TotalRotation { get; set; }

        public string TotalOutOfRotation { get; set; }

        public string TotalWork { get; set; }

        public string TotalLogin { get; set; }

        public string IdleTime { get; set; }

        public string TimeBetweenCalls { get; set; }

        public bool IsLoggedOn { get; set; }

        public bool IsVisable { get; set; }

        public List<StationReturnModel> Positions { get; set; }

        public List<CallRecordReturnModel> RecentCalls { get; set; }

        public Dictionary<string, string> RotationSummary { get; set; }

        public bool IsLicensed { get { return Positions.Any(x => x.IsLicensed); } set { } }
    }
}