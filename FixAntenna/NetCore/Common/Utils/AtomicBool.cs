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

using System.Threading;

namespace Epam.FixAntenna.NetCore.Common.Utils
{
	internal struct AtomicBool
	{
		private volatile int _value;

		public AtomicBool(bool value = false)
		{
			_value = value? 1: 0;
		}

		public static implicit operator AtomicBool(bool value)
		{
			return new AtomicBool(value);
		}

		public bool ToBoolean()
		{
			return _value != 0;
		}

		public static implicit operator bool(AtomicBool value)
		{
			return value.ToBoolean();
		}

		public bool AtomicExchange(bool value)
		{
			int newValue = value ? 1 : 0;
			if (_value != newValue)
			{
				int oldValue = Interlocked.Exchange(ref _value, newValue);
				return oldValue != 0;
			}
			else
			{
				return value;
			}
		}
	}
}
