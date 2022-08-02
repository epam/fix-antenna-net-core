// Copyright (c) 2022 EPAM Systems
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Epam.FixAntenna.NetCore.FixEngine.Session;

using NUnit.Framework;

using System.Collections.Generic;
using System.Threading;

namespace Epam.FixAntenna.TestUtils.Hooks
{
	internal class DisconnectEventHook : EventHook
	{
		private static readonly object ListLock = new object();
		private readonly LinkedList<DisconnectReason> _reasons = new LinkedList<DisconnectReason>();

		public DisconnectEventHook(string eventName, int timeToWait) : base(eventName, timeToWait)
		{
		}

		public virtual void RaiseEvent(DisconnectReason reason)
		{
			lock (ListLock)
			{
				_reasons.AddLast(reason);
			}
			RaiseEvent();
		}

		public virtual bool IsEventRaised(DisconnectReason expectedReason)
		{
			if (EventState == Event.RaisedEvent)
			{
				lock (ListLock)
				{
					if (_reasons.First.Value == expectedReason)
					{
						_reasons.RemoveFirst();
						return true;
					}
				}
			}
			lock (LockObject)
			{
				Monitor.Wait(LockObject, TimeToWait);
				var raised = EventState == Event.RaisedEvent;
				if (raised)
				{
					DisconnectReason firstReason;
					lock (ListLock)
					{
						firstReason = _reasons.First.Value;
						_reasons.RemoveFirst();
					}
					Assert.AreEqual(expectedReason, firstReason, "Incorrect reason");
				}
				return raised;
			}
		}


		public virtual bool IsEventRaised(int timeout, DisconnectReason expectedReason)
		{
			if (EventState == Event.RaisedEvent)
			{
				lock (ListLock)
				{
					if (_reasons.First.Value == expectedReason)
					{
						return true;
					}
				}
			}
			lock (LockObject)
			{
				Monitor.Wait(LockObject, timeout);
				if (EventState != Event.RaisedEvent)
				{
					TimeToWaitSum += timeout;
					if (TimeToWaitSum >= TimeToWait)
					{
						EventState = Event.Timeout;
					}
				}

				var raised = EventState == Event.RaisedEvent;
				if (raised)
				{
					DisconnectReason firstReason;
					lock (ListLock)
					{
						firstReason = _reasons.First.Value;
						_reasons.RemoveFirst();
					}
					Assert.AreEqual(expectedReason, firstReason, "Incorrect reason");
				}
				return raised;
			}
		}
	}
}