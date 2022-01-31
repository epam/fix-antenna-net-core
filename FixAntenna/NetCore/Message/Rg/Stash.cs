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

namespace Epam.FixAntenna.NetCore.Message.Rg
{
	internal class Stash
	{
		public const int InitialSize = 4;
		private const int DelimTagOffset = 0;
		private const int RgPointerOffset = 1;
		private const int StashEntrySize = 2;
		private int _pointer = -StashEntrySize;

		private int[] _stash;

		public virtual void StashValue(int delimTag, int rgPointer)
		{
			if (_stash == null)
			{
				_stash = RepeatingGroupStorageIntArrayPool.GetIntArrayFromPool(InitialSize);
			}

			EnsureStashCapacityAndEnlarge();
			_pointer += StashEntrySize;
			_stash[_pointer + DelimTagOffset] = delimTag;
			_stash[_pointer + RgPointerOffset] = rgPointer;
		}

		public virtual void Unstash()
		{
			_pointer -= StashEntrySize;
		}

		public virtual int GetDelimTag()
		{
			return _stash[_pointer + DelimTagOffset];
		}

		public virtual int GetRgPointer()
		{
			return _stash[_pointer + RgPointerOffset];
		}

		public virtual void UpdatePointers(int oldLink, int newLink)
		{
			if (_pointer >= 0)
			{
				for (var i = 0; i <= _pointer; i += StashEntrySize)
				{
					if (_stash[i + RgPointerOffset] == oldLink)
					{
						_stash[i + RgPointerOffset] = newLink;
					}
				}
			}
		}

		public virtual bool HasRgStash()
		{
			return _pointer != 0;
		}

		public virtual void Copy(Stash src)
		{
			_pointer = src._pointer;
			if (src._stash != null)
			{
				_stash =
					RepeatingGroupStorageIntArrayPool.GetIntArrayFromPool(src._stash
						.Length);
				Array.Copy(src._stash, 0, _stash, 0, src._stash.Length);
			}
			else
			{
				_stash = RepeatingGroupStorageIntArrayPool.GetIntArrayFromPool(InitialSize);
			}
		}

		public virtual void Clear()
		{
			if (_stash != null)
			{
				RepeatingGroupStorageIntArrayPool.ReturnObj(_stash);
				_stash = null;
			}

			_pointer = -StashEntrySize;
		}

		internal virtual void EnsureStashCapacityAndEnlarge()
		{
			if (_pointer + StashEntrySize >= _stash.Length)
			{
				var newStash = RepeatingGroupStorageIntArrayPool.GetIntArrayFromPool(_stash.Length * 2);
				Array.Copy(_stash, 0, newStash, 0, _stash.Length);
				RepeatingGroupStorageIntArrayPool.ReturnObj(_stash);
				_stash = newStash;
			}
		}
	}
}