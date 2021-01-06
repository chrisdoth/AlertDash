using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Asterisk_Queue_Viewer.Utility
{
    internal static class StartelData
    {
        static private Dictionary<string, string> ClientNames { get; set; }

        static private Dictionary<string, string> Affinities { get; set; }

        static internal string GetClientName(string clientID)
        {
            lock (ClientNames)
            {
                if (!ClientNames.ContainsKey(clientID))
                {
                    try
                    {
                        ClientNames.Add(clientID, SQLWrapper.GetClientName(clientID));
                    }
                    catch (Exception ex) { }
                }
            }
            return ClientNames[clientID];
        }

        static internal string GetAffinityName(string queueID)
        {
            lock (Affinities)
            {
                if (!Affinities.ContainsKey(queueID))
                {
                    try
                    {
                        Affinities.Add(queueID, SQLWrapper.GetAffinityName(queueID));
                    }
                    catch (Exception ex) { }
                }

            }
            return Affinities[queueID];
        }

        static internal void Initialize()
        {
            ClientNames = new Dictionary<string, string>();
            Affinities = new Dictionary<string, string>();
        }
    }
}