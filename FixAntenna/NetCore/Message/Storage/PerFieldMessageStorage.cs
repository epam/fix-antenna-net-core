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

namespace Epam.FixAntenna.NetCore.Message.Storage
{
	internal class PerFieldMessageStorage : ByteBufferStorage
	{
		protected internal const int PerfirldStorageInitialSize = 16;
		protected internal ByteBuffer[] PerFieldStorage;

		public PerFieldMessageStorage(int initialSize)
		{
			PerFieldStorage = new ByteBuffer[initialSize];
		}

		public virtual void Enlarge(int ratio)
		{
			var newStgCache = new ByteBuffer[PerFieldStorage.Length * ratio];
			Array.Copy(PerFieldStorage, 0, newStgCache, 0, PerFieldStorage.Length);
			PerFieldStorage = newStgCache;
		}

		//TBD! do accurate!
		public virtual void Shift(int index, int offset, int filledSize)
		{
			if (index < filledSize - 1)
			{
				Array.Copy(PerFieldStorage, index, PerFieldStorage, index + offset, filledSize - index - 1);
			}

			for (var i = 0; i < offset; i++)
			{
				PerFieldStorage[index + i] = null;
			}
		}

		public virtual void ShiftBack(int index, int offset, int filledSize)
		{
			Array.Copy(PerFieldStorage, index + offset, PerFieldStorage, index, filledSize - index - offset);
		}

		public override byte[] GetByteArray(int index)
		{
			var byteBuffer = PerFieldStorage[index];
			return byteBuffer?.GetByteArray();
		}

		public override void ClearAll()
		{
			for (var i = 0; i < PerFieldStorage.Length; i++)
			{
				Clear(i);
			}
		}

		public virtual void Clear(int index)
		{
			PerFieldStorage[index] = null;
		}

		public override bool IsEmpty
		{
			get
			{
				foreach (var st in PerFieldStorage)
				{
					if (st != null && !st.IsEmpty)
					{
						return false;
					}
				}

				return true;
			}
		}

		public override void Add(int tagIndex, byte[] value, int offset, int length)
		{
			Init(tagIndex);
			PerFieldStorage[tagIndex].Add(value, offset, length);
		}

		public override void SetValue(int tagIndex, long value, int length)
		{
			var stg = PerFieldStorage[tagIndex];
			var offset = stg.Offset;
			stg.Offset = offset + length;
			FixTypes.FormatInt(value, stg.GetByteArray(), offset);
		}

		protected internal override ByteBuffer GetByteBuffer(int tagIndex)
		{
			return PerFieldStorage[tagIndex];
		}

		public virtual void Init(int tagIndex)
		{
			var stg = PerFieldStorage[tagIndex];
			if (stg == null)
			{
				stg = NewPerFieldStorage();
				PerFieldStorage[tagIndex] = stg;
			}
			else
			{
				stg.Offset = 0;
			}
		}

		protected internal virtual ByteBuffer NewPerFieldStorage()
		{
			return new ByteBuffer(PerfirldStorageInitialSize);
		}

		public virtual void Copy(PerFieldMessageStorage srcStorage)
		{
			var srcSize = srcStorage.PerFieldStorage.Length;
			if (PerFieldStorage.Length < srcSize)
			{
				PerFieldStorage = new ByteBuffer[srcSize];
			}

			for (var i = 0; i < srcSize; i++)
			{
				var srcBuffer = srcStorage.PerFieldStorage[i];
				if (srcBuffer != null)
				{
					PerFieldStorage[i] = NewPerFieldStorage();
					PerFieldStorage[i].Add(srcBuffer.GetByteArray(), 0, srcBuffer.Offset);
				}
			}
		}
	}
}