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

namespace Epam.FixAntenna.Tester.Updater
{
	public class SmartLazyUpdater : LazyUpdater
	{
		public override string UpdateSendingTime(string message)
		{
			string result = message;
			if (message.IndexOf("52=00000000-00:00:00", StringComparison.Ordinal) >= 0)
			{
				result = base.UpdateSendingTime(result);
			}
			if (message.IndexOf("122=00000000-00:00:00", StringComparison.Ordinal) >= 0)
			{
				result = base.UpdateOriginalSendingTime(result);
			}
			return result;
		}
	}

}