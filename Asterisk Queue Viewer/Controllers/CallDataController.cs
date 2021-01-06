using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Description;
using Asterisk_Queue_Viewer.Models;

namespace Asterisk_Queue_Viewer.Controllers
{
    public class CallDataController : ApiController
    {
        [HttpGet]
        public IHttpActionResult GetAgents(string site)
        {
            return Ok(Utility.Answer1Dashboard.GetAgents(site));
        }

        [HttpGet]
        public IHttpActionResult GetPositions(string site)
        {
            return Ok(Utility.Answer1Dashboard.GetStations(site));
        }

        [HttpGet]
        public IHttpActionResult GetCalls()
        {
            return Ok(Utility.Answer1Dashboard.GetCalls());
        }

		[HttpGet]
		public IHttpActionResult GetActions()
		{
			return Ok(Utility.Answer1Dashboard.GetActions());
		}

        [HttpGet]
        public IHttpActionResult GetDayStatistics(string site)
        {
            //Alert- Ignore sites on statistics
            return Ok(Utility.Answer1Dashboard.DayStatistics["All"]);

            //return Ok(Utility.Answer1Dashboard.DayStatistics[site]);
        }

        [HttpGet]
        public IHttpActionResult GetStatHistory(string site)
        {
            //Alert- Ignore sites on history
            return Ok(Utility.Answer1Dashboard.GetPeriodTotals("All").Where(x => !x.IsPeriodInFuture()).OrderByDescending(x => x.PeriodStart));

            //return Ok(Utility.Answer1Dashboard.GetPeriodTotals(site).Where(x => !x.IsPeriodInFuture()).OrderByDescending(x => x.PeriodStart));
        }

        [HttpGet]
        public IHttpActionResult GetRotationAndLog()
        {
            return Ok(Utility.Answer1Dashboard.RotationAndLogin().Take(100));
        }

        [HttpGet]
        public IHttpActionResult GetAgentById(int id)
        {
            Models.New.AgentReturnModel agent = null;
            while(agent == null)
            {
                agent = Utility.Answer1Dashboard.GetAgents().FirstOrDefault(x => x.Id == id);
                Thread.Sleep(10);
            }

            return Ok(agent);
        }

        [HttpGet]
        public IHttpActionResult GetQueueStats(int id, string site)
        {
            //Alert- Ignore queue stats for sites
            return Ok(Utility.Answer1Dashboard.GetQueuePeriodTotals(id, "All").Where(x => !x.IsPeriodInFuture()).OrderByDescending(x => x.PeriodStart));
            //return Ok(Utility.Answer1Dashboard.GetQueuePeriodTotals(id, site).Where(x => !x.IsPeriodInFuture()).OrderByDescending(x => x.PeriodStart));
        }

        [HttpGet]
        public IHttpActionResult GetQueueTotal(int id, string site)
        {
            //Alert - Ignore 
            return Ok(Utility.Answer1Dashboard.GetQueuePeriodTotal(id, "All"));

            //return Ok(Utility.Answer1Dashboard.GetQueuePeriodTotal(id, site));
        }

        [HttpGet]
        public IHttpActionResult GetVerticalStats(string id, string site)
        {
            //Alert- ignore site
            return Ok(Utility.Answer1Dashboard.GetVerticalPeriodTotals(id, "All").Where(x => !x.IsPeriodInFuture()).OrderByDescending(x => x.PeriodStart));

            //return Ok(Utility.Answer1Dashboard.GetVerticalPeriodTotals(id, site).Where(x => !x.IsPeriodInFuture()).OrderByDescending(x => x.PeriodStart));
        }

        [HttpGet]
        public IHttpActionResult GetVerticalTotal(string id, string site)
        {
            //Alert- Ignore site
            return Ok(Utility.Answer1Dashboard.GetVerticalPeriodTotal(id, "All"));

            //return Ok(Utility.Answer1Dashboard.GetVerticalPeriodTotal(id, site));
        }

        [HttpGet]
        public IHttpActionResult GetVerticals()
        {
            var verticals = new List<object>();
            foreach(string key in Utility.Answer1Dashboard.VerticalQueueMap.Keys)
            {
                var queues = Utility.Answer1Dashboard.GetQueues().Where(x => Utility.Answer1Dashboard.VerticalQueueMap[key].Contains(x.Id));
                var calls = queues.Sum(x => x.Calls.Count);
                verticals.Add(new { id = key, calls = calls });
            }
            //Utility.Answer1Dashboard.VerticalQueueMap.Keys.ToList().ForEach(x => verticals.Add(new { id = x, calls =  }));
            return Ok(verticals);
        }

        [HttpGet]
        public IHttpActionResult GetQueuesForAgent(int id)
        {
            var queues = Utility.Answer1Dashboard.GetQueues().Where(x => x.Agents.FirstOrDefault(y => y.Id == id) != null);
            var returnData = queues.OrderBy(x => x.Id).Select(x => x.BuildReturnModel());

            return Ok(returnData);
        }

        [HttpGet]
        public IHttpActionResult GetEventsForAgent(int id)
        {
            var rotationAndLoginEvents = Utility.Answer1Dashboard.RotationAndLogin().Where(x => x.AgentId == id);
            return Ok(rotationAndLoginEvents);
        }

        [HttpGet]
        public IHttpActionResult GetQueues()
        {
            var queues = Utility.Answer1Dashboard.GetQueues().OrderBy(x => x.Id).ThenBy(x => x.Id).Select(x => x.BuildReturnModel());
            
            //queues.ForEach(x => x.Agents.Clear());
            return Ok(queues);
        }

        [HttpGet]
        public IHttpActionResult ClearCaches()
        {
            Utility.Answer1Dashboard.ResetCache();
            return Ok();
        }


        [HttpGet]
        public IHttpActionResult GetCallsForVertical(string id, string site)
        {
            if (Utility.Answer1Dashboard.VerticalQueueMap.ContainsKey(id))
            {
                var queueids = Utility.Answer1Dashboard.VerticalQueueMap[id];
                var queues = Utility.Answer1Dashboard.GetQueues().Where(x => queueids.Contains(x.Id));
                var calls = new List<Models.New.QueueCallReturnModel>();
                foreach(Models.New.Queue queue in queues)
                {
                    if(queue.Calls.Count > 0)
                    {
                        calls.AddRange(queue.Calls);
                    }
                }
                return Ok(calls.OrderByDescending(x => x.TimerInSeconds));
                //return Ok(calls.Where(x => x.Site == "All").OrderByDescending(x => x.TimerInSeconds));
                //return Ok(calls.Where(x => x.Site == site || site == "All").OrderByDescending(x => x.TimerInSeconds));
            }
            else
            {
                return Ok(new List<Models.New.QueueCallReturnModel>());
            }
        }

        [HttpGet]
        public IHttpActionResult GetSites()
        {
            try
            {
                return Ok(Utility.Answer1Dashboard.SiteList);
            }
            catch
            {
                return Ok(new List<string>());
            }
        }

        [HttpGet]
        public IHttpActionResult GetAgentsForVertical(string id, string site)
        {
            if (Utility.Answer1Dashboard.VerticalQueueMap.ContainsKey(id))
            {
                var queueids = Utility.Answer1Dashboard.VerticalQueueMap[id];
                var queues = Utility.Answer1Dashboard.GetQueues().Where(x => queueids.Contains(x.Id));
                var returnData = new List<Models.New.StationReturnModel>();
                foreach (Models.New.Queue queue in queues)
                {
                    foreach(Models.New.AgentInformation agent in queue.Agents)
                    {
                        var positions = agent.Positions.Select(x => x.PositionNumber);
                        if (!returnData.Any(x => positions.Contains(x.PostionNumber)))
                        {
                            returnData.AddRange(agent.GetStationReturnModel());
                        }
                    }
                    //queue.Agents.ForEach(x => returnData.AddRange(x.GetStationReturnModel()));
                }
                return Ok(returnData.Where(x => x.Site == site || site == "All").OrderBy(x => x.State).ThenByDescending(x => x.IdleTimeInSeconds).ToList());
            }
            else
            {
                return Ok(new List<Models.New.StationReturnModel>());
            }
        }

        [HttpGet]
        public IHttpActionResult GetQueueCalls(int id, string site)
        {
            Models.New.Queue queue = null;
            while(queue == null)
            {
                queue = Utility.Answer1Dashboard.GetQueues().Where(x => x.Id == id).ToList().FirstOrDefault();
                Thread.Sleep(10);
            }
            return Ok(queue.Calls.Where(x => x.Site == site || site == "All").OrderByDescending(x => x.TimerInSeconds));
        }

        //[HttpGet]
        //public IHttpActionResult GetSites()
        //{
        //    return Ok(Utility.Answer1Dashboard.Sites());
        //}

        [HttpGet]
        public IHttpActionResult GetQueueAgents(int id, string site)
        {
            Models.New.Queue queue = null;
            List<Models.New.StationReturnModel> returnData = new List<Models.New.StationReturnModel>();
            while (queue == null)
            {
                queue = Utility.Answer1Dashboard.GetQueues().Where(x => x.Id == id).ToList().FirstOrDefault();
                Thread.Sleep(10);
            }
            queue.Agents.ForEach(x => returnData.AddRange(x.GetStationReturnModel()));
			//returnData.ForEach(d => d.State)
            returnData = returnData.Where(x => x.Site == site || site == "All").OrderBy(x => x.State).ToList();

            return Ok(returnData);
        }

    }
}
