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
using System.Diagnostics;

//
// strtod.c
//
// Convert string to double
//
// Copyright (C) 2002 Michael Ringgaard. All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
//
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. Neither the name of the project nor the names of its contributors
//    may be used to endorse or promote products derived from this software
//    without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
// ARE DISCLAIMED.  IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
// OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
// HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
// LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
// OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
// SUCH DAMAGE.
//

namespace Epam.FixAntenna.NetCore.Message.Format
{
	// Sanos is open source under a BSD style license.
	// http://www.jbox.dk/sanos/

	internal class DoubleFormatter
	{
		internal const int DblMinExp = -1021;
		internal const int DblMaxExp = 1024;

		internal const long HugeVal = 0x7ff0000000000000L;

		private static readonly long MaxValueDivide5 = long.MaxValue / 5;

		public static double ParseDouble(byte[] str, int offset, int length)
		{
			double number;
			int exponent;
			bool negative;
			double p10;
			int n;
			int numDigits;
			int numDecimals;
			var limit = offset + length;

			//// Skip leading whitespace
			//while (isspace(*p)) p++;

			// Handle optional sign
			negative = false;
			switch (str[offset])
			{
				case (byte)'-':
					negative = true; // Fall through to increment position
					goto case (byte)'+';
				case (byte)'+':
					offset++;
					break;
			}

			number = 0.0;
			exponent = 0;
			numDigits = 0;
			numDecimals = 0;

			byte ch = 0;

			// Process string of digits
			while (offset < limit)
			{
				ch = str[offset];
				if (!(ch >= (byte)'0' && ch <= (byte)'9'))
				{
					break;
				}

				number = number * 10.0 + (ch - '0');
				offset++;
				numDigits++;
			}

			// Process decimal part
			if (offset < limit && ch == (byte)'.')
			{
				offset++;

				while (offset < limit)
				{
					ch = str[offset];
					if (!(ch >= (byte)'0' && ch <= (byte)'9'))
					{
						break;
					}

					number = number * 10.0 + (ch - '0');
					offset++;
					numDigits++;
					numDecimals++;
				}

				exponent -= numDecimals;
			}

			if (numDigits == 0)
			{
				throw new ArgumentException("Not a number");
			}

			// Correct for sign
			if (negative)
			{
				number = -number;
			}

			// Process an exponent string

			if (offset + 1 < limit && (ch == (byte)'e' || ch == (byte)'E'))
			{
				// Handle optional sign
				negative = false;
				ch = str[++offset];
				switch (ch)
				{
					case (byte)'-':
						negative = true; // Fall through to increment pos
						goto case (byte)'+';
					case (byte)'+':
						offset++;
						break;
				}

				// Process string of digits
				n = 0;
				while (offset < limit)
				{
					ch = str[offset];
					if (!(ch >= (byte)'0' && ch <= (byte)'9'))
					{
						break;
					}

					n = n * 10 + (ch - '0');
					offset++;
				}

				if (negative)
				{
					exponent -= n;
				}
				else
				{
					exponent += n;
				}
			}

			if (exponent < DblMinExp || exponent > DblMaxExp)
			{
				throw new ArgumentException("Not a number");
			}

			// Scale the result
			//p10 = 10.;
			p10 = 1;
			n = exponent;
			if (n < 0)
			{
				n = -n;
			}

			while (n > 0)
			{
				//if ((n & 1) != 0) {
				//    if (exponent < 0) {
				//        number /= p10;
				//    } else {
				//        number *= p10;
				//    }
				//}
				//n >>= 1;
				//p10 *= p10;
				n--;
				p10 *= 10.0;
			}

			if (exponent < 0)
			{
				number /= p10;
			}
			else
			{
				number *= p10;
			}

			if (number == HugeVal)
			{
				throw new ArgumentException("Not a number");
			}

			return number;
		}

		public static int GetFormatLength(double d, int extPrecision)
		{
			return GetRequiredFormattedLength(d, extPrecision);
		}

		public static int GetRequiredFormattedLength(double d, int extPrecision)
		{
			var length = 0;

			var val = BitConverter.DoubleToInt64Bits(d);
			var sign = (int)(long)((ulong)val >> 63);
			var exp = (int)((long)((ulong)val >> 52) & 0x7ff);
			var mantissa = val & ((1L << 52) - 1);
			if (sign != 0)
			{
				length++;
			}

			if (exp == 0 && mantissa == 0)
			{
				length++;
				return length;
			}

			if (exp == 0x7ff)
			{
				if (mantissa == 0)
				{
					length += "Infinity".Length;
				}
				else
				{
					length += "NaN".Length;
				}

				return length;
			}

			if (exp > 0)
			{
				//set the 52 bit (means 1.xxxx) which is missed
				mantissa += 1L << 52;
			}

			var shift = 1023 + 52 - exp;
			if (shift > 0)
			{
				// integer and faction
				if (shift < 53)
				{
					var intValue = mantissa >> shift;
					length += GetIntLength(intValue);
					//sb.append(intValue);
					mantissa -= intValue << shift;
					if (mantissa > 0)
					{
						length++;
						var decimalCount = 0;
						mantissa <<= 1;
						mantissa++;
						var precision = shift + 1;
						long error = 1;

						var value = intValue;
						var decimalPlaces = 0;
						while (mantissa > error)
						{
							// times 5*2 = 10
							mantissa *= 5;
							error *= 5;
							precision--;
							var num = mantissa >> precision;
							value = value * 10 + num;
							if (++decimalCount > extPrecision)
							{
								return length;
							}

							length++;
							mantissa -= num << precision;

							var parsedValue = AsDouble(value, 0, sign != 0, ++decimalPlaces);
							if (parsedValue == d)
							{
								break;
							}
						}
					}

					return length;
				}

				{
					// faction.
					length += 2;
					var decimalCount = 0;
					mantissa <<= 6;
					mantissa += 1 << 5;
					var precision = shift + 6;

					//precision = 16;

					long error = 1 << 5;

					long value = 0;
					var decimalPlaces = 0;
					while (mantissa > error)
					{
						while (mantissa > MaxValueDivide5)
						{
							mantissa = (long)((ulong)mantissa >> 1);
							error = (long)((ulong)(error + 1) >> 1);
							precision--;
						}

						// times 5*2 = 10
						mantissa *= 5;
						error *= 5;
						precision--;
						if (precision >= 64)
						{
							if (++decimalCount > extPrecision)
							{
								return length;
							}

							length++;
							continue;
						}

						var num = (long)((ulong)mantissa >> precision);
						value = value * 10 + num;
						if (++decimalCount > extPrecision)
						{
							return length;
						}

						var c = (byte)('0' + num);
						Debug.Assert(!(c < (byte)'0' || c > (byte)'9'));
						length++;
						mantissa -= num << precision;
						var parsedValue = AsDouble(value, 0, sign != 0, ++decimalPlaces);
						if (parsedValue == d)
						{
							break;
						}
					}

					return length;
				}
			}

			// large number
			mantissa <<= 10;
			var precision1 = -10 - shift;
			var digits = 0;
			while ((precision1 > 53 || mantissa > long.MaxValue >> precision1) && precision1 > 0)
			{
				digits++;
				precision1--;
				var mod = mantissa % 5;
				mantissa /= 5;
				var modDiv = 1;
				while (mantissa < MaxValueDivide5 && precision1 > 1)
				{
					precision1 -= 1;
					mantissa <<= 1;
					modDiv <<= 1;
				}

				mantissa += modDiv * mod / 5;
			}

			var val2 = precision1 > 0 ? mantissa << precision1 : (long)((ulong)mantissa >> -precision1);

			length += GetIntLength(val2);
			for (var i = 0; i < digits; i++)
			{
				length++;
			}

			return length;
		}

		private static int GetIntLength(long val)
		{
			return FixTypes.FormatIntLength(val);
		}

		public static void FormatWithPadding(double value, int precision, int padLength, byte[] buffer, int offset)
		{
			var length = Format(value, precision, buffer, offset);
			var isNegative = value < 0;
			if (padLength > length)
			{
				var zeroFillSize = padLength - length;
				Array.Copy(buffer, offset, buffer, offset + zeroFillSize, length);
				for (var i = 0; i < zeroFillSize + (isNegative ? 1 : 0); i++)
				{
					buffer[offset + i] = (byte)'0';
				}

				if (isNegative)
				{
					buffer[offset] = (byte)'-';
				}
			}
		}

		public static int Format(double d, int extPrecision, byte[] buff, int offset)
		{
			var localOffset = offset;

			var val = BitConverter.DoubleToInt64Bits(d);
			var sign = (int)(long)((ulong)val >> 63);
			var exp = (int)((long)((ulong)val >> 52) & 0x7ff);
			var mantissa = val & ((1L << 52) - 1);
			if (sign != 0)
			{
				buff[localOffset++] = (byte)'-';
			}

			if (exp == 0 && mantissa == 0)
			{
				buff[localOffset++] = (byte)'0';
				return localOffset - offset;
			}

			if (exp == 0x7ff)
			{
				if (mantissa == 0)
				{
					buff[localOffset++] = (byte)'I';
					buff[localOffset++] = (byte)'n';
					buff[localOffset++] = (byte)'f';
					buff[localOffset++] = (byte)'i';
					buff[localOffset++] = (byte)'n';
					buff[localOffset++] = (byte)'i';
					buff[localOffset++] = (byte)'t';
					buff[localOffset++] = (byte)'y';
				}
				else
				{
					buff[localOffset++] = (byte)'N';
					buff[localOffset++] = (byte)'a';
					buff[localOffset++] = (byte)'N';
				}

				return localOffset - offset;
			}

			if (exp > 0)
			{
				//set the 52 bit (means 1.xxxx) which is missed
				mantissa += 1L << 52;
			}

			var shift = 1023 + 52 - exp;
			if (shift > 0)
			{
				// integer and faction
				if (shift < 53)
				{
					var intValue = mantissa >> shift;
					localOffset = FormatInt(intValue, buff, localOffset);
					//sb.append(intValue);
					mantissa -= intValue << shift;
					if (mantissa > 0)
					{
						buff[localOffset++] = (byte)'.';
						var decimalCount = 0;
						mantissa <<= 1;
						mantissa++;
						var precision = shift + 1;
						long error = 1;

						var value = intValue;
						var decimalPlaces = 0;
						while (mantissa > error)
						{
							// times 5*2 = 10
							mantissa *= 5;
							error *= 5;
							precision--;
							var num = mantissa >> precision;
							value = value * 10 + num;
							if (++decimalCount > extPrecision)
							{
								if (num >= 5)
								{
									return RoundUpFormatted(buff, offset, localOffset - offset);
								}

								return CleanFormatted(buff, offset, localOffset - offset);
							}

							buff[localOffset++] = (byte)('0' + num);
							mantissa -= num << precision;

							var parsedValue = AsDouble(value, 0, sign != 0, ++decimalPlaces);
							if (parsedValue == d)
							{
								break;
							}
						}
					}

					return localOffset - offset;
				}

				{
					// faction.
					buff[localOffset++] = (byte)'0';
					buff[localOffset++] = (byte)'.';
					var decimalCount = 0;
					mantissa <<= 6;
					mantissa += 1 << 5;
					var precision = shift + 6;

					//precision = 16;

					long error = 1 << 5;

					long value = 0;
					var decimalPlaces = 0;
					while (mantissa > error)
					{
						while (mantissa > MaxValueDivide5)
						{
							mantissa = (long)((ulong)mantissa >> 1);
							error = (long)((ulong)(error + 1) >> 1);
							precision--;
						}

						// times 5*2 = 10
						mantissa *= 5;
						error *= 5;
						precision--;
						if (precision >= 64)
						{
							if (++decimalCount > extPrecision)
							{
								return CleanFormatted(buff, offset, localOffset - offset);
							}

							buff[localOffset++] = (byte)'0';
							continue;
						}

						var num = (long)((ulong)mantissa >> precision);
						value = value * 10 + num;
						if (++decimalCount > extPrecision)
						{
							if (num >= 5)
							{
								return RoundUpFormatted(buff, offset, localOffset - offset);
							}

							return CleanFormatted(buff, offset, localOffset - offset);
						}

						var c = (byte)('0' + num);
						Debug.Assert(!(c < (byte)'0' || c > (byte)'9'));
						buff[localOffset++] = c;
						mantissa -= num << precision;
						var parsedValue = AsDouble(value, 0, sign != 0, ++decimalPlaces);
						if (parsedValue == d)
						{
							break;
						}
					}

					return localOffset - offset;
				}
			}

			// large number
			mantissa <<= 10;
			var precision1 = -10 - shift;
			var digits = 0;
			while ((precision1 > 53 || mantissa > long.MaxValue >> precision1) && precision1 > 0)
			{
				digits++;
				precision1--;
				var mod = mantissa % 5;
				mantissa /= 5;
				var modDiv = 1;
				while (mantissa < MaxValueDivide5 && precision1 > 1)
				{
					precision1 -= 1;
					mantissa <<= 1;
					modDiv <<= 1;
				}

				mantissa += modDiv * mod / 5;
			}

			var val2 = precision1 > 0 ? mantissa << precision1 : (long)((ulong)mantissa >> -precision1);

			localOffset = FormatInt(val2, buff, localOffset);
			for (var i = 0; i < digits; i++)
			{
				buff[localOffset++] = (byte)'0';
			}

			return localOffset - offset;
		}

		internal static int RoundUpFormatted(byte[] buff, int offset, int len)
		{
			if (buff[offset + len - 1] == (byte)'.')
			{
				buff[offset + len - 1] = (byte)' ';
				return RoundUpFormattedInt(buff, offset, len - 1, 0);
			}

			var val = (byte)(buff[offset + len - 1] - '0' + 1);
			if (val <= 9)
			{
				buff[offset + len - 1] = (byte)('0' + val);
				return len;
			}

			buff[offset + len - 1] = (byte)' ';
			return RoundUpFormatted(buff, offset, len - 1);
		}

		private static int RoundUpFormattedInt(byte[] buff, int offset, int len, int pos)
		{
			if (pos == len)
			{
				Array.Copy(buff, offset, buff, offset + 1, len);
				buff[offset] = (byte)'1';
				return len + 1;
			}


			var val = (byte)(buff[offset + len - pos - 1] - '0' + 1);
			if (val <= 9)
			{
				buff[offset + len - pos - 1] = (byte)('0' + val);
				return len;
			}

			buff[offset + len - pos - 1] = (byte)'0';
			return RoundUpFormattedInt(buff, offset, len, pos + 1);
		}

		internal static int CleanFormatted(byte[] buff, int offset, int len)
		{
			if (buff[offset + len - 1] == (byte)'0')
			{
				buff[offset + len - 1] = (byte)' ';
				return CleanFormatted(buff, offset, len - 1);
			}

			if (buff[offset + len - 1] == (byte)'.')
			{
				buff[offset + len - 1] = (byte)' ';
				return len - 1;
			}

			return len;
		}

		private static int FormatInt(long intValue, byte[] buff, int localOffset)
		{
			return localOffset + FixTypes.FormatInt(intValue, buff, localOffset);
		}

		internal static double AsDouble(long value, int exp, bool negative, int decimalPlaces)
		{
			if (decimalPlaces > 0 && value < long.MaxValue / 2)
			{
				if (value < long.MaxValue / (1L << 32))
				{
					exp -= 32;
					value <<= 32;
				}

				if (value < long.MaxValue / (1L << 16))
				{
					exp -= 16;
					value <<= 16;
				}

				if (value < long.MaxValue / (1L << 8))
				{
					exp -= 8;
					value <<= 8;
				}

				if (value < long.MaxValue / (1L << 4))
				{
					exp -= 4;
					value <<= 4;
				}

				if (value < long.MaxValue / (1L << 2))
				{
					exp -= 2;
					value <<= 2;
				}

				if (value < long.MaxValue / (1L << 1))
				{
					exp -= 1;
					value <<= 1;
				}
			}

			for (; decimalPlaces > 0; decimalPlaces--)
			{
				exp--;
				var mod = value % 5;
				value /= 5;
				var modDiv = 1;
				if (value < long.MaxValue / (1L << 4))
				{
					exp -= 4;
					value <<= 4;
					modDiv <<= 4;
				}

				if (value < long.MaxValue / (1L << 2))
				{
					exp -= 2;
					value <<= 2;
					modDiv <<= 2;
				}

				if (value < long.MaxValue / (1L << 1))
				{
					exp -= 1;
					value <<= 1;
					modDiv <<= 1;
				}

				value += modDiv * mod / 5;
			}

			var d = value * Math.Pow(2, exp);
			return negative ? -d : d;
		}
	}
}