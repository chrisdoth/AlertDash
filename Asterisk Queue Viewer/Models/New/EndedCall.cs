using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Asterisk_Queue_Viewer.Models.New
{
	public class EndedCallReturnModel
	{
		public string UniqueId { get; set; }
		public DateTime EndTime { get; set; }
	}
}