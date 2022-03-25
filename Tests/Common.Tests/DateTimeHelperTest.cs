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
using NUnit.Framework;

namespace Epam.FixAntenna.Common
{
	[TestFixture]
	public class DateTimeHelperTest
	{
		public static object[] TestCases = {
			new object[] { "UTC", true, TimeSpan.Zero, null },
			new object[] { "UTC+7:30", true, new TimeSpan(7, 30, 0), null},
			new object[] { "UTC +03:00", true, new TimeSpan(3, 00, 0), null},
			new object[] { "UTC-10", true, new TimeSpan(-10, 00, 0), null},
			new object[] { "UTC -10", true, new TimeSpan(-10, 00, 0), null},
			new object[] { "GMT-2", true, new TimeSpan(-2, 0, 0), null},
			new object[] { "GMT+04", true, new TimeSpan(4, 0, 0), null},
			new object[] { "GMT", true, new TimeSpan(0, 0, 0), null},
			new object[] { "CST-2", false, TimeSpan.Zero, null},
			new object[] { "GET-0", false, TimeSpan.Zero, null},
			new object[] { "CST-:30", false, TimeSpan.Zero, null},
			new object[] { "OST :30", false, TimeSpan.Zero, null},
			new object[] { "OAT- :30", false, TimeSpan.Zero, null},
			new object[] { "Russian Standard Time", true, new TimeSpan(3, 0, 0), PlatformID.Win32NT},
			new object[] { "Russia Time Zone 11", true, new TimeSpan(12, 0, 0), PlatformID.Win32NT},
			new object[] { "Nepal Standard Time", true, new TimeSpan(5, 45, 0), PlatformID.Win32NT},
			new object[] { "Europe/Moscow", true, new TimeSpan(3, 0, 0), PlatformID.Unix },
			new object[] { "Asia/Anadyr", true, new TimeSpan(12, 0, 0), PlatformID.Unix },
			new object[] { "Asia/Kathmandu", true, new TimeSpan(5, 45, 0), PlatformID.Unix }
		};

		[TestCaseSource(nameof(TestCases))]
		public void TryParseUnixTimeZoneOffsetTest(string timeZoneId, bool good, TimeSpan expected, PlatformID? platform)
		{
			if (platform.HasValue && platform != Environment.OSVersion.Platform)
			{
				return;
			}

			var result = DateTimeHelper.TryParseTimeZoneOffset(timeZoneId, out var offset);
			Assert.That(result, Is.EqualTo(good), $"Wrong behaviour for {timeZoneId} time zone.");
			Assert.That(offset, Is.EqualTo(expected), $"Cannot parse {timeZoneId} time zone.");
		}
	}
}
