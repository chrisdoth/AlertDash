using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Asterisk_Queue_Viewer.Models
{
    public class QueueMember
    {
        public QueueMember() 
        {
            Queues = new List<string>();
        }

        public string Name { get; set; }

        public List<string> Queues { get; set; }

        public string Extension { get; set; }

        public QueueCall Call { get; set; }

        public DateTime StatusTimer { get; set; }

        private bool _isTalking = false;
        public bool IsTalking 
        { 
            get 
            {
                return _isTalking;
            }
            set 
            {
                _isTalking = value;
                StatusTimer = DateTime.Now;
            }
        }

        private bool _isPaused = false;
        public bool IsPaused 
        { 
            get 
            {
                return _isPaused;
            }
            set 
            {
                _isPaused = value;
                StatusTimer = DateTime.Now;
            }
        }

        public string Status 
        {
            get 
            {
                return string.Format("{0} {1}", IsPaused ? "Out" : "In", IsTalking ? "Talk" : "Mute");
            }
            set { }
        }

        public string Position 
        {
            get 
            {
                if (this.Extension.Contains("SIP/90")) 
                {
                    return this.Extension.Replace("SIP/90", "");
                }
                if (this.Extension.Contains("SIP/9")) 
                {
                    return this.Extension.Replace("SIP/9", "");
                }
                return this.Extension;
            }
            set { }
        }

        
    }
}