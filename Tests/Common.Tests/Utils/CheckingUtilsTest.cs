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
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Utils;
using NUnit.Framework;

namespace Epam.FixAntenna.Common.Utils
{
	[TestFixture]
	public class CheckingUtilsTest
	{
		[Test]
		public virtual void TryCheckWithinTimeout_ShouldRethrowExceptions()
		{
			var waitingTime = 200;
			Assert.Throws(Is.TypeOf<Exception>()
				, () => { CheckingUtils.TryCheckWithinTimeout(() => throw new Exception(), TimeSpan.FromMilliseconds(waitingTime)); });
		}

		[Test]
		public virtual void TryCheckWithinTimeout_ShouldReturnWithoutWaitingIfTheResultIsTrue()
		{
			var waitingTime = 200;
			var startTime = DateTimeHelper.CurrentMilliseconds;
			CheckingUtils.TryCheckWithinTimeout(() => true, TimeSpan.FromMilliseconds(waitingTime));
			Assert.True(DateTimeHelper.CurrentMilliseconds - startTime < waitingTime);
		}

		[Test]
		public virtual void TryCheckWithinTimeout_ShouldWaitUntilTimeout()
		{
			var waitingTime = 200;
			var startTime = DateTimeHelper.CurrentMilliseconds;
			CheckingUtils.TryCheckWithinTimeout(() => false, TimeSpan.FromMilliseconds(waitingTime));
			Assert.True(DateTimeHelper.CurrentMilliseconds - startTime >= waitingTime);
		}

		[Test]
		public virtual void TryCheckWithinTimeout_ShouldWaitUntilTimeoutIfTheResultIsNull()
		{
			var waitingTime = 200;
			var startTime = DateTimeHelper.CurrentMilliseconds;
			CheckingUtils.TryCheckWithinTimeout(() => null, TimeSpan.FromMilliseconds(waitingTime));
			Assert.True(DateTimeHelper.CurrentMilliseconds - startTime >= waitingTime);
		}

		[Test]
		public virtual void CheckWithinTimeout_ShouldFailIfTheResultIsNotMet()
		{
			var waitingTime = 200;
			Assert.Throws<TimeoutException>(() =>
				CheckingUtils.CheckWithinTimeout(() => false, TimeSpan.FromMilliseconds(waitingTime)));
		}

		[Test]
		public virtual void CheckWithinTimeout_ShouldFailIfTheResultIsNull()
		{
			var waitingTime = 200;
			Assert.Throws<TimeoutException>(() =>
				CheckingUtils.CheckWithinTimeout(() => null, TimeSpan.FromMilliseconds(waitingTime)));
		}
	}
}