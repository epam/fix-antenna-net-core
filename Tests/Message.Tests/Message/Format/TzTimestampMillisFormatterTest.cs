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
using Epam.FixAntenna.NetCore.Message.Format;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Message.Tests.Format
{
	[TestFixture]
	internal class TzTimestampMillisFormatterTest : GenericDateFormatterTst
	{
		internal TzTimestampMillisFormatter FixDateFormatter = new TzTimestampMillisFormatter();

		[Test]
		public virtual void TestFormattedStringLengthForHourTz()
		{
			var cal = CalendarHelper.GetUtcShiftedTestCalendar(TimeSpan.FromHours(-2));
			ClassicAssert.AreEqual("YYYYMMDD-HH:MM:SS.sss-hh".Length, FixDateFormatter.GetFormattedStringLength(cal),
				"Wrong format length");
		}

		[Test]
		public virtual void TestFormattedStringLengthForMinTz()
		{
			var cal = CalendarHelper.GetUtcShiftedTestCalendar(TimeSpan.FromMinutes(-150));
			ClassicAssert.AreEqual("YYYYMMDD-HH:MM:SS.sss-hh:mm".Length, FixDateFormatter.GetFormattedStringLength(cal),
				"Wrong format length");
		}

		[Test]
		public virtual void TestFormattedStringLengthForUtc()
		{
			var cal = CalendarHelper.GetUtcTestCalendar();
			ClassicAssert.AreEqual("YYYYMMDD-HH:MM:SS.sssZ".Length, FixDateFormatter.GetFormattedStringLength(cal),
				"Wrong format length");
		}

		[Test]
		public virtual void TestHourTzCalendarFormat()
		{
			var cal = CalendarHelper.GetUtcShiftedTestCalendar(TimeSpan.FromHours(-2));
			CheckFormat(FixDateFormatter, cal, "20191231-23:59:59.999-02");
		}

		[Test]
		public virtual void TestMinTzCalendarFormat()
		{
			var cal = CalendarHelper.GetUtcShiftedTestCalendar(TimeSpan.FromMinutes(-150));
			CheckFormat(FixDateFormatter, cal, "20191231-23:59:59.999-02:30");
		}

		[Test]
		public virtual void TestUtcCalendarFormat()
		{
			var cal = CalendarHelper.GetUtcTestCalendar();
			CheckFormat(FixDateFormatter, cal, "20191231-23:59:59.999Z");
		}
	}
}