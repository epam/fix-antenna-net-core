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

namespace Epam.FixAntenna.NetCore.Message.Format
{
	internal class SendingTimeMicro : ISendingTime
	{
		private static int Length = 24;

		private readonly byte[] _sendingTimeBuf = new byte[Length];

		public virtual byte[] CurrentDateValue
		{
			get
			{
				var ticks = DateTimeHelper.CurrentTicks;
				var date = new DateTime(ticks, DateTimeKind.Utc);
				var fractions = ticks / 10 % 1000;

				FixTypes.FormatTimestamp(_sendingTimeBuf, 0, date, fractions, TimestampPrecision.Micro);
				return _sendingTimeBuf;
			}
		}

		public int FormatLength => Length;
	}
}