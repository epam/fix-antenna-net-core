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

using System.Buffers.Binary;
using System.IO;

namespace Epam.FixAntenna.NetCore.FixEngine.Storage.File
{
	internal static class BigEndianExtensions
	{
		public static long ReadLongBe(this UnmanagedMemoryAccessor accessor, long position)
		{
			var beValue = accessor.ReadInt64(position);
			return BinaryPrimitives.ReverseEndianness(beValue);
		}

		public static void WriteLongBe(this UnmanagedMemoryAccessor accessor, long position, long value)
		{
			accessor.Write(position, BinaryPrimitives.ReverseEndianness(value));
		}

		public static int ReadIntBe(this UnmanagedMemoryAccessor accessor, long position)
		{
			var beValue = accessor.ReadInt32(position);
			return BinaryPrimitives.ReverseEndianness(beValue);
		}

		public static void WriteIntBe(this UnmanagedMemoryAccessor accessor, long position, int value)
		{
			accessor.Write(position, BinaryPrimitives.ReverseEndianness(value));
		}
	}
}
