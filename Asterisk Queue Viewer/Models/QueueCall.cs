using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Asterisk_Queue_Viewer.Models
{
    public class QueueCall
    {
        //public string UniqueID { get; set; }
        public string ChannelID { get; set; }
        public string ClientID { get; set; }
        public string ClientName { get; set; }
        public string CallerName { get; set; }
        public string ANI { get; set; }
        public int Position { get; set; }
        public string Queue { get; set; }
        public string Affinity { get; set; }
        public bool Answered { get; set; }
        public bool IsInternal { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? AnswerTime { get; set; }
        public DateTime? HoldStartTime { get; set; }
        public DateTime QueueStartTime { get; set; }
        public bool InQueue { get; set; }
        public int TimeToAnswer { get; set; }
        public int HoldTime { get; set; }
        public string CallType { get; set; }
        public bool IsLocked { get; set; }

        private int _waitTime = 0;
        public int WaitTime 
        {
            get 
            {
                if (!IsLocked) _waitTime = (int)DateTime.Now.Subtract(QueueStartTime).TotalSeconds;
                return _waitTime;
            }
            set { }
        }
        public string ProgressbarType
        {
            get 
            {
                int waitTime = this.WaitTime;
                if (waitTime < 12)
                {
                    return "success";
                }
                else if (waitTime < 24)
                {
                    return "warning";
                }
                else 
                {
                    return "danger";
                }
            }
            set { }
        }
        public int ProgressbarMinValue 
        {
            get 
            {
                int waitTime = this.WaitTime;
                if (waitTime < 12) 
                {
                    return 0;
                }
                else if (waitTime < 24)
                {
                    return 12;
                }
                else 
                {
                    return 24;
                }
            }
        }
        public int ProgressbarMaxValue
        {
            get
            {
                int waitTime = this.WaitTime;
                if (waitTime < 12)
                {
                    return 12;
                }
                else if (waitTime < 24)
                {
                    return 24;
                }
                else
                {
                    return waitTime;
                }
            }
        }
    }
}