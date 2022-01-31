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

namespace Epam.FixAntenna.NetCore.FixEngine.iLinkFailover
{
	internal sealed class Fti
	{
		public static readonly Fti Primary = new Fti("PRIMARY", InnerEnum.Primary, "P");
		public static readonly Fti Backup = new Fti("BACKUP", InnerEnum.Backup, "B");
		public static readonly Fti Undefined = new Fti("UNDEFINED", InnerEnum.Undefined, "U");
		public static readonly Fti None = new Fti("NONE", InnerEnum.None, "N");

		private static readonly IList<Fti> valueList = new List<Fti>();

		static Fti()
		{
			valueList.Add(Primary);
			valueList.Add(Backup);
			valueList.Add(Undefined);
			valueList.Add(None);
		}

		internal enum InnerEnum
		{
			Primary,
			Backup,
			Undefined,
			None
		}

		public readonly InnerEnum innerEnumValue;
		private readonly string nameValue;
		private readonly int ordinalValue;
		private static int nextOrdinal = 0;

		private string _value;

		public Fti(string name, InnerEnum innerEnum, string value)
		{
			_value = value;

			nameValue = name;
			ordinalValue = nextOrdinal++;
			innerEnumValue = innerEnum;
		}

		public string GetValue()
		{
			return _value;
		}

		public static IList<Fti> Values()
		{
			return valueList;
		}

		public int Ordinal()
		{
			return ordinalValue;
		}

		public override string ToString()
		{
			return nameValue;
		}

		public static Fti ValueOf(string name)
		{
			foreach (var enumInstance in valueList)
			{
				if (enumInstance.nameValue == name)
				{
					return enumInstance;
				}
			}
			throw new System.ArgumentException(name);
		}
	}
}