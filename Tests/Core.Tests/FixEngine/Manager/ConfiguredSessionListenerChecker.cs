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

using System.Collections.Generic;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Manager
{
	internal class ConfiguredSessionListenerChecker : IConfiguredSessionListener
	{
		internal LinkedList<CslcEvent> Events = new LinkedList<CslcEvent>();

		public void OnAddSession(SessionParameters @params)
		{
			Events.AddLast(new AddSessionCslcEvent(@params));
		}

		public void OnRemoveSession(SessionParameters @params)
		{
			Events.AddLast(new RemoveSessionCslcEvent(@params));
		}

		public virtual void ClearEvents()
		{
			Events.Clear();
		}

		public virtual void ClassicAssertEmpty()
		{
			ClassicAssert.AreEqual(0, Events.Count, "There are extra events");
		}

		public virtual void CheckAddSessionEvent(SessionParameters @params)
		{
			var @event = PollNextEvent();
			ClassicAssert.IsTrue(@event is AddSessionCslcEvent, "There is another event: " + @event);
			ClassicAssert.AreEqual(@params, @event.GetParams(), "Event has other parameters");
		}

		public virtual void CheckRemoveSessionEvent(SessionParameters @params)
		{
			var @event = PollNextEvent();
			ClassicAssert.IsTrue(@event is RemoveSessionCslcEvent, "There is another event: " + @event);
			ClassicAssert.AreEqual(@params, @event.GetParams(), "Event has other parameters");
		}

		private CslcEvent PollNextEvent()
		{
			if (Events.Count == 0)
			{
				ClassicAssert.Fail("No events");
			}

			var first = Events.First.Value;
			Events.Remove(first);
			return first;
		}

		internal class CslcEvent
		{
			internal SessionParameters Params;

			public CslcEvent(SessionParameters @params)
			{
				Params = @params;
			}

			public virtual SessionParameters GetParams()
			{
				return Params;
			}

			public override string ToString()
			{
				return GetType().FullName + "(" + Params + ")";
			}
		}

		internal class AddSessionCslcEvent : CslcEvent
		{
			public AddSessionCslcEvent(SessionParameters @params) : base(@params)
			{
			}
		}

		internal class RemoveSessionCslcEvent : CslcEvent
		{
			public RemoveSessionCslcEvent(SessionParameters @params) : base(@params)
			{
			}
		}
	}
}