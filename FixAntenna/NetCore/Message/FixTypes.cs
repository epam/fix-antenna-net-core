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
using System.Globalization;
using System.Text;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message.Format;

namespace Epam.FixAntenna.NetCore.Message
{
	/// <summary>
	/// FIX Types helper class.
	/// Provides ability for parsing values from buffer of bytes.
	/// </summary>
	internal sealed class FixTypes
	{
		private const long MaxMantissaThreshold = 10000000000000000L;
		private const int MaxExponent = 300;
		private const int MinExponent = -299;
		public const int MaxPrecision = 16;
		private static readonly byte[] True = { (byte)'Y' };
		private static readonly byte[] False = { (byte)'N' };

		private static readonly int[] MonthLength = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
		private static readonly int[] LeapMonthLength = { 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

		internal static readonly NumberFormatInfo UsFormatSymbols = CultureInfo.GetCultureInfo("en-US").NumberFormat;

		internal static readonly string[] DecimalPatterns =
		{
			"#.", "#.#", "#.##", "#.###", "#.####", "#.#####", "#.######", "#.#######",
			"#.########", "#.#########", "#.##########", "#.###########", "#.############", "#.#############",
			"#.##############", "#.###############", "#.################"
		};

		private static readonly long MaxIntBound = long.MaxValue / 10;

		private FixTypes()
		{
		}

		/// <summary>
		/// Parses the float value from <see cref="TagValue"/>.
		/// </summary>
		/// <param name="tag"></param>
		/// <exception cref="ArgumentException"> </exception>
		public static double ParseFloat(TagValue tag)
		{
			return ParseFloat(tag.Buffer, tag.Offset, tag.Length);
		}

		/// <summary>
		/// Parses the float value from bytes.
		/// </summary>
		/// <param name="value"> the buffer of bytes </param>
		/// <exception cref="ArgumentException"> </exception>
		public static double ParseFloat(byte[] value)
		{
			return ParseFloat(value, 0, value.Length);
		}

		/// <summary>
		/// Garbage-free method for converting string to double
		/// </summary>
		// TBD!  Add unit tests !
		public static double ParseFloat(byte[] buffer, int offset, int count)
		{
			return DoubleFormatter.ParseDouble(buffer, offset, count);
		}

		/// <summary>
		/// Formats the <c>double</c> value.
		/// </summary>
		/// <param name="value"> the value </param>
		public static byte[] FormatDouble(double value)
		{
			var floatStr = value.ToString(DecimalPatterns[MaxPrecision], UsFormatSymbols).ToCharArray();
			var floatLen = floatStr.Length;
			var floatBlock = new byte[floatLen];
			for (var i = 0; i < floatLen; i++)
			{
				floatBlock[i] = (byte)floatStr[i];
			}

			return floatBlock;
		}

		/// <summary>
		/// Returns false, if <c>tag</c> contains the invalid float value.
		/// </summary>
		/// <param name="tag"><see cref="TagValue"/> tag.</param>
		/// <returns></returns>
		public static bool IsInvalidFloat(TagValue tag)
		{
			return IsInvalidFloat(tag.Buffer, tag.Offset, tag.Length);
		}

		/// <summary>
		/// Returns false, if <c>buffer</c> contains the invalid float value.
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		public static bool IsInvalidFloat(byte[] buffer)
		{
			return IsInvalidFloat(buffer, 0, buffer.Length);
		}

		/// <summary>
		/// Returns true, if subarray contains the invalid float value.
		/// <p/>
		/// The <c>offset</c> argument is the
		/// index of the first byte of the subarray and the <c>count</c>
		/// argument specifies the length of the subarray.
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		/// <param name="offset"> the initial offset </param>
		/// <param name="count">  the length </param>
		public static bool IsInvalidFloat(byte[] buffer, int offset, int count)
		{
			var limit = offset + count;
			if (limit > offset && buffer[offset] == (byte)'-')
			{
				offset++;
			}

			if (limit == offset)
			{
				return true;
			}

			var mantissa = 0L;
			var expCorr = 0;
			var isDecPointDetected = false;
			while (offset < limit)
			{
				var ch = buffer[offset++];
				if (ch == (byte)'.')
				{
					if (isDecPointDetected)
					{
						return true;
					}

					isDecPointDetected = true;
				}
				else if (ch < (byte)'0' || ch > (byte)'9')
				{
					return true;
				}
				else
				{
					if (mantissa < MaxMantissaThreshold)
					{
						mantissa = mantissa * 10L + (ch - '0');
						if (isDecPointDetected)
						{
							expCorr--;
						}
					}
					else
					{
						if (!isDecPointDetected)
						{
							expCorr++;
						}
					}
				}
			}

			return expCorr > MaxExponent || expCorr < MinExponent;
		}

		/// <summary>
		/// Parses the <c>boolean</c> value from subarray.
		/// <p/>
		/// The <c>offset</c> argument is the
		/// index of the first byte of the subarray and the <c>count</c>
		/// argument specifies the length of the subarray.
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		/// <param name="offset"> the initial offset </param>
		/// <param name="count">  the length </param>
		/// <exception cref="ArgumentException"> </exception>
		public static bool ParseBoolean(byte[] buffer, int offset, int count)
		{
			if (IsInvalidBoolean(buffer, offset, count))
			{
				throw new ArgumentException("Boolean field is 1 char 'Y' or 'N'");
			}

			return buffer[offset] == True[0];
		}

		/// <summary>
		/// Parses the <c>boolean</c> value from bytes.
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		/// <exception cref="ArgumentException"> </exception>
		public static bool ParseBoolean(byte[] buffer)
		{
			return ParseBoolean(buffer, 0, buffer.Length);
		}

		/// <summary>
		/// Formats the boolean <c>value</c>.
		/// </summary>
		/// <param name="value"> the value </param>
		public static byte[] FormatBoolean(bool value)
		{
			return value ? True : False;
		}

		/// <summary>
		/// Returns true, if subarray contains the invalid <c>boolean</c> value.
		/// <p/>
		/// The <c>offset</c> argument is the
		/// index of the first byte of the subarray and the <c>count</c>
		/// argument specifies the length of the subarray.
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		/// <param name="offset"> the initial offset </param>
		/// <param name="count">  the length </param>
		public static bool IsInvalidBoolean(byte[] buffer, int offset, int count)
		{
			return count != 1 || buffer[offset] != True[0] && buffer[offset] != False[0];
		}

		/// <summary>
		/// Returns true, if <c>buffer</c> contains the invalid <c>boolean</c> value.
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		public static bool IsInvalidBoolean(byte[] buffer)
		{
			return IsInvalidBoolean(buffer, 0, buffer.Length);
		}

		/// <summary>
		/// Returns true, if <c>tag</c> contains the invalid <c>boolean</c> value.
		/// </summary>
		/// <param name="tag"><see cref="TagValue"/> tag.</param>
		/// <returns></returns>
		public static bool IsInvalidBoolean(TagValue tag)
		{
			return IsInvalidBoolean(tag.Buffer, tag.Offset, tag.Length);
		}

		internal static int ParseNumberPart(byte[] block, int off, int limit)
		{
			var res = 0;
			while (off < limit)
			{
				var ch = block[off++];
				if (ch >= (byte)'0' && ch <= (byte)'9')
				{
					res = res * 10 + ch - '0';
				}
				else
				{
					throw new ArgumentException();
				}
			}

			return res;
		}

		public static bool IsInvalidTimestamp(TagValue tag)
		{
			return IsInvalidTimestamp(tag.Buffer, tag.Offset, tag.Length);
		}

		public static bool IsInvalidTimestamp(byte[] buffer)
		{
			return IsInvalidTimestamp(buffer, 0, buffer.Length);
		}

		public static bool IsInvalidTimestamp40(TagValue tag)
		{
			return IsInvalidTimestamp40(tag.Buffer, tag.Offset, tag.Length);
		}

		public static bool IsInvalidTimestamp40(byte[] buf, int off, int len)
		{
			return len != 17 || IsInvalidTimestamp(buf, off, len);
		}

		/// <summary>
		/// Returns true, if subarray contains the invalid <c>Timestamp</c> value.
		/// <p/>
		/// The <c>offset</c> argument is the
		/// index of the first byte of the subarray and the <c>count</c>
		/// argument specifies the length of the subarray.
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		/// <param name="offset"> the initial offset </param>
		/// <param name="count">  the length </param>
		public static bool IsInvalidTimestamp(byte[] buffer, int offset, int count)
		{
			try
			{
				if (count != 17 && count != 21 && count != 24 && count != 27)
				{
					return true;
				}

				if (buffer[offset + 8] != (byte)'-' || buffer[offset + 11] != (byte)':' ||
					buffer[offset + 14] != (byte)':')
				{
					return true;
				}

				var year = ParseNumberPart(buffer, offset, offset + 4);
				if (year < 1583)
				{
					return true;
				}

				var month = ParseNumberPart(buffer, offset + 4, offset + 6) - 1;
				if (month < 0 || month > 11)
				{
					return true;
				}

				var date = ParseNumberPart(buffer, offset + 6, offset + 8);
				if (date < 1 || date > (year % 4 == 0 && (year % 100 != 0 || year % 400 == 0)
					? LeapMonthLength[month]
					: MonthLength[month]))
				{
					return true;
				}

				return isInvalidTimeOnly(buffer, 9, buffer.Length - 9);
			}
			catch (Exception)
			{
				return true;
			}
		}

		/// <summary>
		/// Returns true, if TagValue contains the invalid <c>TZTimestamp</c> value.
		/// </summary>
		/// <param name="tag"> TagValue tag. </param>
		public static bool IsInvalidTzTimestamp(TagValue tag)
		{
			return IsInvalidTzTimestamp(tag.Buffer, tag.Offset, tag.Length);
		}

		/// <summary>
		/// Returns true, if subarray contains the invalid <c>TZTimestamp</c> value.
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		public static bool IsInvalidTzTimestamp(byte[] buffer)
		{
			return IsInvalidTzTimestamp(buffer, 0, buffer.Length);
		}

		/// <summary>
		/// Returns true, if subarray contains the invalid <c>TZTimestamp</c> value.
		/// <p/>
		/// The <c>offset</c> argument is the
		/// index of the first byte of the subarray and the <c>count</c>
		/// argument specifies the length of the subarray.
		/// <p/>
		/// The format for <c>TZTimestamp</c> is YYYYMMDD-HH:MM[:SS[.sss][sss][sss]][Z | [ + | - hh[:mm]]].
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		/// <param name="offset"> the initial offset </param>
		/// <param name="count">  the length </param>
		public static bool IsInvalidTzTimestamp(byte[] buffer, int offset, int count)
		{
			// Example:  20060901-07:39Z is 07:39 UTC on 1st of September 2006
			// Example:  20060901-07:39:30.001234Z is 07:39 UTC on 1st of September 2006
			// Example:  20060901-02:39-05 is five hours behind UTC, thus Eastern Time on 1st of September 2006
			// Example:  20060901-15:39+08 is eight hours ahead of UTC, Hong Kong/Singapore time on 1st of September 2006
			// Example:  20060901-15:39:30.123456789+08 is eight hours ahead of UTC, Hong Kong/Singapore time on 1st of September 2006
			// Example:  20060901-13:09+05:30 is 5.5 hours ahead of UTC, India time on 1st of September 2006
			// Example:  20060901-13:09:30+05:30 is 5.5 hours ahead of UTC, India time on 1st of September 2006
			// Example:  20060901-13:09:30.333+05:30 is 5.5 hours ahead of UTC, India time on 1st of September 2006
			if (count != 15 && count != 17 && count != 18 && count != 20 && count != 22 && count != 23 && count != 24 &&
				count != 25 && count != 27 && count != 28 && count != 30 && count != 33)
			{
				return true;
			}

			if (buffer[offset + 8] != (byte)'-' || buffer[offset + 11] != (byte)':')
			{
				return true;
			}

			var year = ParseNumberPart(buffer, offset, offset + 4);
			if (year < 1583)
			{
				return true;
			}

			var month = ParseNumberPart(buffer, offset + 4, offset + 6) - 1;
			if (month < 0 || month > 11)
			{
				return true;
			}

			var date = ParseNumberPart(buffer, offset + 6, offset + 8);
			if (date < 1 || date > (year % 4 == 0 && (year % 100 != 0 || year % 400 == 0)
				? LeapMonthLength[month]
				: MonthLength[month]))
			{
				return true;
			}

			return isInvalidTZTimeOnly(buffer, 9, buffer.Length - 9);
		}

		/// <summary>
		/// Returns true, if <c>tag</c> contains the invalid <c>TimeOnly</c> value.
		/// <p/>
		/// The format for <c>TimeOnly</c> is HH:MM:SS[.sss][sss][sss]
		/// </summary>
		/// <param name="tag"> the buffer of bytes </param>
		public static bool isInvalidTimeOnly(TagValue tag)
		{
			return isInvalidTimeOnly(tag.Buffer, tag.Offset, tag.Length);
		}

		/// <summary>
		/// Returns true, if <c>buffer</c> contains the invalid <c>TimeOnly</c> value.
		/// <p/>
		/// The format for <c>TimeOnly</c> is HH:MM:SS[.sss][sss][sss]
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		public static bool isInvalidTimeOnly(byte[] buffer)
		{
			return isInvalidTimeOnly(buffer, 0, buffer.Length);
		}

		/// <summary>
		/// Returns true, if subarray contains the invalid <c>TimeOnly</c> value.
		/// <p/>
		/// The <c>offset</c> argument is the
		/// index of the first byte of the subarray and the <c>count</c>
		/// argument specifies the length of the subarray.
		/// <p/>
		/// The format for <c>TimeOnly</c> is HH:MM:SS[.sss]
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		/// <param name="offset"> the initial offset </param>
		/// <param name="count">  the length </param>
		public static bool isInvalidTimeOnly(byte[] buffer, int offset, int count)
		{
			try
			{
				if (count != 8 && count != 12 && count != 15 && count != 18)
				{
					return true;
				}

				if (buffer[offset + 2] != (byte)':' || buffer[offset + 5] != (byte)':')
				{
					return true;
				}

				var hour = ParseNumberPart(buffer, offset, offset + 2);
				if (hour < 0 || hour > 23)
				{
					return true;
				}

				var minute = ParseNumberPart(buffer, offset + 3, offset + 5);
				if (minute < 0 || minute > 59)
				{
					return true;
				}

				var second = ParseNumberPart(buffer, offset + 6, offset + 8);
				if (second < 0 || second > 60)
				{
					return true;
				}

				if (count >= 12)
				{
					if (buffer[offset + 8] != (byte)'.')
					{
						return true;
					}

					switch (count)
					{
						case 12:
							var millis = ParseNumberPart(buffer, offset + 9, offset + 12);
							if (millis < 0 || millis > 999)
							{
								return true;
							}

							break;
						case 15:
							var micros = ParseNumberPart(buffer, offset + 9, offset + 15);
							if (micros < 0 || micros > 999999)
							{
								return true;
							}

							break;
						case 18:
							var nanos = ParseNumberPart(buffer, offset + 9, offset + 18);
							if (nanos < 0 || nanos > 999999999)
							{
								return true;
							}

							break;
					}
				}
			}
			catch (Exception)
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Returns true, if subarray contains the invalid <c>TZTimeOnly</c> value.
		/// <p/>
		/// The format for <c>TZTimeOnly</c> is HH:MM[:SS][.sss][Z | [ + | - hh[:mm]]].
		/// </summary>
		/// <param name="tag"> the buffer of bytes </param>
		public static bool isInvalidTZTimeOnly(TagValue tag)
		{
			return isInvalidTZTimeOnly(tag.Buffer, tag.Offset, tag.Length);
		}

		/// <summary>
		/// Returns true, if subarray contains the invalid <c>TZTimeOnly</c> value.
		/// <p/>
		/// The format for <c>TZTimeOnly</c> is HH:MM[:SS][.sss][Z | [ + | - hh[:mm]]].
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		public static bool isInvalidTZTimeOnly(byte[] buffer)
		{
			return isInvalidTZTimeOnly(buffer, 0, buffer.Length);
		}

		/// <summary>
		/// Returns true, if subarray contains the invalid <c>TZTimeOnly</c> value.
		/// <p/>
		/// The <c>offset</c> argument is the
		/// index of the first byte of the subarray and the <c>count</c>
		/// argument specifies the length of the subarray.
		/// <p/>
		/// The format for <c>TZTimeOnly</c> is HH:MM[:SS][.sss][sss][sss][Z | [ + | - hh[:mm]]].
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		/// <param name="offset"> the initial offset </param>
		/// <param name="count">  the length </param>
		public static bool isInvalidTZTimeOnly(byte[] buffer, int offset, int count)
		{
			//    Example: 07:39Z is 07:39 UTC
			//    Example: 02:39-05 is five hours behind UTC, thus Eastern Time
			//    Example: 15:39+08 is eight hours ahead of UTC, Hong Kong/Singapore time
			//    Example: 13:09+05:30 is 5.5 hours ahead of UTC, India time
			try
			{
				if (count != 6 && count != 8 && count != 9 && count != 11 && count != 13 && count != 14 &&
					count != 15 && count != 16 && count != 18 && count != 19 && count != 21 && count != 24)
				{
					return true;
				}

				if (buffer[offset + 2] != (byte)':')
				{
					return true;
				}

				// hours
				var hour = ParseNumberPart(buffer, offset, offset + 2);
				if (hour < 0 || hour > 23)
				{
					return true;
				}

				// minute
				var minute = ParseNumberPart(buffer, offset + 3, offset + 5);
				if (minute < 0 || minute > 59)
				{
					return true;
				}

				if (buffer[offset + 5] != (byte)':')
				{
					// only minutes
					switch (count)
					{
						case 6:
						{
							//07:39Z
							if (buffer[offset + 5] != (byte)'Z')
							{
								return true;
							}
						}
							break;
						case 8:
						{
							//13:09+05
							if (!(buffer[offset + 5] == (byte)'+' || buffer[offset + 5] == (byte)'-'))
							{
								return true;
							}

							var tzHour = ParseNumberPart(buffer, offset + 6, offset + 8);
							if (tzHour < 1 || tzHour > 12)
							{
								return true;
							}
						}
							break;
						case 11:
						{
							//07:39+02:31
							if (!(buffer[offset + 5] == (byte)'+' || buffer[offset + 5] == (byte)'-'))
							{
								return true;
							}

							var tzHour = ParseNumberPart(buffer, offset + 6, offset + 8);
							if (tzHour < 1 || tzHour > 12)
							{
								return true;
							}

							if (buffer[offset + 8] != (byte)':')
							{
								return true;
							}

							var tzMinutes = ParseNumberPart(buffer, offset + 9, offset + 11);
							if (tzMinutes < 0 || tzMinutes > 59)
							{
								return true;
							}
						}
							break;
						default:
							return true;
					}
				}
				else
				{
					//as minimum seconds are present
					var second = ParseNumberPart(buffer, offset + 6, offset + 8);
					if (second < 0 || second > 60)
					{
						return true;
					}

					switch (count)
					{
						case 9:
						{
							//13:09:30Z
							if (buffer[offset + 8] != (byte)'Z')
							{
								return true;
							}
						}
							break;
						case 11:
						{
							//07:39:30+02
							if (!(buffer[offset + 8] == (byte)'+' || buffer[offset + 8] == (byte)'-'))
							{
								return true;
							}

							var tzHour = ParseNumberPart(buffer, offset + 9, offset + 11);
							if (tzHour < 1 || tzHour > 12)
							{
								return true;
							}
						}
							break;
						case 13:
						{
							//07:39:30.301Z
							if (buffer[offset + 8] != (byte)'.')
							{
								return true;
							}

							var mills = ParseNumberPart(buffer, offset + 9, offset + 12);
							if (mills < 0 || mills > 999)
							{
								return true;
							}

							if (buffer[offset + 12] != (byte)'Z')
							{
								return true;
							}
						}
							break;
						case 14:
						{
							//07:39:33+02:31
							if (!(buffer[offset + 8] == (byte)'+' || buffer[offset + 8] == (byte)'-'))
							{
								return true;
							}

							var tzHour = ParseNumberPart(buffer, offset + 9, offset + 11);
							if (tzHour < 1 || tzHour > 12)
							{
								return true;
							}

							if (buffer[offset + 11] != (byte)':')
							{
								return true;
							}

							var tzMinutes = ParseNumberPart(buffer, offset + 12, offset + 14);
							if (tzMinutes < 0 || tzMinutes > 59)
							{
								return true;
							}
						}
							break;
						case 15:
						{
							//07:39:54.101+02
							if (buffer[offset + 8] != (byte)'.')
							{
								return true;
							}

							var mills = ParseNumberPart(buffer, offset + 9, offset + 12);
							if (mills < 0 || mills > 999)
							{
								return true;
							}

							if (!(buffer[offset + 12] == (byte)'+' || buffer[offset + 12] == (byte)'-'))
							{
								return true;
							}

							var tzHour = ParseNumberPart(buffer, offset + 13, offset + 15);
							if (tzHour < 1 || tzHour > 12)
							{
								return true;
							}
						}
							break;
						case 16:
						{
							//07:39:30.301301Z
							if (buffer[offset + 8] != (byte)'.')
							{
								return true;
							}

							var micros = ParseNumberPart(buffer, offset + 9, offset + 15);
							if (micros < 0 || micros > 999999)
							{
								return true;
							}

							if (buffer[offset + 15] != (byte)'Z')
							{
								return true;
							}
						}
							break;
						case 18:
						{
							//07:39:54.101+02:17
							//07:39:54.101101-02
							if (buffer[offset + 8] != (byte)'.')
							{
								return true;
							}

							if (buffer[offset + 15] == (byte)':')
							{
								//07:39:54.101+02:17
								var mills = ParseNumberPart(buffer, offset + 9, offset + 12);
								if (mills < 0 || mills > 999)
								{
									return true;
								}

								if (!(buffer[offset + 12] == (byte)'+' || buffer[offset + 12] == (byte)'-'))
								{
									return true;
								}

								var tzHour = ParseNumberPart(buffer, offset + 13, offset + 15);
								if (tzHour < 1 || tzHour > 12)
								{
									return true;
								}

								var tzMinutes = ParseNumberPart(buffer, offset + 16, offset + 18);
								if (tzMinutes < 0 || tzMinutes > 59)
								{
									return true;
								}
							}
							else if (buffer[offset + 15] == (byte)'+' || buffer[offset + 15] == (byte)'-')
							{
								//07:39:54.101101-02
								var micros = ParseNumberPart(buffer, offset + 9, offset + 15);
								if (micros < 0 || micros > 999999)
								{
									return true;
								}

								var tzHour = ParseNumberPart(buffer, offset + 16, offset + 18);
								if (tzHour < 1 || tzHour > 12)
								{
									return true;
								}
							}
							else
							{
								return true;
							}
						}
							break;
						case 19:
						{
							//07:39:30.301301301Z
							if (buffer[offset + 8] != (byte)'.')
							{
								return true;
							}

							var nanos = ParseNumberPart(buffer, offset + 9, offset + 18);
							if (nanos < 0 || nanos > 999999999)
							{
								return true;
							}

							if (buffer[offset + 18] != (byte)'Z')
							{
								return true;
							}
						}
							break;
						case 21:
						{
							//07:39:54.101101+02:17
							//07:39:54.101101101-02
							if (buffer[offset + 8] != (byte)'.')
							{
								return true;
							}

							if (buffer[offset + 18] == (byte)':')
							{
								//07:39:54.101101+02:17
								var micros = ParseNumberPart(buffer, offset + 9, offset + 15);
								if (micros < 0 || micros > 999999)
								{
									return true;
								}

								if (!(buffer[offset + 15] == (byte)'+' || buffer[offset + 15] == (byte)'-'))
								{
									return true;
								}

								var tzHour = ParseNumberPart(buffer, offset + 16, offset + 18);
								if (tzHour < 1 || tzHour > 12)
								{
									return true;
								}

								var tzMinutes = ParseNumberPart(buffer, offset + 19, offset + 21);
								if (tzMinutes < 0 || tzMinutes > 59)
								{
									return true;
								}
							}
							else if (buffer[offset + 18] == (byte)'+' || buffer[offset + 18] == (byte)'-')
							{
								//07:39:54.101101101-02
								var nanos = ParseNumberPart(buffer, offset + 9, offset + 18);
								if (nanos < 0 || nanos > 999999999)
								{
									return true;
								}

								var tzHour = ParseNumberPart(buffer, offset + 19, offset + 21);
								if (tzHour < 1 || tzHour > 12)
								{
									return true;
								}
							}
							else
							{
								return true;
							}
						}
							break;
						case 24:
						{
							//07:39:54.101101101-02:17
							if (buffer[offset + 8] != (byte)'.')
							{
								return true;
							}

							var nanos = ParseNumberPart(buffer, offset + 9, offset + 18);
							if (nanos < 0 || nanos > 999999999)
							{
								return true;
							}

							if (!(buffer[offset + 18] == (byte)'+' || buffer[offset + 18] == (byte)'-'))
							{
								return true;
							}

							var tzHour = ParseNumberPart(buffer, offset + 19, offset + 21);
							if (tzHour < 1 || tzHour > 12)
							{
								return true;
							}

							if (buffer[offset + 21] != (byte)':')
							{
								return true;
							}

							var tzMinutes = ParseNumberPart(buffer, offset + 22, offset + 24);
							if (tzMinutes < 0 || tzMinutes > 59)
							{
								return true;
							}
						}
							break;
					}
				}
			}
			catch (Exception)
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Returns true, if subarray contains the invalid <c>Date</c> value.
		/// <p/>
		/// The format for <c>Date</c> is yyyymmdd.
		/// </summary>
		/// <param name="tag"> the buffer of bytes </param>
		public static bool IsInvalidDate(TagValue tag)
		{
			return IsInvalidDate(tag.Buffer, tag.Offset, tag.Length);
		}

		/// <summary>
		/// Returns true, if subarray contains the invalid <c>Date</c> value.
		/// <p/>
		/// The format for <c>Date</c> is yyyymmdd.
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		public static bool IsInvalidDate(byte[] buffer)
		{
			return IsInvalidDate(buffer, 0, buffer.Length);
		}

		/// <summary>
		/// Returns true, if subarray contains the invalid <c>Date</c> value.
		/// <p/>
		/// The <c>offset</c> argument is the
		/// index of the first byte of the subarray and the <c>count</c>
		/// argument specifies the length of the subarray.
		/// <p/>
		/// The format for <c>Date</c> is yyyymmdd.
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		/// <param name="offset"> the initial offset </param>
		/// <param name="count">  the length </param>
		public static bool IsInvalidDate(byte[] buffer, int offset, int count)
		{
			try
			{
				if (count != 8)
				{
					return true;
				}

				var year = ParseNumberPart(buffer, offset, offset + 4);

				var month = ParseNumberPart(buffer, offset + 4, offset + 6) - 1;
				if (month < 0 || month > 11)
				{
					return true;
				}

				var date = ParseNumberPart(buffer, offset + 6, offset + 8);
				if (date < 1 || date > (year % 4 == 0 && (year % 100 != 0 || year % 400 == 0)
					? LeapMonthLength[month]
					: MonthLength[month]))
				{
					return true;
				}
			}
			catch (Exception)
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Returns true, if <c>tag</c> contains the invalid <c>MonthYear</c> value.
		/// </summary>
		/// <p/>
		/// The format for <c>MonthYear</c> is YYYYMM.
		/// <param name="tag"><see cref="TagValue"/> tag.</param>
		/// <returns></returns>
		public static bool IsInvalidMonthYear(TagValue tag)
		{
			return IsInvalidMonthYear(tag.Buffer, tag.Offset, tag.Length);
		}

		/// <summary>
		/// Returns true, if subarray contains the invalid <c>MonthYear</c> value.
		/// <p/>
		/// The format for <c>MonthYear</c> is YYYYMM.
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		public static bool IsInvalidMonthYear(byte[] buffer)
		{
			return IsInvalidMonthYear(buffer, 0, buffer.Length);
		}

		/// <summary>
		/// Returns true, if subarray contains the invalid <c>MonthYear</c> value.
		/// <p/>
		/// The <c>offset</c> argument is the
		/// index of the first byte of the subarray and the <c>count</c>
		/// argument specifies the length of the subarray.
		/// <p/>
		/// The format for <c>MonthYear</c> is YYYYMM.
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		/// <param name="offset"> the initial offset </param>
		/// <param name="count">  the length </param>
		public static bool IsInvalidMonthYear(byte[] buffer, int offset, int count)
		{
			// YYYYMM (i.e. 199903)
			try
			{
				if (count != 6)
				{
					return true;
				}

				var month = ParseNumberPart(buffer, offset + 4, offset + 6) - 1;
				if (month < 0 || month > 11)
				{
					return true;
				}
			}
			catch (Exception)
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Returns true, if TagValue contains the invalid <c>MonthYear44</c> value.
		/// </summary>
		/// <param name="tag"></param>
		/// <param name=""></param>
		/// <returns></returns>
		public static bool IsInvalidMonthYear44(TagValue tag)
		{
			return IsInvalidMonthYear44(tag.Buffer, tag.Offset, tag.Length);
		}

		/// <summary>
		/// Returns true, if subarray contains the invalid <c>MonthYear44</c> value.
		/// <p/>
		/// The format for <c>MonthYear</c> are YYYYMM,YYYYMMDD and YYYYMMWW.
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		public static bool IsInvalidMonthYear44(byte[] buffer)
		{
			return IsInvalidMonthYear44(buffer, 0, buffer.Length);
		}

		/// <summary>
		/// Returns true, if subarray contains the invalid <c>MonthYear44</c> value.
		/// <p/>
		/// The <c>offset</c> argument is the
		/// index of the first byte of the subarray and the <c>count</c>
		/// argument specifies the length of the subarray.
		/// <p/>
		/// The format for <c>MonthYear</c> are YYYYMM,YYYYMMDD and YYYYMMWW.
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		/// <param name="offset"> the initial offset </param>
		/// <param name="count">  the length </param>
		public static bool IsInvalidMonthYear44(byte[] buffer, int offset, int count)
		{
			try
			{
				if (count != 6 && count != 8)
				{
					return true;
				}

				var year = ParseNumberPart(buffer, offset, offset + 4);
				var month = ParseNumberPart(buffer, offset + 4, offset + 6) - 1;
				if (month < 0 || month > 11)
				{
					return true;
				}

				if (count == 8)
				{
					if (buffer[offset + 6] == (byte)'w')
					{
						var week = buffer[offset + 7] - '0';
						if (week < 1 || week > 5)
						{
							return true;
						}
					}
					else
					{
						var date = ParseNumberPart(buffer, offset + 6, offset + 8);
						if (date < 1 || date > (year % 4 == 0 && (year % 100 != 0 || year % 400 == 0)
							? LeapMonthLength[month]
							: MonthLength[month]))
						{
							return true;
						}
					}
				}
			}
			catch (Exception)
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Parses the <c>MonthYear44</c> value from <c>buffer</c>.
		/// <p/>
		/// The format for <c>MonthYear44</c> are YYYYMM, YYYYMMDD and YYYYMMWW.
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		/// <exception cref="ArgumentException"> </exception>
		public static DateTime ParseMonthYear44(byte[] buffer)
		{
			return ParseMonthYear44(buffer, 0, buffer.Length);
		}

		/// <summary>
		/// Parses the <c>MonthYear44</c> value from <c>buffer</c>.
		/// <p/>
		/// The format for <c>MonthYear44</c> are YYYYMM, YYYYMMDD and YYYYMMWW.
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		/// <exception cref="ArgumentException"> </exception>
		public static DateTime ParseMonthYear44(byte[] buffer, int offset, int length)
		{
			if (buffer == null || length != 6 && length != 8)
			{
				throw new ArgumentException();
			}

			var year = ParseYearPart(buffer, offset);
			var month = ParseMonthPart(buffer, offset);
			if (length == 8)
			{
				if (buffer[offset + 6] == (byte)'w')
				{
					var week = buffer[offset + 7] - '0';
					if ((week < 1) | (week > 5))
					{
						throw new ArgumentException();
					}

					// set first business day to avoid unexpected recalculating of year, month, date fields
					// when retrieving field data after parsing
					var result = DateTimeHelper.GetDate(year, month, week, DayOfWeek.Monday);
					return DateTime.SpecifyKind(result, DateTimeKind.Utc);
				}

				var date = ParseDatePart(buffer, offset, year, month);
				return new DateTimeBuilder(year, month, date).Build(DateTimeKind.Utc);
			}

			return new DateTimeBuilder().SetYear(year).SetMonth(month).Build(DateTimeKind.Utc);
		}

		/// <summary>
		/// Formats the <c>MonthYear44</c> value from <c>calendar</c>.
		/// <p/>
		/// The format for <c>MonthYear44</c> are YYYYMM, YYYYMMDD and YYYYMMWW.
		/// </summary>
		/// <param name="calendar"> the calendar </param>
		/// <exception cref="ArgumentException"> </exception>
		public static byte[] FormatMonthYear44(DateTime calendar)
		{
			var utcCal = GetUtcCalendar(calendar);

			var block = new byte[8];
			var val = utcCal.Year;
			block[0] = (byte)(val / 1000 % 10 + '0');
			block[1] = (byte)(val / 100 % 10 + '0');
			block[2] = (byte)(val / 10 % 10 + '0');
			block[3] = (byte)(val % 10 + '0');
			val = utcCal.Month;
			block[4] = (byte)(val / 10 % 10 + '0');
			block[5] = (byte)(val % 10 + '0');

			val = utcCal.Day;
			block[6] = (byte)(val / 10 % 10 + '0');
			block[7] = (byte)(val % 10 + '0');

			return block;
		}

		/// <summary>
		/// Returns true, if subarray contains the invalid <c>Time</c> value.
		/// <p/>
		/// The format for <c>Time</c> is YYYYMMDD-HH:MM:SS.
		/// </summary>
		/// <param name="tag"> the buffer of bytes </param>
		public static bool IsInvalidTime(TagValue tag)
		{
			return IsInvalidTime(tag.Buffer, tag.Offset, tag.Length);
		}

		/// <summary>
		/// Returns true, if subarray contains the invalid <c>Time</c> value.
		/// <p/>
		/// The format for <c>Time</c> is YYYYMMDD-HH:MM:SS.
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		public static bool IsInvalidTime(byte[] buffer)
		{
			return IsInvalidTime(buffer, 0, buffer.Length);
		}

		/// <summary>
		/// Returns true, if subarray contains the invalid <c>Time</c> value.
		/// <p/>
		/// The <c>offset</c> argument is the
		/// index of the first byte of the subarray and the <c>count</c>
		/// argument specifies the length of the subarray.
		/// <p/>
		/// The format for <c>Time</c> is YYYYMMDD-HH:MM:SS.
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		/// <param name="offset"> the initial offset </param>
		/// <param name="count">  the length </param>
		public static bool IsInvalidTime(byte[] buffer, int offset, int count)
		{
			try
			{
				if (count != 17)
				{
					return true;
				}

				if (buffer[offset + 8] != (byte)'-' || buffer[offset + 11] != (byte)':' ||
					buffer[offset + 14] != (byte)':')
				{
					return true;
				}

				var year = ParseNumberPart(buffer, offset, offset + 4);
				if (year < 1583)
				{
					return true;
				}

				var month = ParseNumberPart(buffer, offset + 4, offset + 6) - 1;
				if (month < 0 || month > 11)
				{
					return true;
				}

				var date = ParseNumberPart(buffer, offset + 6, offset + 8);
				if (date < 1 || date > (year % 4 == 0 && (year % 100 != 0 || year % 400 == 0)
					? LeapMonthLength[month]
					: MonthLength[month]))
				{
					return true;
				}

				var hour = ParseNumberPart(buffer, offset + 9, offset + 11);
				if (hour < 0 || hour > 23)
				{
					return true;
				}

				var minute = ParseNumberPart(buffer, offset + 12, offset + 14);
				if (minute < 0 || minute > 59)
				{
					return true;
				}

				var second = ParseNumberPart(buffer, offset + 15, offset + 17);
				if (second < 0 || second > 60)
				{
					return true;
				}
			}
			catch (Exception)
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Parses the <c>Timestamp40</c> value from <c>buffer</c>.
		/// <p/>
		/// The format for <c>Timestamp40</c> is YYYYMMDD-HH:MM:SS
		/// </summary>
		/// <param name="buffer">   the buffer of bytes </param>
		/// <exception cref="ArgumentException"> </exception>
		public static DateTime ParseTimestamp40(byte[] buffer)
		{
			if (buffer.Length > 17)
			{
				throw new ArgumentException("Illegal timestamp length");
			}

			return ParseTimestamp(buffer);
		}

		/// <summary>
		/// Parses the <c>Timestamp</c> value from <c>buffer</c> to <c>calendar</c>.
		/// <p/>
		/// The format for <c>Timestamp</c> is YYYYMMDD-HH:MM:SS[.sss]
		/// </summary>
		/// <param name="buffer">   the buffer of bytes </param>
		/// <exception cref="ArgumentException"> </exception>
		public static DateTime ParseTimestamp(byte[] buffer)
		{
			return ParseTimestamp(buffer, 0, buffer.Length);
		}

		/// <summary>
		/// Parses the <c>Timestamp</c> value from <c>buffer</c> to <c>calendar</c>.
		/// <p/>
		/// The format for <c>Timestamp</c> is YYYYMMDD-HH:MM:SS[.sss]
		/// </summary>
		/// <param name="buffer">   the buffer of bytes </param>
		/// <exception cref="ArgumentException"> </exception>
		public static DateTime ParseTimestamp(byte[] buffer, int offset, int length)
		{
			if (buffer == null || length > 27 || buffer[offset + 8] != (byte)'-' ||
				buffer[offset + 11] != (byte)':' || buffer[offset + 14] != (byte)':')
			{
				throw new ArgumentException(
					"Invalid timestamp value: " + StringHelper.NewString(buffer, offset, length));
			}

			var year = ParseYearPart(buffer, offset);
			var month = ParseMonthPart(buffer, offset);
			var date = ParseDatePart(buffer, offset, year, month);
			var hour = ParseNumberPart(buffer, offset + 9, offset + 11);
			if (hour < 0 || hour > 23)
			{
				throw new ArgumentException();
			}

			var minute = ParseNumberPart(buffer, offset + 12, offset + 14);
			if (minute < 0 || minute > 59)
			{
				throw new ArgumentException();
			}

			var second = ParseNumberPart(buffer, offset + 15, offset + 17);
			if (second < 0 || second > 60)
			{
				throw new ArgumentException();
			}

			var builder = new DateTimeBuilder(year, month, date, hour, minute, second);

			if (length >= 18)
			{
				if (buffer[offset + 17] != (byte)'.')
				{
					throw new ArgumentException();
				}

				var millisecond = ParseNumberPart(buffer, offset + 18, offset + 21);
				builder = builder.SetMillisecond(millisecond);
			}

			return builder.Build(DateTimeKind.Utc);
		}

		/// <summary>
		/// Parses the <c>TimeOnly</c> value from <c>buffer</c>.
		/// </summary>
		/// <p/>
		/// The format for <c>TimeOnly</c> is HH:MM:SS[.sss]
		/// <param name="buffer">   the buffer of bytes </param>
		/// <exception cref="ArgumentException"> </exception>
		public static DateTime parseTimeOnly(byte[] buffer)
		{
			return parseTimeOnly(buffer, 0, buffer.Length);
		}

		/// <summary>
		/// Parses the <c>TimeOnly</c> value from <c>buffer</c>.
		/// </summary>
		/// <p/>
		/// The format for <c>TimeOnly</c> is HH:MM:SS[.sss]
		/// <param name="buffer">   the buffer of bytes </param>
		/// <exception cref="ArgumentException"> </exception>
		public static DateTime parseTimeOnly(byte[] buffer, int offset, int length)
		{
			if (buffer == null || length > 18 || buffer[offset + 2] != (byte)':' || buffer[offset + 5] != (byte)':')
			{
				throw new ArgumentException();
			}

			var hour = ParseNumberPart(buffer, offset, offset + 2);
			if (hour < 0 || hour > 23)
			{
				throw new ArgumentException();
			}

			var minute = ParseNumberPart(buffer, offset + 3, offset + 5);
			if (minute < 0 || minute > 59)
			{
				throw new ArgumentException();
			}

			var second = ParseNumberPart(buffer, offset + 6, offset + 8);
			if (second < 0 || second > 60)
			{
				throw new ArgumentException();
			}

			if (length >= 12)
			{
				if (buffer[offset + 8] != (byte)'.')
				{
					throw new ArgumentException();
				}

				var millisecond = ParseNumberPart(buffer, offset + 9, offset + 12);
				return new DateTimeBuilder().SetHour(hour).SetMinute(minute).SetSecond(second)
					.SetMillisecond(millisecond).Build(DateTimeKind.Utc);
			}

			return new DateTimeBuilder().SetHour(hour).SetMinute(minute).SetSecond(second).Build(DateTimeKind.Utc);
		}

		/// <summary>
		/// Parses the <c>TZTimeOnly</c> value from <c>buffer</c>.
		/// </summary>
		/// <p/>
		/// The format for <c>TZTimeOnly</c> is HH:MM[:SS][.sss][Z | [ + | - hh[:mm]]]
		/// <param name="buffer"> the buffer of bytes </param>
		/// <exception cref="ArgumentException"> </exception>
		public static DateTimeOffset parseTZTimeOnly(byte[] buffer)
		{
			// Example: 07:39Z is 07:39 UTC
			// Example: 02:39-05 is five hours behind UTC, thus Eastern Time
			// Example: 15:39+08 is eight hours ahead of UTC, Hong Kong/Singapore time
			return parseTZTimeOnlyPart(buffer, 0, buffer.Length);
		}

		public static DateTimeOffset parseTZTimeOnly(byte[] buffer, int offset, int length)
		{
			return parseTZTimeOnlyPart(buffer, offset, length);
		}

		private static DateTimeOffset parseTZTimeOnlyPart(byte[] buffer, int offset, int length)
		{
			var hour = ParseNumberPart(buffer, offset, offset + 2);
			if (hour < 0 || hour > 23)
			{
				throw new ArgumentException();
			}

			// Starting from year 2 to prevent ArgumentOutOfRangeException in case when current time inside offset.
			var timeBuilder = new DateTimeBuilder(2, 1, 1);
			timeBuilder = timeBuilder.SetHour(hour);
			var minute = ParseNumberPart(buffer, offset + 3, offset + 5);
			if (minute < 0 || minute > 59)
			{
				throw new ArgumentException();
			}

			timeBuilder = timeBuilder.SetMinute(minute);
			var timeZoneLength = length - 5;

			if (length > 5 && buffer[offset + 5] == (byte)':')
			{
				// seconds
				var seconds = ParseNumberPart(buffer, offset + 6, offset + 8);
				if (seconds < 0 || seconds > 60)
				{
					throw new ArgumentException();
				}

				timeBuilder = timeBuilder.SetSecond(seconds);
				timeZoneLength -= 3;

				if (length > 8 && buffer[offset + 8] == (byte)'.' && length + offset > 5)
				{
					// milliseconds
					var mills = ParseNumberPart(buffer, offset + 9, offset + 12);
					if (mills < 0 || mills > 999)
					{
						throw new ArgumentException();
					}

					timeBuilder = timeBuilder.SetMillisecond(mills);
					timeZoneLength -= 4;

					var fractionsCount = 3; // as minimum milliseconds are present
					for (var i = 12; i < length; i++)
					{
						if (buffer[offset + i] >= (byte)'0' && buffer[offset + i] <= (byte)'9')
						{
							fractionsCount++;
						}
						else
						{
							break;
						}
					}

					if (fractionsCount == 6)
					{
						timeZoneLength -= 3;
					}
					else if (fractionsCount == 9)
					{
						timeZoneLength -= 6;
					}
					else if (fractionsCount != 3)
					{
						throw new ArgumentException("Invalid count of second fractions");
					}
				}
			}

			var tzStartPosition = offset + length - timeZoneLength;
			var timeZoneOffset = DateTimeHelper.ParseZoneOffset(buffer, tzStartPosition, timeZoneLength);
			return timeBuilder.Build(timeZoneOffset);
		}

		/// <summary>
		/// Parses the <c>TZTimestamp</c> value from <c>buffer</c>.
		/// </summary>
		/// <p/>
		/// The format for <c>TZTimeOnly</c> is YYYYMMDD-HH:MM[:SS][Z | [ + | - hh[:mm]]].
		/// <param name="buffer"> the buffer of bytes </param>
		/// <exception cref="ArgumentException"> </exception>
		public static DateTimeOffset ParseTzTimestamp(byte[] buffer)
		{
			return ParseTzTimestamp(buffer, 0, buffer.Length);
		}

		/// <summary>
		/// Parses the <c>TZTimestamp</c> value from <c>buffer</c>.
		/// </summary>
		/// <p/>
		/// The format for <c>TZTimestamp</c> is YYYYMMDD-HH:MM[:SS][Z | [ + | - hh[:mm]]].
		/// <param name="buffer"> the buffer of bytes </param>
		/// <exception cref="ArgumentException"> </exception>
		public static DateTimeOffset ParseTzTimestamp(byte[] buffer, int offset, int length)
		{
			if (length < 8)
			{
				throw new ArgumentException("Parsing date is to short: " + StringHelper.NewString(buffer));
			}

			// Example: 20060901-07:39Z is 07:39 UTC on 1st of September 2006
			// Example: 20060901-02:39-05 is five hours behind UTC, thus Eastern Time on 1st of September 2006
			// Example: 20060901-15:39+08 is eight hours ahead of UTC, Hong Kong/Singapore time on 1st of September 2006
			// Example: 20060901-13:09+05:30 is 5.5 hours ahead of UTC, India time on 1st of September 2006
			// parse date yyyymmdd
			var date = ParseDate(buffer, offset, 8);

			// parse HH:MM[:SS][Z | [ + | - hh[:mm]]]
			var time = parseTZTimeOnlyPart(buffer, offset + 9, length - 9);
			return new DateTimeBuilder(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second)
				.SetMillisecond(time.Millisecond)
				.SetNanosecond(time.GetNanosecondsOfMillisecond())
				.Build(time.Offset);
		}

		/// <summary>
		/// Parses the <c>Date</c> value from <c>buffer</c>.
		/// </summary>
		/// <p/>
		/// The format for <c>Date</c> is YYYYMMDD.
		/// <param name="buffer"> the buffer of bytes </param>
		/// <exception cref="ArgumentException"> </exception>
		public static DateTime ParseDate(byte[] buffer)
		{
			return ParseDate(buffer, 0, buffer.Length);
		}

		/// <summary>
		/// Parses the <c>Date</c> value from <c>buffer</c>.
		/// </summary>
		/// <p/>
		/// The format for <c>Date</c> is YYYYMMDD.
		/// <param name="buffer"> the buffer of bytes</param>
		/// <exception cref="ArgumentException"> </exception>
		public static DateTime ParseDate(byte[] buffer, int offset, int length)
		{
			if (buffer == null || length < 8)
			{
				throw new ArgumentException("Parsing date is to short: " + StringHelper.NewString(buffer));
			}

			var year = ParseYearPart(buffer, offset);
			var month = ParseMonthPart(buffer, offset);
			var date = ParseDatePart(buffer, offset, year, month);
			return DateTime.SpecifyKind(new DateTime(year, month, date), DateTimeKind.Utc);
		}

		internal static int ParseDatePart(byte[] buffer, int year, int month)
		{
			return ParseDatePart(buffer, 0, year, month);
		}

		internal static int ParseDatePart(byte[] buffer, int offset, int year, int month)
		{
			var date = ParseNumberPart(buffer, offset + 6, offset + 8);
			if (date < 1 || date > (year % 4 == 0 && (year % 100 != 0 || year % 400 == 0)
				? LeapMonthLength[month - 1]
				: MonthLength[month - 1]))
			{
				throw new ArgumentException("Incorrect day value");
			}

			return date;
		}

		internal static int ParseMonthPart(byte[] block)
		{
			return ParseMonthPart(block, 0);
		}

		internal static int ParseMonthPart(byte[] block, int offset)
		{
			var month = ParseNumberPart(block, offset + 4, offset + 6);
			if (month < 1 || month > 12)
			{
				throw new ArgumentException("Incorrect month value");
			}

			return month;
		}

		internal static int ParseYearPart(byte[] block)
		{
			return ParseYearPart(block, 0);
		}

		internal static int ParseYearPart(byte[] block, int offset)
		{
			var year = ParseNumberPart(block, offset, offset + 4);

			return year;
		}

		/// <summary>
		/// Parses the <c>LocalMktDate</c> value from <c>buffer</c>.
		/// </summary>
		/// <p/>
		/// The format for <c>LocalMktDate</c> is YYYYMMDD.
		/// <param name="buffer"> the buffer of bytes  </param>
		/// <exception cref="ArgumentException"> </exception>
		public static DateTime ParseLocalMktDate(byte[] buffer)
		{
			return ParseDate(buffer);
		}

		/// <summary>
		/// Parses the <c>MonthYear</c> value from <c>buffer</c>.
		/// </summary>
		/// <p/>
		/// The format for <c>MonthYear</c> is YYYYMM.
		/// <param name="buffer"> the buffer of bytes </param>
		/// <exception cref="ArgumentException"> </exception>
		public static DateTime ParseMonthYear(byte[] buffer)
		{
			return ParseMonthYear(buffer, 0, buffer.Length);
		}

		/// <summary>
		/// Parses the <c>MonthYear</c> value from <c>buffer</c>.
		/// </summary>
		/// <p/>
		/// The format for <c>MonthYear</c> is YYYYMM.
		/// <param name="buffer">   the buffer of bytes </param>
		/// <exception cref="ArgumentException"> </exception>
		public static DateTime ParseMonthYear(byte[] buffer, int offset, int length)
		{
			if (buffer == null || length != 6)
			{
				throw new ArgumentException();
			}

			var year = ParseYearPart(buffer, offset);
			var month = ParseMonthPart(buffer, offset);
			return new DateTimeBuilder().SetYear(year).SetMonth(month).Build(DateTimeKind.Utc);
		}

		/// <summary>
		/// Parses the <c>Time</c> value from <c>buffer</c>.
		/// </summary>
		/// <p/>
		/// The format for <c>Time</c> is YYYYMMDD-HH:MM:SS.
		/// <param name="buffer">   the buffer of bytes </param>
		/// <exception cref="ArgumentException"> </exception>
		public static DateTime ParseTime(byte[] buffer)
		{
			if (buffer == null || buffer.Length != 17 || buffer[8] != (byte)'-' || buffer[11] != (byte)':' ||
				buffer[14] != (byte)':')
			{
				throw new ArgumentException();
			}

			var year = ParseYearPart(buffer);
			var month = ParseMonthPart(buffer);
			var date = ParseDatePart(buffer, year, month);
			var hour = ParseNumberPart(buffer, 9, 11);
			if (hour < 0 || hour > 23)
			{
				throw new ArgumentException();
			}

			var minute = ParseNumberPart(buffer, 12, 14);
			if (minute < 0 || minute > 59)
			{
				throw new ArgumentException();
			}

			var second = ParseNumberPart(buffer, 15, 17);
			if (second < 0 || second > 60)
			{
				throw new ArgumentException();
			}

			return new DateTimeBuilder(year, month, date, hour, minute, second).Build(DateTimeKind.Utc);
		}

		/// <summary>
		/// Parses the <c>Time</c> value from <c>buffer</c>.
		/// </summary>
		/// <p/>
		/// The format for <c>Time</c> is HH:MM:SS.
		/// <param name="buffer">   the buffer of bytes </param>
		/// <exception cref="ArgumentException"> </exception>
		public static DateTime ParseShortTime(byte[] buffer)
		{
			if (buffer == null || buffer.Length != 8)
			{
				throw new ArgumentException();
			}

			var hour = ParseNumberPart(buffer, 0, 2);
			if (hour < 0 || hour > 23)
			{
				throw new ArgumentException();
			}

			var minute = ParseNumberPart(buffer, 3, 5);
			if (minute < 0 || minute > 59)
			{
				throw new ArgumentException();
			}

			var second = ParseNumberPart(buffer, 6, 8);
			if (second < 0 || second > 60)
			{
				throw new ArgumentException();
			}

			return new DateTimeBuilder().SetHour(hour).SetMinute(minute).SetSecond(second).Build(DateTimeKind.Utc);
		}

		/// <summary>
		/// Formats the value of <c>calendar</c> to the <c>UTCTimestamp40</c> format.
		/// <p/>
		/// The format for <c>UTCTimestamp40</c> is YYYYMMDD-HH:MM:SS.
		/// </summary>
		/// <param name="calendar"> the value </param>
		public static byte[] FormatUtcTimestamp40(DateTime calendar)
		{
			calendar = GetUtcCalendar(calendar);
			var builder = new DateTimeBuilder(calendar);
			builder.SetMillisecond(0);
			var dateTimeInUtcWithoutMilliseconds = builder.Build(DateTimeKind.Utc);

			return FormatUtcTimestamp(dateTimeInUtcWithoutMilliseconds, TimestampPrecision.Second);
		}

		/// <summary>
		/// Formats the value of <c>calendar</c> to the <c>UTCTimestamp</c> format.
		/// <p/>
		/// The format for <c>UTCTimestamp</c> is YYYYMMDD-HH:MM:SS[.sss].
		/// </summary>
		/// <param name="calendar">the value</param>
		/// <param name="precision">precision for formatting: <see cref="TimestampPrecision.Second"/> or <see cref="TimestampPrecision.Milli"/></param>
		public static byte[] FormatUtcTimestamp(DateTime calendar, TimestampPrecision precision)
		{
			if (precision != TimestampPrecision.Second && precision != TimestampPrecision.Milli)
			{
				throw new ArgumentException("Invalid value of the desired precision: " + precision);
			}

			calendar = GetUtcCalendar(calendar);

			var length = 17;
			if (precision == TimestampPrecision.Milli)
			{
				length += 4;
			}

			var block = new byte[length];
			var val = calendar.Year;
			block[0] = (byte)(val / 1000 % 10 + '0');
			block[1] = (byte)(val / 100 % 10 + '0');
			block[2] = (byte)(val / 10 % 10 + '0');
			block[3] = (byte)(val % 10 + '0');
			val = calendar.Month;
			block[4] = (byte)(val / 10 % 10 + '0');
			block[5] = (byte)(val % 10 + '0');
			val = calendar.Day;
			block[6] = (byte)(val / 10 % 10 + '0');
			block[7] = (byte)(val % 10 + '0');
			block[8] = (byte)'-';
			val = calendar.Hour;
			block[9] = (byte)(val / 10 % 10 + '0');
			block[10] = (byte)(val % 10 + '0');
			block[11] = (byte)':';
			val = calendar.Minute;
			block[12] = (byte)(val / 10 % 10 + '0');
			block[13] = (byte)(val % 10 + '0');

			block[14] = (byte)':';
			val = calendar.Second;
			block[15] = (byte)(val / 10 % 10 + '0');
			block[16] = (byte)(val % 10 + '0');

			if (precision == TimestampPrecision.Milli)
			{
				block[17] = (byte)'.';
				val = calendar.Millisecond;
				block[18] = (byte)(val / 100 % 10 + '0');
				block[19] = (byte)(val / 10 % 10 + '0');
				block[20] = (byte)(val % 10 + '0');
			}

			return block;
		}

		/// <summary>
		/// Format UTC timestamp with include milliseconds
		/// </summary>
		/// <param name="calendar">         timestamp </param>
		/// <param name="sendingTimeBufMs"> buffer size must be 21 </param>
		public static void FormatUtcTimestampWithMs(DateTime calendar, byte[] sendingTimeBufMs)
		{
			calendar = GetUtcCalendar(calendar);
			FillTimestampNoMs(calendar, sendingTimeBufMs);
			FillTimestampMs(calendar, sendingTimeBufMs);
		}

		/// <summary>
		/// Format UTC timestamp with include milliseconds
		/// </summary>
		/// <param name="calendar">           timestamp </param>
		/// <param name="sendingTimeBufNoMs"> buffer size must be 17 </param>
		public static void FormatUtcTimestampWithoutMs(DateTime calendar, byte[] sendingTimeBufNoMs)
		{
			calendar = GetUtcCalendar(calendar);
			FillTimestampNoMs(calendar, sendingTimeBufNoMs);
		}

		private static void FillTimestampNoMs(DateTime calendar, byte[] block)
		{
			var val = calendar.Year;
			block[0] = (byte)(val / 1000 % 10 + '0');
			block[1] = (byte)(val / 100 % 10 + '0');
			block[2] = (byte)(val / 10 % 10 + '0');
			block[3] = (byte)(val % 10 + '0');
			val = calendar.Month;
			block[4] = (byte)(val / 10 % 10 + '0');
			block[5] = (byte)(val % 10 + '0');
			val = calendar.Day;
			block[6] = (byte)(val / 10 % 10 + '0');
			block[7] = (byte)(val % 10 + '0');
			block[8] = (byte)'-';
			val = calendar.Hour;
			block[9] = (byte)(val / 10 % 10 + '0');
			block[10] = (byte)(val % 10 + '0');
			block[11] = (byte)':';
			val = calendar.Minute;
			block[12] = (byte)(val / 10 % 10 + '0');
			block[13] = (byte)(val % 10 + '0');
			block[14] = (byte)':';
			val = calendar.Second;
			block[15] = (byte)(val / 10 % 10 + '0');
			block[16] = (byte)(val % 10 + '0');
		}

		private static void FillTimestampMs(DateTime calendar, byte[] block)
		{
			block[17] = (byte)'.';
			var val = calendar.Millisecond;
			block[18] = (byte)(val / 100 % 10 + '0');
			block[19] = (byte)(val / 10 % 10 + '0');
			block[20] = (byte)(val % 10 + '0');
		}

		/// <summary>
		/// Formats the value of <c>calendar</c> to the <c>UTCTimeOnly</c> format.
		/// <p/>
		/// The format for <c>UTCTimeOnly</c> is HH:MM:SS[.sss].
		/// </summary>
		/// <param name="calendar">the value</param>
		/// <param name="precision">precision for formatting: <see cref="TimestampPrecision.Second"/> or <see cref="TimestampPrecision.Milli"/></param>
		public static byte[] formatUTCTimeOnly(DateTime calendar, TimestampPrecision precision)
		{
			if (precision != TimestampPrecision.Second && precision != TimestampPrecision.Milli)
			{
				throw new ArgumentException("Invalid value of the desired precision: " + precision);
			}

			calendar = GetUtcCalendar(calendar);
			var length = 8;
			if (precision == TimestampPrecision.Milli)
			{
				length += 4;
			}

			var block = new byte[length];
			var val = calendar.Hour;
			block[0] = (byte)(val / 10 % 10 + '0');
			block[1] = (byte)(val % 10 + '0');
			block[2] = (byte)':';
			val = calendar.Minute;
			block[3] = (byte)(val / 10 % 10 + '0');
			block[4] = (byte)(val % 10 + '0');

			val = calendar.Second;
			block[5] = (byte)':';
			block[6] = (byte)(val / 10 % 10 + '0');
			block[7] = (byte)(val % 10 + '0');

			if (precision == TimestampPrecision.Milli)
			{
				val = calendar.Millisecond;
				block[8] = (byte)'.';
				block[9] = (byte)(val / 100 % 10 + '0');
				block[10] = (byte)(val / 10 % 10 + '0');
				block[11] = (byte)(val % 10 + '0');
			}

			return block;
		}

		/// <summary>
		/// Formats the value of <c>calendar</c> to the <c>TZTimeOnly</c> format.
		/// <p/>
		/// The format for <c>TZTimeOnly</c> is HH:MM[:SS][.sss][Z | [ + | - hh[:mm]]].
		/// </summary>
		/// <param name="calendar"> the value </param>
		public static byte[] formatTZTimeOnly(DateTimeOffset calendar)
		{
			var result = new StringBuilder();

			result.Append(calendar.Hour.ToString("00"));
			result.Append(':');
			result.Append(calendar.Minute.ToString("00"));

			var millsVal = calendar.Millisecond;
			var secVal = calendar.Second;
			if (millsVal != 0)
			{
				result.Append(':');
				result.Append(secVal.ToString("00"));
				result.Append('.');
				result.Append(millsVal.ToString("000"));
			}
			else
			{
				if (secVal != 0)
				{
					result.Append(':');
					result.Append(secVal.ToString("00"));
				}
			}

			result.Append(GetTimeZone(calendar));
			return result.ToString().AsByteArray();
		}

		private static string GetTimeZone(DateTimeOffset calendar)
		{
			var res = new StringBuilder();
			var offset = calendar.Offset.GetTotalMilliseconds() / (1000 * 60);
			if (offset == 0)
			{
				res.Append('Z');
			}
			else
			{
				if (offset < 0)
				{
					res.Append("-");
					offset = -offset;
				}
				else
				{
					res.Append("+");
				}

				if (offset % 60 == 0)
				{
					res.Append((offset / 60).ToString("00"));
				}
				else
				{
					res.Append((offset / 60).ToString("00"));
					res.Append(':');
					res.Append((offset % 60).ToString("00"));
				}
			}

			return res.ToString();
		}

		/// <summary>
		/// Formats the value of <c>calendar</c> to the <c>TZTimestamp</c> format.
		/// <p/>
		/// The format for <c>TZTimestamp</c> is YYYYMMDD-HH:MM[:SS[.sss]][Z | [ + | - hh[:mm]]].
		/// </summary>
		/// <param name="calendar">the calendar</param>
		/// <param name="precision">precision for formatting:
		/// <see cref="TimestampPrecision.Second"/>, <see cref="TimestampPrecision.Milli"/> or <see cref="TimestampPrecision.Second"/></param>
		public static byte[] FormatTzTimestamp(DateTimeOffset calendar, TimestampPrecision precision)
		{
			if (precision != TimestampPrecision.Minute
				&& precision != TimestampPrecision.Second
				&& precision != TimestampPrecision.Milli)
			{
				throw new ArgumentException("Invalid value of the desired precision: " + precision);
			}

			var len = 14;
			if (precision == TimestampPrecision.Milli)
			{
				len += 7;
			}

			if (precision == TimestampPrecision.Second)
			{
				len += 3;
			}

			var block = new byte[len];

			var val = calendar.Year;
			block[0] = (byte)(val / 1000 % 10 + '0');
			block[1] = (byte)(val / 100 % 10 + '0');
			block[2] = (byte)(val / 10 % 10 + '0');
			block[3] = (byte)(val % 10 + '0');
			val = calendar.Month;
			block[4] = (byte)(val / 10 % 10 + '0');
			block[5] = (byte)(val % 10 + '0');
			val = calendar.Day;
			block[6] = (byte)(val / 10 % 10 + '0');
			block[7] = (byte)(val % 10 + '0');
			block[8] = (byte)'-';
			val = calendar.Hour;
			block[9] = (byte)(val / 10 % 10 + '0');
			block[10] = (byte)(val % 10 + '0');
			block[11] = (byte)':';
			val = calendar.Minute;
			block[12] = (byte)(val / 10 % 10 + '0');
			block[13] = (byte)(val % 10 + '0');

			if (precision == TimestampPrecision.Second)
			{
				// seconds
				block[14] = (byte)':';
				val = calendar.Second;
				block[15] = (byte)(val / 10 % 10 + '0');
				block[16] = (byte)(val % 10 + '0');
			}

			if (precision == TimestampPrecision.Milli)
			{
				// seconds
				block[14] = (byte)':';
				val = calendar.Second;
				block[15] = (byte)(val / 10 % 10 + '0');
				block[16] = (byte)(val % 10 + '0');
				// mills
				val = calendar.Millisecond;
				block[17] = (byte)'.';
				block[18] = (byte)(val / 100 % 10 + '0');
				block[19] = (byte)(val / 10 % 10 + '0');
				block[20] = (byte)(val % 10 + '0');
			}

			block = (StringHelper.NewString(block) + GetTimeZone(calendar)).AsByteArray();

			return block;
		}

		/// <summary>
		/// Formats the value of <c>calendar</c> to the <c>Date</c> format.
		/// <p/>
		/// The format for <c>Date</c> is YYYYMMDD.
		/// </summary>
		/// <param name="calendar"> the calendar </param>
		public static byte[] FormatDate(DateTime calendar)
		{
			var block = new byte[8];
			var val = calendar.Year;
			block[0] = (byte)(val / 1000 % 10 + '0');
			block[1] = (byte)(val / 100 % 10 + '0');
			block[2] = (byte)(val / 10 % 10 + '0');
			block[3] = (byte)(val % 10 + '0');
			val = calendar.Month;
			block[4] = (byte)(val / 10 % 10 + '0');
			block[5] = (byte)(val % 10 + '0');
			val = calendar.Day;
			block[6] = (byte)(val / 10 % 10 + '0');
			block[7] = (byte)(val % 10 + '0');
			return block;
		}

		/// <summary>
		/// Formats the value of <c>calendar</c> to the <c>UTCDate</c> format.
		/// <p/>
		/// The format for <c>UTCDate</c> is YYYYMMDD.
		/// </summary>
		/// <param name="calendar"> the calendar </param>
		public static byte[] FormatUtcDate(DateTime calendar)
		{
			return FormatDate(GetUtcCalendar(calendar));
		}

		/// <summary>
		/// Formats the value of <c>calendar</c> to the <c>LocalMktDate</c> format.
		/// <p/>
		/// The format for <c>LocalMktDate</c> is YYYYMMDD.
		/// </summary>
		/// <param name="calendar"> the calendar </param>
		public static byte[] FormatLocalMktDate(DateTime calendar)
		{
			return FormatDate(GetLocalCalendar(calendar));
		}

		/// <summary>
		/// Formats the value of <c>calendar</c> to the <c>MonthYear</c> format.
		/// <p/>
		/// The format for <c>MonthYear</c> is YYYYMM.
		/// </summary>
		/// <param name="calendar"> the calendar </param>
		public static byte[] FormatMonthYear(DateTime calendar)
		{
			calendar = GetUtcCalendar(calendar);
			var block = new byte[6];
			var val = calendar.Year;
			block[0] = (byte)(val / 1000 % 10 + '0');
			block[1] = (byte)(val / 100 % 10 + '0');
			block[2] = (byte)(val / 10 % 10 + '0');
			block[3] = (byte)(val % 10 + '0');
			val = calendar.Month;
			block[4] = (byte)(val / 10 % 10 + '0');
			block[5] = (byte)(val % 10 + '0');
			return block;
		}

		/// <summary>
		/// Formats the value of <c>calendar</c> to the <c>Time</c> format.
		/// <p/>
		/// The format for <c>Time</c> is YYYYMMDD-HH:MM:SS.
		/// </summary>
		/// <param name="calendar"> the calendar </param>
		public static byte[] FormatTime(DateTime calendar)
		{
			calendar = GetUtcCalendar(calendar);
			var block = new byte[17];
			var val = calendar.Year;
			block[0] = (byte)(val / 1000 % 10 + '0');
			block[1] = (byte)(val / 100 % 10 + '0');
			block[2] = (byte)(val / 10 % 10 + '0');
			block[3] = (byte)(val % 10 + '0');
			val = calendar.Month;
			block[4] = (byte)(val / 10 % 10 + '0');
			block[5] = (byte)(val % 10 + '0');
			val = calendar.Day;
			block[6] = (byte)(val / 10 % 10 + '0');
			block[7] = (byte)(val % 10 + '0');
			block[8] = (byte)'-';
			val = calendar.Hour;
			block[9] = (byte)(val / 10 % 10 + '0');
			block[10] = (byte)(val % 10 + '0');
			block[11] = (byte)':';
			val = calendar.Minute;
			block[12] = (byte)(val / 10 % 10 + '0');
			block[13] = (byte)(val % 10 + '0');
			block[14] = (byte)':';
			val = calendar.Second;
			block[15] = (byte)(val / 10 % 10 + '0');
			block[16] = (byte)(val % 10 + '0');
			return block;
		}

		private static DateTime GetUtcCalendar(DateTime cal)
		{
			return cal.ToUniversalTime();
		}

		private static DateTime GetLocalCalendar(DateTime cal)
		{
			// todo: will change time if kind is unspecified; consider using TimeZoneInfo.ConvertTime(tmp, TimeZoneInfo.Local);
			return cal.ToLocalTime();
		}

		/// <summary>
		/// Parses the <c>integer</c> value from <c>tag</c>.
		/// </summary>
		/// <param name="tag"></param>
		/// <returns></returns>
		public static long ParseInt(TagValue tag)
		{
			return ParseInt(tag.Buffer, tag.Offset, tag.Length);
		}

		/// <summary>
		/// Parses the <c>integer</c> value from <c>buffer</c>.
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		/// <exception cref="ArgumentException"> </exception>
		public static long ParseInt(byte[] buffer)
		{
			return ParseInt(buffer, 0, buffer.Length);
		}

		/// <summary>
		/// Try to parse the <c>long</c> value from <c>buffer</c>.
		/// <p/>
		/// The <c>offset</c> argument is the
		/// index of the first byte of the subarray and the <c>count</c>
		/// argument specifies the length of the subarray.
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		/// <param name="offset"> the initial offset </param>
		/// <param name="count">  the length </param>
		/// <param name="value"> parsed value if parsing succeeded or 0L</param>
		/// <returns>True, if parsing was sucessful.</returns>
		public static bool TryParseLong(byte[] buffer, int offset, int count, out long value)
		{
			value = 0L;
			var limit = offset + count;
			var isNegative = limit > offset && buffer[offset] == (byte)'-';
			if (isNegative)
			{
				offset++;
			}

			if (limit == offset)
			{
				return false;
			}

			while (offset < limit)
			{
				var ch = buffer[offset++];
				if (ch < (byte)'0' || ch > (byte)'9' || value > long.MaxValue / 10L)
				{
					return false;
				}

				value = value * 10L + (ch - '0');
			}

			value = isNegative ? -value : value;
			return true;
		}

		/// <summary>
		/// Parses the <c>integer</c> value from <c>buffer</c>.
		/// <p/>
		/// The <c>offset</c> argument is the
		/// index of the first byte of the subarray and the <c>count</c>
		/// argument specifies the length of the subarray.
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		/// <param name="offset"> the initial offset </param>
		/// <param name="count">  the length </param>
		/// <exception cref="ArgumentException"> </exception>
		public static long ParseInt(byte[] buffer, int offset, int count)
		{
			var limit = offset + count;
			var isNegative = limit > offset && buffer[offset] == (byte)'-';
			if (isNegative)
			{
				offset++;
			}

			if (limit == offset)
			{
				throw new ArgumentException("Empty number.");
			}

			var value = 0L;
			while (offset < limit)
			{
				var ch = buffer[offset++];
				if (ch < (byte)'0' || ch > (byte)'9' || value > long.MaxValue / 10L)
				{
					throw new ArgumentException("Not a number");
				}

				value = value * 10L + (ch - '0');
			}

			return isNegative ? -value : value;
		}

		/// <summary>
		/// Parses the <c>integer</c> value from <c>str</c>.
		/// <p/>
		/// The <c>offset</c> argument is the
		/// index of the first char of the substring and the <c>count</c>
		/// argument specifies the length of the substring.
		/// </summary>
		/// <param name="str">    a string representation of an integer. </param>
		/// <param name="offset"> the initial offset </param>
		/// <param name="count">  the length </param>
		/// <exception cref="ArgumentException"> </exception>
		public static long ParseInt(string str, int offset, int count)
		{
			var limit = offset + count;
			var isNegative = limit > offset && str[offset] == '-';
			if (isNegative)
			{
				offset++;
			}

			if (limit == offset)
			{
				throw new ArgumentException("Empty number.");
			}

			var value = 0L;
			while (offset < limit)
			{
				var ch = str[offset++];
				if (ch < '0' || ch > '9' || value > long.MaxValue / 10)
				{
					throw new ArgumentException("Not a number");
				}

				value = value * 10L + (ch - '0');
			}

			return isNegative ? -value : value;
		}

		/// <summary>
		/// Formats the integer value.
		/// </summary>
		/// <param name="value"> the value </param>
		/// <returns> the buffer </returns>
		public static byte[] FormatInt(long value)
		{
			var isNegative = false;
			if (value < 0)
			{
				isNegative = true;
				value = -value;
			}

			var length = 1;
			var valueCopy = value;
			while ((valueCopy /= 10) > 0)
			{
				length++;
			}

			byte[] buffer;
			if (isNegative)
			{
				length++;
				buffer = new byte[length];
				buffer[0] = (byte)'-';
			}
			else
			{
				buffer = new byte[length];
			}

			do
			{
				buffer[--length] = (byte)(value % 10 + '0');
			} while ((value /= 10) > 0);

			return buffer;
		}

		/// <summary>
		/// Calculates length (number of bytes) required to store provided value.
		/// </summary>
		/// <param name="value">Value to calculate length.</param>
		/// <param name="minLen">Minimum length of the value, optional.</param>
		/// <returns>Returns length (number of bytes) required to store provided value considering minimal length.</returns>
		public static int FormatIntLength(long value, int minLen = 1)
		{
			var length = 1;
			if (value < 0)
			{
				length++;
				value = -value;
			}

			while ((value /= 10) > 0)
			{
				length++;
			}

			return minLen > length ? minLen : length;
		}

		/// <summary>
		/// Calculates length of the SeqNum field for provided value and minimal length.
		/// </summary>
		/// <param name="value">Value to calculate length.</param>
		/// <param name="minLen">Minimal length of the field, optional.</param>
		/// <returns>Returns length (number of bytes) required to store provided value considering minimal length.</returns>
		public static int GetSeqNumLength(long value, int minLen)
		{
			return FormatIntLength(value, minLen);
		}

		public static int FormatInt(long value, byte[] buffer)
		{
			var isNegative = false;
			if (value < 0)
			{
				isNegative = true;
				value = -value;
			}

			var length = 1;
			var valueCopy = value;
			while ((valueCopy /= 10) > 0)
			{
				length++;
			}

			if (isNegative)
			{
				length++;
				buffer[0] = (byte)'-';
			}

			var len = length;
			do
			{
				buffer[--length] = (byte)(value % 10 + '0');
			} while ((value /= 10) > 0);

			return len;
		}

		public static int FormatInt(long value, byte[] buffer, int offset)
		{
			var isNegative = false;
			if (value < 0)
			{
				isNegative = true;
				value = -value;
			}

			var length = 1;
			var valueCopy = value;
			while ((valueCopy /= 10) > 0)
			{
				length++;
			}

			if (isNegative)
			{
				length++;
				buffer[offset] = (byte)'-';
			}

			var len = length;
			do
			{
				buffer[offset + --length] = (byte)(value % 10 + '0');
			} while ((value /= 10) > 0);

			return len;
		}

		public static int FormatIntLengthWithPadding(long value, int padLength)
		{
			var length = FormatIntLength(value);

			if (padLength > length)
			{
				length = padLength;
			}

			return length;
		}

		public static int FormatIntWithPadding(long value, int padLength, Span<byte> buffer)
		{
			var isNegative = false;
			var length = 1;

			if (value < 0)
			{
				isNegative = true;
				value = -value;
				length++;
			}

			var valueCopy = value;
			while ((valueCopy /= 10) > 0)
			{
				length++;
			}

			if (padLength > length)
			{
				length = padLength;
			}

			var len = length;
			do
			{
				buffer[--length] = (byte)(value % 10L + '0');
				value /= 10L;
			} while (length >= 1);

			if (isNegative)
			{
				buffer[0] = (byte)'-';
			}

			return len;
		}

		public static void FormatDoubleWithPadding(double value, int precision, int padLength, byte[] buffer,
			int offset)
		{
			DoubleFormatter.FormatWithPadding(value, precision, padLength, buffer, offset);
		}

		/// <summary>
		/// Formats the integer value.
		/// </summary>
		/// <param name="value"> the value </param>
		/// <returns> the buffer </returns>
		public static byte[] FormatUInt(long value)
		{
			if (value < 0)
			{
				value = -value;
			}

			var length = 1;
			var valueCopy = value;
			while ((valueCopy /= 10) > 0)
			{
				length++;
			}

			var buffer = new byte[length];
			do
			{
				buffer[--length] = (byte)(value % 10 + '0');
			} while ((value /= 10) > 0);

			return buffer;
		}

		/// <summary>
		/// Returns true, if subarray contains the invalid <c>integer</c> value.
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		public static bool IsInvalidInt(byte[] buffer)
		{
			return IsInvalidInt(buffer, 0, buffer.Length);
		}

		/// <summary>
		/// Returns true, if <see cref="TagValue"/> contains the invalid <c>integer</c> value.
		/// </summary>
		/// <param name="tag"></param>
		/// <returns></returns>
		public static bool IsInvalidInt(TagValue tag)
		{
			return IsInvalidInt(tag.Buffer, tag.Offset, tag.Length);
		}

		/// <summary>
		/// Check if buffer has invalid integer number
		/// </summary>
		/// <param name="buf"> buffer </param>
		/// <param name="off"> buffer offset </param>
		/// <param name="len"> buffer length </param>
		/// <returns> true if invalid </returns>
		public static bool IsInvalidInt(byte[] buf, int off, int len)
		{
			var limit = off + len;
			if (limit > off && buf[off] == (byte)'-')
			{
				off++;
			}

			if (limit == off)
			{
				return true;
			}

			var value = 0L;
			while (off < limit)
			{
				var ch = buf[off++];
				if (ch < (byte)'0' || ch > (byte)'9' || value > MaxIntBound)
				{
					return true;
				}

				value = value * 10L + (ch - '0');
			}

			return false;
		}

		/// <summary>
		/// Formats the check sum.
		/// </summary>
		/// <param name="checkSum"> the check sum </param>
		/// <returns> formated string </returns>
		public static byte[] FormatCheckSum(int checkSum)
		{
			return new[]
			{
				(byte)(checkSum / 100 + (byte)'0'), (byte)(checkSum / 10 % 10 + (byte)'0'),
				(byte)(checkSum % 10 + (byte)'0')
			};
		}

		// Used for high precision sending time tags formatting
		public static byte[] FormatTimestamp(byte[] buffer, int offset, DateTime calendar, long additionalFractions,
			TimestampPrecision precision)
		{
			FormatDateTimeWithDelimiter(buffer, offset, calendar, additionalFractions, precision, '-');
			return buffer;
		}

		// Used for storage timestamps formatting
		public static byte[] FormatStorageTimestamp(byte[] buffer, int offset, DateTime calendar,
			long additionalFractions, TimestampPrecision precision)
		{
			offset = FormatDateTimeWithDelimiter(buffer, offset, calendar, additionalFractions, precision, ' ');
			buffer[offset] = (byte)' ';
			buffer[offset + 1] = (byte)'-';
			buffer[offset + 2] = (byte)' ';
			return buffer;
		}

		private static int FormatDateTimeWithDelimiter(byte[] buffer, int offset, DateTime calendar,
			long additionalFractions, TimestampPrecision precision, char delimeter)
		{
			var localOffset = offset;
			var val = calendar.Year;
			buffer[localOffset++] = (byte)(val / 1000 % 10 + '0');
			buffer[localOffset++] = (byte)(val / 100 % 10 + '0');
			buffer[localOffset++] = (byte)(val / 10 % 10 + '0');
			buffer[localOffset++] = (byte)(val % 10 + '0');
			val = calendar.Month;
			buffer[localOffset++] = (byte)(val / 10 % 10 + '0');
			buffer[localOffset++] = (byte)(val % 10 + '0');
			val = calendar.Day;
			buffer[localOffset++] = (byte)(val / 10 % 10 + '0');
			buffer[localOffset++] = (byte)(val % 10 + '0');
			buffer[localOffset++] = (byte)delimeter;
			val = calendar.Hour;
			buffer[localOffset++] = (byte)(val / 10 % 10 + '0');
			buffer[localOffset++] = (byte)(val % 10 + '0');
			buffer[localOffset++] = (byte)':';
			val = calendar.Minute;
			buffer[localOffset++] = (byte)(val / 10 % 10 + '0');
			buffer[localOffset++] = (byte)(val % 10 + '0');
			buffer[localOffset++] = (byte)':';
			val = calendar.Second;
			buffer[localOffset++] = (byte)(val / 10 % 10 + '0');
			buffer[localOffset++] = (byte)(val % 10 + '0');

			buffer[localOffset++] = (byte)'.';
			val = calendar.Millisecond;
			buffer[localOffset++] = (byte)(val / 100 % 10 + '0');
			buffer[localOffset++] = (byte)(val / 10 % 10 + '0');
			buffer[localOffset++] = (byte)(val % 10 + '0');

			switch (precision)
			{
				case TimestampPrecision.Micro:
				{
					buffer[localOffset++] = (byte)(additionalFractions / 100 % 10 + '0');
					buffer[localOffset++] = (byte)(additionalFractions / 10 % 10 + '0');
					buffer[localOffset++] = (byte)(additionalFractions % 10 + '0');
					break;
				}
				case TimestampPrecision.Nano:
				{
					var microPart = additionalFractions / 1000;
					var nanoPart = additionalFractions % 1000;

					buffer[localOffset++] = (byte)(microPart / 100 % 10 + '0');
					buffer[localOffset++] = (byte)(microPart / 10 % 10 + '0');
					buffer[localOffset++] = (byte)(microPart % 10 + '0');

					buffer[localOffset++] = (byte)(nanoPart / 100 % 10 + '0');
					buffer[localOffset++] = (byte)(nanoPart / 10 % 10 + '0');
					buffer[localOffset++] = (byte)(nanoPart % 10 + '0');
				}
					goto default;
				default:
					break;
			}

			return localOffset;
		}

		public static byte[] FormatBackupStorageTimestamp(byte[] buffer, DateTime calendar, long additionalFractions,
			TimestampPrecision precision)
		{
			//yyMMddHHmmss
			var position = 0;
			var val = calendar.Year;
			buffer[position++] = (byte)(val / 10 % 10 + '0');
			buffer[position++] = (byte)(val % 10 + '0');
			val = calendar.Month;
			buffer[position++] = (byte)(val / 10 % 10 + '0');
			buffer[position++] = (byte)(val % 10 + '0');
			val = calendar.Day;
			buffer[position++] = (byte)(val / 10 % 10 + '0');
			buffer[position++] = (byte)(val % 10 + '0');
			val = calendar.Hour;
			buffer[position++] = (byte)(val / 10 % 10 + '0');
			buffer[position++] = (byte)(val % 10 + '0');
			val = calendar.Minute;
			buffer[position++] = (byte)(val / 10 % 10 + '0');
			buffer[position++] = (byte)(val % 10 + '0');
			val = calendar.Second;
			buffer[position++] = (byte)(val / 10 % 10 + '0');
			buffer[position++] = (byte)(val % 10 + '0');
			val = calendar.Millisecond;
			buffer[position++] = (byte)(val / 100 % 10 + '0');
			buffer[position++] = (byte)(val / 10 % 10 + '0');
			buffer[position++] = (byte)(val % 10 + '0');

			switch (precision)
			{
				case TimestampPrecision.Micro:
				{
					buffer[position++] = (byte)(additionalFractions / 100 % 10 + '0');
					buffer[position++] = (byte)(additionalFractions / 10 % 10 + '0');
					buffer[position++] = (byte)(additionalFractions % 10 + '0');
					break;
				}
				case TimestampPrecision.Nano:
				{
					var microPart = additionalFractions / 1000;
					var nanoPart = additionalFractions % 1000;

					buffer[position++] = (byte)(microPart / 100 % 10 + '0');
					buffer[position++] = (byte)(microPart / 10 % 10 + '0');
					buffer[position++] = (byte)(microPart % 10 + '0');

					buffer[position++] = (byte)(nanoPart / 100 % 10 + '0');
					buffer[position++] = (byte)(nanoPart / 10 % 10 + '0');
					buffer[position++] = (byte)(nanoPart % 10 + '0');

					break;
				}
			}

			return buffer;
		}
	}
}