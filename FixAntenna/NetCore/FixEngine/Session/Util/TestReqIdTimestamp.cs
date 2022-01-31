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
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.Util
{
	internal sealed class TestReqIdTimestamp
	{
		private byte[] _bytes;
		private long _value;

		public long Value
		{
			get => _value;
			set
			{
				var valueLength = FixTypes.FormatIntLength(value);
				if (_bytes == null || _bytes.Length != valueLength)
				{
					_bytes = new byte[valueLength];
				}

				FixTypes.FormatInt(value, _bytes);
				_value = value;
			}
		}

		public byte[] AsByteArray()
		{
			return _bytes;
		}

		public override string ToString()
		{
			return Convert.ToString(_value);
		}
	}
}