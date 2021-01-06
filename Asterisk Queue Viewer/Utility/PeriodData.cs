using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Asterisk_Queue_Viewer.Models;
using System.Timers;

namespace Asterisk_Queue_Viewer.Utility
{
    public static class PeriodData
    {
        private static List<PeriodStats> _stats = null;
        private static Timer _refreshTimer = null;

        public static CallStatsReturnModel DayTotals { get; set; }

        public static List<PeriodStats> PeriodStats
        {
            get
            {
                return _stats;
            }
        }

        public static void Initialize()
        {
            DayTotals = new CallStatsReturnModel();

            _stats = new List<PeriodStats>();
            _refreshTimer = new Timer();
            _refreshTimer.Interval = 30000;
            _refreshTimer.AutoReset = true;
            _refreshTimer.Elapsed += _refreshTimer_Elapsed;

            SetupStatCollection();

            _refreshTimer.Start();
        }

        private static void _refreshTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var currentStat = _stats.FirstOrDefault(x => x.IsActive());
            if(currentStat != null)
            {
                GetStats(currentStat);
            }

            UpdateDayTotals();
            ClearOldData();
        }

        private static void SetupStatCollection()
        {
            _stats.Clear();

            for(int h = 0; h <= 23; h++)
            {
                for(int m = 0; m <= 31; m += 30)
                {
                    var today = DateTime.Today;
                    if (m == 0)
                    {
                        _stats.Add(new PeriodStats()
                        {
                            PeriodStartTime = today.Add(new TimeSpan(h, m, 0)),
                            PeriodEndTime = today.Add(new TimeSpan(h,29,59))
                        });
                    }
                    else
                    {
                        _stats.Add(new PeriodStats()
                        {
                            PeriodStartTime = today.Add(new TimeSpan(h, m, 0)),
                            PeriodEndTime = today.Add(new TimeSpan(h, 59, 59))
                        });
                    }
                }
            }

            foreach(PeriodStats stat in _stats)
            {
                GetStats(stat);
            }
        }

        private static void GetStats(PeriodStats stats)
        {
            stats.Reset();
            foreach(RawWorkData rawData in SQLWrapper.GetWork(stats.PeriodStartTime, stats.PeriodEndTime))
            {
                 stats.AddWork(rawData.Duration, rawData.AgentId);
            }
            foreach(LogEvent logEvent in SQLWrapper.GetLogEvents(stats.PeriodStartTime, stats.PeriodEndTime))
            {
                stats.AddLogEvent(logEvent);
            }
            foreach(CallData call in SQLWrapper.GetCalls(stats.PeriodStartTime, stats.PeriodEndTime))
            {
                stats.Calls.Add(call);
            }

            stats.CalculateCallStats();
            stats.CalculateLoginTime();
            stats.SetTotalWork();
            stats.SetTotalLogin();
            stats.SetUtilization();
        }

        private static void ClearOldData()
        {
            var today = DateTime.Today;
            var firstStat = _stats.FirstOrDefault();
            if(firstStat.PeriodStartTime.Date != today)
            {
                SetupStatCollection();
            }
        }

        private static void UpdateDayTotals()
        {
            DayTotals.AverageHoldTime = Math.Round(PeriodStats.Average(x => x.AverageHoldTime), 1);
            DayTotals.AverageTalkTime = Math.Round(PeriodStats.Average(x => x.AverageTalkTime), 1);
            DayTotals.AverageTimeToAnswer = Math.Round(PeriodStats.Average(x => x.AverageTimeToAnswer), 1);
            DayTotals.LongestHoldTime = PeriodStats.Max(x => x.LongestHoldTime);
            DayTotals.LongestTimeToAnswer = PeriodStats.Max(x => x.LongestTimeToAnswer);
            DayTotals.SLAQualifiedCalls = PeriodStats.Sum(x => x.SLAQualifiedCalls);
            DayTotals.TotalCalls = PeriodStats.Sum(x => x.TotalCalls);
            DayTotals.TotalCallsHandled = PeriodStats.Sum(x => x.TotalCallsHandled);
            DayTotals.TotalCallsOffered = PeriodStats.Sum(x => x.TotalCallsOffered);
            DayTotals.TotalCallsQueued = PeriodStats.Sum(x => x.TotalCallsQueued);

            var totalWork = new TimeSpan(0, 0, (int)PeriodStats.Sum(x => x.TotalWork.TotalSeconds));
            DayTotals.WorkTime = (int)totalWork.TotalHours + totalWork.ToString(@"\:mm\:ss");
            try
            {
                DayTotals.SLAInt = Math.Round(DayTotals.SLAQualifiedCalls / DayTotals.TotalCallsHandled * 100, 1);
                DayTotals.SLA = string.Format("{0} %", DayTotals.SLAInt);

                var allLogEvents = new List<LogEvent>();
                foreach(PeriodStats stat in PeriodStats)
                {
                    foreach(AgentInformation agent in stat.Agents)
                    {
                        if(agent.LoginEvents.Count > 0)
                        {
                            allLogEvents.AddRange(agent.LoginEvents);
                        }
                    }
                }

                //var totalLog = PeriodStats.Sum(x => x.TotalLogin.TotalSeconds);
                var totalLog = CalculateTotalLog(allLogEvents);

                var utilization = Math.Round((totalWork.TotalSeconds / totalLog)  * 100, 1);

                DayTotals.Utilization = string.Format("{0} %", utilization);
            }
            catch
            {
                DayTotals.SLAInt = 0;
                DayTotals.SLA = "0 %";
            }
        }

        private static double CalculateTotalLog(List<LogEvent> logEvents)
        {
            if (logEvents.Count == 0) { return 0; };

            var agentIds = logEvents.Select(x => x.AgentId).Distinct().OrderBy(x => x).ToList();
            agentIds.RemoveAll(x => Models.PeriodStats._ignoreAgents.Contains(x));
            var lastEvent = logEvents.LastOrDefault();
            double loginTime = 0;

            foreach(int agentId in agentIds)
            {
                var agentLoginEvents = logEvents.Where(x => x.AgentId == agentId).OrderBy(x => x.TimeStamp);
                var isLoggedIn = false;
                var logStart = DateTime.Today;
                var logTime = new TimeSpan(0);
                var logEndPeriod = new DateTime(lastEvent.TimeStamp.Year, lastEvent.TimeStamp.Month, lastEvent.TimeStamp.Day, lastEvent.TimeStamp.Hour, lastEvent.TimeStamp.Minute, 59).AddMinutes(29);

                foreach(LogEvent logevent in agentLoginEvents)
                {
                    switch (logevent.EventType)
                    {
                        case LogEvent.LogEventType.login:
                            isLoggedIn = true;
                            logStart = logevent.TimeStamp;
                            break;
                        case LogEvent.LogEventType.Logout:
                            isLoggedIn = false;
                            logTime = logTime.Add(logevent.TimeStamp - logStart);
                            break;
                    }
                }
                if (isLoggedIn)
                {
                    logTime = logTime.Add(logEndPeriod - logStart);
                }

                loginTime += logTime.TotalSeconds;
            }


            return loginTime;
        }
    }
}