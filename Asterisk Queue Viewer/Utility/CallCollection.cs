using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Asterisk_Queue_Viewer.Models;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;

namespace Asterisk_Queue_Viewer.Utility
{
    internal delegate void CallAddedHandler(QueueCall call);
    internal delegate void CallRemovedHandler(QueueCall call);

    internal static class CallCollection
    {
        static private List<QueueCall> Calls { get; set; }

        //static private List<QueueCall> CallHistory { get; set; }

        static internal event CallAddedHandler CallAdded;
        static internal event CallRemovedHandler CallRemoved;

        //static internal CallHistoryStats CallStats { get; private set; }

        static internal void Initialize()
        {
            Calls = new List<QueueCall>();
            //CallHistory = new List<QueueCall>();
            //CallStats = new CallHistoryStats()
            //{
            //    AverageHoldTime = 0,
            //    AverageTimeToAnswer = 0,
            //    CachedCalls = 0,
            //    TotalCalls = 0,
            //    TotalCallsAnswered = 0,
            //    CallsAbandoned3To8 = 0,
            //    CallsAbandoned9To16 = 0,
            //    CallsAbandoned17To24 = 0,
            //    CallsAbandoned24Plus = 0,
            //    ActiveCalls = 0,
            //    SLA = "0%"
            //};
        }

        static internal void Add(QueueCall call)
        {
            //lock (Calls)
            //{
            //    CallStats.TotalCalls += 1;
            //    if (!Contains(call.UniqueID))
            //    {
            //        Calls.Add(call);
            //    }
            //    Calls.RemoveAll(x => x == null);
            //    CallStats.ActiveCalls = Calls.Count;
            //}
            lock (Calls)
            {
                //CallStats.TotalCalls += 1;
                if (!Contains(call.ChannelID))
                {
                    Calls.Add(call);
                }
                Calls.RemoveAll(x => x == null);
                //CallStats.ActiveCalls = Calls.Count;
            }
        }

        static internal void Remove(QueueCall call)
        {
            lock (Calls)
            {
                if (Contains(call))
                {
                    //if (!call.ChannelID.ToLower().Contains("centuri")) AddToCallHistory(call);
                    Calls.Remove(call);
                    Calls.RemoveAll(x => x.ANI == null);
                    call.IsLocked = true;
                    //UpdateCallStats();
                }
            }
        }

        static internal void Remove(string channelId)
        {
            lock (Calls)
            {
                if (Contains(channelId))
                {
                    QueueCall call = Calls.FirstOrDefault(x => x.ChannelID == channelId);
                    //if (!call.ChannelID.ToLower().Contains("centuri")) AddToCallHistory(call);
                    Calls.Remove(call);
                    Calls.RemoveAll(x => x.ANI == null);
                    call.IsLocked = true;
                    //UpdateCallStats();
                }
            }
        }

        static internal bool Contains(QueueCall call)
        {
            return Calls.Contains(call);
        }

        static internal bool Contains(string channelId)
        {
            return Calls.FirstOrDefault(x => x.ChannelID == channelId) == null ? false : true;
        }

        static internal void Clear()
        {
            Calls.Clear();
        }

        static internal QueueCall Get(string channelId)
        {
            lock (Calls)
            {
                return Calls.FirstOrDefault(x => x.ChannelID == channelId);
            }
        }

        static internal List<QueueCall> GetList()
        {
            return Calls.ToList();
        }

        static internal IEnumerator<QueueCall> GetEnumerator()
        {
            return Calls.GetEnumerator();
        }

        static internal int Count { get { return Calls.Count; } }

        //static private void AddToCallHistory(QueueCall call)
        //{
        //    lock (CallHistory)
        //    {
        //        if (CallHistory.Count >= 500) { CallHistory.RemoveAt(0); }
        //        CallHistory.Add(call);
        //    }
        //}

        //static private void UpdateCallStats()
        //{
        //    lock (CallHistory)
        //    {
        //        List<QueueCall> heldCalls = CallHistory.FindAll(x => x.HoldTime > 0);
        //        List<QueueCall> answeredCalls = CallHistory.FindAll(x => x.Answered);
        //        List<QueueCall> abandonedCalls = CallHistory.FindAll(x => x.Queue != null && !x.Answered);

        //        CallStats.CachedCalls = CallHistory.Count;

        //        if (heldCalls.Count > 0) 
        //        { 
        //            CallStats.AverageHoldTime = (int)heldCalls.Average(x => x.HoldTime); 
        //        }

        //        if (answeredCalls.Count > 0)
        //        {
        //            CallStats.AverageTimeToAnswer = (int)answeredCalls.Average(x => x.TimeToAnswer);
        //            CallStats.TotalCallsAnswered = answeredCalls.Count;
        //        }

        //        if (abandonedCalls.Count > 0) 
        //        {
        //            CallStats.CallsAbandoned3To8 = abandonedCalls.Count(x => x.WaitTime >= 3 && x.WaitTime <= 8);
        //            CallStats.CallsAbandoned9To16 = abandonedCalls.Count(x => x.WaitTime >= 9 && x.WaitTime <= 16);
        //            CallStats.CallsAbandoned17To24 = abandonedCalls.Count(x => x.WaitTime >= 17 && x.WaitTime <= 24);
        //            CallStats.CallsAbandoned24Plus = abandonedCalls.Count(x => x.WaitTime > 24);
        //        }

        //        try
        //        {
        //            int callsUnder24 = answeredCalls.Count(x => x.TimeToAnswer < 24);
        //            if (callsUnder24 > 0) 
        //            {
        //                double slaNum = Math.Round(((double)callsUnder24 / (double)answeredCalls.Count) * 100, 1);
        //                if (!double.IsInfinity(slaNum) || !double.IsNaN(slaNum)) CallStats.SLA = string.Format("{0} %", slaNum.ToString());
        //                else CallStats.SLA = "0 %"; 
        //            }
        //        }
        //        catch 
        //        {
        //            CallStats.SLA = "0 %";
        //        }
        //    }
        //}
    }
}