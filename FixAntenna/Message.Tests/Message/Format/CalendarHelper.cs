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

namespace Epam.FixAntenna.Message.Tests.Format
{
	internal class CalendarHelper
	{
		public static DateTimeOffset GetLocalTestCalendar()
		{
			return Fill(DateTimeOffset.Now.Offset);
		}

		public static DateTimeOffset GetLocalShiftedTestCalendar()
		{
			return Fill(DateTimeOffset.Now.Offset - TimeSpan.FromHours(2));
		}

		public static DateTimeOffset GetUtcTestCalendar()
		{
			return Fill(TimeSpan.Zero);
		}

		public static DateTimeOffset GetUtcShiftedTestCalendar()
		{
			return GetUtcShiftedTestCalendar(TimeSpan.FromHours(-2));
		}

		public static DateTimeOffset GetUtcShiftedTestCalendar(TimeSpan offset)
		{
			return Fill(offset);
		}

		private static DateTimeOffset Fill(TimeSpan offset)
		{
			return new DateTimeOffset(2019, 12, 31, 23, 59, 59, 999, offset);
		}
	}
}