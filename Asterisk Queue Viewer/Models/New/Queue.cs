using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Asterisk_Queue_Viewer.Models.New
{
    public class Queue
    {
        
        public int Id { get; set; }

        public string Name { get; set; }

        public string AffinityName { get; set; }

        internal List<AgentInformation> Agents { get; set; }

        public List<QueueCallReturnModel> Calls { get; set; }

        public Queue()
        {
            this.Agents = new List<AgentInformation>();
            this.Calls = new List<QueueCallReturnModel>();
        }

        internal List<StationReturnModel> GetPositions()
        {
            var returnData = new List<StationReturnModel>();
            lock (Agents)
            {
                Agents.ForEach(x => returnData.AddRange(x.GetStationReturnModel()));
            }
            return returnData;
        }

        internal void RemoveAgent(int agentId)
        {
            lock (Agents)
            {
                Agents.RemoveAll(x => x.Id == agentId);
            }
        }

        internal void AddAgent(AgentInformation agent)
        {
            lock (Agents)
            {
                if(!Agents.Any(x => x.Id == agent.Id))
                {
                    Agents.Add(agent);
                }
                //var model = agent.GetAgentReturnModel();
                //if(!Agents.Any(x => x.Id == agent.Id))
                //{
                //    Agents.Add(agent);
                //}
            }
        }

        internal bool DoesCallExist(string uniqueId)
        {
            var returnValue = false;
            lock (Calls)
            {
                returnValue = Calls.Any(x => x.UniqueId == uniqueId);
            }
            return returnValue;
        }

        internal List<QueueCallReturnModel> GetCalls()
        {
            var returnData = new List<QueueCallReturnModel>();
            Calls.ForEach(x => returnData.Add(x));
            return returnData;
        }

        internal void RemoveCall(string uniqueId)
        {
            lock (Calls)
            {
                Calls.RemoveAll(x => x.UniqueId == uniqueId);
            }
        }

        internal void AddCall(QueueCallReturnModel call)
        {
            lock (Calls)
            {
                var queueCall = Calls.FirstOrDefault(x => x.UniqueId == call.UniqueId);
                if(queueCall == null)
                {
                    Calls.Add(call);
                }
            }
        }

        internal QueueReturnModel BuildReturnModel()
        {
            int inMute = 0;
            int outMute = 0;
            int inTalk = 0;
            int outTalk = 0;

            foreach(AgentInformation agt in Agents)
            {
                foreach(PositionInformation pos in agt.Positions)
                {
                    if (pos.CurrentState.HasFlag(PositionInformation.PositionState.InRotation) && !pos.CurrentState.HasFlag(PositionInformation.PositionState.InTalk))
                    {
                        inMute++;
                    }
                    if (pos.CurrentState.HasFlag(PositionInformation.PositionState.InRotation) && pos.CurrentState.HasFlag(PositionInformation.PositionState.InTalk))
                    {
                        inTalk++;
                    }
                    if (!pos.CurrentState.HasFlag(PositionInformation.PositionState.InRotation) && !pos.CurrentState.HasFlag(PositionInformation.PositionState.InTalk))
                    {
                        outMute++;
                    }
                    if (!pos.CurrentState.HasFlag(PositionInformation.PositionState.InRotation) && pos.CurrentState.HasFlag(PositionInformation.PositionState.InTalk))
                    {
                        outTalk++;
                    }
                }
            }

            return new QueueReturnModel()
            {
                AffinityName = AffinityName,
                Agents = inMute + inTalk,
                Calls = Calls.Count,
                Id = Id,
                InMuteAgents = inMute,
                InTalkAgents = inTalk,
                OutMuteAgents = outMute,
                OutTalkAgents = outTalk
            };
        }
    }
}