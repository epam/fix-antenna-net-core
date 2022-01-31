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

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global
{
	internal sealed class PossDupRejectCode
	{

		public static readonly PossDupRejectCode MissingOrgSendTime = new PossDupRejectCode("MissingOrgSendTime", InnerEnum.MissingOrgSendTime, "OriginalSendingTime is missing for PossDup message");
		public static readonly PossDupRejectCode OrgSendTimeAfterSendTime = new PossDupRejectCode("OrgSendTimeAfterSendTime", InnerEnum.OrgSendTimeAfterSendTime, "OriginalSendingTime is after SendingTime");
		public static readonly PossDupRejectCode InvalidSendingTime = new PossDupRejectCode("InvalidSendingTime", InnerEnum.InvalidSendingTime, "invalid OriginalSendingTime");

		private static readonly IList<PossDupRejectCode> ValueList = new List<PossDupRejectCode>();

		static PossDupRejectCode()
		{
			ValueList.Add(MissingOrgSendTime);
			ValueList.Add(OrgSendTimeAfterSendTime);
			ValueList.Add(InvalidSendingTime);
		}

		internal enum InnerEnum
		{
			MissingOrgSendTime,
			OrgSendTimeAfterSendTime,
			InvalidSendingTime
		}

		public readonly InnerEnum InnerEnumValue;
		private readonly string _nameValue;
		private readonly int _ordinalValue;
		private static int _nextOrdinal = 0;

		internal string Value;

		public PossDupRejectCode(string name, InnerEnum innerEnum, string value)
		{
			this.Value = value;

			_nameValue = name;
			_ordinalValue = _nextOrdinal++;
			InnerEnumValue = innerEnum;
		}

		public override string ToString()
		{
			return Value;
		}

		public static IList<PossDupRejectCode> Values()
		{
			return ValueList;
		}

		public int Ordinal()
		{
			return _ordinalValue;
		}

		public static PossDupRejectCode ValueOf(string name)
		{
			foreach (var enumInstance in PossDupRejectCode.ValueList)
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