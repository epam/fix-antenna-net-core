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

	internal class RepeatingGroupArray
	{
		private int[][] _rgArray;

		public RepeatingGroupArray(int[][] rgArray)
		{
			_rgArray = rgArray;
		}

		public virtual void SetRgArray(int[][] rgArray)
		{
			_rgArray = rgArray;
		}

		public virtual int GetRgLeadingTag(int rgIndex)
		{
			return _rgArray[rgIndex][RgHashTag];
		}

		public virtual int GetRgLeadingTag(int[] repeatingGroup)
		{
			return repeatingGroup[RgHashTag];
		}

		public virtual void SetRgLeadingTag(int rgIndex, int value)
		{
			_rgArray[rgIndex][RgHashTag] = value;
		}

		public virtual void SetRgLeadingTag(int[] repeatingGroup, int value)
		{
			repeatingGroup[RgHashTag] = value;
		}

		public virtual int GetRgId(int rgIndex)
		{
			return _rgArray[rgIndex][RgHashId];
		}

		public virtual int GetRgId(int[] repeatingGroup)
		{
			return repeatingGroup[RgHashId];
		}

		public virtual void SetRgId(int rgIndex, int value)
		{
			_rgArray[rgIndex][RgHashId] = value;
		}

		public virtual void SetRgArrayEnd(int rgIndex, int value)
		{
			_rgArray[rgIndex][RgHashLastEntryPointer] = value;
		}

		public virtual void SetRgArrayEnd(int[] repeatingGroup, int value)
		{
			repeatingGroup[RgHashLastEntryPointer] = value;
		}

		public virtual void IncrementRgArrayEnd(int rgIndex)
		{
			_rgArray[rgIndex][RgHashLastEntryPointer] += RgHashEntrySize;
		}

		public virtual void IncrementRgArrayEnd(int[] repeatingGroup)
		{
			repeatingGroup[RgHashLastEntryPointer] += RgHashEntrySize;
		}

		public virtual int GetRgArrayEnd(int rgIndex)
		{
			return _rgArray[rgIndex][RgHashLastEntryPointer];
		}

		public virtual int GetRgArrayEnd(int[] repeatingGroup)
		{
			return repeatingGroup[RgHashLastEntryPointer];
		}

		public virtual void SetZeroSize(int rgIndex)
		{
			_rgArray[rgIndex][RgHashLastEntryPointer] = RgHashHeaderSize;
		}

		public virtual void SetZeroSize(int[] repeatingGroup)
		{
			repeatingGroup[RgHashLastEntryPointer] = RgHashHeaderSize;
		}

		public virtual void AddEntry(int rgIndex, int entryLink)
		{
			var repeatingGroup = _rgArray[rgIndex];
			repeatingGroup[GetRgArrayEnd(repeatingGroup)] = entryLink;
			IncrementRgArrayEnd(repeatingGroup);
		}

		public virtual void AddEntry(int[] repeatingGroup, int entryLink)
		{
			repeatingGroup[GetRgArrayEnd(repeatingGroup)] = entryLink;
			IncrementRgArrayEnd(repeatingGroup);
		}

		public virtual void AddEntryAtIndex(int[] repeatingGroup, int index, int entryLink)
		{
			var startPos = RgHashHeaderSize + index * RgHashEntrySize;
			var destPos = RgHashHeaderSize + (index + 1) * RgHashEntrySize;
			var size = GetRgArrayEnd(repeatingGroup) - startPos;
			Array.Copy(repeatingGroup, startPos, repeatingGroup, destPos, size);
			SetEntryLink(repeatingGroup, startPos, entryLink);
			IncrementRgArrayEnd(repeatingGroup);
		}

		public virtual int GetLastEntryLink(int rgIndex)
		{
			return _rgArray[rgIndex][_rgArray[rgIndex][RgHashLastEntryPointer] - 1];
		}

		public virtual int GetLastEntryLink(int[] repeatingGroup)
		{
			return repeatingGroup[repeatingGroup[RgHashLastEntryPointer] - 1];
		}

		public virtual int GetEntryLink(int rgIndex, int entryIndex)
		{
			return _rgArray[rgIndex][entryIndex + RgHashEntryLinkIndex];
		}

		public virtual int GetEntryLink(int[] repeatingGroup, int entryIndex)
		{
			return repeatingGroup[entryIndex + RgHashEntryLinkIndex];
		}

		public virtual void SetEntryLink(int rgIndex, int entryIndex, int value)
		{
			_rgArray[rgIndex][entryIndex + RgHashEntryLinkIndex] = value;
		}

		public virtual void SetEntryLink(int[] repeatingGroup, int entryIndex, int value)
		{
			repeatingGroup[entryIndex + RgHashEntryLinkIndex] = value;
		}

		public virtual int GetRgLeadingTagIndexInFixMsg(int rgIndex)
		{
			return GetRgLeadingTagIndexInFixMsg(_rgArray[rgIndex]);
		}

		public virtual int GetRgLeadingTagIndexInFixMsg(int[] repeatingGroup)
		{
			return repeatingGroup[RgHashTagLink];
		}

		public virtual void SetRgLeadingTagIndexInFixMsg(int rgIndex, int value)
		{
			_rgArray[rgIndex][RgHashTagLink] = value;
		}

		public virtual void SetRgLeadingTagIndexInFixMsg(int[] repeatingGroup, int value)
		{
			repeatingGroup[RgHashTagLink] = value;
		}

		public virtual void AddRgLeadingTagIndexInFixMsg(int[] repeatingGroup, int appendValue)
		{
			repeatingGroup[RgHashTagLink] += appendValue;
		}

		public virtual void AddRgLeadingTagIndexInFixMsg(int rgIndex, int appendValue)
		{
			_rgArray[RgHashTagLink][rgIndex] += appendValue;
		}

		public virtual void SetRgId(int[] repeatingGroup, int value)
		{
			repeatingGroup[RgHashId] = value;
		}

		public virtual int GetParentEntryIndex(int rgIndex)
		{
			return _rgArray[rgIndex][RgHashParentEntryLink];
		}

		public virtual int GetParentEntryIndex(int[] repeatingGroup)
		{
			return repeatingGroup[RgHashParentEntryLink];
		}

		public virtual void SetParentEntryIndex(int rgIndex, int value)
		{
			_rgArray[rgIndex][RgHashParentEntryLink] = value;
		}

		public virtual void SetParentEntryIndex(int[] repeatingGroup, int value)
		{
			repeatingGroup[RgHashParentEntryLink] = value;
		}

		public virtual int GetVirtualLeadingTagIndex(int rgIndex)
		{
			return _rgArray[rgIndex][RgHashVirtualTagLink];
		}

		public virtual int GetVirtualLeadingTagIndex(int[] repeatingGroup)
		{
			return repeatingGroup[RgHashVirtualTagLink];
		}

		public virtual void SetVirtualLeadingTagIndex(int rgIndex, int value)
		{
			_rgArray[rgIndex][RgHashVirtualTagLink] = value;
		}

		public virtual void SetVirtualLeadingTagIndex(int[] repeatingGroup, int value)
		{
			repeatingGroup[RgHashVirtualTagLink] = value;
		}

		/// 
		/// <param name="repeatingGroup"> </param>
		/// <param name="index"> index for entry. For first entry - 0, for second - 1, so on </param>
		/// <returns> link to entry in entries array </returns>
		public virtual int GetEntryLinkByIndex(int[] repeatingGroup, int index)
		{
			return repeatingGroup[index * RgHashEntrySize + RgHashHeaderSize];
		}

		public virtual void RemoveEntryByIndex(int rgIndex, int index)
		{
			RemoveEntryByIndex(_rgArray[rgIndex], index);
		}

		/// 
		/// <param name="rgEntryIndex"> index for entry in repeating group. For first entry index equals to 0, for second - 1 and so on </param>
		/// <returns> pointer to entry in repeating group array. </returns>
		public virtual int GetIndexInRgArray(int rgEntryIndex)
		{
			return rgEntryIndex * RgHashEntrySize + RgHashHeaderSize;
		}

		public virtual void RemoveEntryByIndex(int[] repeatingGroup, int entryIndexInRg)
		{
			var start = GetIndexInRgArray(entryIndexInRg + 1);
			var dest = GetIndexInRgArray(entryIndexInRg);
			int size;
			if (dest == repeatingGroup[RgHashLastEntryPointer])
			{
				size = RgHashEntrySize;
			}
			else
			{
				size = GetRgArrayEnd(repeatingGroup) - dest;
			}

			Array.Copy(repeatingGroup, start, repeatingGroup, dest, size);

			repeatingGroup[repeatingGroup[RgHashLastEntryPointer]] = 0;
			repeatingGroup[RgHashLastEntryPointer] -= RgHashEntrySize;
		}

		public virtual int GetRgIndexByEntryIndex(int entryIndex)
		{
			for (var i = 0; i < _rgArray.Length; i++)
			{
				var repeatingGroup = _rgArray[i];
				if (repeatingGroup != null)
				{
					for (var j = RgHashHeaderSize; j < GetRgArrayEnd(repeatingGroup); j += RgHashEntrySize)
					{
						if (GetEntryLink(repeatingGroup, j) == entryIndex)
						{
							return i;
						}
					}
				}
			}

			return -1;
		}

		public virtual int[] GetRgArrayByEntryIndex(int entryIndex)
		{
			return _rgArray[GetRgIndexByEntryIndex(entryIndex)];
		}

		public virtual int GetIndexByEntryIndex(int[] repeatingGroup, int entryIndex)
		{
			int index;
			int indexInRg;
			if (repeatingGroup == null)
			{
				return -1;
			}

			for (index = 0;
				(indexInRg = index * RgHashEntrySize + RgHashHeaderSize) < repeatingGroup.Length;
				index++)
			{
				if (repeatingGroup[indexInRg + RgHashEntryLinkIndex] == entryIndex)
				{
					break;
				}
			}

			if (indexInRg >= repeatingGroup.Length)
			{
				return -1;
			}

			return index;
		}

		public virtual int[] GetRgArrayById(int tag, int rgId)
		{
			var rgIndex = FindRgIndex(tag, rgId);
			if (rgIndex == -1)
			{
				return null;
			}

			return _rgArray[rgIndex];
		}

		public virtual int GetRgLeadingTagIndexById(int tag, int rgId)
		{
			var rgIndex = FindRgIndex(tag, rgId);
			return GetRgLeadingTagIndexInFixMsg(rgIndex);
		}

		public virtual int FindNotShowedGroup(int leadingTag)
		{
			for (var i = 0; i < _rgArray.Length; i++)
			{
				var repeatingGroup = _rgArray[i];
				if (repeatingGroup == null)
				{
					break;
				}

				if (GetRgLeadingTag(repeatingGroup) == leadingTag && GetRgId(repeatingGroup) == -1)
				{
					return i;
				}
			}

			return IndexedStorage.NotFound;
		}

		public virtual int FindRgIndex(int leadingTag)
		{
			for (var i = 0; i < _rgArray.Length; i++)
			{
				var repeatingGroup = _rgArray[i];
				if (repeatingGroup == null)
				{
					break;
				}

				if (GetRgLeadingTag(repeatingGroup) == leadingTag && GetRgId(repeatingGroup) != -1)
				{
					return i;
				}
			}

			return IndexedStorage.NotFound;
		}

		public virtual int FindRgIndex(int leadingTag, int rgId)
		{
			for (var i = 0; i < _rgArray.Length; i++)
			{
				var repeatingGroup = _rgArray[i];
				if (repeatingGroup == null)
				{
					break;
				}

				if (GetRgLeadingTag(repeatingGroup) == leadingTag && GetRgId(repeatingGroup) == rgId)
				{
					return i;
				}
			}

			return IndexedStorage.NotFound;
		}

		public virtual int[] GetRepeatingGroup(int rgIndex)
		{
			return _rgArray[rgIndex];
		}

		public virtual int GetPrevEntryLink(int[] repeatingGroup, int entryIndex)
		{
			for (var i = RgHashHeaderSize + RgHashEntrySize;
				i < GetRgArrayEnd(repeatingGroup);
				i += RgHashEntrySize)
			{
				if (GetEntryLink(repeatingGroup, i) == entryIndex)
				{
					return GetEntryLink(repeatingGroup, i - RgHashEntrySize);
				}
			}

			return -1;
		}
	}
}