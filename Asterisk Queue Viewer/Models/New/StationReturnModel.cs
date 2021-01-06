using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Asterisk_Queue_Viewer.Models.New
{
    public class StationReturnModel
    {

        public int AgnetId { get; set; }
        public string Site { get; set; }
        public string AgentFirstName { get; set; }
        public string AgentLastName { get; set; }
        public string AgentInitial { get; set; }
        public int PostionNumber { get; set; }
        public int New { get; set; }
        public int Holding { get; set; }
        public string ClientId { get; set; }
        public string ClientName { get; set; }
        public string CallType { get; set; }
        public string State { get; set; }
        public string OutOfRotationReason { get; set; }
        public bool IsTalking { get; set; }
        public bool IsInRotation { get; set; }
        public string Timer { get; set; }
        public int IdleTimeInSeconds { get; set; }
        public bool IsVisable { get; set; }
        public bool IsLicensed { get; set; }
    }
}