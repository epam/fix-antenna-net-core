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

	internal class HiddenLeadingTagsArray
	{
		private int[] _hiddenLeadingTags;

		public HiddenLeadingTagsArray(int[] hiddenLeadingTags)
		{
			_hiddenLeadingTags = hiddenLeadingTags;
		}

		public virtual void SetHiddenLeadingTags(int[] hiddenLeadingTags)
		{
			_hiddenLeadingTags = hiddenLeadingTags;
		}

		public virtual int ArrayEnd => _hiddenLeadingTags[HidedHeaderArrayEnd];

		public virtual bool IsEmpty => _hiddenLeadingTags[HidedHeaderArrayEnd] == HidedHeaderSize;

		public virtual int GetTag(int index)
		{
			return _hiddenLeadingTags[index + HidedTag];
		}

		public virtual int GetTagLink(int index)
		{
			return _hiddenLeadingTags[index + HidedTagLinkIndex];
		}

		public virtual int GetRgId(int index)
		{
			return _hiddenLeadingTags[index + HidedRgId];
		}

		public virtual int GetEntryLink(int index)
		{
			return _hiddenLeadingTags[index + HidedEntry];
		}

		public virtual int GetTagLinkVirtual(int index)
		{
			return _hiddenLeadingTags[index + HidedTagLinkVirtual];
		}

		public virtual void AddEntry(int leadingTag, int link, int rgId, int entryLink, int linkVirtual)
		{
			var arrayEnd = ArrayEnd;
			_hiddenLeadingTags[arrayEnd + HidedTag] = leadingTag;
			_hiddenLeadingTags[arrayEnd + HidedTagLinkIndex] = link;
			_hiddenLeadingTags[arrayEnd + HidedRgId] = rgId;
			_hiddenLeadingTags[arrayEnd + HidedEntry] = entryLink;
			_hiddenLeadingTags[arrayEnd + HidedTagLinkVirtual] = linkVirtual;
			_hiddenLeadingTags[HidedHeaderArrayEnd] += HidedEntrySize;
		}

		public virtual void ShiftHidedTagLink(int index, int offset)
		{
			_hiddenLeadingTags[index + HidedTagLinkIndex] += offset;
		}

		public virtual void ShiftHidedVirtualTag(int index, int offset)
		{
			_hiddenLeadingTags[index + HidedTagLinkVirtual] += offset;
		}

		public virtual void RemoveFromHidedTags(int leadingTag, int rgId)
		{
			var foundEntryIndex = FindInHidedLeadingTags(leadingTag, rgId);
			if (foundEntryIndex != -1)
			{
				RemoveByIndex(foundEntryIndex);
			}
		}

		private void RemoveByIndex(int index)
		{
			var lastPartOfArray = ArrayEnd - (index + HidedEntrySize);
			if (lastPartOfArray == 0)
			{
				lastPartOfArray = HidedEntrySize;
			}

			Array.Copy(_hiddenLeadingTags, index + HidedEntrySize, _hiddenLeadingTags, index, lastPartOfArray);
			_hiddenLeadingTags[HidedHeaderArrayEnd] -= HidedEntrySize;
		}

		public virtual int FindInHidedLeadingTagsByEntryOwner(int leadingTag, int entryIndex)
		{
			var arrayEnd = ArrayEnd;
			for (var i = HidedHeaderSize; i < arrayEnd; i += HidedEntrySize)
			{
				if (GetTag(i) == leadingTag && GetEntryLink(i) == entryIndex)
				{
					return i;
				}
			}

			return -1;
		}

		public virtual int FindInHidedLeadingTags(int leadingTag, int rgId)
		{
			var arrayEnd = ArrayEnd;
			for (var i = HidedHeaderSize; i < arrayEnd; i += HidedEntrySize)
			{
				if (GetTag(i) == leadingTag && (rgId == -1 || GetRgId(i) == rgId))
				{
					return i;
				}
			}

			return -1;
		}

		public virtual void RemoveFromHidedTagsByEntryLink(int entryLink)
		{
			for (var i = HidedHeaderSize; i < ArrayEnd; i += HidedEntrySize)
			{
				if (GetEntryLink(i) == entryLink)
				{
					RemoveByIndex(i);
				}
			}
		}

		public virtual bool RemoveFromHidedTagsByLeadingTagAndEntry(int leadingTag, int entryLink)
		{
			for (var i = HidedHeaderSize; i < ArrayEnd; i += HidedEntrySize)
			{
				if (leadingTag == GetTag(i) && GetEntryLink(i) == entryLink)
				{
					RemoveByIndex(i);
					return true;
				}
			}

			return false;
		}
	}
}