using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Asterisk_Queue_Viewer.Models.New
{
    internal class StartelServiceDataModel
    {
        internal string TPServer { get; set; }
        internal string APIServer { get; set; } 
        internal int TPPort { get; set; }
        internal int APIPort { get; set; }
    }
}