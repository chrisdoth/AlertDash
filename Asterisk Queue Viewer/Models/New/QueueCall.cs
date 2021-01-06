using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Asterisk_Queue_Viewer.Models.New
{
    public class QueueCallReturnModel
    {
        public string Site { get; set; }

        public string UniqueId { get; set; }

        public string ChannelId { get; set; }

        public string Queue { get; set; }

        public string ClientId { get; set; }

        public string ClientName { get; set; }

        public string CallerName { get; set; }

        public string ANI { get; set; }

        public int Postion { get; set; }

        public string CallType { get; set; }

        public bool Answered { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime AnswerTime { get; set; }

        public int TimerInSeconds
        {
            get
            {
                if (Answered)
                {
                    return (int)DateTime.UtcNow.Subtract(AnswerTime).TotalSeconds;
                }
                else
                {
                    return (int)DateTime.UtcNow.Subtract(StartTime).TotalSeconds;
                }
            }
        }

        public TimeSpan TimerTimeSpan
        {
            get
            {
                if (Answered)
                {
                    return DateTime.UtcNow.Subtract(AnswerTime);
                }
                else
                {
                    return DateTime.UtcNow.Subtract(StartTime);
                }
            }
            set { }
        }

        public string ProgressBarType
        {
            get
            {
                var queue = Utility.Answer1Dashboard.GetQueue(this.Queue);
                Utility.QueueConfiguration configuration;
                if(queue == null)
                {
                    configuration = Utility.Answer1Dashboard.DEFAULT_QUEUE_CONFIGURATION;
                }
                else
                {
                    configuration = Utility.Answer1Dashboard.QueueConfiguration.FirstOrDefault(x => x.QueueId == queue.Id) ?? Utility.Answer1Dashboard.DEFAULT_QUEUE_CONFIGURATION;
                }

                if (!Answered)
                {
                    int timeInSeconds = this.TimerInSeconds;
                    if (timeInSeconds < configuration.WarningTimeoutInSeconds)
                    {
                        return "success";
                    }
                    else if (timeInSeconds < configuration.DangerTimeoutInSeconds)
                    {
                        return "warning";
                    }
                    else
                    {
                        return "danger";
                    }
                }
                else
                {
                    return "success";
                }
                
            }
            set { }
        }

        public int ProgressBarMinValue
        {
            get
            {
                if (!Answered)
                {
                    int timeInSeconds = this.TimerInSeconds;
                    if(timeInSeconds < 12)
                    {
                        return 0;
                    }
                    else if(timeInSeconds < 24)
                    {
                        return 12;
                    }
                    else
                    {
                        return 24;
                    }
                }
                else
                {
                    return 0;
                }   
            }
            set { }
        }

        public int ProgressBarMaxValue
        {
            get
            {
                if (!Answered)
                {
                    int timeInSeconds = this.TimerInSeconds;
                    if (timeInSeconds < 12)
                    {
                        return 12;
                    }
                    else if (timeInSeconds < 24)
                    {
                        return 24;
                    }
                    else
                    {
                        return timeInSeconds;
                    }
                }
                else
                {
                    return 0;
                }
            }
            set { }
        }

        public QueueCallReturnModel()
        {
            StartTime = DateTime.UtcNow;
            CallType = "";
        }
    }
}