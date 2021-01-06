using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;
using AsterNET.Manager;
using AsterNET;
using System.Collections.ObjectModel;

namespace Asterisk_Queue_Viewer.Utility
{
    internal static class AsteriskWrapper
    {
        static private System.Timers.Timer refreshTimer;
        static private List<string> tempChannelList = null;
        static private List<AsterNET.Manager.Event.StatusEvent> tester;

        static private ManagerConnection Connection { get; set; }

        static private System.Text.RegularExpressions.Regex InternalExtensionMatcher { get; set; }

        static internal void Initialize()
        {
            Connection = new ManagerConnection("ss-a.a1sopps.local", 5038, "Christopher", "muhahaha");
            tester = new List<AsterNET.Manager.Event.StatusEvent>();
            try
            {
                Connection.Login();
            }
            catch (Exception ex)
            {
                throw new Exception("AsteriskWrapper Failed To Login", ex);
            }

            InternalExtensionMatcher = new System.Text.RegularExpressions.Regex(@"(SIP/9)\d\d\d");

            //Event Handler for calls Joining/Leaving queues
            Connection.Join += Connection_Join;
            Connection.Leave += Connection_Leave;

            Connection.AgentConnect += Connection_AgentConnect;

            //Event Handlers for new calls going into the switch 
            Connection.NewChannel += Connection_NewChannel;
            Connection.Hangup += Connection_Hangup;
            Connection.Status += Connection_Status;
            Connection.QueueParams += Connection_QueueParams;

            refreshTimer = new System.Timers.Timer();
            refreshTimer.Interval = 30000;
            refreshTimer.AutoReset = true;
            refreshTimer.Enabled = true;
            refreshTimer.Elapsed += RefreshTimer_Elapsed;
            refreshTimer.Start();

            ////get the current calls in queue 
            //var response = Connection.SendAction(new AsterNET.Manager.Action.CommandAction("queue show"));
            //if(response is AsterNET.Manager.Response.CommandResponse)
            //{
            //    var readingQueue = -1;
            //    var queueStartLineMatcher = new Regex(@"^(\d{3}\shas\s\d*)");
            //    var queueIdMatcher = new Regex(@"^(\d*)\shas");
            //    foreach(string line in ((AsterNET.Manager.Response.CommandResponse)response).Result)
            //    {
            //        //Check and see if this line is a start of a new queue
            //        if (queueStartLineMatcher.IsMatch(line))
            //        {
            //            var queueid = -1;
            //            if(int.TryParse(queueIdMatcher.Match(line).Groups[1].ToString(), out queueid))
            //            {
            //                readingQueue = queueid;
            //            }
                        
            //        }

            //        if(readingQueue > 0 && line == "Callers")
            //        {

            //        }
            //    }
            //}

        }

        private static void Connection_QueueParams(object sender, AsterNET.Manager.Event.QueueParamsEvent e)
        {
            var test = "";
        }

        private static void Connection_Status(object sender, AsterNET.Manager.Event.StatusEvent e)
        {
            if (e.Context != null && e.Context == "DynConference") return;
            if (e.Attributes != null &&  !e.Attributes.ContainsKey("bridgedchannel"))
            {
                //tempChannelList.Add(e.UniqueId);
                tempChannelList.Add(e.Channel);
            }
        }

        private static void RefreshTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (tempChannelList == null)
            {
                tempChannelList = new List<string>();
            }
            else
            {
                //var difference = tempChannelList.Except(CallCollection.GetList().Select(x => x.UniqueID));
                ////var difference = CallCollection.GetList().Select(x => x.UniqueID).Except(tempChannelList);
                //foreach (string item in difference)
                //{
                //    CallCollection.Remove(item);
                //}

                //foreach (var call in CallCollection.GetList().Where(x => x.WaitTime > 100))
                //{
                //    if (!tempChannelList.Contains(call.UniqueID))
                //    {
                //        CallCollection.Remove(call.UniqueID);
                //    }
                //}

                foreach (var call in CallCollection.GetList().Where(x => x.WaitTime > 100))
                {
                    if (!tempChannelList.Contains(call.ChannelID))
                    {
                        CallCollection.Remove(call.ChannelID);
                    }
                }
            }
            try
            {
                tempChannelList = new List<string>();
                tester.Clear();
                Connection.SendAction(new AsterNET.Manager.Action.StatusAction());
                //Connection.SendAction(new AsterNET.Manager.Action.QueueStatusAction());
            }
            catch { }
        }

        static void Connection_Hangup(object sender, AsterNET.Manager.Event.HangupEvent e)
        {
            try
            {
                //CallCollection.Remove(e.UniqueId);
                CallCollection.Remove(e.Channel);
            }
            catch (Exception ex)
            {
                var test = "";
            }
        }

        static void Connection_NewChannel(object sender, AsterNET.Manager.Event.NewChannelEvent e)
        {
            try
            {
                if(e.Channel == null) { return; }

                if (!InternalExtensionMatcher.IsMatch(e.Channel)) 
                {
                    //CallCollection.Add(new Models.QueueCall()
                    //{
                    //    CallerName = e.CallerIdName,
                    //    ChannelID = e.Channel,
                    //    ANI = e.CallerIdNum,
                    //    ClientID = e.Attributes.ContainsKey("exten") ? e.Attributes["exten"] : "",
                    //    StartTime = DateTime.Now,
                    //    UniqueID = e.UniqueId.Trim()
                    //});
                    CallCollection.Add(new Models.QueueCall()
                    {
                        CallerName = e.CallerIdName,
                        ChannelID = e.Channel,
                        ANI = e.CallerIdNum,
                        ClientID = e.Attributes.ContainsKey("exten") ? e.Attributes["exten"] : "",
                        StartTime = DateTime.Now
                    });
                }
            }
            catch (Exception ex)
            {
                var test = "";
            }
        }

        static void Connection_AgentConnect(object sender, AsterNET.Manager.Event.AgentConnectEvent e)
        {
            try
            {
                //Models.QueueCall c = CallCollection.Get(e.UniqueId);
                Models.QueueCall c = CallCollection.Get(e.Channel);
                if (c != null)
                {
                    if (!c.Answered)
                    {
                        c.Answered = true;
                        if (c.AnswerTime == null) 
                        {
                            c.AnswerTime = DateTime.Now;

                        }
                        c.TimeToAnswer = (int)c.AnswerTime.Value.Subtract(c.StartTime.Value).TotalSeconds;
                    }

                    if (c.CallType != null)
                    {
                        if (c.CallType.ToLower() == "h")
                        {

                            c.HoldTime += (int)DateTime.Now.Subtract(c.HoldStartTime.Value).TotalSeconds;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                var stop = "";
            }
        }

        static void Connection_Leave(object sender, AsterNET.Manager.Event.LeaveEvent e)
        {
            try
            {
                //Models.QueueCall c = CallCollection.Get(e.UniqueId);
                Models.QueueCall c = CallCollection.Get(e.Channel);
                if (c != null)
                {
                    c.InQueue = false;
                }
            }
            catch (Exception ex)
            {
                var test = "";
            }
        }

        static void Connection_Join(object sender, AsterNET.Manager.Event.JoinEvent e)
        {
            try
            {
                //Models.QueueCall c = CallCollection.Get(e.UniqueId);
                Models.QueueCall c = CallCollection.Get(e.Channel);
                if (c != null)
                {
                    c.Position = e.Position;
                    c.Answered = false;
                    c.QueueStartTime = DateTime.Now;
                    c.InQueue = true;
                    if (e.Attributes.ContainsKey("type"))
                    {
                        c.CallType = e.Attributes["type"];
                        if (c.CallType.ToLower() == "h") 
                        {
                            c.HoldStartTime = DateTime.Now;
                        }
                    }
                    else
                    {
                        c.CallType = "";
                    }
                    if (e.CallerIdName.Contains("_"))
                    {
                        c.ClientID = e.CallerIdName.Substring(0, e.CallerIdName.IndexOf("_"));
                        c.ClientName = StartelData.GetClientName(c.ClientID);
                    }
                    else
                    {
                        c.ClientID = e.CallerIdName;
                        c.ClientName = StartelData.GetClientName(c.ClientID);
                    }

                    c.Queue = e.Queue;
                    c.Affinity = StartelData.GetAffinityName(e.Queue);
                }
            }
            catch (Exception ex)
            {
                var stop = "";
            }
        }

        static internal void Dispose()
        {
            Connection.Logoff();
        }
    }
}