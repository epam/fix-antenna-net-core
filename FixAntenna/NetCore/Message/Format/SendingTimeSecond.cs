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

namespace Epam.FixAntenna.NetCore.Message.Format
{
	internal class SendingTimeSecond : ISendingTime
	{
		private static int Length = 17;

		private readonly IFixDateFormatter _formatter =
			FixDateFormatterFactory.GetFixDateFormatter(FixDateFormatterFactory.FixDateType.UtcTimestampShort);

		private readonly byte[] _sendingTimeBufNoMs = new byte[Length];

		private long _lastSeconds;
		private byte[] _preparedTimeBuf;

		public virtual byte[] CurrentDateValue
		{
			get
			{
				var currentTicks = DateTimeHelper.CurrentTicks;
				var sTime = currentTicks / TimeSpan.TicksPerSecond;

				if (sTime != _lastSeconds)
				{
					_lastSeconds = sTime;
					var date = new DateTimeOffset(currentTicks, TimeSpan.Zero);

					_formatter.Format(date, _sendingTimeBufNoMs, 0);
					_preparedTimeBuf = _sendingTimeBufNoMs;
				}

				return _preparedTimeBuf;
			}
		}

		public int FormatLength => Length;
	}
}