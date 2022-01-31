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

using Epam.FixAntenna.NetCore.Common.Utils;

namespace Epam.FixAntenna.NetCore.Message.Storage
{
	internal class ArenaMessageStorage : ByteBufferStorage, IContinuousMessageStorage
	{
		protected internal const int ArenaStorageInitialSize = 256;
		public const int MaxBytesInArenaStorage = 4 * 1024;

		internal readonly ByteBuffer ArenaStorage = new ByteBuffer(ArenaStorageInitialSize);

		public virtual byte[] Buffer
		{
			get { return ArenaStorage.GetByteArray(); }
		}

		public virtual int Offset
		{
			get { return 0; }
		}

		public virtual int Length
		{
			get { return ArenaStorage.Length; }
		}

		public virtual bool Overflow()
		{
			return ArenaStorage.Offset >= MaxBytesInArenaStorage;
		}

		public virtual int GetOffset()
		{
			return ArenaStorage.Offset;
		}

		public override byte[] GetByteArray(int index)
		{
			return ArenaStorage.GetByteArray();
		}

		public override void ClearAll()
		{
			ArenaStorage.Offset = 0;
		}

		public virtual void Add(byte[] value, int offset, int length)
		{
			ArenaStorage.Add(value, offset, length);
		}

		public override void Add(int tagIndex, byte[] value, int offset, int length)
		{
			Add(value, offset, length);
		}

		protected internal override ByteBuffer GetByteBuffer(int tagIndex)
		{
			return ArenaStorage;
		}

		public virtual void Copy(ArenaMessageStorage srcStorage)
		{
			if (!srcStorage.ArenaStorage.IsEmpty)
			{
				ClearAll();
				ArenaStorage.Add(srcStorage.ArenaStorage.GetByteArray(), 0, srcStorage.ArenaStorage.Offset);
			}
		}

		public override bool IsEmpty
		{
			get { return ArenaStorage.IsEmpty; }
		}
	}
}