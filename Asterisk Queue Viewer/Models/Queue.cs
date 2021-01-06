using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Asterisk_Queue_Viewer.Utility;

namespace Asterisk_Queue_Viewer.Models
{
    public class Queue
    {
        public String QueueID { get; set; }
        public List<QueueCall> Calls { get; private set; }
        //public List<QueueMember> Members { get; private set; }

        public Queue() 
        { 
           this.Calls = new List<QueueCall>();
           //this.Members = new List<QueueMember>();

           CallCollection.CallAdded += CallCollection_CallAdded;
           CallCollection.CallRemoved += CallCollection_CallRemoved;
           CallCollection.CallRemovedFromQueue += CallCollection_CallRemovedFromQueue;
           CallCollection.CallAddedToQueue += CallCollection_CallAddedToQueue;

           //MemberCollection.MemberAdded += MemeberCollection_MemberAdded;
           //MemberCollection.MemberRemoved += MemeberCollection_MemberRemoved;
           //MemberCollection.MemberRemovedFromQueue += MemberCollection_MemberRemovedFromQueue;
           //MemberCollection.MemberAddedToQueue += MemberCollection_MemberAddedToQueue;


        }

        void CallCollection_CallAddedToQueue(QueueCall call, string queueID)
        {
            if (queueID == this.QueueID) 
            {
                if (!Calls.Contains(call)) 
                {
                    Calls.Add(call);
                    ReconfigurePositions();
                }
            }
        }

        void CallCollection_CallRemovedFromQueue(QueueCall call, string queueID)
        {
            if (queueID == this.QueueID) 
            {
                if (Calls.Contains(call)) 
                {
                    Calls.Remove(call);
                    ReconfigurePositions();
                }
            }
        }

        //void MemberCollection_MemberAddedToQueue(QueueMember member, string QueueID)
        //{
        //    if (QueueID == this.QueueID)
        //    {
        //        if (!Members.Contains(member))
        //        {
        //            Members.Add(member);
        //        }
        //    }
        //}

        //void MemberCollection_MemberRemovedFromQueue(QueueMember member, string QueueID)
        //{
        //    if (QueueID == this.QueueID)
        //    {
        //        if (Members.Contains(member))
        //        {
        //            Members.Remove(member);
        //        }
        //    }
        //}

        //void MemeberCollection_MemberRemoved(QueueMember member)
        //{
        //    if (member.Queues.Contains(QueueID))
        //    {
        //        Members.Remove(member);
        //    }
        //}

        //void MemeberCollection_MemberAdded(QueueMember member)
        //{
        //    if (member.Queues.Contains(QueueID))
        //    {
        //        if (Members.FirstOrDefault(x => x.Name == member.Name) == null)
        //        {
        //            Members.Add(member);
        //        }
        //    }
        //}

        void CallCollection_CallRemoved(QueueCall call)
        {
            if (call.Queue == QueueID) 
            {
                Calls.Remove(call);
                ReconfigurePositions();
            }
        }

        void CallCollection_CallAdded(QueueCall call)
        {
            if (call.Queue == QueueID) 
            {
                Calls.Add(call);
                ReconfigurePositions();
            }
        }

        void ReconfigurePositions() 
        {
            int currentPosition = 1;
            foreach (QueueCall call in Calls.OrderBy(x => x.Position)) 
            {
                call.Position = currentPosition;
                currentPosition++;
            }
        }
    }
}