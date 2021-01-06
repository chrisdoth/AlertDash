using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Asterisk_Queue_Viewer.Models;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Asterisk_Queue_Viewer.Utility
{
    static internal class QueueCollection
    {

        static private List<Queue> Queues { get; set; }

        static internal void Initialize() 
        {
            Queues = new List<Queue>();
        }

        static internal void Add(Queue queue) 
        {
            if (!Contains(queue)) 
            {
                Queues.Add(queue);
            }
        }

        static internal void Remove(Queue queue) 
        { 
            if (Contains(queue))
            {
                Queues.Remove(queue);
            }
        }

        static internal void Remove(string queueID) 
        { 
        
        }

        static internal bool Contains(Queue queue) 
        {
            return Queues.Contains(queue);
        }

        static internal bool Contains(string queueID) 
        {
            return Queues.FirstOrDefault(x => x.QueueID == queueID) == null ? true : false;
        }

        static internal void Clear() 
        {
            Queues.Clear();
        }

        static internal Queue Get(string queueID) 
        {
            return Queues.FirstOrDefault(x => x.QueueID == queueID);
        }

        static internal IList<Queue> GetList() 
        {
            return Queues;
        }

        static internal IEnumerator<Queue> GetEnumerator() 
        {
            return Queues.GetEnumerator();
        }

        static internal int Count { get { return Queues.Count; } }
    }
}