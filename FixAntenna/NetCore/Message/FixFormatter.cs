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
using System.Collections.Generic;
using System.Text;

namespace Epam.FixAntenna.NetCore.Message
{
	internal class FixFormatter
	{
		internal const int ByteRanges = 25;

		internal readonly StringBuilder StrBuffer = new StringBuilder();

		public List<byte[][]> ByteArrayPool;

		public FixFormatter()
		{
			ByteArrayPool = new List<byte[][]>(10);

			EnsureEntryExists(0);
		}

		internal void EnsureEntryExists(int maxInstance)
		{
			for (var i = ByteArrayPool.Count; i < maxInstance + 1; i++)
			{
				AddByteRange();
			}
		}

		internal void AddByteRange()
		{
			var byteRanges = new byte[ByteRanges][];

			for (var i = 0; i < byteRanges.Length; i++)
			{
				byteRanges[i] = new byte[i];
			}

			ByteArrayPool.Add(byteRanges);
		}

		public byte[] FormatInt(long value, int entryNum = 0, int minLength = 1)
		{
			EnsureEntryExists(entryNum);
			var byteArr = ByteArrayPool[entryNum];
			var valueLength = 1;

			var isNegative = false;
			if (value < 0L)
			{
				isNegative = true;
				value = -value;
				valueLength++;
			}

			var valueCopy = value;
			while ((valueCopy /= 10L) > 0L)
			{
				valueLength++;
			}

			var length = Math.Max(minLength, valueLength);

			var buffer = byteArr[length];

			do
			{
				buffer[--length] = (byte)(value % 10L + '0');
				value /= 10L;
			} while (length >= 1);

			if (isNegative)
			{
				buffer[0] = (byte)'-';
			}

			return buffer;
		}

		public StringBuilder FormatFloat(double value)
		{
			StrBuffer.Append(value.ToString(FixTypes.DecimalPatterns[FixTypes.MaxPrecision],
				FixTypes.UsFormatSymbols));
			return StrBuffer;
		}
	}
}