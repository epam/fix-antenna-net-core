// Copyright (c) 2021 EPAM Systems
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

using System;
using System.Threading;
using Epam.FixAntenna.NetCore.Common.Logging;

namespace Epam.FixAntenna.TestUtils.Hooks
{
	internal class EventHook
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(EventHook));

		protected readonly object LockObject = new object();
		protected Event EventState = Event.Unknown;
		protected long TimeToWaitSum;

		public object EventData { get; set; }

		public string Name { get; }

		public int TimeToWait { get; }

		public EventHook(string eventName) : this(eventName, int.MaxValue)
		{
		}

		public EventHook(string eventName, int timeToWait)
		{
			TimeToWait = timeToWait;
			Name = eventName;
		}

		[Obsolete]
		public EventHook(string eventName, long timeToWait)
		{
			TimeToWait = (int)timeToWait;
			Name = eventName;
		}

		public virtual void RaiseEvent()
		{
			EventState = Event.RaisedEvent;
			lock (LockObject)
			{
				Monitor.PulseAll(LockObject);
			}

			if (Log.IsDebugEnabled)
			{
				Log.Debug("Event " + Name + " raised");
			}
		}

		public virtual void ResetEvent()
		{
			lock (LockObject)
			{
				EventState = Event.Unknown;
			}

			if (Log.IsDebugEnabled)
			{
				Log.Debug("Event " + Name + " reset");
			}
		}

		public virtual bool IsEventRaised()
		{
			if (EventState == Event.RaisedEvent)
			{
				return true;
			}

			lock (LockObject)
			{
				Monitor.Wait(LockObject, TimeToWait);
			}

			return EventState == Event.RaisedEvent;
		}

		public virtual bool IsEventRaised(int timeout)
		{
			if (EventState == Event.RaisedEvent)
			{
				return true;
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

				return EventState == Event.RaisedEvent;
			}
		}

		public virtual bool IsTimeOut()
		{
			lock (LockObject)
			{
				return EventState == Event.Timeout;
			}
		}

		[Obsolete]
		public virtual string GetName()
		{
			return Name ?? ToString();
		}

		[Obsolete]
		public virtual long GetTimeToWait()
		{
			return TimeToWait;
		}
	}
}