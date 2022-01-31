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
using System.Text;

namespace Epam.FixAntenna.Example
{
    internal class MsgType
	{
		public int Headline { get; }
		public int Text { get; }
		private byte[] _value = Array.Empty<byte>();

		public MsgType(int headline, int text, byte[] value)
		{
			Headline = headline;
			Text = text;
			_value = value;
		}

		public bool Equals(byte[] val, int offset, int length)
		{
			if (_value == null)
			{
				return val == null;
			}

			if (val == null)
			{
				throw new ArgumentException("Value is null.", nameof(val));
			}

			if (_value.Length != length)
			{
				return false;
			}

			for (var i = 0; i < length; i++)
			{
				if (_value[i] != val[offset + i])
				{
					return false;
				}
			}

			return true;
		}

		public static MsgType News = new MsgType(148, 58, Encoding.UTF8.GetBytes("B"));
	}
}
