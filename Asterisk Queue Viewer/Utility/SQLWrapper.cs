using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Data;
using Asterisk_Queue_Viewer.Models.New;
using NLog;

namespace Asterisk_Queue_Viewer.Utility
{
    internal static class SQLWrapper
    {

        private static string _connectionString = ConfigurationManager.ConnectionStrings["STLNTDBConnectionString"].ConnectionString;
        //private static string _connectionString = "Data Source=cmc-a.a1sopps.local;Initial Catalog=STLNTDB;Persist Security Info=True;User ID=startel;Password=1letrats";
        //private static string _connectionString = "Data Source=cmc-aa;Initial Catalog=STLNTDB;Persist Security Info=True;User ID=startel;Password=1letrats";
        private static Answer1APILib.Plugin.Startel.Database.DBHelper _dbHelper = null;
        private static Dictionary<int, Answer1APILib.Plugin.Startel.Models.SiteInformation> _siteCache = null;

        internal static void ClearCaches()
        {
            _dbHelper = new Answer1APILib.Plugin.Startel.Database.DBHelper(_connectionString);
            _siteCache = new Dictionary<int, Answer1APILib.Plugin.Startel.Models.SiteInformation>();
        }
        internal static string GetClientName(string clientID) 
        {
            string returnData = "-Unknown-";
            SqlCommand command = new SqlCommand("SELECT name FROM cl_index WHERE textid = @clientID");
            command.Parameters.Add(new SqlParameter("@clientID", clientID));

            foreach(DataRow row in ExecuteQuery(command).Rows)
            {
                returnData = row["name"].ToString();
            }

            return returnData;
        }

        internal static string GetAffinityName(string queueID) 
        { 
            string returnData = queueID;
            SqlCommand command = new SqlCommand("SELECT textid FROM aff_index WHERE id = @queueID");
            command.Parameters.Add(new SqlParameter("@queueID", queueID));

            foreach (DataRow row in ExecuteQuery(command).Rows) 
            {
                returnData = row["textid"].ToString();
            }

            return returnData;
        }

		internal static List<ClientInformation> GetClients()
		{
            List<ClientInformation> oReturn = new List<ClientInformation>();
            SqlCommand command = new SqlCommand("SELECT id, textid, name, site_id, aff_id FROM cl_index WHERE site_id IS NOT NULL");

            foreach (DataRow row in ExecuteQuery(command).Rows)
            {
                oReturn.Add(new ClientInformation
                {
                    ClientId = row["textid"].ToString(),
                    Id = (int)row["id"],
                    Name = row["name"].ToString(),
                    Site = GetSite((int)row["site_id"]).DisplayName,
                    AffinityId = (int)row["aff_id"]
                });
            }

            return oReturn;
        }

        internal static ClientInformation GetClient(int clientDbid)
        {
            ClientInformation returnData = new ClientInformation();
            SqlCommand command = new SqlCommand("SELECT textid,name,site_id,aff_id FROM cl_index WHERE id=@id");
            command.Parameters.AddWithValue("@id", clientDbid);

            foreach(DataRow row in ExecuteQuery(command).Rows)
            {
                returnData = new ClientInformation();
                returnData.ClientId = row["textid"].ToString();
                returnData.Name = row["name"].ToString();
                returnData.Id = clientDbid;
                returnData.Site = GetSite((int)row["site_id"]).DisplayName;
                returnData.AffinityId = (int)row["aff_id"];
            }
            return returnData;
        }

        internal static ClientInformation GetClient(string clientId)
        {
            try
            {
                ClientInformation returnData = null;
                SqlCommand command = new SqlCommand("SELECT id,name,site_id,aff_id FROM cl_index WHERE textid=@ClientId");
                command.Parameters.AddWithValue("@ClientId", clientId);

                foreach (DataRow row in ExecuteQuery(command).Rows)
                {
                    returnData = new ClientInformation();
                    returnData.ClientId = clientId;
                    returnData.Name = row["name"].ToString();
                    returnData.Id = (int)row["id"];
                    returnData.Site = GetSite((int)row["site_id"]).DisplayName;
                    returnData.AffinityId = (int)row["aff_id"];
                }
                return returnData;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Failed to get Client {0}", clientId), ex);
            }
            
        }

        internal static AgentInformation GetAgent(int agentId)
        {
            AgentInformation returnData = null;
            SqlCommand command = new SqlCommand(@"
                        SELECT opr_index.id, 
                               opr_index.last_name, 
                               opr_index.first_name, 
                               opr_index.initial,
							   opr_index.option1,
                               access_index.textid AS group_name,
							   ISNULL(sys_multisite.display_name, 'Default') AS SiteName
                        FROM opr_index 
						LEFT OUTER JOIN sys_multisite ON opr_index.home_siteid = sys_multisite.id
						INNER JOIN access_index ON opr_index.access_id = access_index.id
						WHERE opr_index.id = @AgentId
            ");
            command.Parameters.AddWithValue("@AgentId", agentId);

            foreach(DataRow row in ExecuteQuery(command).Rows)
            {
                returnData = new AgentInformation()
                {
                    Id = agentId,
                    FirstName = row["first_name"].ToString(),
                    LastName = row["last_name"].ToString(),
                    Initial = row["initial"].ToString(),
                    Site = row["SiteName"].ToString()
                };
                var agentOptions = (Answer1APILib.Plugin.Startel.Models.AgentOptions)row["option1"];
                if (agentOptions.HasFlag(Answer1APILib.Plugin.Startel.Models.AgentOptions.HideFromSoftSwitchDashboard))
                {
                    returnData.IsVisable = false;
                }
                else
                {
                    returnData.IsVisable = true;
                }
            }

            return returnData;
        }

        internal static AgentInformation GetAgent(string initial)
        {
            AgentInformation returnData = null;
            SqlCommand command = new SqlCommand(@"
                        SELECT opr_index.id, 
                               opr_index.last_name, 
                               opr_index.first_name, 
							   opr_index.option1,
                               access_index.textid AS group_name,
							   ISNULL(sys_multisite.display_name, 'Default') AS SiteName
                        FROM opr_index 
						LEFT OUTER JOIN sys_multisite ON opr_index.home_siteid = sys_multisite.id
						INNER JOIN access_index ON opr_index.access_id = access_index.id
						WHERE opr_index.initial = @Initial
            ");
            command.Parameters.AddWithValue("@Initial", initial);

            foreach (DataRow row in ExecuteQuery(command).Rows)
            {
                returnData = new AgentInformation()
                {
                    Id = (int)row["id"],
                    FirstName = row["first_name"].ToString(),
                    LastName = row["last_name"].ToString(),
                    Initial = initial,
                    Site = row["SiteName"].ToString()
                };
                var agentOptions = (Answer1APILib.Plugin.Startel.Models.AgentOptions)row["option1"];
                if (agentOptions.HasFlag(Answer1APILib.Plugin.Startel.Models.AgentOptions.HideFromSoftSwitchDashboard))
                {
                    returnData.IsVisable = false;
                }
                else
                {
                    returnData.IsVisable = true;
                }
            }

            return returnData;
        }

        internal static Models.AgentInformation GetAgentOld(int agentId)
        {
            var returnData = new Models.AgentInformation();
            var command = new SqlCommand("SELECT id, first_name, last_name FROM opr_index WHERE id = @ID");
            command.Parameters.AddWithValue("@ID", agentId);
            foreach(DataRow row in ExecuteQuery(command).Rows)
            {
                returnData.FirstName = row["first_name"].ToString();
                returnData.LastName = row["last_name"].ToString();
                returnData.Id = agentId;
            }
            return returnData;
        }

        internal static List<Models.RawWorkData> GetWork(DateTime startDate, DateTime endDate)
        {
            endDate = endDate.AddMilliseconds(999);
            var returnData = new List<Models.RawWorkData>();
            var command = new SqlCommand("SELECT dt, opr_id, elapsed_time FROM rts_event2 WHERE event_type = 3100 AND dt BETWEEN @Start AND @End order by dt");
            command.Parameters.AddWithValue("@Start", startDate);
            command.Parameters.AddWithValue("@End", endDate);

            foreach(DataRow row in ExecuteQuery(command).Rows)
            {
                returnData.Add(new Models.RawWorkData()
                {
                    AgentId = (int)row["opr_id"],
                    Duration = (int)row["elapsed_time"],
                    TimeStamp = (DateTime)row["dt"]
                });
            }

            return returnData;
        }

        internal static string GetLastOutRotationReason(int agentId)
        {
            var returnData = string.Empty;
            var command = new SqlCommand("SELECT TOP (1) reason FROM opr_event WHERE event_type = 100 AND opr_id = @OprID ORDER BY dt DESC");
            command.Parameters.AddWithValue("@OprID", agentId);

            foreach(DataRow row in ExecuteQuery(command).Rows)
            {
                returnData = row["reason"].ToString();
            }
            return returnData;
        }

        internal static List<Models.LogEvent> GetLogEvents(DateTime startDate, DateTime endDate)
        {
            var returnData = new List<Models.LogEvent>();
            var command = new SqlCommand("SELECT dt, opr_id, elapsed_time, event_type FROM rts_event2 WHERE event_type IN (3001,3004) AND dt BETWEEN @Start AND @End ORDER BY dt");
            command.Parameters.AddWithValue("@Start", startDate);
            command.Parameters.AddWithValue("@End", endDate);

            foreach(DataRow row in ExecuteQuery(command).Rows)
            {
                returnData.Add(new Models.LogEvent()
                {
                    EventType = (int)row["event_type"] == 3001 ? Models.LogEvent.LogEventType.Logout : Models.LogEvent.LogEventType.login,
                    TimeStamp = (DateTime)row["dt"],
                    AgentId = (int)row["opr_id"] 
                });
            }

            return returnData;
        }

        internal static List<Models.CallData> GetCalls(DateTime startDate, DateTime endDate)
        {
            var returnData = new List<Models.CallData>();
            var command = new SqlCommand(@"SELECT client_callid,call_record.dt, id, call_type, dnis_number, agt_ids, content, 
                                          ISNULL(CAST(CAST(call_record_overflow.overflow_content AS varbinary(MAX)) AS varchar(MAX)), '') AS [overflow]
                                          FROM call_record
                                          LEFT OUTER JOIN call_record_overflow ON call_record.id = call_record_overflow.record_id
                                          WHERE call_type = 1000 AND call_record.dt BETWEEN @Start and @End ORDER BY dt");
            command.Parameters.AddWithValue("@Start", startDate);
            command.Parameters.AddWithValue("@End", endDate);

            foreach(DataRow row in ExecuteQuery(command).Rows)
            {
                try
                {
                    var callRecord = new Startel.CDRParser();
                    callRecord.Initialize((int)row["call_type"], row["dnis_number"].ToString(), row["agt_ids"].ToString(), row["content"].ToString() + row["overflow"].ToString());
                    var call = new Models.CallData()
                    {
                        Affinity = callRecord.QueueId,
                        AgentId = GetLastAgentId(row["agt_ids"].ToString()),
                        CallId = (int)row["id"],
                        RingTime = ((double)callRecord.RingTime / 10),
                        TalkTime = ((double)callRecord.TalkTime / 10),
                        TimeStamp = (DateTime)row["dt"],
                        TimeToAnswer = Math.Round(((double)callRecord.TimeToAgtAnswer / 10), 1),
                        TotalCallTime = ((double)callRecord.TotalTime / 10),
                        TotalHoldTime = ((double)callRecord.TotalHoldTime / 10),
                        ClientID = row["client_callid"].ToString()
                    };
                    if (call.RingTime != call.TimeToAnswer)
                    {
                        call.TimeToAnswer = call.RingTime + call.TimeToAnswer;
                    }
                    returnData.Add(call);
                }
                catch { }
            }

            return returnData;
        }

		internal static List<ActionDueEvent> GetActionsDue(DateTime startDate, DateTime endDate, int lookbackDays = 15)
		{
			#region SQLQuery
			var command = new SqlCommand(@"
select 
	msg_id, 
	cl_index.textid as ClientID,
	cl_index.name as ClientName,
	SetTime,
	SetDuration,
	DueTime,
	datediff(second, DueTime, getdate()) as OverDueSec,
	'Time Delay' as ActionType
from (select
		msg_id,
		client_dbid,
		dt as SetTime,
		SetDuration,
		dateadd(minute, isnull(SetDuration, 0), dt) as DueTime,
		ActType
		from 
(
		select 
			alltrace.id, alltrace.msg_id, alltrace.dt, isnull(mf.dt, ordf.dt) as filed_date, alltrace.opr_id, isnull(isnull(mu.client_dbid, ordu.client_dbid), isnull(mf.client_dbid, ordf.client_dbid)) as client_dbid
			, case 
				when [type] in (10, 24) and duration > 0 then 'Set'
				when [type] in (10, 24) and duration = 0 then 'Cancel'
				when [type] in (9, 23) then 'Pickup'
			end as ActType
			, case
				when [type] in (10, 24) and duration > 0 then duration
				else NULL
			end as SetDuration
			, case when [type] in (10, 24) then 10 else 9 end as [type]
		from (
			select id, msg_id msg_id, dt, opr_id, lead([type]) over (partition by msg_id order by id) as nextType, try_parse(comment as int) as duration, case when [type] in (10, 24) and try_parse(comment as int) > 0 then 'Set' when [type] in (10, 24) and try_parse(comment as int) = 0 then 'Cancel' when [type] in (9, 23) then 'Pickup' end as ActType, case when [type] in (10, 24) and try_parse(comment as int) > 0 then try_parse(comment as int) else NULL end as SetDuration, case when [type] in (10, 24) then 10 else 9 end as [type] from textmsg_trace where type in (9, 10, 23, 24) and dt > dateadd(day, @history, @StartDate)
			union
			select id, order_id msg_id, dt, opr_id, lead([type]) over (partition by order_id order by id) as nextType, try_parse(comment as int) as duration, case when [type] in (10, 24) and try_parse(comment as int) > 0 then 'Set' when [type] in (10, 24) and try_parse(comment as int) = 0 then 'Cancel' when [type] in (9, 23) then 'Pickup' end as ActType, case when [type] in (10, 24) and try_parse(comment as int) > 0 then try_parse(comment as int) else NULL end as SetDuration, case when [type] in (10, 24) then 10 else 9 end as [type] from order_trace where type in (9, 10, 23, 24) and dt > dateadd(day, @history, @StartDate)
		) alltrace
		left outer join textmsg_filed mf on alltrace.msg_id = mf.id
		left outer join order_filed ordf on alltrace.msg_id = ordf.id
		left outer join textmsg_unfiled mu on alltrace.msg_id = mu.id
		left outer join order_unfiled ordu on alltrace.msg_id = ordu.id
		where nextType is null and mf.id is null and ordf.id is null and duration > 0
	) InnerQuery
		) SetActions
	left outer join cl_index on cl_index.id = SetActions.client_dbid
	where ActType = 'Set' and DueTime < getdate()

union all

select
	msg_id,
	cl.textid as ClientID,
	cl.name as ClientName,
	SetTime,
	SetDuration,
	DueTime,
	datediff(second, DueTime, getdate()) as OverDueSec,
	'Email Response' as ActionType
from
	(select
		alltrace.msg_id,
		alltrace.opr_id,
		er.client_dbid,
		er.dt as SetTime,
		0 as SetDuration, 
		er.dt as DueTime,
		alltrace.dt as ActionTime
	from email_response er
	left outer join (
		select msg_id, dt, opr_id, event_id from textmsg_trace
		union
		select order_id msg_id, dt, opr_id, event_id from order_trace
	) alltrace on er.id = alltrace.event_id
	left outer join textmsg_filed msgf on alltrace.msg_id = msgf.id
	left outer join textmsg_unfiled msgu on alltrace.msg_id = msgu.id
	left outer join order_filed ordf on alltrace.msg_id = ordf.id
	left outer join order_unfiled ordu on alltrace.msg_id = ordu.id) InnerQuery
left outer join opr_index o on o.id = InnerQuery.opr_id
left outer join cl_index cl on cl.id = InnerQuery.client_dbid
where msg_id is null and DueTime between @StartDate and @EndDate

union all

select
	msg_id,
	cl.textid as ClientID,
	cl.name as ClientName,
	SetTime,
	SetDuration,
	DueTime,
	datediff(second, DueTime, getdate()) as OverDueSec,
	'SM Response' as ActionType
from
	(select
		alltrace.msg_id,
		alltrace.opr_id,
		evt.client_id as client_dbid,
		evt.dt as SetTime,
		0 as SetDuration, 
		evt.dt as DueTime,
		alltrace.dt as ActionTime
	from rts_event2 evt
	left outer join (
		select msg_id, dt, opr_id, event_id, [type] from textmsg_trace
		union
		select order_id msg_id, dt, opr_id, event_id, [type] from order_trace
	) alltrace on evt.id = alltrace.event_id
	left outer join textmsg_filed msgf on alltrace.msg_id = msgf.id
	left outer join textmsg_unfiled msgu on alltrace.msg_id = msgu.id
	left outer join order_filed ordf on alltrace.msg_id = ordf.id
	left outer join order_unfiled ordu on alltrace.msg_id = ordu.id
	where evt.event_type IN (1210, 1212, 1214) and alltrace.[type] in (26, 30)) InnerQuery
left outer join opr_index o on o.id = InnerQuery.opr_id
left outer join cl_index cl on cl.id = InnerQuery.client_dbid
where msg_id is null and DueTime between @StartDate and @EndDate

order by DueTime desc");
			#endregion

			command.Parameters.AddWithValue("@StartDate", startDate);
			command.Parameters.AddWithValue("@EndDate", endDate);
			command.Parameters.AddWithValue("@history", lookbackDays * -1);

			var oReturn = new List<ActionDueEvent>();

			foreach (DataRow row in ExecuteQuery(command).Rows)
			{
				try
				{
					oReturn.Add(new ActionDueEvent
					{
						ClientID = row["ClientID"].ToString(),
						ClientName = row["ClientName"].ToString(),
						SetTime = (DateTime)row["SetTime"],
						SetDuration = (int)row["SetDuration"],
						DueTime = (DateTime)row["DueTime"],
						ActionType = row["ActionType"].ToString()
					});
				} catch { }
			}

			return oReturn;
		}

		internal static List<ActionEvent> GetActionsCompleted(DateTime startDate, DateTime endDate, int lookbackDays = 15)
		{
			#region SQL Query
			var command = new SqlCommand(@"
select 
	msg_id, 
	case 
		when Result = 'Filed' then opr_filed.user_name
		else opr_action.user_name
	end as Agent,
	case
		when Result = 'Filed' then opr_filed.id
		else opr_action.id
	end as AgentId,
	cl_index.textid as ClientID,
	cl_index.name as ClientName,
	SetTime,
	SetDuration,
	DueTime,
	ActionTime,
	'Time Delay' as ActionType,
    Result as Disposition
from (select
		id, 
		msg_id,
		lead(opr_id) over (partition by msg_id order by id) as opr_id,
		filed_id,
		client_dbid,
		dt as SetTime,
		SetDuration,
		dateadd(minute, isnull(SetDuration, 0), dt) as DueTime,
		ActType,
		lead(ActType) over (partition by msg_id order by id) as NextAct,
		case when lead(ActType) over (partition by msg_id order by id) is not null
			then
				case
					when lead(ActType) over (partition by msg_id order by id) = 'Set' then 'Reset'
					when lead(ActType) over (partition by msg_id order by id) = 'Pickup' then 'Picked Up'
					when lead(ActType) over (partition by msg_id order by id) = 'Cancel' then 'Cancelled'
					when filed_date is not null then 'Filed'
				end 
			else
				case when filed_date is not null then 'Filed' else 'Unfiled' end
			end as Result,
		case
			when 
				lead(ActType) over (partition by msg_id order by id) = 'Pickup' 
				or lead(ActType) over (partition by msg_id order by id) = 'Cancel' 
				or lead(ActType) over (partition by msg_id order by id) = 'Set'
				then lead(dt) over (partition by msg_id order by id)
			else isnull(filed_date, case when @EndDate > getdate() then getdate() else @EndDate end)
		end as ActionTime
		from 
(
		select 
			alltrace.id, alltrace.msg_id, alltrace.dt, isnull(mf.dt, ordf.dt) as filed_date, alltrace.opr_id, isnull(mf.opr_dbid, ordf.opr_dbid) as filed_id, isnull(isnull(mu.client_dbid, ordu.client_dbid), isnull(mf.client_dbid, ordf.client_dbid)) as client_dbid
			, case 
				when [type] in (10, 24) and duration > 0 then 'Set'
				when [type] in (10, 24) and duration = 0 then 'Cancel'
				when [type] in (9, 23) then 'Pickup'
			end as ActType
			, case
				when [type] in (10, 24) and duration > 0 then duration
				else NULL
			end as SetDuration
			, case when [type] in (10, 24) then 10 else 9 end as [type]
		from (
			select id, msg_id msg_id, dt, opr_id, try_parse(comment as int) as duration, case when [type] in (10, 24) and try_parse(comment as int) > 0 then 'Set' when [type] in (10, 24) and try_parse(comment as int) = 0 then 'Cancel' when [type] in (9, 23) then 'Pickup' end as ActType, case when [type] in (10, 24) and try_parse(comment as int) > 0 then try_parse(comment as int) else NULL end as SetDuration, case when [type] in (10, 24) then 10 else 9 end as [type] from textmsg_trace where type in (9, 10, 23, 24) and dt > dateadd(day, @history, @StartDate) and dt < @EndDate
			union
			select id, order_id msg_id, dt, opr_id, try_parse(comment as int) as duration, case when [type] in (10, 24) and try_parse(comment as int) > 0 then 'Set' when [type] in (10, 24) and try_parse(comment as int) = 0 then 'Cancel' when [type] in (9, 23) then 'Pickup' end as ActType, case when [type] in (10, 24) and try_parse(comment as int) > 0 then try_parse(comment as int) else NULL end as SetDuration, case when [type] in (10, 24) then 10 else 9 end as [type] from order_trace where type in (9, 10, 23, 24) and dt > dateadd(day, @history, @StartDate) and dt < @EndDate
		) alltrace
		left outer join textmsg_filed mf on alltrace.msg_id = mf.id and mf.dt < @EndDate
		left outer join order_filed ordf on alltrace.msg_id = ordf.id and ordf.dt < @EndDate
		left outer join textmsg_unfiled mu on alltrace.msg_id = mu.id and mu.dt < @EndDate
		left outer join order_unfiled ordu on alltrace.msg_id = ordu.id and ordu.dt < @EndDate
) InnerQuery
		) SetActions
	left outer join opr_index opr_action on opr_action.id = SetActions.opr_id
	left outer join opr_index opr_filed on opr_filed.id = SetActions.filed_id
	left outer join cl_index on cl_index.id = SetActions.client_dbid
	where ActType = 'Set' and (DueTime between @StartDate and @EndDate) -- and Result = 'Unfiled'

union all

select
	msg_id,
	o.user_name as Agent,
	o.id as AgentId,
	cl.textid as ClientID,
	cl.name as ClientName,
	SetTime,
	SetDuration,
	DueTime,
	ActionTime,
	'Email Response' as ActionType,
    'Response' as Disposition
from
	(select
		alltrace.msg_id,
		alltrace.opr_id,
		isnull(isnull(msgf.client_dbid, msgu.client_dbid), isnull(ordf.client_dbid, ordu.client_dbid)) as client_dbid,
		er.dt as SetTime,
		0 as SetDuration, 
		er.dt as DueTime,
		alltrace.dt as ActionTime
	from email_response er
	inner join (
		select msg_id, dt, opr_id, event_id from textmsg_trace
		union
		select order_id msg_id, dt, opr_id, event_id from order_trace
	) alltrace on er.id = alltrace.event_id
	left outer join textmsg_filed msgf on alltrace.msg_id = msgf.id
	left outer join textmsg_unfiled msgu on alltrace.msg_id = msgu.id
	left outer join order_filed ordf on alltrace.msg_id = ordf.id
	left outer join order_unfiled ordu on alltrace.msg_id = ordu.id) InnerQuery
left outer join opr_index o on o.id = InnerQuery.opr_id
left outer join cl_index cl on cl.id = InnerQuery.client_dbid
where DueTime between @StartDate and @EndDate

union all

select
	msg_id,
	o.user_name as Agent,
	o.id as AgentId,
	cl.textid as ClientID,
	cl.name as ClientName,
	SetTime,
	SetDuration,
	DueTime,
	ActionTime,
	'SM Response' as ActionType,
    'Response' as Disposition
from
	(select
		alltrace.msg_id,
		alltrace.opr_id,
		isnull(isnull(msgf.client_dbid, msgu.client_dbid), isnull(ordf.client_dbid, ordu.client_dbid)) as client_dbid,
		evt.dt as SetTime,
		0 as SetDuration, 
		evt.dt as DueTime,
		alltrace.dt as ActionTime
	from rts_event2 evt
	inner join (
		select msg_id, dt, opr_id, event_id, [type] from textmsg_trace
		union
		select order_id msg_id, dt, opr_id, event_id, [type] from order_trace
	) alltrace on evt.id = alltrace.event_id
	left outer join textmsg_filed msgf on alltrace.msg_id = msgf.id
	left outer join textmsg_unfiled msgu on alltrace.msg_id = msgu.id
	left outer join order_filed ordf on alltrace.msg_id = ordf.id
	left outer join order_unfiled ordu on alltrace.msg_id = ordu.id
	where evt.event_type IN (1210, 1212, 1214) and alltrace.[type] in (26, 30)) InnerQuery
left outer join opr_index o on o.id = InnerQuery.opr_id
left outer join cl_index cl on cl.id = InnerQuery.client_dbid
where DueTime between @StartDate and @EndDate
");
			#endregion

			command.Parameters.AddWithValue("@StartDate", startDate);
			command.Parameters.AddWithValue("@EndDate", endDate);
			command.Parameters.AddWithValue("@history", lookbackDays * -1);

			var oReturn = new List<ActionEvent>();

			foreach (DataRow row in ExecuteQuery(command).Rows)
			{
				try
				{
					oReturn.Add(new ActionEvent
					{
						msg_id = (int)row["msg_id"],
						Agent = row["Agent"].ToString(),
						AgentId = row["AgentId"] != DBNull.Value ? (int)row["AgentId"] : 0,
						ClientId = row["ClientID"].ToString(),
						ClientName = row["ClientName"].ToString(),
						SetTime = (DateTime)row["SetTime"],
						SetDuration = (int)row["SetDuration"],
						DueTime = (DateTime)row["DueTime"],
						ActionTime = (DateTime)row["ActionTime"],
						ActionType = row["ActionType"].ToString(),
						Disposition = row["Disposition"].ToString()
					});
				} catch { }
			}

			return oReturn;
		}

		internal static StartelServiceDataModel GetStartelServiceData()
        {
            var returnData = new StartelServiceDataModel()
            {
                APIServer = "127.0.0.1",
                TPServer = "127.0.0.1",
                APIPort = 5038,
                TPPort = 9101
            };

            var command = new SqlCommand("SELECT [tp_ip], [tp_port] FROM [stl_site]");
            foreach(DataRow row in ExecuteQuery(command).Rows)
            {
                returnData.TPServer = row["tp_ip"].ToString();
                returnData.TPPort = (int)row["tp_port"];
            }
            command = new SqlCommand("SELECT [server_name], [param1] FROM [stl_ctinfo] WHERE [switch_type] = 10");
            foreach(DataRow row in ExecuteQuery(command).Rows)
            {
                returnData.APIServer = row["server_name"].ToString();
                returnData.APIPort = (int)row["param1"];
            }
            return returnData;
        }

        internal static bool IsPositionLicensed(int posId)
        {
            var command = new SqlCommand("SELECT id FROM pos_index WHERE id = @pos");
            command.Parameters.AddWithValue("@pos", posId);
            return ExecuteQuery(command).Rows.Count > 0 ? true : false;
        }

		internal static List<Answer1APILib.Plugin.Startel.Models.WorkEvent> GetWorkEvents(DateTime startDate)
		{
			return GetWorkEvents(startDate, 0);
		}

		internal static List<Answer1APILib.Plugin.Startel.Models.WorkEvent> GetWorkEvents(int startId)
		{
			return GetWorkEvents(DateTime.MinValue, startId);
		}

		internal static List<Answer1APILib.Plugin.Startel.Models.WorkEvent> GetWorkEvents(DateTime startDate, int startId)
        {
            //Testing a null reference error
            var logger = NLog.LogManager.GetCurrentClassLogger();
            Answer1APILib.Plugin.Startel.Models.WorkEventType[] eventTypes =
                {
                    Answer1APILib.Plugin.Startel.Models.WorkEventType.ScreenPop,
                    Answer1APILib.Plugin.Startel.Models.WorkEventType.SAILogin,
                    Answer1APILib.Plugin.Startel.Models.WorkEventType.SAILogout,
                    Answer1APILib.Plugin.Startel.Models.WorkEventType.StartRotation,
                    Answer1APILib.Plugin.Startel.Models.WorkEventType.EndRotation
                };
            try
            {
				List<Answer1APILib.Plugin.Startel.Models.WorkEvent> events;
				if (startDate > DateTime.MinValue)
				{
					var endDate = DateTime.Today.AddDays(1).AddTicks(-1);
					events = _dbHelper.WorkRecords.GetEvents(startDate, endDate, eventTypes).ToList();
				} else
				{
					events = _dbHelper.WorkRecords.GetEvents(startId, eventTypes).ToList();
				}

                return events;
            }
            catch (Exception ex)
            {
                logger.Error("SQLWrapper -> {0} Start:{1}", ex.Message, startDate == null ? "Null" : "Not Null");
                return new List<Answer1APILib.Plugin.Startel.Models.WorkEvent>();
            }
            
        }

        internal static List<Answer1APILib.Plugin.Startel.CallRecord> GetCallHistory(DateTime startDate)
        {
            var endDate = DateTime.Today.AddDays(1).AddTicks(-1);
            var calls = _dbHelper.CallRecords.GetCalls(startDate, endDate, Answer1APILib.Plugin.Startel.Models.CallDirection.Incoming);

            return calls.ToList();
        }

        internal static List<Answer1APILib.Plugin.Startel.Models.SiteInformation> GetSites()
        {
            var test = _dbHelper.AgentRecords.GetSite();
            return _dbHelper.AgentRecords.GetSite();
        }

        internal static List<Answer1APILib.Plugin.Startel.Models.ClientInformation> GetClientsForAffinity(int affinityId)
        {
            return _dbHelper.ClientRecords.GetClientsFromAffinity(affinityId).ToList();
        }

        internal static List<Answer1APILib.Plugin.Startel.Models.AffinityInformation> GetAffinities()
        {
            return _dbHelper.ClientRecords.GetAffinities().ToList();
        }

        internal static void Initialize()
        {
            _dbHelper = new Answer1APILib.Plugin.Startel.Database.DBHelper(_connectionString);
            _siteCache = new Dictionary<int, Answer1APILib.Plugin.Startel.Models.SiteInformation>();
        }

        private static DataTable ExecuteQuery(SqlCommand command) 
        {
            DataTable dt = new DataTable();
            SqlDataAdapter da = new SqlDataAdapter();

            //command.Connection = new SqlConnection("Data Source=10.0.1.225;Initial Catalog=STLNTDB;Persist Security Info=True;User ID=startel;Password=1letrats");
            command.Connection = new SqlConnection(_connectionString);

            try
            {
                da.SelectCommand = command;
                command.Connection.Open();
                da.Fill(dt);
            }
            catch (Exception ex)
            {
                var test = "";
            }
            finally { command.Connection.Close(); }

            return dt;
        }

        private static Answer1APILib.Plugin.Startel.Models.SiteInformation GetSite(int siteId)
        {
            lock (_siteCache)
            {
                if (_siteCache.ContainsKey(siteId))
                {
                    return _siteCache[siteId];
                }
                else
                {
                    var site = _dbHelper.AgentRecords.GetSite(siteId);
                    _siteCache.Add(siteId, site);
                    return site;
                }
            }
            
        }

        private static int GetLastAgentId(string agt_ids)
        {
            if(agt_ids.Length > 0)
            {
                var agtAndPos = agt_ids.Split(',');
                var agentid = agtAndPos.Last().Split(':')[1];
                return int.Parse(agentid);
            }
            return -1;
        }
    }
}