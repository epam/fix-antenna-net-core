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

namespace Epam.FixAntenna.NetCore.Message
{
	internal class FieldIndex : IFieldIndexData
	{
		public const int Notfound = -1;

		internal const int ItemsPerField = 4;
		private const int Tag = 0;
		private const int Offset = 1;
		private const int Length = 2;
		private const int Flags = 3;

		internal const int FlagMaxAvailInplaceMax = 0x7fff;
		internal const int FlagMaxAvailInplaceMask = 0x7fff0000;
		internal const int FlagMaxAvailInplaceShift = 16;

		internal const int FlagOrigbufStorage = 1;
		internal const int FlagArenaStorage = 2;
		internal const int FlagPerfieldStorage = 4;
		internal const int FlagMaxAvailInplace = 8;
		internal const int FlagPreparedTag = 16;

		internal const int ItemsInHashentry = 2;

		//offset for tag id in hashtbl's block.
		internal const int HashtblTagEntry = 0;

		//offset for value in index array in hashtbl's block.
		internal const int HashtblIndexEntry = 1;

		//Must be power of two
		internal const int HashtableRedundancy = 2;

		internal static readonly int FlagAllStorageBits =
			FlagOrigbufStorage | FlagArenaStorage | FlagPerfieldStorage;

		//Count of blocks in index array
		private int _fieldCount;

		/// <summary>
		/// Each blocks in index array consists from 4 ints:
		/// 1 - tag id
		/// 2 - offset of value
		/// 3 - length of value
		/// 4 - bit set of flags:
		/// 1 - value in original buffer
		/// 2 - value in arena storage
		/// 3 - value in per_field storage
		/// 4 - FLAG_MAX_AVAIL_INPLACE (TODO: add description)
		/// </summary>
		private int[] _index = new int[AbstractFixMessage.InitMaxFields * ItemsPerField];

		/// <summary>
		/// Each block of hashtbl array consists from 2 ints:
		/// 1 - tag id
		/// 2 - position in index array
		/// </summary>
		internal int[] Hashtbl =
			new int[AbstractFixMessage.InitMaxFields * ItemsInHashentry * HashtableRedundancy];

		//Count of blocks in hashtbl array
		internal int HashtblElems;

		public virtual int GetTag(int tagIndex)
		{
			return _index[tagIndex * ItemsPerField + Tag];
		}

		public virtual int GetOffset(int tagIndex)
		{
			return _index[tagIndex * ItemsPerField + Offset];
		}

		public virtual int GetLength(int tagIndex)
		{
			return _index[tagIndex * ItemsPerField + Length];
		}

		public virtual int Count => _fieldCount;

		public virtual int GetIndexCapacity()
		{
			return _index.Length / ItemsPerField;
		}

		public virtual void EnlargeIndex(int ratio)
		{
			if (_fieldCount * ItemsPerField == _index.Length)
			{
				var newArray = new int[_index.Length * ratio];
				Array.Copy(_index, 0, newArray, 0, _index.Length);
				_index = newArray;
			}
		}

		public virtual void EnlargeHastable(int ratio)
		{
			Hashtbl = EnlargeHashtable(Hashtbl, ratio);
		}

		private int[] EnlargeHashtable(int[] tbl, int ratio)
		{
			int[] newTbl;

			if (ratio == 1)
			{
				ClearHashTbl(tbl);
				newTbl = tbl;
			}
			else
			{
				newTbl = new int[tbl.Length * ratio];
			}

			// rehash the table
			for (var i = 0; i < _fieldCount * ItemsPerField; i += ItemsPerField)
			{
				var tag = _index[i + Tag];
				AddToHashTbl(newTbl, tag, i, 0);
			}

			//returnObj( tbl );
			return newTbl;
		}

		private void AddToHashTbl(int tag, int index)
		{
			HashtblElems = AddToHashTbl(Hashtbl, tag, index, HashtblElems);
		}

		private int AddToHashTbl(int[] tbl, int tag, int index, int elems)
		{
			var i = FindElementInHashTbl(tag, tbl);

			if (i != IndexedStorage.NotFound)
			{
				return elems;
			}

			var hashcode = Hash(tbl.Length, tag);

			for (i = hashcode * ItemsInHashentry; i < tbl.Length; i += ItemsInHashentry)
			{
				if (tbl[i + HashtblTagEntry] == 0)
				{
					tbl[i + HashtblTagEntry] = tag;
					tbl[i + HashtblIndexEntry] = index;
					elems++;
					return elems;
				}
			}

			// wrap around and go from start up to this element

			for (i = 0; i < hashcode * ItemsInHashentry; i += ItemsInHashentry)
			{
				if (tbl[i + HashtblTagEntry] == 0)
				{
					tbl[i + HashtblTagEntry] = tag;
					tbl[i + HashtblIndexEntry] = index;
					elems++;
					return elems;
				}
			}

			//TBD! fix error message
			throw new Exception("internal error: hashtable full?");
		}

		private void ClearHashTbl()
		{
			if (HashtblElems > 0)
			{
				for (var i = 0; i < Hashtbl.Length; i += ItemsInHashentry)
				{
					Hashtbl[i + HashtblTagEntry] = 0;
				}
			}

			HashtblElems = 0;
		}

		private void ClearHashTbl(int[] tbl)
		{
			//WHY: hashtblElems? may be need another param for size?
			if (HashtblElems > 0)
			{
				for (var i = 0; i < tbl.Length; i += ItemsInHashentry)
				{
					tbl[i + HashtblTagEntry] = 0;
				}
			}
		}

		public virtual bool IsNeedToEnlarge()
		{
			return _fieldCount * ItemsPerField == _index.Length;
		}

		/// <param name="requiredSize"> </param>
		/// <returns> ratio for enlarging </returns>
		public virtual int IsNeedToEnlarge(int requiredSize)
		{
			if (requiredSize > _index.Length)
			{
				return requiredSize / _index.Length + 1;
			}

			return 0;
		}

		private int FindElementInHashTbl(int tag)
		{
			return FindElementInHashTbl(tag, Hashtbl);
		}

		private int FindElementInHashTbl(int tag, int[] hashtbl)
		{
			var hashcode = Hash(hashtbl.Length, tag);
			for (var i = hashcode * ItemsInHashentry; i < hashtbl.Length; i += ItemsInHashentry)
			{
				var item = hashtbl[i + HashtblTagEntry];
				if (item == tag)
				{
					return i;
				}

				if (item == 0)
				{
					break;
				}
			}

			//let search from index start
			for (var i = 0; i < hashcode * ItemsInHashentry; i += ItemsInHashentry)
			{
				var item = hashtbl[i + HashtblTagEntry];
				if (item == tag)
				{
					return i;
				}

				if (item == 0)
				{
					break;
				}
			}


			return IndexedStorage.NotFound;
		}

		public virtual int FindIndexEntryInHashTbl(int tag)
		{
			return FindIndexEntryInHashTbl(Hashtbl, tag);
		}

		private int FindIndexEntryInHashTbl(int[] tbl, int tag)
		{
			var startPos = Hash(tbl.Length, tag) * ItemsInHashentry;
			for (var i = startPos; i < tbl.Length; i += ItemsInHashentry)
			{
				var item = tbl[i + HashtblTagEntry];
				if (item == tag)
				{
					return tbl[i + HashtblIndexEntry];
				}

				if (item == 0)
				{
					break;
				}
			}

			//let search from index start
			for (var i = 0; i < startPos; i += ItemsInHashentry)
			{
				var item = tbl[i + HashtblTagEntry];
				if (item == tag)
				{
					return tbl[i + HashtblIndexEntry];
				}

				if (item == 0)
				{
					break;
				}
			}

			return IndexedStorage.NotFound;
		}

		private int Hash(int tableLength, int tag)
		{
			return ((tableLength - 1) / ItemsInHashentry) & tag;
		}

		public virtual void ShiftBuffer(int shift)
		{
			var tagOffset = 0;
			for (var i = 0; i < _fieldCount; i++)
			{
				if (IsOriginalMessageStorage(i))
				{
					_index[tagOffset + Offset] += shift;
				}

				tagOffset += ItemsPerField;
			}
		}

		public virtual void ShiftBufferAndChangeStorage(int shift, int storageType)
		{
			var tagOffset = 0;
			for (var i = 0; i < _fieldCount; i++)
			{
				if (IsOriginalMessageStorage(i))
				{
					_index[tagOffset + Offset] += shift;
					var flags = _index[tagOffset + Flags];
					_index[tagOffset + Flags] = (flags & ~FlagAllStorageBits) | storageType;
				}

				tagOffset += ItemsPerField;
			}
		}

		private int GetFlags(int tagIndex)
		{
			return _index[tagIndex * ItemsPerField + Flags];
		}

		public virtual int GetStorageType(int tagIndex)
		{
			return GetFlags(tagIndex) & FlagAllStorageBits;
		}

		public virtual bool IsArenaStorage(int i)
		{
			return GetStorageType(i) == FlagArenaStorage;
		}

		public virtual bool IsPerFieldStorage(int i)
		{
			return GetStorageType(i) == FlagPerfieldStorage;
		}

		public virtual bool IsOriginalMessageStorage(int i)
		{
			return GetStorageType(i) == FlagOrigbufStorage;
		}

		public virtual bool IsPreparedOriginalStorage(int i)
		{
			return (GetFlags(i) & FlagPreparedTag) == FlagPreparedTag;
		}

		public virtual int Add(int tag, int offset, int length, int flag)
		{
			var i = _fieldCount * ItemsPerField;
			_index[i + Tag] = tag;
			_index[i + Offset] = offset;
			_index[i + Length] = length;
			_index[i + Flags] = flag;

			AddToHashTbl(tag, i);

			return _fieldCount++;
		}

		//TBD! make sure that checkTagExistsAtIndex always called with right index
		public virtual void CheckTagExistsAtIndex(int tagIndex)
		{
			if (tagIndex < 0 || _fieldCount <= tagIndex)
			{
				throw new IndexOutOfRangeException("No tag at tagIndex " + tagIndex);
			}
		}

		public virtual void AddAtIndex(int addAtIndex, int tag, int length)
		{
			if (_fieldCount != addAtIndex)
			{
				//isn't new element at the end - check tag index
				CheckTagExistsAtIndex(addAtIndex);
			}

			var tagDataStartPos = addAtIndex * ItemsPerField;
			var tagDataEndPos = tagDataStartPos + ItemsPerField;

			var insertNew = true;
			var reinsert = false;
			var foundPosInHash = FindElementInHashTbl(tag);
			if (foundPosInHash != IndexedStorage.NotFound)
			{
				insertNew = false;
				if (Hashtbl[foundPosInHash + HashtblIndexEntry] >= tagDataStartPos)
				{
					reinsert = true;
				}
			}

			_fieldCount++;

			if (addAtIndex < _fieldCount - 1)
			{
				Array.Copy(_index, tagDataStartPos, _index, tagDataEndPos,
					(_fieldCount - addAtIndex - 1) * ItemsPerField);
			}

			for (var i = tagDataStartPos; i < tagDataEndPos; i++)
			{
				_index[i] = 0;
			}

			_index[tagDataStartPos + Tag] = tag;
			_index[tagDataStartPos + Length] = length;

			// adjust mapping into index array for the index elements greater than the added
			for (var k = 0; k < Hashtbl.Length; k += ItemsInHashentry)
			{
				if (Hashtbl[k + HashtblTagEntry] != 0 && Hashtbl[k + HashtblIndexEntry] >= tagDataStartPos)
				{
					Hashtbl[k + HashtblIndexEntry] += ItemsPerField;
				}
			}

			if (insertNew)
			{
				AddToHashTbl(tag, tagDataStartPos);
			}
			else if (reinsert)
			{
				//remove element
				Hashtbl[foundPosInHash + HashtblTagEntry] = 0;
				AddToHashTbl(tag, tagDataStartPos);
			}
		}

		public virtual void UpdateStorageData(int tagIndex, int storageTypeFlag, int offset, int length)
		{
			var posInIndex = tagIndex * ItemsPerField;
			var flags = _index[posInIndex + Flags];
			if ((flags & storageTypeFlag) == 0)
			{
				flags = (flags & ~FlagAllStorageBits) | storageTypeFlag;
				_index[posInIndex + Flags] = flags;
			}

			_index[posInIndex + Offset] = offset;

			if (length > FlagMaxAvailInplaceMax)
			{
				length = FlagMaxAvailInplaceMax;
			}

			flags = (flags & ~FlagMaxAvailInplaceMask) | FlagMaxAvailInplace |
					(length << FlagMaxAvailInplaceShift);
			_index[posInIndex + Flags] = flags;
		}

		public virtual int GetMaxAvailableInPlace(int index)
		{
			var flags = GetFlags(index);
			var maxAvail = 0;
			if ((flags & FlagMaxAvailInplace) != 0)
			{
				maxAvail = (flags & FlagMaxAvailInplaceMask) >> FlagMaxAvailInplaceShift;
			}

			return maxAvail;
		}

		public virtual int UpdateLength(int tagIndex, int length)
		{
			var posInIndex = tagIndex * ItemsPerField;
			var oldLen = _index[posInIndex + Length];
			_index[posInIndex + Length] = length;
			return oldLen;
		}

		public virtual void Clear()
		{
			ClearHashTbl();
			_fieldCount = 0;
			HashtblElems = 0;
		}

		public virtual int GetTagIndex(int tagId)
		{
			var indexEntryInHashTbl = FindIndexEntryInHashTbl(tagId);
			if (indexEntryInHashTbl == IndexedStorage.NotFound)
			{
				return IndexedStorage.NotFound;
			}

			return indexEntryInHashTbl / ItemsPerField;
		}

		public int GetTagIndex(int tag, int fromIndex, int toIndex)
		{
			for (var i = fromIndex * ItemsPerField; i < toIndex * ItemsPerField; i += ItemsPerField)
			{
				if (_index[i + Tag] == tag)
				{
					return i / ItemsPerField;
				}
			}

			return IndexedStorage.NotFound;
		}

		public int GetTagOccurrenceIndex(int tag, int occurrence)
		{
			var first = GetTagIndex(tag);

			if (occurrence <= 1 || first == IndexedStorage.NotFound)
			{
				return first;
			}

			var counter = 0;
			for (var i = first * ItemsPerField; i < _fieldCount * ItemsPerField; i += ItemsPerField)
			{
				if (_index[i + Tag] == tag)
				{
					counter++;
					if (counter >= occurrence)
					{
						return i / ItemsPerField;
					}
				}
			}

			return -1;
		}

		public int GetTagOccurrenceCount(int tag)
		{
			var i = GetTagIndex(tag);
			if (i == IndexedStorage.NotFound)
			{
				return 0;
			}

			var count = 0;
			for (; i < _fieldCount * ItemsPerField; i += ItemsPerField)
			{
				if (_index[i + Tag] == tag)
				{
					count++;
				}
			}

			return count;
		}

		public virtual bool CheckTagsStorageType(int storageType)
		{
			for (var i = 0; i < _fieldCount * ItemsPerField; i += ItemsPerField)
			{
				if ((_index[i + Flags] & storageType) == 0)
				{
					return false;
				}
			}

			return true;
		}

		public virtual void RemoveElementFromIndex(int tagIndex)
		{
			if (tagIndex < _fieldCount - 1)
			{
				Array.Copy(_index, (tagIndex + 1) * ItemsPerField, _index, tagIndex * ItemsPerField,
					(_fieldCount - tagIndex - 1) * ItemsPerField);
			}

			_fieldCount--;
		}

		public virtual void RemoveFromHashtbl(int tagIndex)
		{
			var indexOffset = tagIndex * ItemsPerField;
			var tagId = _index[indexOffset + Tag];

			var hashIndex = FindElementInHashTbl(tagId);
			if (hashIndex != -1)
			{
				RemoveFromHashtblInternal(tagId, indexOffset);
			}
		}

		//TODO!
		private void RemoveFromHashtblInternal(int tagId, int indexOffset)
		{
			var secondOccuranceIndex = GetTagOccurrenceIndex(tagId, 2);
			var hashIndex = FindElementInHashTbl(tagId);
			var needToRehash = false;
			if (secondOccuranceIndex != IndexedStorage.NotFound)
			{
				Hashtbl[hashIndex + HashtblIndexEntry] = secondOccuranceIndex * ItemsPerField;

				// adjust mapping into index array for the index elements greater than the removed
				for (var j = 0; j < Hashtbl.Length; j += ItemsInHashentry)
				{
					if (Hashtbl[j + HashtblTagEntry] != 0 && Hashtbl[j + HashtblIndexEntry] > indexOffset)
					{
						Hashtbl[j + HashtblIndexEntry] -= ItemsPerField;
					}
				}

				return;
			}

			HashtblElems--;

			if (!needToRehash)
			{
				// check next element - if it have same hashCode - we need rehash
				var size = Hashtbl.Length / ItemsInHashentry;
				var deleted = hashIndex / ItemsInHashentry;
				var next = deleted + 1;
				if (next >= size)
				{
					next = 0;
				}

				if (Hashtbl[next * ItemsInHashentry] != 0)
				{
					needToRehash = true;
				}

				//            // see if this created a gap that we potentially need to close
				//            // consider the sequence of elements: 0, x1, x2, [gap: deleted element], x3, x4, 0
				//            // gap is created if both x2 and x3 were present (not 0)
				//
				//            int size = hashtbl.length / ITEMS_IN_HASHENTRY;
				//            int deleted = hashIndex / ITEMS_IN_HASHENTRY;
				//            int x2 = mod((deleted - 1), size);
				//            int x3 = mod((deleted + 1), size);
				//
				//            if (hashtbl[x2 * ITEMS_IN_HASHENTRY] != 0 && hashtbl[x3 * ITEMS_IN_HASHENTRY] != 0) {
				//                // yes, we created a gap
				//
				//                // TBD!
				//                // An optimal solution would be: to make sure the element addressing is correct in the range from x1 to x4
				//                // and re-hash elements in the range from x4 to x1 (starting from x4 and going to x1)
				//
				//                // ... for now we're going just re-hash the whole table, which is less optimal
				//
				//                needToRehash = true;
				//            }
			}

			if (needToRehash)
			{
				ClearHashTbl(Hashtbl);
				// rehash the table
				for (var i = 0; i < _fieldCount * ItemsPerField; i += ItemsPerField)
				{
					var tag = _index[i + Tag];
					// skip tag that we removing
					if (tag != tagId)
					{
						AddToHashTbl(Hashtbl, tag, i, 0);
					}
				}
			}
			else
			{
				// remove a single element
				// erase the element
				Hashtbl[hashIndex + HashtblTagEntry] = 0;
			}

			// adjust mapping into index array for the index elements greater than the removed
			for (var j = 0; j < Hashtbl.Length; j += ItemsInHashentry)
			{
				if (Hashtbl[j + HashtblTagEntry] != 0 && Hashtbl[j + HashtblIndexEntry] > indexOffset)
				{
					Hashtbl[j + HashtblIndexEntry] -= ItemsPerField;
				}
			}
		}

		internal virtual void RemoveFromHashtbl2(int i, int indexOffset, bool tagNotUnique)
		{
			var needToRehash = tagNotUnique;

			HashtblElems--;

			if (!needToRehash)
			{
				// see if this created a gap that we potentially need to close
				// consider the sequence of elements: 0, x1, x2, [gap: deleted element], x3, x4, 0
				// gap is created if both x2 and x3 were present (not 0)

				var size = Hashtbl.Length / ItemsInHashentry;
				var deleted = i / ItemsInHashentry;
				var x2 = Mod(deleted - 1, size);
				var x3 = Mod(deleted + 1, size);

				if (Hashtbl[x2 * ItemsInHashentry] != 0 && Hashtbl[x3 * ItemsInHashentry] != 0)
				{
					// yes, we created a gap

					// TBD!
					// An optimal solution would be: to make sure the element addressing is correct in the range from x1 to x4
					// and re-hash elements in the range from x4 to x1 (starting from x4 and going to x1)

					// ... for now we're going just re-hash the whole table, which is less optimal

					needToRehash = true;
				}
			}

			if (needToRehash)
			{
				Hashtbl = EnlargeHashtable(Hashtbl, 1);
				return;
			}

			// remove a single element

			// erase the element
			Hashtbl[i + HashtblTagEntry] = 0;

			// adjust mapping into index array for the index elements greater than the removed
			for (var j = 0; j < Hashtbl.Length; j += ItemsInHashentry)
			{
				if (Hashtbl[j + HashtblTagEntry] != 0 && Hashtbl[j + HashtblIndexEntry] > indexOffset)
				{
					Hashtbl[j + HashtblIndexEntry] -= ItemsPerField;
				}
			}
		}

		private int Mod(int x, int size)
		{
			if (x >= 0)
			{
				return x;
			}

			return size + x;
		}

		public override bool Equals(object o)
		{
			return base.Equals(o);
		}

		public override int GetHashCode()
		{
			var result = 0;
			for (var i = 0; i < Count; i++)
			{
				result = 31 * result + _index[i * ItemsPerField + Tag];
				result = 31 * result + _index[i * ItemsPerField + Length];
			}

			return result;
		}

		public virtual void DeepCopyFrom(FieldIndex donor)
		{
			_fieldCount = donor._fieldCount;
			HashtblElems = donor.HashtblElems;

			var donorIndexSize = donor.Count * ItemsPerField;
			var donorHashSize = donor.Hashtbl.Length;
			if (_index.Length < donorIndexSize)
			{
				_index = new int[donorIndexSize];
			}

			if (Hashtbl.Length < donorHashSize)
			{
				Hashtbl = new int[donorHashSize];
			}

			Array.Copy(donor._index, 0, _index, 0, donorIndexSize);
			Hashtbl = EnlargeHashtable(Hashtbl, 1);
		}
	}
}