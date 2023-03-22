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

namespace Epam.FixAntenna.NetCore.Common.Utils
{
	internal sealed class HashCodeBuilder
	{
		private int _hashCode;
		private readonly int _mult;

		public HashCodeBuilder(int a, int b)
		{
			_hashCode = a;
			_mult = b;
		}

		public HashCodeBuilder Append(int value)
		{
			unchecked
			{
				_hashCode = _hashCode * _mult + value;
			}
			return this;
		}

		public HashCodeBuilder Append(string value)
		{
			return Append(value?.GetHashCode() ?? 0);
		}

		public HashCodeBuilder Append(object obj)
		{
			return Append(obj?.GetHashCode() ?? 0);
		}

		public int Build()
		{
			return _hashCode;
		}
	}
}
