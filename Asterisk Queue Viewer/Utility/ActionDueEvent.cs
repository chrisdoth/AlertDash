using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Asterisk_Queue_Viewer.Utility
{
	public class ActionEvent
	{
		public int msg_id { get; set; }
		public string Agent { get; set; }
		public int AgentId { get; set; }
		public string ClientId { get; set; }
		public string ClientName { get; set; }
		public DateTime SetTime { get; set; }
		public int SetDuration { get; set; }
		public DateTime DueTime { get; set; }
		public DateTime ActionTime { get; set; }
		public double TimeOverdue { get { return (int)Math.Floor((ActionTime - DueTime).TotalSeconds); } }
		public string ActionType { get; set; }
		public string Disposition { get; set; }
	}

	public class ActionDueEvent
	{
		public int AgentId { get; set; }
		public string ClientID { get; set; }
		public string ClientName { get; set; }
		public DateTime SetTime { get; set; }
		public int SetDuration { get; set; }
		public DateTime DueTime { get; set; }
		public DateTime ActionTime { get; set; }
		public double TimeOverdue { get { return (int)Math.Floor((DateTime.Now - DueTime).TotalSeconds); } }
		public string ActionType { get; set; }
		public string ProgressBarType
		{
			get
			{
				if (TimeOverdue < 150)
				{
					return "success";
				} else if (TimeOverdue < 300)
				{
					return "warning";
				} else
				{
					return "danger";
				}
			}
			set { }
		}

		public int ProgressBarMinValue
		{
			get
			{
				if (TimeOverdue < 150)
				{
					return 0;
				} else if (TimeOverdue < 300)
				{
					return 150;
				} else
				{
					return 300;
				}
			}
			set { }
		}

		public int ProgressBarMaxValue
		{
			get
			{
				if (TimeOverdue < 150)
				{
					return 150;
				} else if (TimeOverdue < 300)
				{
					return 300;
				} else
				{
					return (int)(Math.Floor(TimeOverdue));
				}
			}
			set { }
		}

		public ActionDueEvent()
		{

		}
	}
}