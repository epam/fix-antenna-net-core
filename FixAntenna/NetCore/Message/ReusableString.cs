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
using System.Linq;
using System.Text;

namespace Epam.FixAntenna.NetCore.Message
{
	internal class ReusableString : ICloneable, IEquatable<string>
	{
		private readonly StringBuilder _sb;

		public ReusableString()
		{
			_sb = new StringBuilder();
		}

		public char this[int index] => _sb[index];

		public object Clone()
		{
			var newObject = new ReusableString();
			newObject._sb.Append(_sb);
			return newObject;
		}

		public bool Equals(string other)
		{
			return other != null && other.Equals(_sb.ToString(), StringComparison.InvariantCulture);
		}

		public override int GetHashCode()
		{
			var h = 0;
			if (_sb.Length > 0)
			{
				for (var i = 0; i < _sb.Length; i++)
				{
					h = 31 * h + _sb[i];
				}
			}

			return h;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}

			if (obj is string strObj)
			{
				return Equals(strObj);
			}

			if (obj is IEnumerable<char> charSequenceObj)
			{
				return charSequenceObj.SequenceEqual(_sb.ToString());
			}

			return false;
		}

		public override string ToString()
		{
			return _sb.ToString();
		}

		public virtual void SetLength(int newLength)
		{
			_sb.Length = newLength;
		}

		public virtual void SetCharAt(int index, char ch)
		{
			_sb[index] = ch;
		}
	}
}