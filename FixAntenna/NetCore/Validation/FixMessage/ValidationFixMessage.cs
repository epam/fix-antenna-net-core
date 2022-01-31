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

using System.Collections.Generic;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.Validation.FixMessage
{
	internal class ValidationFixMessage : ValidationFixGroup
	{
		private readonly Message.FixMessage _fullFixMessage;

		public ValidationFixMessage(Message.FixMessage fixMessage, IList<ValidationFixGroup> validationFixGroups,
			Message.FixMessage fullFixMessage) : base(fixMessage, validationFixGroups)
		{
			_fullFixMessage = fullFixMessage;
		}

		public virtual TagValue GetTag(int reqTag)
		{
			return _fullFixMessage.GetTag(reqTag);
		}

		public virtual long GetMsgSeqNumber()
		{
			return _fullFixMessage.MsgSeqNumber;
		}

		public virtual int CalculateBodyLength()
		{
			return _fullFixMessage.CalculateBodyLength();
		}

		public virtual Message.FixMessage FullFixMessage => _fullFixMessage;

		public virtual int CalculateChecksum()
		{
			return _fullFixMessage.CalculateChecksum();
		}

		public virtual string GetMsgType()
		{
			return StringHelper.NewString(_fullFixMessage.MsgType);
		}

		public virtual bool ContainsTagInMessageFields(int tag)
		{
			return FixMessage.IsTagExists(tag);
		}

		public virtual bool ContainsTagInAllFields(int tag)
		{
			return _fullFixMessage.IsTagExists(tag);
		}
	}
}