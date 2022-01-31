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

namespace Epam.FixAntenna.NetCore.FixEngine.Storage.Queue
{
	internal class MmfPersistentInMemoryQueueUtils
	{
		internal byte[] GetLengthHeader(int length)
		{
			var lengthLength = GetIntByteLength(length);
			var result = new byte[lengthLength + 1];
			result[0] = (byte)lengthLength;
			var started = false;
			var j = 1;
			for (var i = 3; i >= 0; i--)
			{
				var b = unchecked((byte)((length >> (i * 8)) & 0xFF));
				if (b != 0 || started)
				{
					result[j++] = b;
					started = true;
				}
			}

			return result;
		}

		internal int GetIntByteLength(int val)
		{
			for (var i = 3; i >= 0; i--)
			{
				if (((val >> (i * 8)) & 0xFF) > 0)
				{
					return i + 1;
				}
			}

			return 0;
		}
	}
}