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
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Storage.Timestamp
{
	internal class StorageTimestampNano : IStorageTimestamp
	{
		private TimeSpan _offset = DateTimeHelper.LocalZoneOffset;

		public virtual void ResetTimeZone(TimeSpan timeZoneOffset)
		{
			_offset = timeZoneOffset;
		}

		public virtual void Format(long ticks, byte[] buffer)
		{
			var fractions = GetFractions(ticks);
			var dto = new DateTimeOffset(ticks + _offset.Ticks, _offset);
			FixTypes.FormatStorageTimestamp(buffer, 0, dto.DateTime, fractions, TimestampPrecision.Nano);
		}

		public virtual int GetFormatLength()
		{
			return 30;
		}

		public virtual byte[] FormatBackup()
		{
			var now = new DateTimeOffset(DateTimeHelper.CurrentTicks + _offset.Ticks, _offset);
			var buffer = new byte[21];
			var fraction = GetFractions(now.Ticks);
			FixTypes.FormatBackupStorageTimestamp(buffer, now.DateTime, fraction, TimestampPrecision.Nano);
			return buffer;
		}

		private static long GetFractions(long ticks)
		{
			return ticks % 10000 * 100;
		}
	}
}