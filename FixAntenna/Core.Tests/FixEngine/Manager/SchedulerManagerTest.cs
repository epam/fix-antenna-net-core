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
using Epam.FixAntenna.TestUtils.Hooks;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.FixEngine.Manager.Scheduler;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Manager
{
	[TestFixture]
	internal class SchedulerManagerTest
	{
		private SchedulerManager _manager;
		private QuantityEventHook _eventHook;
		private static ILog _log = LogFactory.GetLog(typeof(SchedulerManagerTest));

		[SetUp]
		public void SetUp()
		{
			_manager = new SchedulerManager();
		}

		[TearDown]
		public void TearDown()
		{
			_manager.Shutdown();
		}

		[Test, Timeout(10000)]
		public virtual void TestScheduleOneSession()
		{
			_eventHook = new QuantityEventHook("", 1, 30000);
			using (var mySchedulerTask = new MySchedulerTask(this, "Test"))
			{
				_manager.Schedule(mySchedulerTask, DateTimeHelper.CurrentMilliseconds + 100);
				Assert.IsTrue(_eventHook.IsEventRaised());
			}
		}

		[Test, Timeout(15000)]
		public virtual void TestScheduleManySessions()
		{
			var sessions = 10;
			var tasks = new List<SchedulerTask>();
			_eventHook = new QuantityEventHook("", sessions, 30000);
			try
			{
				for (var i = 0; i < sessions; i++)
				{
					var index = i;
					var mySchedulerTask = new MySchedulerTask(this, $"Test_{i}");
					tasks.Add(mySchedulerTask);
					_manager.Schedule(mySchedulerTask, DateTimeHelper.CurrentMilliseconds + (100 * index + 1));
				}

				Assert.IsTrue(_eventHook.IsEventRaised());
			}
			finally
			{
				foreach (var task in tasks)
				{
					task.Dispose();
				}
			}
		}

		[Test, Timeout(20000)]
		public virtual void TestSchedulePeriodicSession()
		{
			var sessions = 10;
			_eventHook = new QuantityEventHook("", sessions, 30000);
			using (var mySchedulerTask = new MySchedulerTask(this, "Test"))
			{
				_manager.Schedule(mySchedulerTask, DateTimeHelper.CurrentMilliseconds + 100, 1000);
				Assert.IsTrue(_eventHook.IsEventRaised());
			}
		}

		private class MySchedulerTask : SchedulerTask
		{
			private readonly SchedulerManagerTest _outerInstance;

			public MySchedulerTask(SchedulerManagerTest outerInstance, string name) : base(name)
			{
				_outerInstance = outerInstance;
			}

			public override void Run()
			{
				_outerInstance._eventHook.RaiseEvent();
			}
		}
	}
}