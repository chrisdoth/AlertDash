using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Asterisk_Queue_Viewer.Models;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Asterisk_Queue_Viewer.Utility
{
    internal delegate void MemberAddedHandler(QueueMember member);
    internal delegate void MemberRemovedHandler(QueueMember member);
    internal delegate void MemberRemovedFromQueueHandler(QueueMember member, string QueueID);
    internal delegate void MemberAddedToQueueHandler(QueueMember member, string QueueID);

    internal static class MemberCollection
    {
        static private ObservableCollection<QueueMember> Members { get; set; }

        static internal event MemberAddedHandler MemberAdded;
        static internal event MemberRemovedHandler MemberRemoved;
        static internal event MemberRemovedFromQueueHandler MemberRemovedFromQueue;
        static internal event MemberAddedToQueueHandler MemberAddedToQueue;

        static internal void Initialize() 
        {
            Members = new ObservableCollection<QueueMember>();

            Members.CollectionChanged += Members_CollectionChanged;
        }

        static internal void Add(QueueMember member) 
        {
            if (!Contains(member))
            {
                Members.Add(member);
            }
        }

        static internal  void Remove(QueueMember member) 
        {
            if (Contains(member)) 
            {
                Members.Remove(member);
            }
        }

        static internal void Remove(string memberName) 
        {
            if (Contains(memberName)) 
            {
                Members.Remove(Members.FirstOrDefault(x => x.Name == memberName));
            }
        }

        static internal void RemoveMemberFromQueue(string memberName, string queueID) 
        {
            if (Contains(memberName)) 
            { 
                QueueMember m = Get(memberName);
                if (MemberRemovedFromQueue != null && m.Queues.Contains(queueID)) 
                {
                    m.Queues.Remove(queueID);
                    MemberRemovedFromQueue(m, queueID);
                }
                if (Get(memberName).Queues.Count == 0) 
                { 
                    Remove(m);
                }
            }
        }

        static internal void AddMemberToQueue(string memberName, string queueID) 
        {
            if (Contains(memberName))
            {
                QueueMember m = Get(memberName);
                if (MemberAddedToQueue != null & !m.Queues.Contains(queueID))
                {
                    m.Queues.Add(queueID);
                    MemberAddedToQueue(m, queueID);
                }
            }
            else 
            {
                throw new Exception("Member Not In Collection");
            }
        }

        static internal void AddMemberToQueue(QueueMember member, string queueID)
        {
            if (Contains(member.Name))
            {
                if (MemberAddedToQueue != null & !member.Queues.Contains(queueID))
                {
                    member.Queues.Add(queueID);
                    MemberAddedToQueue(member, queueID);
                }
            }
            else
            {
                Members.Add(member);
                member.Queues.Add(queueID);
                if (MemberAddedToQueue != null) 
                {
                    MemberAddedToQueue(member, queueID);
                }
            }
        }

        static internal bool Contains(QueueMember member) 
        {
            return Members.Contains(member);
        }

        static internal bool Contains(string memberName)
        {
            return Members.FirstOrDefault(x => x.Name == memberName) == null ? false : true;
        }

        static internal void Clear() 
        {
            Members.Clear();
        }

        static internal QueueMember Get(string memberName) 
        {
            return Members.FirstOrDefault(x => x.Name == memberName);
        }

        static internal QueueMember GetByExten(string extension)
        {
            return Members.FirstOrDefault(x => x.Extension == extension);
        }

        static internal IList<QueueMember> GetList() 
        {
            return Members.ToList();
        }

        static internal IEnumerator<QueueMember> GetEnumerator() 
        {
            return Members.GetEnumerator();
        }

        static internal int Count { get { return Members.Count; } }

        static void Members_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action) 
            { 
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    if (MemberAdded != null) 
                    {
                        foreach (QueueMember m in e.NewItems) 
                        {
                            MemberAdded(m);
                        }
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    if (MemberRemoved != null) 
                    {
                        foreach (QueueMember m in e.OldItems)
                        {
                            MemberRemoved(m);
                        }
                    }
                    break;
            }
        }

    }
}