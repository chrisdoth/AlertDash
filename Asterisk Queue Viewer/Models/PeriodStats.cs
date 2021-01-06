using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Asterisk_Queue_Viewer.Models
{
    public class PeriodStats
    {
        internal static List<int> _ignoreAgents = new List<int>() { 1, 143, 149, 178, 232, 233, 235, 257, 258, 270, 293, 365, 433, 464, 639, 756, 466, 766, 767, 795, 871, 894, 919, 921, 939, 1005, 1020, 1074, 1114, 1116, 1187, 1200 };

        public DateTime PeriodStartTime { get; set; }

        public DateTime PeriodEndTime { get; set; }

        public string PeriodStartStr
        { get
            {
                return PeriodStartTime.ToShortTimeString();
            }
        }

        public List<AgentInformation> Agents { get; private set; }

        [Newtonsoft.Json.JsonIgnore]
        public List<CallData> Calls { get; private set; }

        public double SLA { get; set; }

        public double SLAQualifiedCalls { get; set; }

        public double TotalCalls { get; set; }

        public double TotalCallsOffered { get; set; }

        public double TotalCallsHandled { get; set; }

        public double TotalCallsQueued { get; set; }

        public double AverageHoldTime { get; set; }

        public double AverageTimeToAnswer { get; set; }

        public double LongestHoldTime { get; set; }

        public double LongestTimeToAnswer { get; set; }

        public double AverageTalkTime { get; set; }

        public string Utilization { get; set; }

        public TimeSpan TotalWork { get; set; }

        public TimeSpan TotalLogin { get; set; }

        public PeriodStats()
        {
            Agents = new List<AgentInformation>();
            Calls = new List<CallData>();
        }

        public void AddWork(int time, int agentId)
        {
            lock (Agents)
            {
                var agent = Agents.FirstOrDefault(x => x.Id == agentId);
                if (agent == null)
                {
                    agent = Utility.SQLWrapper.GetAgentOld(agentId);
                    Agents.Add(agent);
                }

                agent.TotalWork = agent.TotalWork.Add(new TimeSpan(0, 0, time));
            }
        }

        public void AddLogEvent(LogEvent logEvent)
        {
            lock (Agents)
            {
                var agent = Agents.FirstOrDefault(x => x.Id == logEvent.AgentId);
                if (agent == null)
                {
                    agent = Utility.SQLWrapper.GetAgentOld(logEvent.AgentId);
                    Agents.Add(agent);
                }

                agent.LoginEvents.Add(logEvent);
            }
        }

        public void Reset()
        {
            lock (Agents)
            {
                Agents.Clear();
            }
            lock (Calls)
            {
                Calls.Clear();
            }
        }

        public void SetTotalWork()
        {
            if(Agents.Count > 0)
            {
                TotalWork = Agents.Select(x => x.TotalWork).Aggregate((t1, t2) => t1.Add(t2));
            }
            
        }
        public void SetTotalLogin()
        {
            if(Agents.Count > 0)
            {
                TotalLogin = Agents.Select(x => x.TotalLogin).Aggregate((t1, t2) => t1.Add(t2));
            }
            
        }
        public void SetUtilization()
        {
            try
            {
                var utilization = Math.Round(TotalWork.TotalSeconds / TotalLogin.TotalSeconds * 100, 1);
                Utilization = string.Format("{0}", utilization);
            }
            catch
            {
                Utilization = string.Format("100 %");
            }
        }
        public void CalculateLoginTime()
        {
            lock (Agents)
            {
                foreach(AgentInformation agent in Agents)
                {
                    CalculateAgentLogTime(agent);
                }
            }
        }
        public bool IsActive()
        {
            var now = DateTime.Now;
            return PeriodStartTime < now && PeriodEndTime > now;
        }
        public bool IsFuture()
        {
            var now = DateTime.Now;
            return PeriodStartTime > now;
        }

        public void CalculateCallStats()
        {
            if(Calls.Count != 0)
            {
                this.AverageHoldTime = Math.Round(Calls.Average(x => x.TotalHoldTime), 1);
                this.AverageTalkTime = Math.Round(Calls.Where(x => x.TalkTime > 0).Average(x => x.TalkTime), 1);
                this.AverageTimeToAnswer = Math.Round(Calls.Where(x => x.TalkTime > 0).Average(x => x.TimeToAnswer), 1);
                this.LongestHoldTime = Calls.Max(x => x.TotalHoldTime);
                this.LongestTimeToAnswer = Math.Round(Calls.Max(x => x.TimeToAnswer), 1);
                this.SLAQualifiedCalls = Calls.Where(x => x.TimeToAnswer < 24 && x.TalkTime > 0).Count();
                this.TotalCalls = Calls.Count;
                this.TotalCallsHandled = Calls.Where(x => x.TalkTime > 0).Count();
                this.TotalCallsOffered = Calls.Where(x => x.AgentId != -1).Count();
                this.TotalCallsQueued = Calls.Where(x => x.Affinity != -1).Count();

                try
                {
                    this.SLA = Math.Round(SLAQualifiedCalls / TotalCallsHandled * 100, 1);
                }
                catch
                {
                    this.SLA = 0;
                }
            }
        }

        private void CalculateAgentLogTime(AgentInformation agent)
        {
            if (_ignoreAgents.Contains(agent.Id))
            {
                agent.TotalLogin = new TimeSpan(0, 0, 0);
                return;
            }

            var events = agent.LoginEvents.OrderBy(x => x.TimeStamp);
            var isLoggedIn = false;
            var startLoginTime = new DateTime();
            var loginTime = new TimeSpan(0, 0, 0);

            if(events.Count() == 0)
            {
                loginTime = loginTime.Add(new TimeSpan(0, 0, 1800));
            }

            foreach(LogEvent item in events)
            {
                if(item.EventType == LogEvent.LogEventType.login)
                {
                    isLoggedIn = true;
                    startLoginTime = item.TimeStamp;
                }

                if(item.EventType == LogEvent.LogEventType.Logout)
                {
                    if (isLoggedIn)
                    {
                        isLoggedIn = false;
                        loginTime = loginTime.Add(item.TimeStamp - startLoginTime);
                    }
                    else
                    {
                        loginTime = loginTime.Add(item.TimeStamp - PeriodStartTime);
                    }
                }
            }

            if (isLoggedIn)
            {
                loginTime = loginTime.Add(PeriodEndTime - startLoginTime);
            }

            //Losing percision by calling total seconds. use the loginTime object instead
            //agent.TotalLogin = new TimeSpan(0, 0, (int)loginTime.TotalSeconds);
            agent.TotalLogin = loginTime;

            var stop = "";
        }
    }
}