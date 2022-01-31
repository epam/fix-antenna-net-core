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

using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Message.Format;

namespace Epam.FixAntenna.NetCore.FixEngine
{
	internal class SerializationContext
	{
		private readonly ISendingTime _dateFormatter;
		private readonly int _minSeqNumLength;

		public SerializationContext() : this(new SendingTimeMilli()) {}

		public SerializationContext(IFixMessageFactory messageFactory)
		{
			IntFormatter = new FixFormatter();
			_dateFormatter = messageFactory.SendingTime;
			_minSeqNumLength = messageFactory.MinSeqNumFieldsLength;
		}

		internal SerializationContext(ISendingTime dateFormatter)
		{
			IntFormatter = new FixFormatter();
			_dateFormatter = dateFormatter;
			_minSeqNumLength = 1;
		}

		public FixFormatter IntFormatter { get; }

		public virtual byte[] CurrentDateValue => _dateFormatter.CurrentDateValue;

		public virtual int FormatLength => _dateFormatter.FormatLength;
	}
}