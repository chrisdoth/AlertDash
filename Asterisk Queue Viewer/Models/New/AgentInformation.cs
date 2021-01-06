using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Answer1APILib.Plugin.Startel;
using Answer1APILib.Plugin.Startel.Models;
namespace Asterisk_Queue_Viewer.Models.New
{
    internal class AgentInformation
    {

        internal int Id { get; set; }
        internal string Site { get; set; }
        internal List<PositionInformation> Positions { get; set; }
        internal List<CallRecord> CallHistory { get; set; }
        internal List<WorkEvent> AgentEvents { get; set; }
        internal Dictionary<string, TimeSpan> RotationSummary { get; set; }
        internal string FirstName { get; set; }
        internal string LastName { get; set; }
        internal string Initial { get; set; }
        internal string Utilization { get; set; }
        internal string OutOfRotationReason { get; set; }
        internal double AverageTalkTime { get; set; }
        internal double LongestTimeToAnswer { get; set; }
        internal double AverageTimeToAnswer { get; set; }
        internal int RefuseCount { get; set; }
        internal int IgnoreCount { get; set; }
        internal int AssignedCalls { get; set; }
        internal int AnsweredCalls { get; set; }
        internal TimeSpan TotalRotation { get; set; }
        internal TimeSpan TotalWork { get; set; }
        internal TimeSpan TotalLogin { get; set; }
        internal bool IsLoggedIn { get; set; }
        internal bool IsVisable { get; set; }

        public AgentInformation()
        {
            this.Positions = new List<PositionInformation>();
            this.CallHistory = new List<CallRecord>();
            this.AgentEvents = new List<WorkEvent>();
            this.RotationSummary = new Dictionary<string, TimeSpan>();
        }

        internal AgentReturnModel GetAgentReturnModel()
        {
            var returnData = new AgentReturnModel()
            {
                Id = this.Id,
                Site = this.Site,
                FirstName = this.FirstName,
                LastName = this.LastName,
                Initial = this.Initial,
                Utilization = this.Utilization,
                AverageTalkTime = this.AverageTalkTime,
                AverageTimeToAnswer = this.AverageTimeToAnswer,
                RefuseCount = this.RefuseCount,
                IgnoreCount = this.IgnoreCount,
                AssignedCalls = this.AssignedCalls,
                AnsweredCalls = this.AnsweredCalls,
                TotalRotation = Utility.Answer1Dashboard.FormatTimeSpan(new TimeSpan(0, 0, (int)this.TotalRotation.TotalSeconds)),
                TotalOutOfRotation = Utility.Answer1Dashboard.FormatTimeSpan(new TimeSpan(0,0,(int)this.TotalLogin.TotalSeconds - (int)this.TotalRotation.TotalSeconds)),
                TotalWork = Utility.Answer1Dashboard.FormatTimeSpan(new TimeSpan(0, 0, (int)this.TotalWork.TotalSeconds)),
                TotalLogin = Utility.Answer1Dashboard.FormatTimeSpan(new TimeSpan(0, 0, (int)this.TotalLogin.TotalSeconds)),
                IsLoggedOn = this.Positions.Count > 0,
                Positions = GetStationReturnModel(),
                IsVisable = IsVisable,
                RotationSummary = this.RotationSummary.ToDictionary(k => k.Key, v => Utility.Answer1Dashboard.FormatTimeSpan(new TimeSpan(0, 0, (int)v.Value.TotalSeconds)))
            };
            if (CallHistory.Count > 20)
            {
                returnData.RecentCalls = CallHistory.GetRange(0, 20).Select(x => CreateCallRecordModel(x)).ToList();
            }
            else if (CallHistory.Count > 0)
            {
                returnData.RecentCalls = CallHistory.GetRange(0, CallHistory.Count - 1).Select(x => CreateCallRecordModel(x)).ToList();
            }
            else
            {
                returnData.RecentCalls = new List<CallRecordReturnModel>();
            }

            var callRecords = this.CallHistory.Where(x => x.TalkTime > 0);
            if(this.TotalRotation.TotalSeconds > 0 && callRecords.Sum(x => x.TalkTime) > 0)
            {
                var idleTime = (int)(this.TotalRotation.TotalSeconds - callRecords.Sum(x => x.TalkTime));

                returnData.IdleTime = Utility.Answer1Dashboard.FormatTimeSpan(new TimeSpan(0, 0, idleTime));
                returnData.TimeBetweenCalls = Utility.Answer1Dashboard.FormatTimeSpan(new TimeSpan(0, 0, idleTime / callRecords.Count()));
            }

            return returnData;
        }

        internal void SetStatistics()
        {
            DateTime today = DateTime.Now;
            lock (CallHistory)
            {
                CallHistory.RemoveAll(x => x.Timestamp.Date != today.Date);
                CallHistory = CallHistory.GroupBy(x => x.id).Select(x => x.First()).OrderByDescending(x => x.Timestamp).ToList();

                if (CallHistory.Count > 0)
                {
                    AverageTalkTime = Math.Round(CallHistory.Average(x => x.TalkTime), 1);
                    LongestTimeToAnswer = Math.Round(CallHistory.Max(x => x.TimeToAnswer), 1);
                    AverageTimeToAnswer = Math.Round(CallHistory.Average(x => x.TimeToAnswer), 1);
                }

                AssignedCalls = 0;
                AnsweredCalls = 0;
                RefuseCount = 0;
                IgnoreCount = 0;
                foreach (CallRecord callRecord in CallHistory)
                {
                    var lastAgentId = -1;
                    var callAnswered = false;
                    foreach (CallRecordEvent callEvent in callRecord.Events)
                    {
                        switch (callEvent.EventType)
                        {
                            case CallRecordEventType.AssignCall:
                                var assignedCall = (AssignCallEvent)callEvent;
                                if (assignedCall.AgentId == this.Id)
                                {
                                    AssignedCalls += 1;
                                }
                                lastAgentId = assignedCall.AgentId;
                                break;
                            case CallRecordEventType.Answer:
                                var answer = (AnswerEvent)callEvent;
                                if (!callAnswered && lastAgentId == this.Id)
                                {
                                    AnsweredCalls += 1;
                                    callAnswered = true;
                                }
                                break;
                            case CallRecordEventType.Transfer:
                                var transfer = (TransferEvent)callEvent;
                                if (lastAgentId == this.Id && transfer.Application == "StationHold")
                                {
                                    callAnswered = false;
                                }
                                break;
                            case CallRecordEventType.AgentIgnore:
                                var ignore = (AgentIgnoreEvent)callEvent;
                                if (lastAgentId == this.Id)
                                {
                                    if (ignore.Reason == "TIMED_OUT")
                                    {
                                        IgnoreCount += 1;
                                    }
                                    else
                                    {
                                        RefuseCount += 1;
                                    }
                                }
                                break;
                            case CallRecordEventType.AgentPatchHoldStart:
                                var agentPatchStart = (AgentPatchHoldStartEvent)callEvent;
                                if (lastAgentId == this.Id)
                                {
                                    callAnswered = false;
                                }
                                break;
                            case CallRecordEventType.AgentPatchHoldEnd:
                                var agentPatchend = (AgentPatchHoldEndEvent)callEvent;
                                if (lastAgentId == this.Id)
                                {
                                    callAnswered = true;
                                }
                                break;
                            case CallRecordEventType.Hangup:
                                var hangup = (HangupEvent)callEvent;
                                if (lastAgentId == this.Id)
                                {
                                    callAnswered = false;
                                }
                                break;
                        }
                    }
                }
            }
            lock (AgentEvents)
            {
                AgentEvents.RemoveAll(x => x.Timestamp.Date != today.Date);
                AgentEvents = AgentEvents.GroupBy(x => x.id).Select(x => x.First()).ToList();

                if (AgentEvents.Count > 0)
                {
                    CalculateLogAndRot();
                    TotalWork = new TimeSpan(0, 0, AgentEvents.Where(x => x.Type == WorkEventType.ScreenPop).Sum(x => x.Duration));
                }

            }

            if (TotalWork.TotalSeconds > 0 && TotalLogin.TotalSeconds > 0)
            {
                var utilization = Math.Round((TotalWork.TotalSeconds / TotalLogin.TotalSeconds) * 100, 1);
                this.Utilization = string.Format("{0}%", utilization);
            }
            else
            {
                this.Utilization = "N/A";
            }
        }

        internal void AddEvent(WorkEvent agentEvent)
        {
            lock (AgentEvents)
            {
                AgentEvents.Add(agentEvent);
            }
        }

        internal void AddCallToHistory(CallRecord call)
        {
            lock (CallHistory)
            {
                CallHistory.Add(call);
            }
        }

        internal bool IsLoggedIntoStation(int stationNumber)
        {
            return Positions.FirstOrDefault(x => x.PositionNumber == stationNumber) != null;
        }

        internal List<StationReturnModel> GetStationReturnModel()
        {
            var returnData = new List<StationReturnModel>();
            foreach (PositionInformation postion in Positions)
            {
                returnData.Add(new StationReturnModel()
                {
                    AgentFirstName = FirstName,
                    AgentLastName = LastName,
                    AgentInitial = Initial,
                    AgnetId = Id,
                    ClientId = postion.ClientId,
                    ClientName = postion.ClientName,
                    Holding = postion.Holding,
                    IsInRotation = postion.IsInRotation,
                    IsTalking = postion.IsTalking,
                    New = postion.New,
                    PostionNumber = postion.PositionNumber,
                    Site = Site,
                    State = postion.State,
                    OutOfRotationReason = OutOfRotationReason,
                    CallType = postion.CallType,
                    Timer = new TimeSpan(0, 0, (int)postion.Timer.TotalSeconds).ToString(),
                    IdleTimeInSeconds = (int)postion.Timer.TotalSeconds,
                    IsVisable = IsVisable,
                    IsLicensed = postion.IsLicensed
                });
            }
            return returnData;
        }

        internal void AddPosition(int positionNumber)
        {
            lock (Positions)
            {
                var position = this.Positions.FirstOrDefault(x => x.PositionNumber == positionNumber);
                if(position != null)
                {
                    position.LastUpdate = DateTime.UtcNow;
                }
                else
                {
                    position = new PositionInformation()
                    {
                        PositionNumber = positionNumber,
                        LastUpdate = DateTime.UtcNow,
                        IsLicensed = Utility.SQLWrapper.IsPositionLicensed(positionNumber),
                        Site = this.Site
                    };
                    position.StateChanged += CatchPositionStateChanged;
                    Positions.Add(position);
                }
                
            }
        }

        internal void RemovePosition(int positionNumber)
        {
            lock (Positions)
            {
                Positions.RemoveAll(x => x.PositionNumber == positionNumber);
                IsLoggedIn = Positions.Count > 0;
            }
        }

        internal PositionInformation GetPosition(int positionNumber)
        {
            return Positions.FirstOrDefault(x => x.PositionNumber == positionNumber);
        }

        private CallRecordReturnModel CreateCallRecordModel(CallRecord callRecord)
        {
            var returnData = new CallRecordReturnModel();
            returnData.ANI = callRecord.ANI;
            returnData.CallerName = callRecord.CallerName;
            returnData.Direction = callRecord.Direction == CallDirection.Incoming ? "Incoming" : "Outgoing";
            returnData.HoldTime = callRecord.TotalHoldTime;
            returnData.TalkTime = callRecord.TalkTime;
            returnData.TimeToAnswer = callRecord.TimeToAnswer;
            returnData.TotalCallTime = callRecord.TotalCallTime;
            returnData.TimeStamp = callRecord.Timestamp.ToString();

            var client = Utility.Answer1Dashboard.GetClient(callRecord.ClientID);
            if (client != null)
            {
                returnData.Client = client.Name;
            }
            else
            {
                returnData.Client = "Unknown";
            }

            var queue = Utility.Answer1Dashboard.GetQueue(callRecord.CallQueue.ToString());
            if (queue != null)
            {
                returnData.Queue = queue.AffinityName;
            }
            else
            {
                returnData.Queue = "Unknown";
            }
            return returnData;
        }

        private void CalculateLogAndRot()
        {
            this.RotationSummary.Clear();
            var events = AgentEvents.Where(x => x.Type == WorkEventType.SAILogin ||
                                                x.Type == WorkEventType.SAILogout ||
                                                x.Type == WorkEventType.StartRotation ||
                                                x.Type == WorkEventType.EndRotation
                                          ).OrderBy(x => x.Timestamp);
            bool isLoggedIn = false;
            bool isInRotation = false;
            string lastEndRotationReason = string.Empty;
            DateTime startLoginTime = new DateTime();
            DateTime startRotationTime = new DateTime();
            DateTime endRotationTime = DateTime.Today;
            DateTime now = DateTime.Now;
            DateTime today = DateTime.Today;
            TimeSpan loginTime = new TimeSpan(0);
            TimeSpan rotationTime = new TimeSpan(0);

            if (events.Count() == 0)
            {
                loginTime = now - today;
                rotationTime = now - today;
            }

            foreach (WorkEvent agtEvent in events)
            {
                if (agtEvent.Type == WorkEventType.SAILogin)
                {
                    isLoggedIn = true;
                    startLoginTime = agtEvent.Timestamp;
                }
                if (agtEvent.Type == WorkEventType.StartRotation)
                {
                    isInRotation = true;
                    startRotationTime = agtEvent.Timestamp;

                    if(endRotationTime != today)
                    {
                        if (RotationSummary.ContainsKey(lastEndRotationReason))
                        {
                            RotationSummary[lastEndRotationReason] += startRotationTime - endRotationTime;
                        }
                        else
                        {
                            RotationSummary.Add(lastEndRotationReason, new TimeSpan(0));
                            RotationSummary[lastEndRotationReason] += startRotationTime - endRotationTime;
                        }
                    }

                }
                if (agtEvent.Type == WorkEventType.SAILogout)
                {
                    if (endRotationTime != today)
                    {
                        var test = agtEvent.Timestamp - endRotationTime;

                        if (RotationSummary.ContainsKey(lastEndRotationReason))
                        {
                            RotationSummary[lastEndRotationReason] += agtEvent.Timestamp - endRotationTime;
                        }
                        else
                        {
                            RotationSummary.Add(lastEndRotationReason, new TimeSpan(0));
                            RotationSummary[lastEndRotationReason] += agtEvent.Timestamp - endRotationTime;
                        }
                    }

                    endRotationTime = today;

                    if (isLoggedIn)
                    {
                        isLoggedIn = false;
                        loginTime = loginTime.Add(agtEvent.Timestamp - startLoginTime);
                    }
                    else
                    {
                        loginTime = loginTime.Add(agtEvent.Timestamp - today);
                    }
                }
                if (agtEvent.Type == WorkEventType.EndRotation)
                {
                    endRotationTime = agtEvent.Timestamp;
                    lastEndRotationReason = agtEvent.Destination;
                    if (isInRotation)
                    {
                        isInRotation = false;
                        rotationTime = rotationTime.Add(agtEvent.Timestamp - startRotationTime);
                    }
                    else
                    {
                        rotationTime = rotationTime.Add(agtEvent.Timestamp - today);
                    }
                }
            }

            if (isLoggedIn)
            {
                loginTime = loginTime.Add(now - startLoginTime);
            }
            if (isInRotation)
            {
                rotationTime = rotationTime.Add(now - startRotationTime);
            }

            this.TotalLogin = loginTime;
            this.TotalRotation = rotationTime;
        }

        private void CatchPositionStateChanged(PositionInformation position)
        {
            if (!position.IsInRotation)
            {
                this.OutOfRotationReason = Utility.SQLWrapper.GetLastOutRotationReason(this.Id);
            }
            else
            {
                this.OutOfRotationReason = string.Empty;
            }
        }
    }
}