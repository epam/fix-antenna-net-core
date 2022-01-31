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

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	internal sealed class DisconnectReason
	{
		public static readonly DisconnectReason UserRequest = new DisconnectReason("USER_REQUEST", InnerEnum.UserRequest);
		public static readonly DisconnectReason InitConnectionProblem = new DisconnectReason("INIT_CONNECTION_PROBLEM", InnerEnum.InitConnectionProblem);
		public static readonly DisconnectReason InvalidMessage = new DisconnectReason("INVALID_MESSAGE", InnerEnum.InvalidMessage);
		public static readonly DisconnectReason PossibleRrLoop = new DisconnectReason("POSSIBLE_RR_LOOP", InnerEnum.PossibleRrLoop);
		public static readonly DisconnectReason GotSequenceTooLow = new DisconnectReason("GOT_SEQUENCE_TOO_LOW", InnerEnum.GotSequenceTooLow);
		public static readonly DisconnectReason NoAnswer = new DisconnectReason("NO_ANSWER", InnerEnum.NoAnswer);
		public static readonly DisconnectReason Reject = new DisconnectReason("REJECT", InnerEnum.Reject);
		public static readonly DisconnectReason BrokenConnection = new DisconnectReason("BROKEN_CONNECTION", InnerEnum.BrokenConnection);
		public static readonly DisconnectReason ClosedByCounterparty = new DisconnectReason("CLOSED_BY_COUNTERPARTY", InnerEnum.ClosedByCounterparty);
		public static readonly DisconnectReason PossibleMissedReset = new DisconnectReason("POSSIBLE_MISSED_RESET", InnerEnum.PossibleMissedReset);
		public static readonly DisconnectReason Throttling = new DisconnectReason("THROTTLING", InnerEnum.Throttling);

		private static readonly IList<DisconnectReason> ValueList = new List<DisconnectReason>();

		static DisconnectReason()
		{
			ValueList.Add(UserRequest);
			ValueList.Add(InitConnectionProblem);
			ValueList.Add(InvalidMessage);
			ValueList.Add(PossibleRrLoop);
			ValueList.Add(GotSequenceTooLow);
			ValueList.Add(NoAnswer);
			ValueList.Add(Reject);
			ValueList.Add(BrokenConnection);
			ValueList.Add(ClosedByCounterparty);
			ValueList.Add(PossibleMissedReset);
			ValueList.Add(Throttling);
		}

		internal enum InnerEnum
		{
			UserRequest,
			InitConnectionProblem,
			InvalidMessage,
			PossibleRrLoop,
			GotSequenceTooLow,
			NoAnswer,
			Reject,
			BrokenConnection,
			ClosedByCounterparty,
			PossibleMissedReset,
			Throttling
		}

		public readonly InnerEnum InnerEnumValue;
		private readonly string _nameValue;
		private readonly int _ordinalValue;
		private static int _nextOrdinal = 0;

		private DisconnectReason(string name, InnerEnum innerEnum)
		{
			_nameValue = name;
			_ordinalValue = _nextOrdinal++;
			InnerEnumValue = innerEnum;
		}

		public static DisconnectReason GetDefault()
		{
			return UserRequest;
		}

		public static IList<DisconnectReason> Values()
		{
			return ValueList;
		}

		public int Ordinal()
		{
			return _ordinalValue;
		}

		public override string ToString()
		{
			return _nameValue;
		}

		public static DisconnectReason ValueOf(string name)
		{
			foreach (var enumInstance in DisconnectReason.ValueList)
			{
				if (enumInstance._nameValue == name)
				{
					return enumInstance;
				}
			}
			throw new System.ArgumentException(name);
		}
	}

}