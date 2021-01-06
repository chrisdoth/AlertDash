using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Asterisk_Queue_Viewer.Models.New
{
    public class PositionInformation
    {
        internal delegate void StateChangeHandler(PositionInformation sender);

        internal event StateChangeHandler StateChanged;
        public bool IsLicensed { get; set; }
        public int PositionNumber { get; set; }
        public int Holding { get; set; }
        public int New { get; set; }
        public string ClientId { get; set; }
        public string ClientName { get; set; }
        public string CallType { get; set; }
        public string State { get; set; }
        public string Site { get; set; }
        public PositionState CurrentState { get; set; }
        public bool IsTalking { get; set; }
        public bool IsInRotation { get; set; }
        public DateTime LastUpdate { get; set; }
        public TimeSpan Timer
        {
            get { return DateTime.UtcNow.Subtract(LastUpdate); }
            set { }
        }        

        public PositionInformation()
        {
            ClientId = string.Empty;
            ClientName = string.Empty;
            State = "Unavailable";
        }

        public void SetClient(string clientId, string clientName, string callType)
        {
            this.ClientId = clientId;
            this.ClientName = clientName;
            this.CallType = callType;
            LastUpdate = DateTime.UtcNow;
        }

        public void SetState(PositionState stlState)
        {
            
            if(CurrentState != stlState)
            {
                if(stlState.HasFlag(PositionState.Park) && (!CurrentState.HasFlag(PositionState.Park) || stlState.HasFlag(PositionState.Park)))
                {
                    LastUpdate = DateTime.UtcNow;
                }

                CurrentState = stlState;

                if (stlState.HasFlag(PositionState.InRotation))
                {
                    IsInRotation = true;
                }
                else
                {
                    IsInRotation = false;
                }

                if (stlState.HasFlag(PositionState.InTalk))
                {
                    IsTalking = true;
                }
                else
                {
                    IsTalking = false;
                }
                /*
				if (IsInRotation && IsTalking)
				{
					State = "Talk";
				} else if (IsInRotation && !IsTalking)
				{
					State = "Idle";
				} else if (!IsInRotation && IsTalking)
				{
					State = "Outbound/Dispatching";
				} else if (!IsInRotation && !IsTalking)
				{
					State = "Unavailable";
				}
                */

                if (IsInRotation)
                {
                    State = "In";
                }
                else
                {
                    State = "Out";
                }

                if (IsTalking)
                {
                    State += " Talk";
                }
                else
                {
                    State += " Mute";
                }

                if (stlState.HasFlag(PositionState.HasPatch))
                {
                    State += " Patch";
                }

                if (stlState.HasFlag(PositionState.Unreachable))
                {
                    State += " Unreachable";
                }

                if (stlState.HasFlag(PositionState.InWrap))
                {
                    State += " Wrap";
                }

                if (stlState.HasFlag(PositionState.Park))
                {
                    State += " Park";
                }

                if (stlState.HasFlag(PositionState.Recording))
                {
                    State += " Rec";
                }
				//State = State.Replace("In Talk", "Talk").Replace("In Mute", "Idle").Replace("Out Talk", "Outbound/Dispatching").Replace("Out Mute", "Unavailable");
                LastUpdate = DateTime.UtcNow;
                this.StateChanged?.Invoke(this);
            }

            
        }

        [Flags]
        public enum PositionState
        {
            None = 0,
            InRotation = 1,
            InTalk = 2,
            HasPatch = 4,
            Unreachable = 8,
            InWrap = 16,
            Park = 32,
            Recording = 64
        }
    }
}