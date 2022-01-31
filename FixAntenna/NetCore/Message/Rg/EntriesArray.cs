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
	using static RepeatingGroupStorage;

	internal class EntriesArray
	{
		private int[][] _entries;

		public EntriesArray(int[][] entries)
		{
			_entries = entries;
		}

		public virtual void SetEntries(int[][] entries)
		{
			_entries = entries;
		}

		public virtual int GetArrayEnd(int entryIndex)
		{
			return _entries[entryIndex][EntriesLastTagPointerIndex];
		}

		public virtual void AddEntry(int[] entry, int tag, int link, int type)
		{
			var entryArrayEnd = entry[EntriesLastTagPointerIndex];
			entry[entryArrayEnd + EntriesTag] = tag;
			entry[entryArrayEnd + EntriesLink] = link;
			entry[entryArrayEnd + EntriesType] = type;
			entry[EntriesLastTagPointerIndex] += EntriesEntrySize;
		}

		public virtual void AddEntryAtIndex(int[] entry, int index, int tag, int link, int type)
		{
			var startPos = index;
			var destPos = index + EntriesEntrySize;
			var size = GetArrayEnd(entry) - index;
			Array.Copy(entry, startPos, entry, destPos, size);
			entry[index + EntriesTag] = tag;
			entry[index + EntriesLink] = link;
			entry[index + EntriesType] = type;
			entry[EntriesLastTagPointerIndex] += EntriesEntrySize;
		}

		public virtual bool IsEmpty(int[] entry)
		{
			return entry[EntriesLastTagPointerIndex] == EntriesHeaderSize;
		}

		public virtual bool IsEmpty(int entryIndex)
		{
			return _entries[entryIndex][EntriesLastTagPointerIndex] == EntriesHeaderSize;
		}

		public virtual int GetArrayEnd(int[] entry)
		{
			return entry[EntriesLastTagPointerIndex];
		}

		public virtual bool HasParent(int[] entry)
		{
			return entry[EntriesParentEntryLink] != -1;
		}

		public virtual int GetLastTagIndexInFixMessage(int entryIndex)
		{
			return GetLastTagIndexInFixMessage(GetEntry(entryIndex));
		}

		public virtual int GetLastTagIndexInFixMessage(int[] entry)
		{
			return entry[EntriesLastTagLinkIndex];
		}

		public virtual void IncrementLastTagIndexInFixMessage(int[] entry)
		{
			entry[EntriesLastTagLinkIndex]++;
		}

		public virtual void SetLastTagIndexInFixMessage(int[] entry, int value)
		{
			entry[EntriesLastTagLinkIndex] = value;
		}

		public virtual int GetParentEntryLink(int[] entry)
		{
			return entry[EntriesParentEntryLink];
		}

		public virtual void SetParentEntryLink(int[] entry, int value)
		{
			entry[EntriesParentEntryLink] = value;
		}

		public virtual int GetEntryTag(int[] entry, int index)
		{
			return entry[index + EntriesTag];
		}

		public virtual void SetEntryTag(int[] entry, int index, int value)
		{
			entry[index + EntriesTag] = value;
		}

		public virtual int GetEntryLink(int[] entry, int index)
		{
			return entry[index + EntriesLink];
		}

		public virtual void SetEntryLink(int[] entry, int index, int value)
		{
			entry[index + EntriesLink] = value;
		}

		public virtual int GetEntryType(int[] entry, int index)
		{
			return entry[index + EntriesType];
		}

		public virtual void SetEntryType(int[] entry, int index, int value)
		{
			entry[index + EntriesType] = value;
		}

		public virtual void SetZeroSize(int[] entry)
		{
			entry[EntriesLastTagPointerIndex] = EntriesHeaderSize;
		}

		public virtual int[] GetParentEntry(int[] entry)
		{
			if (entry[EntriesParentEntryLink] == -1)
			{
				return null;
			}

			return _entries[entry[EntriesParentEntryLink]];
		}

		public virtual int GetTagAtIndex(int[] entry, int index)
		{
			return entry[EntriesHeaderSize + index * EntriesEntrySize + EntriesTag];
		}

		public virtual int GetTagLinkAtIndex(int[] entry, int index)
		{
			return entry[EntriesHeaderSize + index * EntriesEntrySize + EntriesLink];
		}

		public virtual int GetTagTypeAtIndex(int[] entry, int index)
		{
			return entry[EntriesHeaderSize + index * EntriesEntrySize + EntriesType];
		}

		public virtual void ShiftLastTagIndexInFixMessage(int[] entry, int offset)
		{
			entry[EntriesLastTagLinkIndex] += offset;
		}

		public virtual void ShiftEntryLink(int[] entry, int index, int offset)
		{
			entry[index + EntriesLink] += offset;
		}

		public virtual void RemoveTagAtIndex(int[] entry, int index)
		{
			var size = GetArrayEnd(entry) - index;
			Array.Copy(entry, index + EntriesEntrySize, entry, index, size);
			entry[EntriesLastTagPointerIndex] -= EntriesEntrySize;
		}

		public virtual int[] GetEntry(int entryIndex)
		{
			return _entries[entryIndex];
		}
	}
}