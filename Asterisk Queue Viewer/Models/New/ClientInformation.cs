using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Asterisk_Queue_Viewer.Models.New
{
    public class ClientInformation
    {
        public int Id { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string Site { get; set; } = string.Empty;
        public int AffinityId { get; set; } = 0;
    }
}