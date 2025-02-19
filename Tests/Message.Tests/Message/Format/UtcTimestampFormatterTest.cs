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
	internal class UtcTimestampFormatterTest : GenericDateFormatterTst
	{
		internal UtcTimestampFormatter FixDateFormatter = new UtcTimestampFormatter();

		[Test]
		public virtual void TestFormattedStringLength()
		{
			ClassicAssert.AreEqual("YYYYMMDD-HH:MM:SS".Length, FixDateFormatter.GetFormattedStringLength(DateTime.Now),
				"Wrong format length");
		}

		[Test]
		public virtual void TestNonUtcCalendarFormat()
		{
			var cal = CalendarHelper.GetUtcShiftedTestCalendar();
			CheckFormat(FixDateFormatter, cal, "20200101-01:59:59");
		}

		[Test]
		public virtual void TestUtcCalendarFormat()
		{
			var cal = CalendarHelper.GetUtcTestCalendar();
			CheckFormat(FixDateFormatter, cal, "20191231-23:59:59");
		}
	}
}