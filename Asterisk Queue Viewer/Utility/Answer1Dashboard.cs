using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Asterisk_Queue_Viewer.Models.New;
using NLog;
using Newtonsoft.Json;

namespace Asterisk_Queue_Viewer.Utility
{
    public static class Answer1Dashboard
    {
        // Values for _ignoreAgents moved to ExclusionList.json to support site-specific lists - 1/24/19 DJ
        internal static List<int> _ignoreAgents; // = new List<int>() { 0, 1, 143, 149, 178, 232, 233, 235, 257, 258, 270, 293, 365, 433, 464, 639, 756, 466, 766, 767, 795, 871, 894, 919, 921, 939, 1005, 1020, 1074, 1114, 1116, 1187, 1200 };
        internal static Dictionary<string, List<int>> VerticalQueueMap = new Dictionary<string, List<int>>();
        internal static QueueConfiguration DEFAULT_QUEUE_CONFIGURATION = new QueueConfiguration() { WarningTimeoutInSeconds = 12, DangerTimeoutInSeconds = 24 ,GroupingName = "None", QueueId = -1 };
        internal static List<int> DEFAULT_EXCLUSION_LIST = new List<int>() { 0, 1, 143, 149, 178, 232, 233, 235, 257, 258, 270, 293, 365, 433, 464, 639, 756, 466, 766, 767, 795, 871, 894, 919, 921, 939, 1005, 1020, 1074, 1114, 1116, 1187, 1200 };
        internal static List<QueueConfiguration> QueueConfiguration = new List<QueueConfiguration>();

		private static object syncRoot = new object();
        private static BlockingCollection<string> StartelServiceMessageQueue { get; set; }
        private static List<Task> ThreadCollection { get; set; }
        private static StartelConnector StartelConnector { get; set; }
        private static bool ShouldThreadExit { get; set; }
        private static bool ShouldBackFill { get; set; }
		private static DateTime LastPopulateWorkCache { get; set; }
		private static DateTime LastPopulateActionCache { get; set; }
        private static List<AgentInformation> AgentCache { get; set; }
        private static List<StationReturnModel> StationReturnCache { get; set; }
        private static List<QueueCallReturnModel> CallReturnCache { get; set; }
        private static Dictionary<string, Dictionary<int, List<QueuePeriodReturnModel>>> QueuePeriodCache { get; set; }
        private static Dictionary<string, Dictionary<string, List<QueuePeriodReturnModel>>> VerticalPeriodCache { get; set; }

		public static List<string> SiteList { get; set; }
        private static List<ClientInformation> ClientCache { get; set; }
        private static List<Answer1APILib.Plugin.Startel.Models.SiteInformation> SiteCache { get; set; }
        private static List<Queue> QueueCache { get; set; }
        private static List<AgentReturnModel> AgentReturnCache { get; set; }
        private static List<QueueCallReturnModel> ActiveCallCache { get; set; }
		private static List<EndedCallReturnModel> EndedCallCache { get; set; }
        private static List<Answer1APILib.Plugin.Startel.Models.WorkEvent> WorkCache { get; set; }
        private static List<Answer1APILib.Plugin.Startel.CallRecord> CallHistoryCache { get; set; }
		private static List<ActionEvent> ActionCompleteCache { get; set; }
		private static List<ActionDueEvent> ActionDueCache { get; set; }
        //private static List<DayPeriodReturnModel> DayPeriodReturnModelCache { get; set; }
		private static Dictionary<string, List<DayPeriodReturnModel>> DayPeriodReturnModelCache { get; set; }
        private static List<RotationAndLoginReturnModel> RotationAndLoginReturnCache { get; set; }
        private static Dictionary<string, Dictionary<int, QueuePeriodReturnModel>> QueuePeriodTotalCache { get; set; }
        private static Dictionary<string, Dictionary<string, QueuePeriodReturnModel>> VerticalPeriodTotalCache { get; set; }
        private static Logger Logger { get; set; }
        
        public static void ResetCache()
        {
            AgentCache = new List<AgentInformation>();
            SiteCache = new List<Answer1APILib.Plugin.Startel.Models.SiteInformation>();
            SQLWrapper.ClearCaches();
        }
        
        public static void Stop()
        {
            Logger.Debug("Answer1Dashboard -> Waiting for Startel Threads to Stop");
            StartelConnector.Stop();
            Logger.Debug("Answer1Dashboard -> Startel Threads Stopped. Waiting for Kernal Threads to Stop");
            ShouldThreadExit = true;
            StartelServiceMessageQueue.CompleteAdding();
            ThreadCollection.ForEach(x => x.Dispose());

            Task.WaitAll(ThreadCollection.ToArray());
            Logger.Debug("Answer1Dashboard -> Kernal Threads Stopped. Shutting Down Kernal");
        }

        public static void Initialize()
        {
            Logger = LogManager.GetCurrentClassLogger();

            Logger.Debug("Answer1Dashboard -> Starting");

            SQLWrapper.Initialize();

            ShouldBackFill = true;

			DayStatistics = new Dictionary<string, DayStatisticsReturnModel>();

            if(VerticalPeriodTotalCache != null)
            {
                VerticalPeriodTotalCache.Clear();
            }
			VerticalPeriodTotalCache = new Dictionary<string, Dictionary<string, QueuePeriodReturnModel>>();

            if(QueuePeriodTotalCache != null)
            {
                QueuePeriodTotalCache.Clear();
            }
			QueuePeriodTotalCache = new Dictionary<string, Dictionary<int, QueuePeriodReturnModel>>();

            if (QueuePeriodCache != null)
            {
                QueuePeriodCache.Clear();
            }
            QueuePeriodCache = new Dictionary<string, Dictionary<int, List<QueuePeriodReturnModel>>>();

            if (VerticalPeriodCache != null)
            {
                VerticalPeriodCache.Clear();
            }
            VerticalPeriodCache = new Dictionary<string, Dictionary<string, List<QueuePeriodReturnModel>>>();

            if(ActiveCallCache != null)
            {
                ActiveCallCache.Clear();
            }
            ActiveCallCache = new List<QueueCallReturnModel>();

			if (EndedCallCache != null)
			{
				EndedCallCache.Clear();
			}
			EndedCallCache = new List<EndedCallReturnModel>();

            if(RotationAndLoginReturnCache != null)
            {
                RotationAndLoginReturnCache.Clear();
            }
            RotationAndLoginReturnCache = new List<RotationAndLoginReturnModel>();

            if (DayPeriodReturnModelCache != null)
            {
                DayPeriodReturnModelCache.Clear();
            }
            DayPeriodReturnModelCache = new Dictionary<string, List<DayPeriodReturnModel>>();

			if (WorkCache != null)
            {
                WorkCache.Clear();
            }
            WorkCache = new List<Answer1APILib.Plugin.Startel.Models.WorkEvent>();

            if (CallHistoryCache != null)
            {
                CallHistoryCache.Clear();
            }
            CallHistoryCache = new List<Answer1APILib.Plugin.Startel.CallRecord>();

			if (ActionDueCache != null)
			{
				ActionDueCache.Clear();
			}
			ActionDueCache = new List<ActionDueEvent>();

			if (ActionCompleteCache != null)
			{
				ActionCompleteCache.Clear();
			}
			ActionCompleteCache = new List<ActionEvent>();

			if (AgentReturnCache != null)
            {
                AgentReturnCache.Clear();
            }
            AgentReturnCache = new List<AgentReturnModel>();

            if (QueueCache != null)
            {
                QueueCache.Clear();
            }
            QueueCache = new List<Queue>();

            if (AgentCache != null)
            {
                AgentCache.Clear();
            }
            AgentCache = new List<AgentInformation>();

            if (StationReturnCache != null)
            {
                StationReturnCache.Clear();
            }
            StationReturnCache = new List<StationReturnModel>();

            if (CallReturnCache != null)
            {
                CallReturnCache.Clear();
            }
            CallReturnCache = new List<QueueCallReturnModel>();

            if (ClientCache != null)
            {
                ClientCache.Clear();
            }
            ClientCache = new List<ClientInformation>();

            if (StartelServiceMessageQueue != null)
            {
                StartelServiceMessageQueue.Dispose();
            }
            StartelServiceMessageQueue = new BlockingCollection<string>();

            if(SiteCache != null)
            {
                SiteCache.Clear();
            }
            SiteCache = new List<Answer1APILib.Plugin.Startel.Models.SiteInformation>();

			SiteList = new List<string>();
            SiteList.Add("All");
            SQLWrapper.GetSites().ForEach(x => SiteList.Add(x.DisplayName));
			//SiteList.Add("All");
			//SiteList.Add("Phoenix");
			//SiteList.Add("Richmond");
			//SiteList.Add("Lake Havasu");
			//SiteList.Add("Mexico");
            
			foreach (string site in SiteList)
			{
				DayStatistics.Add(site, new DayStatisticsReturnModel());
				DayPeriodReturnModelCache.Add(site, new List<DayPeriodReturnModel>());
				VerticalPeriodTotalCache.Add(site, new Dictionary<string, QueuePeriodReturnModel>());
				VerticalPeriodCache.Add(site, new Dictionary<string, List<QueuePeriodReturnModel>>());
				QueuePeriodTotalCache.Add(site, new Dictionary<int, QueuePeriodReturnModel>());
				QueuePeriodCache.Add(site, new Dictionary<int, List<QueuePeriodReturnModel>>());
			}

			if (ThreadCollection != null)
            {
                ShouldThreadExit = true;
                Task.WaitAll(ThreadCollection.ToArray());
                ThreadCollection.ForEach(x => x.Dispose());
                ThreadCollection.Clear();
            }

            try
            {
                var configFilePath = HttpContext.Current.Server.MapPath("~/QueueSettings.json");
                var fileContent = System.IO.File.ReadAllText(configFilePath);
                QueueConfiguration = Newtonsoft.Json.JsonConvert.DeserializeObject<List<QueueConfiguration>>(fileContent);
            }
            catch(Exception ex)
            {
                Logger.Error(string.Format("Initialize -> Cant Set Queue Settings {0}", ex.Message));
                QueueConfiguration = new List<QueueConfiguration>();
                QueueConfiguration.Add(DEFAULT_QUEUE_CONFIGURATION);
            }

            try
            {
                var listFilePath = HttpContext.Current.Server.MapPath("~/ExclusionList.json");
                var listContent = System.IO.File.ReadAllText(listFilePath);
                _ignoreAgents = Newtonsoft.Json.JsonConvert.DeserializeObject<List<int>>(listContent);
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format("Initialize -> Cant Set Exclusion List {0}", ex.Message));
                _ignoreAgents = DEFAULT_EXCLUSION_LIST;
            }

			//Logger.Debug("Init DayPeriodReturnModelCache");
			//SetupDayPeriodReturnModels();
			//Logger.Debug("Done Init DayPeriodReturnModelCache");

			ShouldThreadExit = false;
            ThreadCollection = new List<Task>();
            for (int i = 1; i <= Asterisk_Queue_Viewer.Properties.Settings.Default.StartelMessageWorkers; i++)
            {
                ThreadCollection.Add(Task.Run(() =>
                {
                    while (!ShouldThreadExit)
                    {
                        var message = StartelServiceMessageQueue.Take();
                        Logger.Debug("Answer1Dashboard -> Processing Message: {0} -> Number of Waiting: {1}", message, StartelServiceMessageQueue.Count);
                        HandleStartelMessage(message);
                        Logger.Debug("Answer1Dashboard -> Processing Message: {0} Complete", message);

                    }

                    Logger.Debug("Answer1Dashboard -> Shutting down Startel Message Handler Thread");
                }));
            }
			ThreadCollection.Add(Task.Run(() =>
			{
				while (!ShouldThreadExit)
				{
					lock (EndedCallCache)
					{
						EndedCallCache.RemoveAll(c => c.EndTime < DateTime.Now.AddMinutes(-10));
					}
					System.Threading.Thread.Sleep(60000);
				}
			}));
            ThreadCollection.Add(SetReturnModelThread());
            ThreadCollection.Add(GetWorkAndCallHistoryThread());
			ThreadCollection.Add(GetActionCurrentThread());
			ThreadCollection.Add(GetActionHistoryThread());
            ThreadCollection.Add(SetStatisticsThread());

            var StartelServiceData = SQLWrapper.GetStartelServiceData();
            if (StartelConnector != null)
            {
                StartelConnector.StartelMessageRecieved -= StartelConnector_StartelMessageRecieved;
                StartelConnector.Stop();
            }

            //Set the vertical map.
            var groupedQueueConfiguration = QueueConfiguration.GroupBy(x => x.GroupingName);
            foreach(var group in groupedQueueConfiguration)
            {
                VerticalQueueMap.Add(group.Key, new List<int>());
                foreach(var item in group)
                {
                    VerticalQueueMap[group.Key].Add(item.QueueId);
                }
            }

            //VerticalQueueMap.Add("Professional", new List<int>() { 100, 101, 102 });
            //VerticalQueueMap.Add("Spanish", new List<int>() { 107, 108, 117 });
            //VerticalQueueMap.Add("Medical", new List<int>() { 113, 114, 115, 200, 217 });
            //VerticalQueueMap.Add("Legal", new List<int>() { 202, 203, 215 });
            //VerticalQueueMap.Add("Commercial", new List<int>() { 132, 133, 134 });
            //VerticalQueueMap.Add("Tech", new List<int>() { 109, 110, 111, 212, 213 });
            //VerticalQueueMap.Add("Riverbed", new List<int>() { 209 });
            //VerticalQueueMap.Add("Fuze", new List<int>() { 210 });
            //VerticalQueueMap.Add("HVAC", new List<int>() { 129, 130, 131 });
            //VerticalQueueMap.Add("New Accounts", new List<int>() { 214, 216 });
            //VerticalQueueMap.Add("Raffle Coin Eagle", new List<int>() { 207, 211, 201 });
            //VerticalQueueMap.Add("Inland", new List<int>() { 401, 402, 406, 407, 450 });
            //VerticalQueueMap.Add("Insurance", new List<int>() { 126, 127 });

            //VerticalQueueMap.Add("Basic Accounts", new List<int>() { 21, 22, 23 });
            //VerticalQueueMap.Add("Home Health, Hospice, Consulting", new List<int>() { 24, 30 });
            //VerticalQueueMap.Add("Medical", new List<int>() { 25, 26 });
            //VerticalQueueMap.Add("Fire / Sprinkler Contractors", new List<int>() { 29 });
            //VerticalQueueMap.Add("Specific Training", new List<int>() { 31, 27 });
            //VerticalQueueMap.Add("Real Estate Investement", new List<int>() { 28 });

            StartelConnector = new StartelConnector(StartelServiceData.APIServer, StartelServiceData.APIPort, StartelServiceData.TPServer, StartelServiceData.TPPort);
            StartelConnector.StartelMessageRecieved += StartelConnector_StartelMessageRecieved;
            if (System.Configuration.ConfigurationManager.AppSettings["RuntimeEnviro"] != "Dev")
                StartelConnector.Start();
        }

        public static Dictionary<string, DayStatisticsReturnModel> DayStatistics { get; set; }
		//public static DayStatisticsReturnModel DayStatistics { get; set; }

        public static List<Answer1APILib.Plugin.Startel.Models.SiteInformation> Sites()
        {
            return new List<Answer1APILib.Plugin.Startel.Models.SiteInformation>(SiteCache);
        }

        public static QueuePeriodReturnModel GetQueuePeriodTotal(int queueId, string site = "All")
        {
			QueuePeriodReturnModel oReturn = new QueuePeriodReturnModel();
			//lock (QueuePeriodTotalCache)
			//{
				if (QueuePeriodTotalCache[site].ContainsKey(queueId))
				{
					oReturn = QueuePeriodTotalCache[site][queueId];
				}
			//}
			return oReturn;
        }

        public static QueuePeriodReturnModel GetVerticalPeriodTotal(string verticalName, string site = "All")
        {
			QueuePeriodReturnModel oReturn = new QueuePeriodReturnModel();
			//lock (QueuePeriodTotalCache)
			//{
				if (VerticalPeriodTotalCache[site].ContainsKey(verticalName))
				{
					oReturn = VerticalPeriodTotalCache[site][verticalName];
				}
			//}
			return oReturn;
        }

        public static List<AgentReturnModel> GetAgents(string site = "All")
        {
			while(AgentReturnCache.Count == 0) { System.Threading.Thread.Sleep(10); }
			if (site != "All")
			{
				return AgentReturnCache.Where(a => a.Site == site).ToList();
			} else
			{
				return AgentReturnCache;
			}
		}

        public static List<StationReturnModel> GetStations(string site = "All")
        {
			/*var retryCount = 0;
            if(StationReturnCache.Count > 0)
            {
                return StationReturnCache;
            }
            else
            {
                while(retryCount < 50)
                {
                    if(StationReturnCache.Count > 0) { break; }
                    System.Threading.Thread.Sleep(10);
                    retryCount++;
                }
                return StationReturnCache;
            }*/
			List<StationReturnModel> oReturn;
			//lock (StationReturnCache)
			//{
				if (site != "All")
				{
					oReturn = StationReturnCache.Where(s => s.Site == site).ToList();
				} else
				{
					oReturn = StationReturnCache;
				}
			//}
			return oReturn;
        }

        public static List<QueueCallReturnModel> GetCalls()
        {
			List<QueueCallReturnModel> oReturn;
			//lock (CallReturnCache)
			//{
				oReturn = CallReturnCache;
			//}
            return oReturn;
        }

		public static List<ActionDueEvent> GetActions()
		{
			return ActionDueCache.OrderByDescending(a => a.TimeOverdue).ToList();
		}

		public static List<ActionEvent> GetActionsComplete()
		{
			return ActionCompleteCache;
		}

        public static List<DayPeriodReturnModel> GetPeriodTotals(string site = "All")
        {
			//return DayPeriodReturnModelCache;
			List<DayPeriodReturnModel> oReturn;
			//lock (DayPeriodReturnModelCache)
			//{
				oReturn = DayPeriodReturnModelCache[site];
			//}
			return oReturn;
        }

        public static List<QueuePeriodReturnModel> GetQueuePeriodTotals(int queueId, string site = "All")
        {
			List<QueuePeriodReturnModel> oReturn;
			//lock (QueuePeriodCache)
			//{
				if (QueuePeriodCache[site].ContainsKey(queueId))
				{
					oReturn = QueuePeriodCache[site][queueId];
				} else
				{
					oReturn = new List<QueuePeriodReturnModel>();
				}
			//}
			return oReturn;
        }

        public static List<QueuePeriodReturnModel> GetVerticalPeriodTotals(string verticalName, string site = "All")
        {
			List<QueuePeriodReturnModel> oReturn;
			//lock (VerticalPeriodCache)
			//{
				if (VerticalPeriodCache[site].ContainsKey(verticalName))
				{
					oReturn = VerticalPeriodCache[site][verticalName];
				} else
				{
					oReturn = new List<QueuePeriodReturnModel>();
				}
			//}
			return oReturn;
        }

        public static List<Queue> GetQueues()
        {
			List<Queue> oReturn;
			lock (QueueCache)
			{
				oReturn = QueueCache.ToList();
			}
			return oReturn;
        }

        public static IEnumerable<RotationAndLoginReturnModel> RotationAndLogin()
        {
			List<RotationAndLoginReturnModel> oReturn;
			lock (RotationAndLoginReturnCache)
			{
				/*if (RotationAndLoginReturnCache.Count > 0)
				{
					oReturn = RotationAndLoginReturnCache;
				} else
				{
					while (RotationAndLoginReturnCache.Count == 0) { System.Threading.Thread.Sleep(10); }
					return RotationAndLoginReturnCache.OrderByDescending(x => x.TimeStamp);
				}*/
				oReturn = RotationAndLoginReturnCache.OrderByDescending(x => x.TimeStamp).ToList();
			}
			return oReturn;
        }

        private static void StartelConnector_StartelMessageRecieved(string message)
        {
            if(message.Length > 0)
            {
                Logger.Debug("StartelConnector_StartelMessageRecieved -> Queuing Message: {0}", message);
                StartelServiceMessageQueue.Add(message);
                //lock (StartelServiceMessageQueue)
                //{
                //    StartelServiceMessageQueue.Add(message);
                //}
                Logger.Debug("StartelConnector_StartelMessageRecieved -> Queuing Message: {0} Complete", message);
            }
        }

        private static void HandleStartelMessage(string message)
        {
            try
            {
                if (message.Length > 0)
                {
                    message = message.Substring(1, message.Length - 1);
                    int eventSeperatorIndex = message.IndexOf(":");
                    if (eventSeperatorIndex != -1)
                    {
                        int eventType = Convert.ToInt32(message.Substring(0, eventSeperatorIndex));
                        string eventValues = message.Substring(eventSeperatorIndex + 1);
                        if (eventType == 10)
                        {
                            Handle25thStatusLineInfo(eventValues);
                        }
                        else
                        {
                            HandleStandardStartelMessage(eventType, eventValues);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var stop = "";
            }
        }

        private static void Handle25thStatusLineInfo(string data)
        {
            try
            {
                foreach (string agentStatusInfo in data.Split('|'))
                {
                    //Logger.Debug("Answer1Dashboard -> Handle25thStatusLineInfo -> {0}", agentStatusInfo);
                    string[] agentValues = agentStatusInfo.Split('&');
                    if (agentValues.Length >= 4)
                    {
                        var position = Convert.ToInt32(agentValues[0]);
                        var state = Convert.ToInt32(agentValues[1]);
                        var newCalls = Convert.ToInt32(agentValues[2]);
                        var holdCalls = Convert.ToInt32(agentValues[3]);

                        AgentInformation agent = GetAgentByPosition(position);
                        if (agent != null)
                        {
                            //Logger.Debug("Answer1Dashboard -> Handle25thStatusLineInfo -> Got Agent: {0}", agent.Initial);

                            PositionInformation positionObj = agent.GetPosition(position);
                            positionObj.SetState((PositionInformation.PositionState)state);
                            positionObj.New = newCalls;
                            positionObj.Holding = holdCalls;
                        }
                        else
                        {
                            Logger.Debug("Answer1Dashboard -> Handle25thStatusLineInfo -> Unable to find agent by position: {0}", position);
                        }
                    }

                }
            }
            catch { }
        }

        private static void HandleStandardStartelMessage(int eventType, string message)
        {
            string[] eventValues = message.Split('&');
            switch (eventType)
            {
                case 58:
                    UpdatePositionAssignedClient(eventValues);
                    break;
                case 59:
                    AgentSigninOrOut(eventValues);
                    break;
                case 60:
                    UpdateQueueInfo(eventValues);
                    break;
                case 61:
                    UnsureWhatThisDoesYet(eventValues);
                    break;
                case 62:
                    AgentJoinedQueue(eventValues);
                    break;
                case 63:
                    CallJoinedQueue(eventValues);
                    break;
                case 64:
                    CallLeftQueue(eventValues);
                    break;
                case 65:
                    AgentPaused(eventValues);
                    break;
                case 66:
                    AgentAddedToQueue(eventValues);
                    break;
                case 67:
                    AgentRemovedFromQueue(eventValues);
                    break;
                case 68:
                    CallConnectedToAgent(eventValues);
                    break;
                case 69:
                    AgentCompletedCall(eventValues);
                    break;
                case 70:
                    CallAbandonedQueue(eventValues);
                    break;
                case 71:
                    CallEnded(eventValues);
                    break;

            }
        }

		private static bool CheckEndedCache(string uniqueId)
		{
			bool bReturn = false;
			lock (EndedCallCache)
			{
				bReturn = EndedCallCache.Count(x => x.UniqueId == uniqueId) > 0;
				if (bReturn)
					Logger.Debug("Answer1Dashboard -> Activity for call that was previously ended - " + uniqueId);
			}
			return bReturn;
			//return false;
		}

        private static void CallEnded(string[] data)
        {
            if (data.Length >= 1)
            {
                string uniqueId = data[0];
                try
                {
					lock (EndedCallCache)
					{
						EndedCallCache.Add(new EndedCallReturnModel
						{
							UniqueId = uniqueId,
							EndTime = DateTime.Now
						});
					}
					var queue = GetQueueForCall(uniqueId);
                    if (queue != null)
                    {
                        Logger.Debug("Answer1Dashboard -> CallEnded -> Removing Call: {0}", uniqueId);
						lock (QueueCache)
						{
							queue.RemoveCall(uniqueId);
						}
                    }
					lock (ActiveCallCache)
					{
						ActiveCallCache.RemoveAll(x => x.UniqueId == uniqueId);
					}
                }
                catch (Exception ex)
                {
                    Logger.Error("Answer1Dashboard -> CallEnded -> Error Removing Call: {0}. {1}", uniqueId, ex.ToString());
                }
            }
        }

        private static void CallAbandonedQueue(string[] data)
        {
            if (data.Length >= 4)
            {
                var queueName = data[0];
                var channel = data[1];
                var uniqueId = data[2];
                var unknownValue = data[3];

				try
				{
					Logger.Debug("Answer1Dashboard -> CallAbandonedQueue -> Call Abandoned In Queue: {0} {1}", queueName, uniqueId);
					var queue = GetQueue(queueName, true);
					lock (QueueCache)
					{
						queue.RemoveCall(uniqueId);
					}
					lock (ActiveCallCache)
					{
						ActiveCallCache.RemoveAll(x => x.UniqueId == uniqueId);
					}
				} catch (Exception ex)
				{
					Logger.Error("Answer1Dashboard -> CallAbandonedQueue -> Error Call Abandoned In Queue: {0} {1} {2}", queueName, uniqueId, ex.ToString());
				}
            }
        }

        private static void AgentCompletedCall(string[] data)
        {
            if (data.Length == 5)
            {
                string queue = data[0];
                int positionNumber = Convert.ToInt32(data[1]);
                string clientId = data[2];
                string channelId = data[3];
                string uniqueId = data[4];

                try
                {
                    //Logger.Debug("Answer1Dashboard -> AgentCompletedCall -> Agent Complete Call: {0} {1}", positionNumber, uniqueId);
                    //var agent = AgentCache.FirstOrDefault(x => x.IsLoggedIntoStation(positionNumber));
                    //if (agent != null)
                    //{
                    //    var position = agent.GetPosition(positionNumber);
                    //    position.SetClient(string.Empty, string.Empty);
                    //}
                    //else
                    //{
                    //    Logger.Debug("Answer1Dashboard -> AgentCompletedCall -> Unable to Find Agent at Position: {0}", positionNumber);
                    //}
                }
                catch (Exception ex)
                {
                    Logger.Error("Answer1Dashboard -> AgentCompletedCall -> Error Agent Complete Call: {0} {1} {2}", positionNumber, uniqueId, ex.ToString());
                }
            }
        }

        private static void CallConnectedToAgent(string[] data)
        {
            if (data.Length == 5)
            {
                var queueName = data[0];
                var positionNumber = Convert.ToInt32(data[1]);
                var clientId = data[2];
                var agentExtension = data[3];
                var uniqueId = data[4];

				if (!CheckEndedCache(uniqueId))
				{
					try
					{
						Logger.Debug("Answer1Dashboard -> CallConnectedToAgent -> Call Connected to Agent: {0} {1}", positionNumber, uniqueId);
						var agent = GetAgentByPosition(positionNumber);
						var queue = GetQueue(queueName, true);
						QueueCallReturnModel call = null;
						lock (ActiveCallCache)
						{
							call = ActiveCallCache.FirstOrDefault(x => x.UniqueId == uniqueId);
						}
						if (agent != null)
						{
							lock (QueueCache)
							{
								queue.RemoveCall(uniqueId);
							}
							var position = agent.GetPosition(positionNumber);
							var client = GetClient(clientId);
							if (client == null)
							{
								Logger.Error("Answer1Dashboard -> CallConnectedToAgent -> Couldnt find Client from Client ID: {0}", clientId);
								client = new ClientInformation() { ClientId = clientId, Name = "Invalid Client Id" };
							}
							if (call == null)
							{
								Logger.Error("Answer1Dashboard -> CallConnectedToAgent -> Couldnt find Call from Unique ID: {0}", uniqueId);
								call = new QueueCallReturnModel() { CallType = "?" };
							}

							position.SetClient(client.ClientId, client.Name, call.CallType);
						} else
						{
							Logger.Debug("Answer1Dashboard -> CallConnectedToAgent -> Unable to Find Agent by Position: {0}", positionNumber);
						}
					} catch (Exception ex)
					{
						Logger.Error("Answer1Dashboard -> CallConnectedToAgent -> Error Call Connected to Agent: {0} {1} {2}", positionNumber, uniqueId, ex.ToString());
					}
				}
            }
        }

        private static void AgentRemovedFromQueue(string[] data)
        {
            if (data.Length == 2)
            {
                string queueName = data[0];
                int position = Convert.ToInt32(data[1]);

                try
                {
                    Logger.Debug("Answer1Dashboard -> AgentRemovedFromQueue -> Removing Agent From Queue: {0} {1}", queueName, position);
                    var agent = GetAgentByPosition(position);
                    if (agent != null)
                    {
                        var queue = GetQueue(queueName, true);
						lock (QueueCache)
						{
							queue.RemoveAgent(agent.Id);
						}
                        
                        //If we are removing the agent from the last queue remove them from the position
                        if(!GetQueues().Any(x => x.GetPositions().Any(y => y.PostionNumber == position)))
                        {
							lock (AgentCache)
							{
								agent.RemovePosition(position);
							}
                        }
                    }
                    else
                    {
                        Logger.Debug("Answer1Dashboard -> AgentRemovedFromQueue -> Unable to Find Agent by Position: {0} {1}", queueName, position);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Answer1Dashboard -> AgentRemovedFromQueue -> Error Removing Agent From Queue: {0} {1} {2}", queueName, position, ex.ToString());
                }
            }
        }

        private static void AgentAddedToQueue(string[] data)
        {
            if (data.Length == 3)
            {
                string queueName = data[0];
                string initial = data[1];
                int position = Convert.ToInt32(data[2]);

                try
                {
                    Logger.Debug("Answer1Dashboard -> AgentAddedToQueue -> Adding Agent to Queue: {0} {1}", queueName, position);
                    var agent = GetAgentByPosition(position);
                    if (agent == null)
                    {
                        agent = GetAgent(initial);
                        if (agent == null) return;
						lock (AgentCache)
						{
							agent.AddPosition(position);
						}
                    }
                    var queue = GetQueue(queueName, true);
					lock (QueueCache)
					{
						queue.AddAgent(agent);
					}
                }
                catch (Exception ex)
                {
                    Logger.Error("Answer1Dashboard -> AgentAddedToQueue -> Error Adding Agent to Queue: {0} {1} {2}", queueName, position, ex.ToString());
                }
            }
        }

        private static void AgentPaused(string[] data)
        {
            if (data.Length == 3)
            {
                string unknownVariable = data[0];
                int position = Convert.ToInt32(data[1]);
                int state = Convert.ToInt32(data[2]);

                try
                {
                    Logger.Debug("Answer1Dashboard -> AgentPaused -> Pausing Agent: {0}", position);
                    var agent = GetAgentByPosition(position);
                    if (agent != null)
                    {
						lock (AgentCache)
						{
							agent.Positions.FirstOrDefault(x => x.PositionNumber == position).SetState((PositionInformation.PositionState)state);
						}
                    }
                    else
                    {
                        Logger.Debug("Answer1Dashboard -> AgentPaused -> Unable to Find Agent Using Position {0}", position);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Answer1Dashboard -> AgentPaused -> Error Pausing Agent: {0} {1}", position, ex.ToString());
                }
            }
        }

        private static void CallLeftQueue(string[] data)
        {
            if (data.Length == 4)
            {
                string queueName = data[0];
                string channel = data[1];
                string uniqueId = data[2];
                string unknownVariable = data[3];

				try
				{
					Logger.Debug("Answer1Dashboard -> CallLeftQueue -> Removing Call From Queue: {0} {1}", queueName, uniqueId);
					var queue = GetQueue(queueName, true);
					lock (QueueCache)
					{
						queue.RemoveCall(uniqueId);
					}
				} catch (Exception ex)
				{
					Logger.Debug("Answer1Dashboard -> CallLeftQueue -> Error Removing Call From Queue: {0} {1} {2}", queueName, uniqueId, ex.ToString());
				}
            }
        }

        private static void CallJoinedQueue(string[] data)
        {
            if (data.Length == 7)
            {
                string queueName = data[0];
                int callPosition = Convert.ToInt32(data[1]);
                string channel = data[2];
                string uniqueId = data[3];
                string ani = data[4];
                string callerName = data[5];
                string callType = data[6];

				if (!CheckEndedCache(uniqueId))
				{
					try
					{
						Logger.Debug("Answer1Dashboard -> CallJoinedQueue -> Adding Call to Queue: {0} {1}", queueName, uniqueId);
						var queue = GetQueue(queueName, true);
						if (queue != null)
						{
							var call = new QueueCallReturnModel();
							call.ChannelId = channel;
							call.ANI = ani;
							call.CallerName = callerName;
							call.Queue = queue.AffinityName;
							call.UniqueId = uniqueId;
							call.CallType = callType;
							if (callerName.Contains("_"))
							{
								var clientId = callerName.Substring(0, callerName.IndexOf("_"));
								var formattedCallerName = callerName.Substring(callerName.IndexOf("_") + 2);
								var client = GetClient(clientId);
								if (client == null)
								{
									Logger.Debug("Answer1Dashboard -> CallJoinedQueue -> Unable to find Client: {0}", clientId);
									client = new ClientInformation() { ClientId = clientId, Name = string.Empty };
								}
								call.ClientId = clientId;
								call.ClientName = client.Name;
								call.CallerName = formattedCallerName;
								call.Site = client.Site;
							} else
							{
								Logger.Debug("Answer1Dashboard -> CallJoinedQueue -> Unable to find Client by CallerName: {0}", callerName);
							}
							lock (QueueCache)
							{
								queue.AddCall(call);
							}
							lock (ActiveCallCache)
							{
								ActiveCallCache.Add(call);
							}
						}
					} catch (Exception ex)
					{
						Logger.Error("Answer1Dashboard -> CallJoinedQueue -> Error Unable to Add Call to Queue: {0} {1} {2}", queueName, uniqueId, ex.ToString());
					}
				}
            }
        }

        private static void AgentJoinedQueue(string[] data)
        {
            if (data.Length == 4)
            {
                string queueName = data[0];
                string agentInitial = data[1];
                int position = Convert.ToInt32(data[2]);
                int agentState = Convert.ToInt32(data[3]);
                try
                {
                    Logger.Debug("Answer1Dashboard -> AgentJoinedQueue -> Adding Agent to Queue: {0} Position: {1}", queueName, position);
                    var agent = GetAgent(agentInitial);
                    if (agent != null)
                    {
						lock (AgentCache)
						{
							if (!agent.IsLoggedIntoStation(position))
							{
								agent.AddPosition(position);
							}
							agent.Positions.FirstOrDefault(x => x.PositionNumber == position).SetState((PositionInformation.PositionState)agentState);
						}

                        var queue = GetQueue(queueName, true);
                        if (queue != null)
                        {
							lock (QueueCache)
							{
								queue.AddAgent(agent);
							}
                        }
                    }
                    else
                    {
                        Logger.Debug("Answer1Dashboard -> AgentJoinedQueue -> Unable to Find Agent by Position: {0}", position);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Answer1Dashboard -> AgentJoinedQueue -> Errro Adding Agent to Queue: {0} {1} {2}", queueName, position, ex.ToString());
                }
            }
        }

        private static void UnsureWhatThisDoesYet(string[] data)
        {
            if (data.Length == 8)
            {
                string queueName = data[0];
                int callPosition = Convert.ToInt32(data[1]);
                string channelId = data[2];
                string callUniqueId = data[3];
                string callerId = data[4];
                string callerIdName = data[5];
                int waitTime = Convert.ToInt32(data[6]);
                string callType = data[7];

				if (!CheckEndedCache(callUniqueId))
				{
					try
					{
						lock (QueueCache)
						{
							var queue = QueueCache.FirstOrDefault(x => x.Name == queueName);
							if (queueName != null)
							{
								QueueCallReturnModel call = new QueueCallReturnModel();
								call.Answered = false;
								call.UniqueId = callUniqueId;
								call.ChannelId = channelId;
								call.CallerName = callerIdName;
								call.ANI = callerId;
								call.StartTime = DateTime.UtcNow.AddSeconds(-1 * waitTime);
								call.CallType = callType;

								queue.AddCall(call);
								lock (ActiveCallCache)
								{
									ActiveCallCache.Add(call);
								}
							}
						}
					} catch (Exception ex)
					{
						var stop = "";
					}
				}
            }
        }

        private static void UpdateQueueInfo(string[] data)
        {
            if (data.Length == 11)
            {
                try
                {
                    string queueName = data[0];
                    int queueId = Convert.ToInt32(data[10]);

                    Queue q = GetQueue(queueName, true);
                    if (q != null)
                    {
						lock (QueueCache)
						{
							q.Id = queueId;
						}
                    }
                }
                catch (Exception ex)
                {
                    var stop = "";
                }
            }
        }

        private static void UpdatePositionAssignedClient(string[] data)
        {
            if (data.Length >= 2)
            {
                if (!string.IsNullOrEmpty(data[0]) && !string.IsNullOrEmpty(data[1]))
                {
                    int position = Convert.ToInt32(data[0]);
                    int clientDbid = Convert.ToInt32(data[1]);
                    try
                    {
                        Logger.Debug("Answer1Dashboard -> UpdatePositionAssignedClient -> Assigning Client to Position: {0} {1}", position, clientDbid);

                        AgentInformation agent = GetAgentByPosition(position);
                        ClientInformation client = GetClient(clientDbid);
                        if (agent != null && client != null)
                        {
                            var positionObj = agent.GetPosition(position);
                            positionObj.SetClient(client.ClientId, client.Name, string.Empty);
                        }
                        else
                        {
                            Logger.Debug("Answer1Dashboard -> UpdatePositionAssignedClient -> Couldnt find Agent by Positon: {0}", position);
                        }
                        if (agent != null && client == null)
                        {
                            var positionObj = agent.GetPosition(position);
                            positionObj.SetClient(string.Empty, string.Empty, string.Empty);
                        }
                        else
                        {
                            Logger.Debug("Answer1Dashboard -> UpdatePositionAssignedClient -> Couldnt find Client by Dbid: {0}", clientDbid);
                        }
                    }
                    catch(Exception ex)
                    {
                        Logger.Error("Answer1Dashboard -> UpdatePositionAssignedClient -> Error Assigning Client to Position: {0} {1} {2}", position, clientDbid, ex.ToString());
                    }
                }
            }
        }

        private static void AgentSigninOrOut(string[] data)
        {
            if (data.Length >= 3)
            {
                int position = Convert.ToInt32(data[0]);
                int agentId = Convert.ToInt32(data[1]);
                bool isLoggingIn = Convert.ToBoolean(Convert.ToInt32(data[2]));
                try
                {
                    Logger.Debug("Answer1Dashboard -> AgentSigninOrOut -> Signing Agent In or Out: {0} {1}", position, agentId);
                    
                    AgentInformation agent = GetAgent(agentId);
                    AgentInformation agentInPosition = GetAgentByPosition(position);
                    if (agent != null)
                    {
                        if (isLoggingIn)
                        {
							lock (AgentCache)
							{
								// Check if there's an agent stuck in this position. If so, remove position so new agent is added.
								if (agentInPosition != null)
								{
									agentInPosition.RemovePosition(position);
								}
								agent.IsLoggedIn = true;
								agent.AddPosition(position);
							}
                        }
                        else
                        {
							lock (AgentCache)
							{
								agent.IsLoggedIn = false;
								agent.RemovePosition(position);
							}
							lock (QueueCache)
							{
								QueueCache.ForEach(x => x.RemoveAgent(agentId));
							}
                        }
                    }
                    else
                    {
                        Logger.Debug("Answer1Dashboard -> AgentSigninOrOut -> Unable to find Agent by Id: {0}", agentId);
                    }
                    
                }
                catch (Exception ex)
                {
                    Logger.Error("Answer1Dashboard -> AgentSigninOrOut -> Error Signing Agent In or Out: {0} {1}", position, agentId, ex.ToString());
                }

            }
        }

        internal static List<ClientInformation> GetClientByAffinity(int affinityId)
        {
            if (ClientCache.Count == 0) ClientCache = SQLWrapper.GetClients();
            return ClientCache.Where(x => x.AffinityId == affinityId).ToList();

        }

        internal static ClientInformation GetClient(int dbid)
        {
            lock (ClientCache)
            {
                var client = ClientCache.FirstOrDefault(x => x.Id == dbid);
                if (client != null)
                {
                    return client;
                }
                else
                {
                    client = SQLWrapper.GetClient(dbid);
                    if (client != null)
                    {
                        ClientCache.Add(client);
                    }
                    return client;
                }
            }
        }

        internal static ClientInformation GetClient(string clientId)
        {
            lock (ClientCache)
            {
                var client = ClientCache.FirstOrDefault(x => x.ClientId == clientId);
                if (client != null)
                {
                    return client;
                }
                else
                {
                    client = SQLWrapper.GetClient(clientId);
                    if (client != null)
                    {
                        ClientCache.Add(client);
                    }
                    return client;
                }
            }
        }

        internal static Queue GetQueue(string name, bool createIfNotFound = false)
        {
            lock (QueueCache)
            {
                var queue = QueueCache.FirstOrDefault(x => x.Name == name);
                if (queue != null)
                {
                    return queue;
                }
                else
                {
                    if (createIfNotFound)
                    {
                        var affinity = SQLWrapper.GetAffinityName(name);
                        queue = new Queue() { Name = name, AffinityName = affinity };
                        QueueCache.Add(queue);
                        return queue;
                    }
                    return null;
                }
            }
        }

        internal static Queue GetQueueForCall(string callUniqueId)
        {
            Queue foundQueue = null;
            lock (QueueCache)
            {
                foundQueue = QueueCache.FirstOrDefault(x => x.DoesCallExist(callUniqueId));
            }
            return foundQueue;
        }

        internal static AgentInformation GetAgent(int id)
        {
			AgentInformation oReturn;
            lock (AgentCache)
            {
                var agent = AgentCache.FirstOrDefault(x => x.Id == id);
                if (agent != null)
                {
					oReturn = agent;
                }
                else
                {
                    agent = SQLWrapper.GetAgent(id);
                    if (agent != null) AgentCache.Add(agent);
                    oReturn = agent;
                }
            }
			return oReturn;
        }

        internal static AgentInformation GetAgent(string initial)
        {
			AgentInformation oReturn;
            lock (AgentCache)
            {
                var agent = AgentCache.FirstOrDefault(x => x.Initial == initial);
                if (agent != null)
                {
                    oReturn = agent;
                }
                else
                {
                    agent = SQLWrapper.GetAgent(initial);
                    if (agent != null) AgentCache.Add(agent);
                    oReturn = agent;
                }
            }
			return oReturn;
        }

        internal static AgentInformation GetAgentByPosition(int position)
        {
			AgentInformation oReturn = null;
            lock (AgentCache)
            {
                var agent = AgentCache.FirstOrDefault(x => x.IsLoggedIntoStation(position));
                if (agent != null)
                {
                    oReturn = agent;
                }
            }
			return oReturn;
        }

        private static Task GetWorkAndCallHistoryThread()
        {
            return Task.Run(() =>
            {
                while (!ShouldThreadExit)
                {
					try
					{
						if ((DateTime.Now - LastPopulateWorkCache).TotalSeconds > 120)
						{
							DateTime dStart = DateTime.Now;
							lock (AgentCache)
							{
								LastPopulateWorkCache = DateTime.Now;

								DateTime pullDate = DateTime.Today;
								if (System.Configuration.ConfigurationManager.AppSettings["RuntimeEnviro"] == "Dev")
									pullDate = DateTime.Now.AddHours(-4);

								AgentInformation currentAgent = AgentCache.FirstOrDefault();
								lock (WorkCache)
								{
									List<Answer1APILib.Plugin.Startel.Models.WorkEvent> workEvents;
									if (WorkCache.Count() > 0)
									{
										int iNewest = WorkCache.Max(w => w.id);
										workEvents = SQLWrapper.GetWorkEvents(iNewest).OrderBy(x => x.Agent.Id).ToList();
										Logger.Info("Pulled {0} work events after ID {1}", workEvents.Count, iNewest);
									} else
									{
										workEvents = SQLWrapper.GetWorkEvents(pullDate).OrderBy(x => x.Agent.Id).ToList();
										Logger.Info("Pulled {0} work events after Date {1}", workEvents.Count, pullDate);
									}

									if (workEvents.Count() == 0)
									{
										ShouldBackFill = true;
									}
									WorkCache.RemoveAll(x => x.Timestamp.Date < pullDate.Date);

									Logger.Info("Found {0} new work events to add", workEvents.Count());
									foreach (Answer1APILib.Plugin.Startel.Models.WorkEvent workEvent in workEvents)
									{
										if (currentAgent == null || currentAgent.Id != workEvent.Agent.Id)
										{
											var tempAgent = GetAgent(workEvent.Agent.Id);
											if (tempAgent != null)
											{
												currentAgent = tempAgent;
											} else
											{
												continue;
											}
										}

										currentAgent.AddEvent(workEvent);
										WorkCache.Add(workEvent);
									}
									Logger.Info("Work Cache has {0} items in it", WorkCache.Count());
								}

								lock (CallHistoryCache)
								{
									var callHistory = SQLWrapper.GetCallHistory(pullDate).Where(x => x.Direction == Answer1APILib.Plugin.Startel.Models.CallDirection.Incoming);
									CallHistoryCache.RemoveAll(x => x.Timestamp.Date != DateTime.Today || x.Timestamp >= pullDate);

									foreach (Answer1APILib.Plugin.Startel.CallRecord callRecord in callHistory)
									{
										foreach (Answer1APILib.Plugin.Startel.AgentPostionAndID agentId in callRecord.AgentPositionsAndIDs)
										{
											var agent = GetAgent(agentId.AgentID);
											if (agent != null)
											{
												agent.AddCallToHistory(callRecord);
											}
										}

										CallHistoryCache.Add(callRecord);
									}
								}

								if (ShouldBackFill)
								{
									SetupDayPeriodReturnModels();
									ShouldBackFill = false;
								}
								AgentCache.ForEach(x => x.SetStatistics());
							}
							Logger.Info("GetWorkAndCallHistoryThread took {0} seconds", (DateTime.Now - dStart).TotalSeconds);
						}
					} catch (Exception ex)
					{
						Logger.Error("Answer1Dashboard -> GetWorkAndCallHistoryThread -> Error Getting Call and Work History: {0}", ex.ToString());
					} finally
					{
						//System.Threading.Thread.Sleep(120000);
						System.Threading.Thread.Sleep(1000);
					}
                    
                }
                Logger.Debug("Answer1Dashboard -> Shutting Down Work and Call History Retrieval Thread");
            });
        }

		private static Task GetActionHistoryThread()
		{
			return Task.Run(() =>
			{
				while (!ShouldThreadExit)
				{
					try
					{
						DateTime dStart = DateTime.Today;
						LastPopulateActionCache = DateTime.Now;

						lock (ActionCompleteCache)
						{
							var allActions = SQLWrapper.GetActionsCompleted(dStart, dStart.AddDays(1));
							ActionCompleteCache.Clear();
							ActionCompleteCache.AddRange(allActions);
						}
						Logger.Info("Actions Completed - " + ActionCompleteCache.Count());
					} catch (Exception ex)
					{
						Logger.Error("Answer1Dashboard -> GetActionHistoryThread -> Error Getting Action History: {0}", ex.ToString());
					} finally
					{
						System.Threading.Thread.Sleep(120000);
					}
				}
			});
		}

		private static Task GetActionCurrentThread()
		{
			return Task.Run(() =>
			{
				while (!ShouldThreadExit)
				{
					try
					{
						DateTime dStart = DateTime.Today;

						lock (ActionDueCache)
						{
							var allActions = SQLWrapper.GetActionsDue(dStart, dStart.AddDays(1));
							ActionDueCache.Clear();
							ActionDueCache.AddRange(allActions);
						}

						Logger.Info("Actions Due - " + ActionDueCache.Count());
					} catch (Exception ex)
					{
						Logger.Error("Answer1Dashboard -> GetActionCurrentThread -> Error Getting Current Actions: {0}", ex.ToString());
					} finally
					{
						System.Threading.Thread.Sleep(10000);
					}
				}
			});
		}

        private static Task SetReturnModelThread()
        {
            return Task.Run(() =>
            {
                while (!ShouldThreadExit)
                {
					try
					{
						DateTime dStart = DateTime.Now;
						lock (RotationAndLoginReturnCache)
						{
							RotationAndLoginReturnCache.Clear();
							lock (WorkCache)
							{
								foreach (Answer1APILib.Plugin.Startel.Models.WorkEvent workEvent in WorkCache.Where(x => x.Type != Answer1APILib.Plugin.Startel.Models.WorkEventType.ScreenPop).OrderByDescending(x => x.Timestamp))
								{
									var item = new RotationAndLoginReturnModel();
									item.AgentName = string.Format("{0} {1}", workEvent.Agent.FirstName, workEvent.Agent.LastName);
									item.TimeStamp = workEvent.Timestamp.ToString();
									item.AgentId = workEvent.Agent.Id;
									item.Reason = workEvent.Destination;
									switch (workEvent.Type)
									{
										case Answer1APILib.Plugin.Startel.Models.WorkEventType.StartRotation:
											item.Event = "Start Rotation";
											break;
										case Answer1APILib.Plugin.Startel.Models.WorkEventType.EndRotation:
											item.Event = "End Rotation";
											break;
										case Answer1APILib.Plugin.Startel.Models.WorkEventType.SAILogin:
											item.Event = "Login";
											break;
										case Answer1APILib.Plugin.Startel.Models.WorkEventType.SAILogout:
											item.Event = "Logout";
											break;
										default:
											item.Event = "Unknown";
											break;
									}
									RotationAndLoginReturnCache.Add(item);
								}
							}
						}

						lock (StationReturnCache)
						{
							StationReturnCache.Clear();
							lock (AgentCache)
							{
								foreach (AgentInformation agent in AgentCache)
								{
									StationReturnCache.AddRange(agent.GetStationReturnModel());
								}
							}
							StationReturnCache = StationReturnCache.OrderBy(x => x.PostionNumber).ToList();
						}
						
						lock (CallReturnCache)
						{
							CallReturnCache.Clear();
							if (System.Configuration.ConfigurationManager.AppSettings["RuntimeEnviro"] == "Dev")
							{
								for (int c = 1; c <= 5; c++)
								{
									CallReturnCache.Add(new QueueCallReturnModel
									{
										ANI = $"123-123-123{c}",
										Answered = false,
										CallerName = $"Name {c}",
										CallType = "F",
										ProgressBarMinValue = 0,
										ProgressBarMaxValue = 20,
										ClientName = $"Client {c}"
									});
								}
							} else
							{
								lock (QueueCache)
								{
									foreach (Queue q in QueueCache)
									{
										CallReturnCache.AddRange(q.GetCalls());
									}
								}
							}
							CallReturnCache = CallReturnCache.OrderByDescending(x => x.TimerInSeconds).ToList();
						}

						lock (AgentReturnCache)
						{
							lock (AgentCache)
							{
								AgentReturnCache.Clear();
								AgentCache.ForEach(x => AgentReturnCache.Add(x.GetAgentReturnModel()));
							}
							AgentReturnCache = AgentReturnCache.OrderBy(x => x.FirstName).ToList();
						}
						//Logger.Info("SetReturnModelThread took {0} seconds", (DateTime.Now - dStart).TotalSeconds);
					} catch (Exception ex)
					{
						Logger.Error("Answer1Dashboard -> SetReturnModelThread -> Error Setting Return Models: {0}", ex.ToString());
					} finally
					{
						System.Threading.Thread.Sleep(500);
					}
                    
                }
                Logger.Debug("Answer1Dashboard -> Shutting Down Return Model Creation Thread");
            });
        }

        private static Task SetStatisticsThread()
        {
			Logger.Debug("Answer1Dashboard -> Starting");
            return Task.Run(() =>
            {
				Logger.Debug("Answer1Dashboard -> While Loop");
				while (!ShouldThreadExit)
                {

					Logger.Debug("Answer1Dashboard -> Try");
					try
					{
						DateTime dStart = DateTime.Now;
						bool bDoDebug = true;

						var firstStat = DayPeriodReturnModelCache["All"].FirstOrDefault();
						var firstQueueStat = QueuePeriodCache["All"].Values.FirstOrDefault() != null ? QueuePeriodCache["All"].Values.FirstOrDefault().FirstOrDefault() : null;
						var firstVerticalStat = VerticalPeriodCache["All"].Values.FirstOrDefault() != null ? VerticalPeriodCache["All"].Values.FirstOrDefault().FirstOrDefault() : null;

						Logger.Debug("Set up DayPeriodReturnModels");
						if ((firstStat == null || firstStat.PeriodStart.Date != DateTime.Today) ||
							(firstQueueStat == null || firstQueueStat.PeriodStart.Date != DateTime.Today) ||
							(firstVerticalStat == null || firstVerticalStat.PeriodStart.Date != DateTime.Today)
						)
						{
							Logger.Debug($"Start SetupDayPeriod");
							Logger.Info("Setting up day period return");
							ShouldBackFill = false;
							SetupDayPeriodReturnModels();
							Logger.Info("Set up day period return");
							Logger.Debug($"Done with SetupDayPeriod");
						}
						Logger.Debug("Done with DayPeriodReturnModels");

						DateTime dStart1 = DateTime.Now;
						Logger.Debug("Answer1Dashboard -> Locking");
						//lock (DayPeriodReturnModelCache)
						//{
							Logger.Debug("Answer1Dashboard -> Each Site");
							foreach (var site in SiteList)
							{
								var currentStat = DayPeriodReturnModelCache[site].FirstOrDefault(x => x.IsCurrentPeriod());
								var previousStat = DayPeriodReturnModelCache[site].FirstOrDefault(x => x.IsPreviousPeriod());
								if (bDoDebug) Logger.Info(" - 1: {0} ms", (DateTime.Now - dStart1).TotalMilliseconds);

								int totalWork;
								double totalLogin;
								DateTime dStart2 = DateTime.Now;
								Logger.Debug("Answer1Dashboard -> Lock QueuePeriod");
								//lock (QueuePeriodCache)
								//{
									Logger.Debug("Answer1Dashboard -> Locked QueuePeriod");
									firstQueueStat = QueuePeriodCache[site].Values.FirstOrDefault() != null ? QueuePeriodCache[site].Values.FirstOrDefault().FirstOrDefault() : null;
									if (bDoDebug) Logger.Info(" - 2: {0} ms", (DateTime.Now - dStart1).TotalMilliseconds);
									DateTime dStart3 = DateTime.Now;
									Logger.Debug("Answer1Dashboard -> Lock VerticalPeriod");
									//lock (VerticalPeriodCache)
									//{
										Logger.Debug("Answer1Dashboard -> Locked VerticalPeriod");
										firstVerticalStat = VerticalPeriodCache[site].Values.FirstOrDefault() != null ? VerticalPeriodCache[site].Values.FirstOrDefault().FirstOrDefault() : null;
							Logger.Debug($"About to lock WorkCache for {site}");
										lock (WorkCache)
										{
											totalWork = WorkCache.Where(x => (site == "All" || x.Agent.HomeSite.DisplayName == site) && x.Type == Answer1APILib.Plugin.Startel.Models.WorkEventType.ScreenPop).Sum(x => x.Duration);
										}
							Logger.Debug($"Done with WorkCache for {site}");
										totalLogin = CalculateTotalLoginTime(site);
							Logger.Debug($"Done with Total Login Time for {site}");
										if (bDoDebug) Logger.Info(" - 3: {0} ms", (DateTime.Now - dStart1).TotalMilliseconds);

										#region Temporary hack fix
										// See if the work cache is missing and repopulate it if so
										/*int iAttempts = 0;
										if (WorkCache.Count() == 0)
										{
											// Try to force the work cache to populate
											LastPopulateWorkCache = DateTime.MinValue;
											// Wait until it's done
											while (WorkCache.Count() == 0 && iAttempts++ < 20)
											{
												Logger.Info("Waiting for WorkCache...");
												System.Threading.Thread.Sleep(2000);
											}
										}*/

										// Repopulate all periods
										/*Logger.Info("Starting to repopulate all day periods");
										foreach (DayPeriodReturnModel item in DayPeriodReturnModelCache)
										{
											SetPeriodData(item);
										}*/
										#endregion

										// Repopulate the previous two periods
										if (bDoDebug) Logger.Info("Starting to repopulate previous two day periods");
							Logger.Debug($"Start repop previous two for {site}");
										if (currentStat != null)
										{
											SetPeriodData(currentStat, site);
										}
										if (previousStat != null)
										{
											SetPeriodData(previousStat, site);
										}
										if (bDoDebug) Logger.Info("Done repopulating");
										if (bDoDebug) Logger.Info(" - 4: {0} ms", (DateTime.Now - dStart1).TotalMilliseconds);
							Logger.Debug($"Done with repop previous two for {site}");

										foreach (string key in VerticalPeriodCache[site].Keys)
										{
											var currentPeriod = VerticalPeriodCache[site][key].FirstOrDefault(x => x.IsCurrentPeriod());
											if (currentPeriod != null)
											{
												SetVerticalPeriodData(currentPeriod, site);
											}
											lock (VerticalPeriodTotalCache)
											{
												SetVerticalPeriodTotal(key, site);
											}
										}
										if (bDoDebug) Logger.Info(" - 5: {0} ms", (DateTime.Now - dStart1).TotalMilliseconds);
									//} // Release VerticalPeriodCache lock
									if (bDoDebug) Logger.Info("VerticalPeriodCache was locked for {0} ms", (DateTime.Now - dStart3).TotalMilliseconds);
							Logger.Debug($"Start SetQueuePeriodTotal previous two for {site}");
							lock (QueuePeriodCache)
							{
								foreach (int key in QueuePeriodCache[site].Keys)
								{
									var currentPeriod = QueuePeriodCache[site][key].FirstOrDefault(x => x.IsCurrentPeriod());
									if (currentPeriod != null)
									{
										SetQueuePeriodData(currentPeriod, site);
									}
									SetQueuePeriodTotal(key, site);
								}
							}
							Logger.Debug($"Done with SetQueuePeriodTotal previous two for {site}");
									if (bDoDebug) Logger.Info(" - 6: {0} ms", (DateTime.Now - dStart1).TotalMilliseconds);
								//} // Release QueuePeriodCache lock
								if (bDoDebug) Logger.Info("QueuePeriodCache was locked for {0} ms", (DateTime.Now - dStart2).TotalMilliseconds);

							Logger.Debug("Setting Day Statistics");
								DayStatistics[site].AverageHoldTime = Math.Round(DayPeriodReturnModelCache[site].Average(x => x.AverageHold), 1);
								DayStatistics[site].AverageTalkTime = Math.Round(DayPeriodReturnModelCache[site].Average(x => x.AverageTalkTime), 1);
								DayStatistics[site].AverageTimeToAnswer = Math.Round(DayPeriodReturnModelCache[site].Average(x => x.AverageTimeToAnswer), 1);
								DayStatistics[site].LongestHoldTime = DayPeriodReturnModelCache[site].Max(x => x.LongestHold);
								DayStatistics[site].LongestTimeToAnswer = DayPeriodReturnModelCache[site].Max(x => x.LongestTimeToAnswer);
								DayStatistics[site].SLAQualifiedCalls = DayPeriodReturnModelCache[site].Sum(x => x.SLAQualifiedCalls);
								DayStatistics[site].ActionsPickedUp = DayPeriodReturnModelCache[site].Sum(x => x.ActionsPickedUp);
								DayStatistics[site].ActionsDue = DayPeriodReturnModelCache[site].Sum(x => x.ActionsDue);
								Logger.Debug("Answer1Dashboard -> Lock ActionDue");
								lock (ActionCompleteCache)
								{
									var actionRecordsSite = ActionCompleteCache.Where(x => (site == "All" || AgentCache.Where(ac => ac.Site == site).Select(ac => ac.Id).ToList().Contains(x.AgentId))).ToList();

									Logger.Debug("Answer1Dashboard -> Locked ActionDue");

									if (actionRecordsSite.Count > 0 && actionRecordsSite.Where(x => x.ActionTime > x.DueTime).Count() > 0)
									{
										var avgPast = (int)Math.Round(actionRecordsSite.Where(x => x.ActionTime > x.DueTime).Average(x => x.TimeOverdue), 0);
										DayStatistics[site].ActionsAvgPast = DurationToString(avgPast > 0 ? avgPast : 0);
										DayStatistics[site].ActionsLongPast = DurationToString((int)Math.Round(actionRecordsSite.Max(x => x.TimeOverdue), 0));
										var slaActions = actionRecordsSite.Where(x => x.ActionTime > x.DueTime && x.ActionTime <= x.DueTime.AddMinutes(5)).Count();
										var dueActions = actionRecordsSite.Where(x => x.ActionTime > x.DueTime).Count();
										DayStatistics[site].ActionsSLA = $"{Math.Round((double)slaActions / (double)dueActions * 100, 1)}%";
										//var aMax = actionRecordsSite.OrderByDescending(a => a.TimeOverdue).ToList()[0];
									}
								}
								DayStatistics[site].TotalCalls = DayPeriodReturnModelCache[site].Sum(x => x.TotalCalls);
								DayStatistics[site].TotalCallsHandled = DayPeriodReturnModelCache[site].Sum(x => x.CallsHandled);
								DayStatistics[site].TotalCallsOffered = DayPeriodReturnModelCache[site].Sum(x => x.CallsOffered);
								DayStatistics[site].TotalCallsQueued = DayPeriodReturnModelCache[site].Sum(x => x.CallsQueued);
								DayStatistics[site].TotalAbandonedPct = Math.Round((((double)DayStatistics[site].TotalCallsOffered - DayStatistics[site].TotalCallsHandled) * 100 / DayStatistics[site].TotalCallsOffered), 1);


								if (DayStatistics[site].SLAQualifiedCalls > 0 && DayStatistics[site].TotalCallsHandled > 0)
								{
									DayStatistics[site].SLA = string.Format("{0}%", Math.Round((double)DayStatistics[site].SLAQualifiedCalls / (double)DayStatistics[site].TotalCallsHandled * 100, 1));
								} else
								{
									DayStatistics[site].SLA = "0%";
								}

								if (totalWork > 0 && totalLogin > 0)
								{
									DayStatistics[site].Utilization = string.Format("{0}%", Math.Round(((double)totalWork / (double)totalLogin) * 100, 1));
								} else
								{
									DayStatistics[site].Utilization = "N/A";
								}
								if (bDoDebug) Logger.Info(" - 7: {0} ms", (DateTime.Now - dStart1).TotalMilliseconds);

								DayStatistics[site].TotalWork = FormatTimeSpan(new TimeSpan(0, 0, totalWork));
								DayStatistics[site].TotalLogin = FormatTimeSpan(new TimeSpan(0, 0, (int)totalLogin));
							} // End of site loop
						//} // Release DayPeriodReturnModelCache lock
						if (bDoDebug) Logger.Info("DayPeriodReturnModelCache was locked for {0} ms", (DateTime.Now - dStart1).TotalMilliseconds);
						Logger.Info("SetStatisticsThread took {0} seconds", (DateTime.Now - dStart).TotalSeconds);
					} catch (Exception ex)
					{
						Logger.Error(ex, "Answer1Dashboard -> Error running statistics {0}", ex.Message);
					}

					System.Threading.Thread.Sleep(30000);
				}

				Logger.Debug("Answer1Dashboard -> Shutting Down Statistics and Mathmatics Thread");
            });
        }

        private static void SetupDayPeriodReturnModels()
        {
			var today = DateTime.Today;
			var affinites = SQLWrapper.GetAffinities();

			DateTime dStart = DateTime.Now;
				int iHourStart = 0;
				int iHourEnd = 23;

				if (System.Configuration.ConfigurationManager.AppSettings["RuntimeEnviro"] == "Dev")
				{
					iHourStart = DateTime.Now.Hour - 3;
					iHourEnd = DateTime.Now.Hour;
				}

			Logger.Debug("Locking DayPeriodReturn");
			lock (DayPeriodReturnModelCache)
			{
				//DayPeriodReturnModelCache.Clear();
				Logger.Debug("Locking QueuePeriod");
				lock (QueuePeriodCache)
				{
					Logger.Debug("Locking VerticalPeriod");
					lock (VerticalPeriodCache)
					{
						foreach (var site in SiteList)
						{
							QueuePeriodCache[site].Clear();
							VerticalPeriodCache[site].Clear();

							DayPeriodReturnModelCache[site].Clear();
							for (int h = iHourStart; h <= iHourEnd; h++)
							{
								for (int m = 0; m <= 31; m += 30)
								{
									if (m == 0)
									{
										DayPeriodReturnModelCache[site].Add(new DayPeriodReturnModel()
										{
											PeriodStart = today.Add(new TimeSpan(h, m, 0)),
											PeriodEnd = today.Add(new TimeSpan(h, 29, 59))
										});
									} else
									{
										DayPeriodReturnModelCache[site].Add(new DayPeriodReturnModel()
										{
											PeriodStart = today.Add(new TimeSpan(h, m, 0)),
											PeriodEnd = today.Add(new TimeSpan(h, 59, 59))
										});
									}
								}
							}
							foreach (DayPeriodReturnModel item in DayPeriodReturnModelCache[site])
							{
								SetPeriodData(item, site);
							}

							foreach (string key in VerticalQueueMap.Keys)
							{
								var periodList = new List<QueuePeriodReturnModel>();

								for (int h = iHourStart; h <= iHourEnd; h++)
								{
									for (int m = 0; m <= 31; m += 30)
									{
										if (m == 0)
										{
											periodList.Add(new QueuePeriodReturnModel()
											{
												PeriodStart = today.Add(new TimeSpan(h, m, 0)),
												PeriodEnd = today.Add(new TimeSpan(h, 29, 59)),
												QueueName = key,
												QueueId = -1
											});
										} else
										{
											periodList.Add(new QueuePeriodReturnModel()
											{
												PeriodStart = today.Add(new TimeSpan(h, m, 0)),
												PeriodEnd = today.Add(new TimeSpan(h, 59, 59)),
												QueueName = key,
												QueueId = -1
											});
										}
									}
								}
								VerticalPeriodCache[site].Add(key, periodList);
							}

							foreach (string key in VerticalPeriodCache[site].Keys)
							{
								VerticalPeriodCache[site][key].ForEach(x => SetVerticalPeriodData(x, site));
							}
						}
					} // Release VerticalPeriodCache lock
					Logger.Debug("Released VerticalPeriod");

					foreach (string site in SiteList)
					{
						Logger.Debug("For Affinities");
						foreach (Answer1APILib.Plugin.Startel.Models.AffinityInformation affinity in affinites)
						{
							Logger.Debug($"Affinity {affinity.Name}");
							var periodList = new List<QueuePeriodReturnModel>();
							for (int h = iHourStart; h <= iHourEnd; h++)
							{
								for (int m = 0; m <= 31; m += 30)
								{
									if (m == 0)
									{
										periodList.Add(new QueuePeriodReturnModel()
										{
											PeriodStart = today.Add(new TimeSpan(h, m, 0)),
											PeriodEnd = today.Add(new TimeSpan(h, 29, 59)),
											QueueName = affinity.Name,
											QueueId = affinity.Id
										});
									} else
									{
										periodList.Add(new QueuePeriodReturnModel()
										{
											PeriodStart = today.Add(new TimeSpan(h, m, 0)),
											PeriodEnd = today.Add(new TimeSpan(h, 59, 59)),
											QueueName = affinity.Name,
											QueueId = affinity.Id
										});
									}
								}
							}
							QueuePeriodCache[site].Add(affinity.Id, periodList);
						}
						foreach (int key in QueuePeriodCache[site].Keys)
						{
							QueuePeriodCache[site][key].ForEach(x => SetQueuePeriodData(x, site));
						}
					}
				} // Release QueuePeriodCache lock
				Logger.Debug("Released QueuePeriod");
			} // Release DayPeriodReturnModelCache lock
			Logger.Debug("SetupDayPeriodReturnModels took {0} seconds", (DateTime.Now - dStart).TotalSeconds);
		}

		private static void SetPeriodData(DayPeriodReturnModel stat, string site = "All")
        {
            stat.Reset();
			List<Answer1APILib.Plugin.Startel.CallRecord> callRecords;
			lock (CallHistoryCache)
			{
				callRecords = CallHistoryCache.Where(x => (site == "All" || AgentCache.Where(ac => ac.Site == site).Select(ac => ac.Id).ToList().Contains(x.AgentPositionsAndIDs.LastOrDefault() != null ? x.AgentPositionsAndIDs.LastOrDefault().AgentID : 0)) && x.Timestamp >= stat.PeriodStart && x.Timestamp <= stat.PeriodEnd).ToList();
			}
			List<Answer1APILib.Plugin.Startel.Models.WorkEvent> workRecords;
			lock (WorkCache)
			{
				workRecords = WorkCache.Where(x => (site == "All" || AgentCache.Where(ac => ac.Site == site).Select(ac => ac.Id).ToList().Contains(x.Agent.Id)) && x.Timestamp >= stat.PeriodStart && x.Timestamp <= stat.PeriodEnd).ToList();
			}
			List<ActionEvent> actionRecordsSite;
			List<ActionEvent> actionDueRecords;
			List<ActionEvent> actionPickedRecords;
			List<ActionEvent> actionOverdueRecords;
			lock (ActionCompleteCache)
			{
				actionRecordsSite = ActionCompleteCache.Where(x => (site == "All" || AgentCache.Where(ac => ac.Site == site).Select(ac => ac.Id).ToList().Contains(x.AgentId))).ToList();
				actionDueRecords = actionRecordsSite.Where(x => x.DueTime >= stat.PeriodStart && x.DueTime <= stat.PeriodEnd).ToList();
				actionPickedRecords = actionRecordsSite.Where(x => (x.Disposition == "Picked Up" || x.Disposition == "Response") && x.ActionTime >= stat.PeriodStart && x.ActionTime <= stat.PeriodEnd).ToList();
				actionOverdueRecords = actionRecordsSite.Where(x => (x.Disposition == "Picked Up" || x.Disposition == "Response") && x.ActionTime >= stat.PeriodStart && x.ActionTime <= stat.PeriodEnd && TimeSpan.FromSeconds(x.TimeOverdue).TotalMinutes > 0).ToList();
			}

			if (actionDueRecords.Count() > 0)
			{
				stat.ActionsDue = actionDueRecords.Where(x => x.ActionTime > x.DueTime).Count();
				stat.ActionsPickedUp = actionPickedRecords.Count();
				//stat.ActionsAvgPast = DurationToString((int)Math.Round(actionRecords.Average(a => a.TimeOverdue.TotalSeconds), 0));
				var slaActions = actionDueRecords.Where(x => x.ActionTime > x.DueTime && x.ActionTime <= x.DueTime.AddMinutes(5)).Count();
				stat.ActionSLA = stat.ActionsDue > 0 ? $"{Math.Round((double)slaActions / (double)stat.ActionsDue * 100, 1)}%" : "";
				if (actionOverdueRecords.Count() > 0)
				{
					stat.ActionAPD = FormatTimeSpan(TimeSpan.FromSeconds(Math.Round(actionOverdueRecords.Average(r => r.TimeOverdue), 0)));
					stat.ActionLPD = FormatTimeSpan(TimeSpan.FromSeconds(actionOverdueRecords.Max(r => r.TimeOverdue)));
				}
			}

            if (callRecords.Count() > 0)
            {
                stat.AverageHold = Math.Round(callRecords.Average(x => x.TotalHoldTime), 1);
                stat.AverageTalkTime = Math.Round(callRecords.Average(x => x.TalkTime), 1);
                stat.AverageTimeToAnswer = Math.Round(callRecords.Average(x => x.TimeToAnswer), 1);
                stat.CallsHandled = callRecords.Count(x => x.TalkTime > 0);
                stat.CallsOffered = callRecords.Count(x => x.AgentPositionsAndIDs.Count > 0);
                stat.CallsQueued = callRecords.Count(x => x.CallQueue != -1);
                stat.LongestHold = callRecords.Max(x => x.SystemHoldTime);
                stat.LongestTimeToAnswer = callRecords.Max(x => x.TimeToAnswer);
                stat.SLAQualifiedCalls = callRecords.Count(x => x.TimeToAnswer < 24 && x.TalkTime > 0);
                stat.TotalCalls = callRecords.Count();
                
                try
                {
                    stat.SLA = string.Format("{0}%", Math.Round((double)stat.SLAQualifiedCalls / (double)stat.CallsHandled * 100, 1));
                }
                catch
                {
                    stat.SLA = "0%";
                }
            }

            if (workRecords.Count() > 0)
            {
                var totalLoginTime = new TimeSpan();
                var totalRotationTime = new TimeSpan();
                var totalWork = workRecords.Where(x => (site == "All" || x.Agent.HomeSite.DisplayName == site) && x.Type == Answer1APILib.Plugin.Startel.Models.WorkEventType.ScreenPop).Sum(x => x.Duration);
                stat.TotalWork = FormatTimeSpan(new TimeSpan(0, 0, totalWork));
                stat.TotalLoginSeconds = totalWork;
                var logAndRotEvents = workRecords.Where(x => x.Type == Answer1APILib.Plugin.Startel.Models.WorkEventType.SAILogin || x.Type == Answer1APILib.Plugin.Startel.Models.WorkEventType.SAILogout);

                var uniqueAgents = workRecords.Select(x => x.Agent.Id).Distinct().ToList();
                uniqueAgents.RemoveAll(x => _ignoreAgents.Contains(x));
                logAndRotEvents = logAndRotEvents.OrderBy(x => x.Timestamp);

				int totalSecondsInPeriod = stat.PeriodEnd > DateTime.Now ? (int)(DateTime.Now - stat.PeriodStart).TotalSeconds : 1800;

                foreach (int agentId in uniqueAgents)
                {
                    var agentEvents = logAndRotEvents.Where(x => x.Agent.Id == agentId);
                    if (agentEvents.Count() == 0)
                    {
                        totalLoginTime = totalLoginTime.Add(new TimeSpan(0, 0, totalSecondsInPeriod));
                        totalRotationTime = totalRotationTime.Add(new TimeSpan(0, 0, totalSecondsInPeriod));
                    }
                    else
                    {
                        var isLoggedIn = false;
                        var isInRotation = false;
                        var startLoginTime = stat.PeriodStart;
                        var startRotTime = stat.PeriodStart;
                        var loginTime = new TimeSpan();
                        var rotationTime = new TimeSpan();

                        foreach (Answer1APILib.Plugin.Startel.Models.WorkEvent workEvent in agentEvents)
                        {
                            if (workEvent.Type == Answer1APILib.Plugin.Startel.Models.WorkEventType.SAILogin)
                            {
                                isLoggedIn = true;
                                startLoginTime = workEvent.Timestamp;
                            }

                            if(workEvent.Type == Answer1APILib.Plugin.Startel.Models.WorkEventType.StartRotation)
                            {
                                isInRotation = true;
                                startRotTime = workEvent.Timestamp;
                            }

                            if (workEvent.Type == Answer1APILib.Plugin.Startel.Models.WorkEventType.SAILogout)
                            {
                                if (isLoggedIn)
                                {
                                    isLoggedIn = false;
                                    loginTime = loginTime.Add(workEvent.Timestamp - startLoginTime);
                                }
                                else
                                {
                                    loginTime = loginTime.Add(workEvent.Timestamp - stat.PeriodStart);
                                }
                            }

                            if(workEvent.Type == Answer1APILib.Plugin.Startel.Models.WorkEventType.EndRotation)
                            {
                                if (isInRotation)
                                {
                                    isInRotation = false;
                                    rotationTime = rotationTime.Add(workEvent.Timestamp - startLoginTime);
                                }
                                else
                                {
                                    rotationTime = loginTime.Add(workEvent.Timestamp - stat.PeriodStart);
                                }
                            }
                        }

                        if (isLoggedIn)
                        {
                            //loginTime = loginTime.Add(DateTime.Now - startLoginTime);
                            if (stat.IsCurrentPeriod())
                            {
                                loginTime = loginTime.Add(DateTime.Now - startLoginTime);
                            }
                            else
                            {
                                loginTime = loginTime.Add(stat.PeriodEnd - startLoginTime);
                            }
                            
                        }
                        if (isInRotation)
                        {
                            if (stat.IsCurrentPeriod())
                            {
                                rotationTime = rotationTime.Add(DateTime.Now - startRotTime);
                            }
                            else
                            {
                                rotationTime = rotationTime.Add(stat.PeriodEnd - startRotTime);
                            }
                            
                        }

                        totalLoginTime = totalLoginTime.Add(loginTime);
                        totalRotationTime = totalRotationTime.Add(rotationTime);
                    }

                }

                stat.TotalLogin = FormatTimeSpan(totalLoginTime);
                stat.TotalLoginSeconds = (int)totalLoginTime.TotalSeconds;
                stat.TotalRotation = FormatTimeSpan(totalRotationTime);
                stat.TotalRotationInSeconds = (int)totalRotationTime.TotalSeconds;

                if (totalWork > 0 && totalLoginTime.TotalSeconds > 0)
                {
                    stat.Utilization = string.Format("{0}%", Math.Round((double)totalWork / totalLoginTime.TotalSeconds * 100), 1);
                }
            }

            //Idle time and Time between calls
            if(workRecords.Count() > 0 && callRecords.Count() > 0)
            {
                var totalTalkTime = callRecords.Sum(x => x.TalkTime);
                if(totalTalkTime > 0 && stat.TotalRotationInSeconds > 0)
                {
                    var idleTimeInSeconds = stat.TotalRotationInSeconds - totalTalkTime;
                    stat.IdleTimeInSeconds = (int)idleTimeInSeconds;
                    stat.IdleTime = FormatTimeSpan(new TimeSpan(0, 0, (int)idleTimeInSeconds));
                }
                if(stat.IdleTimeInSeconds > 0)
                {
                    stat.TimeBetweenCallsInSeconds = stat.IdleTimeInSeconds / callRecords.Where(x => x.TalkTime > 0).Count();
                    stat.TimeBetweenCalls = FormatTimeSpan(new TimeSpan(0, 0, stat.IdleTimeInSeconds / callRecords.Where(x => x.TalkTime > 0).Count()));
                }
            }
        }

        private static void SetQueuePeriodData(QueuePeriodReturnModel data, string site)
        {
			DateTime dStart = DateTime.Now;
            data.Reset();
			List<Answer1APILib.Plugin.Startel.CallRecord> callRecords;
			lock (CallHistoryCache)
			{
				callRecords = CallHistoryCache.Where(x => x.Timestamp >= data.PeriodStart && x.Timestamp <= data.PeriodEnd && x.CallQueue == data.QueueId).ToList();
			}
			//Logger.Info(" - 5a: {0} ms", (DateTime.Now - dStart).TotalMilliseconds);
			List<Answer1APILib.Plugin.Startel.Models.WorkEvent> workRecords;
			lock (WorkCache)
			{
				workRecords = WorkCache.Where(x => x.Timestamp >= data.PeriodStart && x.Timestamp <= data.PeriodEnd).ToList();
			}
			//Logger.Info(" - 5b: {0} ms", (DateTime.Now - dStart).TotalMilliseconds);
			var clientIds = GetClientByAffinity(data.QueueId).Select(x => x.ClientId);
            workRecords = workRecords.Where(x => clientIds.Contains(x.ClientID)).ToList();
			//Logger.Info(" - 5c: {0} ms", (DateTime.Now - dStart).TotalMilliseconds);

			if (site != "All")
			{
				callRecords = callRecords.Where(c => AgentCache.Where(ac => ac.Site == site).Select(ac => ac.Id).ToList().Contains(c.AgentPositionsAndIDs.LastOrDefault() != null ? c.AgentPositionsAndIDs.LastOrDefault().AgentID : 0)).ToList();
				workRecords = workRecords.Where(w => AgentCache.Where(ac => ac.Site == site).Select(ac => ac.Id).ToList().Contains(w.Agent.Id)).ToList();
			}

			if (callRecords.Count() > 0)
            {
                data.AverageHold = Math.Round(callRecords.Average(x => x.TotalHoldTime), 1);
                data.AverageTalkTime = Math.Round(callRecords.Average(x => x.TalkTime), 1);
                data.AverageTimeToAnswer = Math.Round(callRecords.Average(x => x.TimeToAnswer), 1);
                data.CallsHandled = callRecords.Count(x => x.TalkTime > 0);
                data.CallsOffered = callRecords.Count(x => x.AgentPositionsAndIDs.Count > 0);
                data.CallsQueued = callRecords.Count(x => x.CallQueue != -1);
                data.LongestHold = callRecords.Max(x => x.SystemHoldTime);
                data.LongestTimeToAnswer = callRecords.Max(x => x.TimeToAnswer);
                data.SLAQualifiedCalls = callRecords.Count(x => x.TimeToAnswer < 24 && x.TalkTime > 0);
                data.TotalCalls = callRecords.Count();
				//Logger.Info(" - 5d: {0} ms", (DateTime.Now - dStart).TotalMilliseconds);

				try
				{
                    data.SLA = string.Format("{0}%", Math.Round((double)data.SLAQualifiedCalls / (double)data.CallsHandled * 100, 1));
                }
                catch
                {
                    data.SLA = "0%";
                }

            }

			//Logger.Info(" - 5e: {0} ms", (DateTime.Now - dStart).TotalMilliseconds);
			if (workRecords.Count() > 0)
            {
                var totalWork = workRecords.Where(x => (site == "All" || x.Agent.HomeSite.DisplayName == site) && x.Type == Answer1APILib.Plugin.Startel.Models.WorkEventType.ScreenPop).Sum(x => x.Duration);
                data.TotalWork = FormatTimeSpan(new TimeSpan(0, 0, totalWork));
            }
			//ogger.Info(" - 5f: {0} ms", (DateTime.Now - dStart).TotalMilliseconds);
		}

		private static void SetVerticalPeriodData(QueuePeriodReturnModel data, string site)
        {
            data.Reset();

            if (!VerticalQueueMap.ContainsKey(data.QueueName))
            {
                return;
            }
            var queues = VerticalQueueMap[data.QueueName];
            var clientIds = new List<string>();
            foreach(int queueId in queues)
            {
                var cids = GetClientByAffinity(queueId);
                if(cids.Count() > 0)
                {
                    clientIds.AddRange(cids.Select(x => x.ClientId));
                }
            }

			List<Answer1APILib.Plugin.Startel.CallRecord> callRecords;
			lock (CallHistoryCache)
			{
				callRecords = CallHistoryCache.Where(x => x.Timestamp >= data.PeriodStart && x.Timestamp <= data.PeriodEnd && queues.Contains(x.CallQueue)).ToList();
			}
			List<Answer1APILib.Plugin.Startel.Models.WorkEvent> workRecords;
			lock (WorkCache)
			{
				workRecords = WorkCache.Where(x => x.Timestamp >= data.PeriodStart && x.Timestamp <= data.PeriodEnd && clientIds.Contains(x.ClientID)).ToList();
			}

			if (site != "All")
			{
				callRecords = callRecords.Where(c => AgentCache.Where(ac => ac.Site == site).Select(ac => ac.Id).ToList().Contains(c.AgentPositionsAndIDs.LastOrDefault() != null ? c.AgentPositionsAndIDs.LastOrDefault().AgentID : 0)).ToList();
				workRecords = workRecords.Where(w => w.Agent.HomeSite.DisplayName == site).ToList();
			}

			if (callRecords.Count() > 0)
            {
                data.AverageHold = Math.Round(callRecords.Average(x => x.TotalHoldTime), 1);
                data.AverageTalkTime = Math.Round(callRecords.Average(x => x.TalkTime), 1);
                data.AverageTimeToAnswer = Math.Round(callRecords.Average(x => x.TimeToAnswer), 1);
                data.CallsHandled = callRecords.Count(x => x.TalkTime > 0);
                data.CallsOffered = callRecords.Count(x => x.AgentPositionsAndIDs.Count > 0);
                data.CallsQueued = callRecords.Count(x => x.CallQueue != -1);
                data.LongestHold = callRecords.Max(x => x.SystemHoldTime);
                data.LongestTimeToAnswer = callRecords.Max(x => x.TimeToAnswer);
                data.SLAQualifiedCalls = callRecords.Count(x => x.TimeToAnswer < 24 && x.TalkTime > 0);
                data.TotalCalls = callRecords.Count();

                try
                {
                    data.SLA = string.Format("{0}%", Math.Round((double)data.SLAQualifiedCalls / (double)data.CallsHandled * 100, 1));
                }
                catch
                {
                    data.SLA = "0%";
                }
            }

            if (workRecords.Count() > 0)
            {
                var totalWork = workRecords.Where(x => (site == "All" || x.Agent.HomeSite.DisplayName == site) && x.Type == Answer1APILib.Plugin.Startel.Models.WorkEventType.ScreenPop).Sum(x => x.Duration);
                data.TotalWork = FormatTimeSpan(new TimeSpan(0, 0, totalWork));
            }
        }

        private static double CalculateTotalLoginTime(string site = "All")
        {
			//var allLoginEvents = WorkCache.Where(x => x.Type == Answer1APILib.Plugin.Startel.Models.WorkEventType.SAILogin || x.Type == Answer1APILib.Plugin.Startel.Models.WorkEventType.SAILogout);
			//if (allLoginEvents.Count() == 0)
			//{
			//    return 0;
			//}
			//var totalLoginTime = new TimeSpan();
			//var agentIds = WorkCache.Where(x => x.Type == Answer1APILib.Plugin.Startel.Models.WorkEventType.ScreenPop).Select(x => x.Agent.Id).Distinct().ToList();
			//var now = DateTime.Now;

			//agentIds.RemoveAll(x => _ignoreAgents.Contains(x));

			//foreach (int agentId in agentIds)
			//{
			//    var agentLoginEvents = allLoginEvents.Where(x => x.Agent.Id == agentId).OrderBy(x => x.Timestamp).ToList();
			//    var isLoggedIn = false;
			//    var logStart = DateTime.Today;
			//    var logTime = new TimeSpan();

			//    var logEndPeriod = now;

			//    if (agentLoginEvents.Count() == 0)
			//    {
			//        agentLoginEvents.Add(new Answer1APILib.Plugin.Startel.Models.WorkEvent() { Timestamp = logStart, Type = Answer1APILib.Plugin.Startel.Models.WorkEventType.SAILogin });
			//    }

			//    if (agentLoginEvents.FirstOrDefault().Type == Answer1APILib.Plugin.Startel.Models.WorkEventType.SAILogout)
			//    {
			//        agentLoginEvents.Add(new Answer1APILib.Plugin.Startel.Models.WorkEvent() { Timestamp = logStart, Type = Answer1APILib.Plugin.Startel.Models.WorkEventType.SAILogin });
			//    }

			//    foreach (Answer1APILib.Plugin.Startel.Models.WorkEvent logEvent in agentLoginEvents)
			//    {
			//        switch (logEvent.Type)
			//        {
			//            case Answer1APILib.Plugin.Startel.Models.WorkEventType.SAILogin:
			//                isLoggedIn = true;
			//                logStart = logEvent.Timestamp;
			//                break;
			//            case Answer1APILib.Plugin.Startel.Models.WorkEventType.SAILogout:
			//                isLoggedIn = false;
			//                logTime = logTime.Add(logEvent.Timestamp - logStart);
			//                break;
			//        }
			//    }
			//    if (isLoggedIn)
			//    {
			//        logTime = logTime.Add(logEndPeriod - logStart);
			//    }

			//    totalLoginTime = totalLoginTime.Add(logTime);
			//}

			//return totalLoginTime.TotalSeconds;
			int oReturn;
			lock (DayPeriodReturnModelCache)
			{
				oReturn = DayPeriodReturnModelCache[site].Where(x => !x.IsPeriodInFuture()).Sum(x => x.TotalLoginSeconds);
			}
			return oReturn;
        }

        internal static string FormatTimeSpan(TimeSpan ts)
        {
            return (int)ts.TotalHours + ts.ToString(@"\:mm\:ss");
        }

		internal static string DurationToString(int duration)
		{
			var ts = new TimeSpan(0, 0, duration);
			return $"{Math.Floor(ts.TotalHours)}:{ts.Minutes.ToString("0#")}:{ts.Seconds.ToString("0#")}";
		}

        private static void SetQueuePeriodTotal(int queueId, string site)
        {
			List<Answer1APILib.Plugin.Startel.CallRecord> callRecords;
			lock (CallHistoryCache)
			{
				callRecords = CallHistoryCache.Where(x => x.CallQueue == queueId).ToList();
			}
            var clientIds = GetClientByAffinity(queueId).Select(x => x.ClientId);
			List<Answer1APILib.Plugin.Startel.Models.WorkEvent> workRecords;
			lock (WorkCache)
			{
				workRecords = WorkCache.Where(x => (site == "All" || x.Agent.HomeSite.DisplayName == site) && clientIds.Contains(x.ClientID) && x.Type == Answer1APILib.Plugin.Startel.Models.WorkEventType.ScreenPop).ToList();
			}
            QueuePeriodReturnModel queuePeriodTotal = new QueuePeriodReturnModel();

            if (callRecords.Count() > 0)
            {
                queuePeriodTotal.AverageHold = Math.Round(callRecords.Average(x => x.TotalHoldTime), 1);
                queuePeriodTotal.AverageTalkTime = Math.Round(callRecords.Average(x => x.TalkTime), 1);
                queuePeriodTotal.AverageTimeToAnswer = Math.Round(callRecords.Average(x => x.TimeToAnswer), 1);
                queuePeriodTotal.CallsHandled = callRecords.Count(x => x.TalkTime > 0);
                queuePeriodTotal.CallsOffered = callRecords.Count(x => x.AgentPositionsAndIDs.Count > 0);
                queuePeriodTotal.CallsQueued = callRecords.Count(x => x.CallQueue > -1);
                queuePeriodTotal.LongestHold = callRecords.Max(x => x.SystemHoldTime);
				queuePeriodTotal.LongestTimeToAnswer = callRecords.Max(x => x.TimeToAnswer);
                queuePeriodTotal.SLAQualifiedCalls = callRecords.Count(x => x.TimeToAnswer < 24 && x.TalkTime > 0);
                queuePeriodTotal.TotalCalls = callRecords.Count();

                try
                {
                    queuePeriodTotal.SLA = string.Format("{0}%", Math.Round((double)queuePeriodTotal.SLAQualifiedCalls / (double)queuePeriodTotal.CallsHandled * 100, 1));
                }
                catch
                {
                    queuePeriodTotal.SLA = "0%";
                }
            }

            if(workRecords.Count() > 0)
            {
                queuePeriodTotal.TotalWork = FormatTimeSpan(new TimeSpan(0, 0, workRecords.Sum(x => x.Duration)));
            }

			lock (QueuePeriodTotalCache)
			{
				if (QueuePeriodTotalCache[site].ContainsKey(queueId))
				{
					QueuePeriodTotalCache[site][queueId] = queuePeriodTotal;
				} else
				{
					QueuePeriodTotalCache[site].Add(queueId, queuePeriodTotal);
				}
			}
        }

        private static void SetVerticalPeriodTotal(string verticalName, string site)
        {
            if (!VerticalQueueMap.ContainsKey(verticalName))
            {
                return;
            }
            var queues = VerticalQueueMap[verticalName];
            var clientIds = new List<string>();
            foreach (int queueId in queues)
            {
                var cids = GetClientByAffinity(queueId);
                if (cids.Count() > 0)
                {
                    clientIds.AddRange(cids.Select(x => x.ClientId));
                }
            }

			List<Answer1APILib.Plugin.Startel.CallRecord> callRecords;
			lock (CallHistoryCache)
			{
				callRecords = CallHistoryCache.Where(x => queues.Contains(x.CallQueue)).ToList();
			}
			List<Answer1APILib.Plugin.Startel.Models.WorkEvent> workRecords;
			lock (WorkCache)
			{
				workRecords = WorkCache.Where(x => clientIds.Contains(x.ClientID) && x.Type == Answer1APILib.Plugin.Startel.Models.WorkEventType.ScreenPop).ToList();
			}

			if (site != "All")
			{
				callRecords = callRecords.Where(c => AgentCache.Where(ac => ac.Site == site).Select(ac => ac.Id).ToList().Contains(c.AgentPositionsAndIDs.LastOrDefault() != null ? c.AgentPositionsAndIDs.LastOrDefault().AgentID : 0)).ToList();
				workRecords = workRecords.Where(w => w.Agent.HomeSite.DisplayName == site).ToList();
			}

            QueuePeriodReturnModel verticalPeriodTotal = new QueuePeriodReturnModel();

            if (callRecords.Count() > 0)
            {
                verticalPeriodTotal.AverageHold = Math.Round(callRecords.Average(x => x.TotalHoldTime), 1);
                verticalPeriodTotal.AverageTalkTime = Math.Round(callRecords.Average(x => x.TalkTime), 1);
                verticalPeriodTotal.AverageTimeToAnswer = Math.Round(callRecords.Average(x => x.TimeToAnswer), 1);
                verticalPeriodTotal.CallsHandled = callRecords.Count(x => x.TalkTime > 0);
                verticalPeriodTotal.CallsOffered = callRecords.Count(x => x.AgentPositionsAndIDs.Count > 0);
                verticalPeriodTotal.CallsQueued = callRecords.Count(x => x.CallQueue != -1);
                verticalPeriodTotal.LongestHold = callRecords.Max(x => x.SystemHoldTime);
                verticalPeriodTotal.LongestTimeToAnswer = callRecords.Max(x => x.TimeToAnswer);
                verticalPeriodTotal.SLAQualifiedCalls = callRecords.Count(x => x.TimeToAnswer < 24 && x.TalkTime > 0);
                verticalPeriodTotal.TotalCalls = callRecords.Count();

                try
                {
                    verticalPeriodTotal.SLA = string.Format("{0}%", Math.Round((double)verticalPeriodTotal.SLAQualifiedCalls / (double)verticalPeriodTotal.CallsHandled * 100, 1));
                }
                catch
                {
                    verticalPeriodTotal.SLA = "0%";
                }

            }

            if (workRecords.Count() > 0)
            {
                verticalPeriodTotal.TotalWork = FormatTimeSpan(new TimeSpan(0, 0, workRecords.Sum(x => x.Duration)));
            }

			lock (VerticalPeriodTotalCache)
			{
				if (VerticalPeriodTotalCache[site].ContainsKey(verticalName))
				{
					VerticalPeriodTotalCache[site][verticalName] = verticalPeriodTotal;
				} else
				{
					VerticalPeriodTotalCache[site].Add(verticalName, verticalPeriodTotal);
				}
			}
        }
    }
}