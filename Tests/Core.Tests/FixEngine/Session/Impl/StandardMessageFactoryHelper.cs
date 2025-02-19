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
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.Impl
{
	internal class StandardMessageFactoryHelper
	{
		public static FixMessage GetFullMessage(params int[] tags)
		{
			var fixMessage = new FixMessage();
			foreach (var tag in tags)
			{
				if (tag == 10)
				{
					fixMessage.AddTag(tag, FixTypes.FormatCheckSum(tag));
				}
				else
				{
					fixMessage.AddTag(tag, Convert.ToString(tag));
				}
			}
			return fixMessage;
		}

		public static void CheckFields(FixMessage message, int[] tags)
		{
			foreach (var tag in tags)
			{
				ClassicAssert.IsNotNull(message.GetTagValueAsString(tag), "Tag " + tag + " is missed");
			}
		}

		public static byte[] GetBytesFromBuffer(ByteBuffer byteBuffer)
		{
			var dest = new byte[byteBuffer.Offset];
			Array.Copy(byteBuffer.GetByteArray(), 0, dest, 0, dest.Length);
			return dest;
		}

		public static byte[] GetBytesFromBuffer(byte[] byteBuffer)
		{
			var len = 0;
			foreach (var b in byteBuffer)
			{
				if (b == 0)
				{
					break;
				}
				len++;
			}
			var dest = new byte[len];
			Array.Copy(byteBuffer, 0, dest, 0, dest.Length);
			return dest;
		}
	}
}