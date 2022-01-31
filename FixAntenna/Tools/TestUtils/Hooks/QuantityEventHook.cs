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
	internal class QuantityEventHook : EventHook
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(QuantityEventHook));
		private readonly CountdownEvent _messageWaiter;
		private long _waitNums;

		public QuantityEventHook(string name, int numEvents, int timeout) : base(name, timeout)
		{
			_waitNums = numEvents;
			_messageWaiter = new CountdownEvent(1);
		}

		public override void RaiseEvent()
		{
			lock (LockObject)
			{
				_waitNums--;
			}

			if (_waitNums <= 0)
			{
				_messageWaiter.Signal();
			}
		}

		public override bool IsEventRaised()
		{
			_messageWaiter.Wait(TimeSpan.FromMilliseconds(TimeToWait));
			lock (LockObject)
			{
				var res = _waitNums <= 0;
				if (!res && Log.IsInfoEnabled)
				{
					Log.Warn(GetName() + " expected yet another " + _waitNums + " events");
				}

				return res;
			}
		}

		public override bool IsEventRaised(long elapsedTime)
		{
			_messageWaiter.Wait(TimeSpan.FromMilliseconds(TimeToWait));
			lock (LockObject)
			{
				TimeToWaitSum += elapsedTime;
				return _waitNums <= 0;
			}
		}
	}
}