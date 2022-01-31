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

using Epam.FixAntenna.NetCore.Common;

namespace Epam.FixAntenna.NetCore.Message.Format
{
	public class FixDateFormatterFactory
	{
		public enum FixDateType
		{
			/// <summary>
			/// Date of Local Market (vs. UTC) in YYYYMMDD format.<br/>
			/// Valid values: YYYY = 0000-9999, MM = 01-12, DD = 01-31.
			/// </summary>
			LocalMktDate,

			/// <summary>
			/// Char field representing month of a year in YYYYMM format.<br/>
			/// Valid values: YYYY = 0000-9999, MM = 01-12.
			/// </summary>
			MonthYearShort,

			/// <summary>
			/// String field representing month of a year in YYYYMMDD format.<br/>
			/// Valid values: YYYY = 0000-9999, MM = 01-12, DD = 01-31.
			/// </summary>
			MonthYearWithDate,

			/// <summary>
			/// String field representing month of a year in YYYYMMWW format.<br/>
			/// Valid values: YYYY = 0000-9999, MM = 01-12, WW = w1, w2, w3, w4, w5.
			/// </summary>
			MonthYearWithWeek,

			/// <summary>
			/// Date in YYYYMMDD format. <br/>
			/// Valid values: YYYY = 0000-9999, MM = 01-12, DD = 01-31.
			/// </summary>
			Date40,

			/// <summary>
			/// Time/date combination in YYYYMMDD-HH:MM:SS format, colons and dash required. <br/>
			/// Valid values: YYYY = 0000-9999, MM = 01-12, DD = 01-31, HH = 00-23, MM = 00-59, SS = 00-59.
			/// </summary>
			Time40,

			/// <summary>
			/// Date represented in UTC (Universal Time Coordinated, also known as "GMT") in YYYYMMDD format. <br/>
			/// Valid values: YYYY = 0000-9999, MM = 01-12, DD = 01-31.
			/// </summary>
			UtcDate,

			/// <summary>
			/// Date represented in UTC (Universal Time Coordinated, also known as "GMT") in YYYYMMDD format. This
			/// special-purpose field is paired with UTCTimeOnly to form a proper UTCTimestamp for bandwidth-sensitive
			/// messages.<br/>
			/// Valid values: YYYY = 0000-9999, MM = 01-12, DD = 01-31.
			/// </summary>
			UtcDateOnly,

			/// <summary>
			/// Time-only represented in UTC (Universal Time Coordinated, also known as "GMT") in HH:MM:SS (whole seconds)
			/// format, colons, and period required. <br/>
			/// Valid values: HH = 00-23, MM = 00-59, SS = 00-5960 (60 only if UTC
			/// leap second) (without milliseconds).
			/// </summary>
			UtcTimeOnlyShort,

			/// <summary>
			/// Time-only represented in UTC (Universal Time Coordinated, also known as "GMT") in HH:MM:SS.sss (milliseconds)
			/// format, colons, and period required. <br/>
			/// Valid values: HH = 00-23, MM = 00-59, SS = 00-5960 (60 only if UTC leap
			/// second), sss=000-999 (indicating milliseconds).
			/// </summary>
			UtcTimeOnlyWithMillis,

			/// <summary>
			/// Time/date combination represented in UTC (Universal Time Coordinated, also known as "GMT")
			/// in YYYYMMDD-HH:MM:SS (whole seconds) format, colons, dash, and period required. Valid values:
			/// YYYY = 0000-9999, MM = 01-12, DD = 01-31, HH = 00-23, MM = 00-59, SS = 00-5960 (60 only if UTC leap second)
			/// (without milliseconds). <br/>
			/// Leap Seconds: Note that UTC includes corrections for leap seconds, which are
			/// inserted to account for slowing of the rotation of the earth. Leap second insertion is declared by the
			/// International Earth Rotation Service (IERS) and has, since 1972, only occurred on the night of Dec. 31
			/// or Jun 30. The IERS considers March 31 and September 30 as secondary dates for leap second insertion,
			/// but has never utilized these dates. During a leap second insertion, a UTCTimestamp field may read
			/// "19981231-23:59:59", "19981231-23:59:60", "19990101-00:00:00".
			/// </summary>
			UtcTimestampShort,

			/// <summary>
			/// Time/date combination represented in UTC (Universal Time Coordinated, also known as "GMT") in
			/// YYYYMMDD-HH:MM:SS.sss (milliseconds) format, colons, dash, and period required. Valid values:
			/// YYYY = 0000-9999, MM = 01-12, DD = 01-31, HH = 00-23, MM = 00-59, SS = 00-5960 (60 only if UTC leap second),
			/// sss=000-999 (indicating milliseconds). Leap Seconds: Note that UTC includes corrections for leap seconds,
			/// which are inserted to account for slowing of the rotation of the earth. Leap second insertion is declared
			/// by the International Earth Rotation Service (IERS) and has, since 1972, only occurred on the night of
			/// Dec. 31 or Jun 30. The IERS considers March 31 and September 30 as secondary dates for leap second
			/// insertion, but has never utilized these dates. During a leap second insertion, a UTCTimestamp field may
			/// read "19981231-23:59:59", "19981231-23:59:60", "19990101-00:00:00".
			/// </summary>
			UtcTimestampWithMillis,

			/// <summary>
			/// The time represented based on ISO 8601. This is the time with a UTC offset to allow identification of local
			/// time and timezone of that time. Format is HH:MM[Z | [ + | - hh[:mm]]] where HH = 00-23 hours,
			/// MM = 00-59 minutes, hh = 01-12 offset hours, mm = 00-59 offset minutes. <br/>
			/// Example: 07:39Z is 07:39 UTC <br/>
			/// Example: 02:39-05 is five hours behind UTC, thus Eastern Time <br/>
			/// Example: 15:39+08 is eight hours ahead of UTC, Hong Kong/Singapore time <br/>
			/// Example: 13:09+05:30 is 5.5 hours ahead of UTC, India time
			/// </summary>
			TzTimeOnlyShort,

			/// <summary>
			/// The time represented based on ISO 8601. This is the time with a UTC offset to allow identification of local
			/// time and timezone of that time. Format is HH:MM:SS[Z | [ + | - hh[:mm]]] where HH = 00-23 hours,
			/// MM = 00-59 minutes, SS = 00-59 seconds, sss = milliseconds, hh = 01-12 offset hours, mm = 00-59 offset
			/// minutes. <br/>
			/// Example: 07:39Z is 07:39 UTC <br/>
			/// Example: 02:39-05 is five hours behind UTC, thus Eastern Time <br/>
			/// Example: 15:39+08 is eight hours ahead of UTC, Hong Kong/Singapore time <br/>
			/// Example: 13:09+05:30 is 5.5 hours ahead of UTC, India time
			/// </summary>
			TzTimeOnlySec,

			/// <summary>
			/// The time represented based on ISO 8601. This is the time with a UTC offset to allow identification of local
			/// time and timezone of that time. Format is HH:MM:SS.sss[Z | [ + | - hh[:mm]]] where HH = 00-23 hours,
			/// MM = 00-59 minutes, SS = 00-59 seconds, hh = 01-12 offset hours, mm = 00-59 offset minutes. <br/>
			/// Example: 07:39Z is 07:39 UTC <br/>
			/// Example: 02:39-05 is five hours behind UTC, thus Eastern Time <br/>
			/// Example: 15:39+08 is eight hours ahead of UTC, Hong Kong/Singapore time <br/>
			/// Example: 13:09+05:30 is 5.5 hours ahead of UTC, India time
			/// </summary>
			TzTimeOnlyMillis,

			/// <summary>
			/// The time/date combination representing local time with an offset to UTC to allow identification of local
			/// time and timezone offset of that time. The representation is based on ISO 8601. Format is
			/// YYYYMMDD-HH:MM:SS[Z | [ + | - hh[:mm]]] where YYYY = 0000 to 9999, MM = 01-12, DD = 01-31,
			/// HH = 00-23 hours, MM = 00-59 minutes, SS = 00-59 seconds, hh = 01-12 offset hours, mm = 00-59 offset minutes <br/>
			/// Example: 20060901-07:39Z is 07:39 UTC on 1st of September 2006 <br/>
			/// Example: 20060901-02:39-05 is five hours behind UTC, thus Eastern Time on 1st of September 2006 <br/>
			/// Example: 20060901-15:39+08 is eight hours ahead of UTC, Hong Kong/Singapore time on 1st of September 2006 <br/>
			/// Example: 20060901-13:09+05:30 is 5.5 hours ahead of UTC, India time on 1st of September 2006
			/// </summary>
			TzTimestamp,

			/// <summary>
			/// The time/date combination representing local time with an offset to UTC to allow identification of local
			/// time and timezone offset of that time. The representation is based on ISO 8601. Format is
			/// YYYYMMDD-HH:MM:SS.sss[Z | [ + | - hh[:mm]]] where YYYY = 0000 to 9999, MM = 01-12, DD = 01-31,
			/// HH = 00-23 hours, MM = 00-59 minutes, SS = 00-59 seconds, sss = milliseconds, hh = 01-12 offset hours,
			/// mm = 00-59 offset minutes <br/>
			/// Example: 20060901-07:39Z is 07:39 UTC on 1st of September 2006 <br/>
			/// Example: 20060901-02:39-05 is five hours behind UTC, thus Eastern Time on 1st of September 2006 <br/>
			/// Example: 20060901-15:39+08 is eight hours ahead of UTC, Hong Kong/Singapore time on 1st of September 2006 <br/>
			/// Example: 20060901-13:09+05:30 is 5.5 hours ahead of UTC, India time on 1st of September 2006
			/// </summary>
			TzTimestampMillis
		}

		private static readonly LocalMktDateFormatter LocalMktDateFormatter = new LocalMktDateFormatter();
		private static readonly MonthYearFormatter MonthYearShortFormatter = new MonthYearFormatter();
		private static readonly UtcDateFormatter UtcDateFormatter = new UtcDateFormatter();
		private static readonly MonthYearWithWeekFormatter MonthYearWeekFormatter = new MonthYearWithWeekFormatter();
		private static readonly UtcTimestampFormatter UtcTimestampFormatter = new UtcTimestampFormatter();
		private static readonly UtcTimeOnlyFormatter UtcTimeOnlyFormatter = new UtcTimeOnlyFormatter();

		private static readonly UtcTimeOnlyWithMillisFormatter UtcTimeOnlyWithMillisFormatter =
			new UtcTimeOnlyWithMillisFormatter();

		private static readonly UtcTimestampWithMillisFormatter UtcTimestampWithMillisFormatter =
			new UtcTimestampWithMillisFormatter();

		private static readonly TzTimeFormatter TzTimeFormatter = new TzTimeFormatter();
		private static readonly TzTimeSecondsFormatter TzTimeSecondsFormatter = new TzTimeSecondsFormatter();
		private static readonly TzTimeMillisFormatter TzTimeMillisFormatter = new TzTimeMillisFormatter();
		private static readonly TzTimestampFormatter TzTimestampFormatter = new TzTimestampFormatter();

		private static readonly TzTimestampMillisFormatter
			TzTimestampMillisFormatter = new TzTimestampMillisFormatter();

		public static IFixDateFormatter GetFixDateFormatter(FixDateType type)
		{
			switch (type)
			{
				case FixDateType.LocalMktDate:
					return LocalMktDateFormatter;
				case FixDateType.MonthYearWithDate:
				case FixDateType.Date40:
				case FixDateType.UtcDate:
				case FixDateType.UtcDateOnly:
					return UtcDateFormatter;
				case FixDateType.MonthYearShort:
					return MonthYearShortFormatter;
				case FixDateType.MonthYearWithWeek:
					return MonthYearWeekFormatter;
				case FixDateType.Time40:
				case FixDateType.UtcTimestampShort:
					return UtcTimestampFormatter;
				case FixDateType.UtcTimeOnlyShort:
					return UtcTimeOnlyFormatter;
				case FixDateType.UtcTimeOnlyWithMillis:
					return UtcTimeOnlyWithMillisFormatter;
				case FixDateType.UtcTimestampWithMillis:
					return UtcTimestampWithMillisFormatter;
				case FixDateType.TzTimeOnlyShort:
					return TzTimeFormatter;
				case FixDateType.TzTimeOnlySec:
					return TzTimeSecondsFormatter;
				case FixDateType.TzTimeOnlyMillis:
					return TzTimeMillisFormatter;
				case FixDateType.TzTimestamp:
					return TzTimestampFormatter;
				case FixDateType.TzTimestampMillis:
					return TzTimestampMillisFormatter;
			}

			return null;
		}

		public static IFixDateFormatter GetSendingTimeFormatter(FixVersion fixVersion)
		{
			return fixVersion == FixVersion.Fix40 ? UtcTimestampFormatter : UtcTimestampWithMillisFormatter;
		}
	}
}