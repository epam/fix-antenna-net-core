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
using Epam.FixAntenna.NetCore.Message.SpecialTags;

namespace Epam.FixAntenna.NetCore.Message
{
	public sealed class MsgBuf
	{
		public byte[] Buffer;
		public int Length;
		public int Offset;

		public MsgBuf()
		{
			Buffer = Array.Empty<byte>();
		}

		/// <summary>
		/// debugging purposes only
		/// </summary>
		public MsgBuf(byte[] arr)
		{
			Buffer = arr;
			Offset = 0;
			Length = arr.Length;
		}

		public FixMessage FixMessage { get; set; }

		public long MessageReadTimeInTicks { get; set; }

		public override string ToString()
		{
			return $"MsgBuf{{{ToMaskedString()}}}";
		}

		public string ToMaskedString()
		{
			return SpecialFixUtil.GetMaskedString(Buffer, Offset, Length, null, null);
		}
	}
}