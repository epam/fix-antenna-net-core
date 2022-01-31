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
using System.Collections;
using System.Collections.Generic;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Helpers;
using static Epam.FixAntenna.NetCore.Message.Rg.RepeatingGroupStorage;

namespace Epam.FixAntenna.NetCore.Message.Rg
{
	/// <summary>
	/// Class for work with repeating group data.
	/// All repeating group data are stored at IndexedStorage, instances of this class is only view for repeating group data.
	/// </summary>
	public sealed class RepeatingGroup : IEnumerable<RepeatingGroup.Entry>
	{
		private readonly List<Entry> _allocatedEntries = new List<Entry>();
		private EntriesArray _entriesArray;
		private RepeatingGroupArray _rgArray;
		private RepeatingGroupStorage _rgStorage;

		private IndexedStorage _storage;
		internal MessageWithGroupDict Dict;
		internal HiddenLeadingTagsArray HiddenLeadingTagsArray;
		internal string MsgType;
		internal int ParentEntryIndex = -1;
		internal int RgId;
		internal int RgIndex;
		internal FixVersionContainer Version;

		/// <summary>
		/// Returns iterator by entries of repeating group </summary>
		/// <returns> iterator </returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary>
		/// Returns iterator by entries of repeating group </summary>
		/// <returns> iterator </returns>
		public IEnumerator<Entry> GetEnumerator()
		{
			return new RepeatingGroupIterator(this);
		}

		internal void Init(int leadingTag, int rgId, int rgIndex, IndexedStorage storage,
			RepeatingGroupStorage rgStorage, int parentEntryIndex, FixVersionContainer version, string msgType)
		{
			_rgStorage = rgStorage;
			LeadingTag = leadingTag;
			RgId = rgId;
			RgIndex = rgIndex;
			_storage = storage;
			ParentEntryIndex = parentEntryIndex;
			Version = version;
			MsgType = msgType;
			Dict = DictionaryHolder.GetDictionary(version).GetMessageDict(msgType);
			_rgArray = rgStorage.RgArrayManager;
			_entriesArray = rgStorage.EntriesArray;
			HiddenLeadingTagsArray = rgStorage.HiddenLeadingTagsArray;
		}

		/// <summary>
		/// Removes passed entry from group </summary>
		/// <param name="entry"> entry for remove </param>
		public void RemoveEntry(Entry entry)
		{
			entry.Deleted = true;
			var repeatingGroup = _rgArray.GetRgArrayById(LeadingTag, RgId);
			//Repeating group can be null in case when we remove all tags from all entries, so there is no need to remove entry from group because this group will not be used anymore
			if (repeatingGroup != null)
			{
				var index = _rgArray.GetIndexByEntryIndex(repeatingGroup, entry.EntryIndex);
				RemoveEntryByIndex(index, entry.EntryIndex, repeatingGroup);
			}
		}

		/// <summary>
		/// Removes passed entry from group and clean message - remove group instance if it is empty </summary>
		/// <param name="entry"> entry for remove </param>
		public void RemoveEntryAndClean(Entry entry)
		{
			RemoveEntry(entry);
			if (Count == 0)
			{
				Remove();
			}
		}

		internal void Clear()
		{
			for (var i = 0; i < _allocatedEntries.Count; i++)
			{
				RepeatingGroupPool.ReturnObj(_allocatedEntries[i]);
			}

			_allocatedEntries.Clear();
			ReleaseNeeded = true;
			LeadingTag = 0;
			_storage = null;
			_rgStorage = null;
			RgId = 0;
			ParentEntryIndex = -1;
			Version = null;
			MsgType = null;
			HiddenLeadingTagsArray = null;
			_entriesArray = null;
			_rgArray = null;
			Dict = null;
			Validation = true;
		}

		/// <summary>
		/// Turn on/off validation </summary>
		/// <value> validation </value>
		public bool Validation { set; get; } = true;

		/// <summary>
		/// Returns group to pool if group was got by <see cref="RepeatingGroupPool.GetRepeatingGroup"/>.
		/// If group was got by calling <c>RepeatingGroup AddRepeatingGroup()</c> method or <c>RepeatingGroup GetRepeatingGroup()</c>, then it does nothing.
		/// </summary>
		public void Release()
		{
			if (ReleaseNeeded)
			{
				RepeatingGroupPool.ReturnObj(this);
			}
		}

		/// <summary>
		/// Adds entry to the end of group </summary>
		/// <returns> instance of Entry from RepeatingGroupPool. There is no need to call release for this object. </returns>
		public Entry AddEntry()
		{
			return AddEntry(Count);
		}

		/// <summary>
		/// Adds entry to the specific index at group. </summary>
		/// <param name="index"> number of entry in group. In other words, if adds first entry, then index equals to 0, if adds second entry, then index equals to 1 and so on. </param>
		/// <returns> instance of Entry from RepeatingGroupPool. There is no need to call release for this object. </returns>
		public Entry AddEntry(int index)
		{
			var entry = RepeatingGroupPool.Entry;
			entry.ReleaseNeeded = false;
			_allocatedEntries.Add(entry);
			AddEntry(index, entry);
			return entry;
		}

		/// <summary>
		/// Adds passed entry to the specific index at group.
		/// </summary>
		/// <param name="index"> number of entry in group. In other words, if adds first entry, then index equals to 0, if adds second entry, then index equals to 1 and so on. </param>
		/// <param name="entry"> entry for add </param>
		public void AddEntry(int index, Entry entry)
		{
			if (IsDeleted)
			{
				throw new InvalidOperationException("Group was removed. You should create new group");
			}

			GetLeadingTagValue();

			var entriesCount = Count;
			int entryIndex;
			if (index < 0 || index > entriesCount)
			{
				throw new IndexOutOfRangeException("Invalid index " + index + " for new entry. Current group size " +
													entriesCount);
			}

			if (entriesCount == 0)
			{
				RgIndex = _rgArray.FindNotShowedGroup(LeadingTag);
				_rgStorage.ReAddRgToArray(RgIndex, RgId);
			}

			if (index == entriesCount)
			{
				entryIndex = _rgStorage.GetEntryForCreate(RgIndex, ParentEntryIndex);
			}
			else
			{
				entryIndex = _rgStorage.GetEntryForCreate(RgIndex, ParentEntryIndex, index);
			}

			_entriesArray.SetParentEntryLink(_entriesArray.GetEntry(entryIndex), ParentEntryIndex);
			entry.Init(this, entryIndex, _storage);
		}

		private bool IsDeleted
		{
			get
			{
				var indexInHidedLeadingTags = HiddenLeadingTagsArray.FindInHidedLeadingTagsByEntryOwner(LeadingTag, ParentEntryIndex);
				if (indexInHidedLeadingTags == -1)
				{
					var indexInRgArray = _rgArray.FindRgIndex(LeadingTag, RgId);
					if (indexInRgArray == -1)
					{
						return true;
					}

					return false;
				}

				return false;
			}
		}

		/// <summary>
		/// Removes current group
		/// </summary>
		public void Remove()
		{
			var size = Count;
			for (var i = size - 1; i >= 0; i--)
			{
				RemoveEntry(i);
			}

			RemoveGroup(LeadingTag, RgId);
		}

		private void RemoveGroup(int leadingTag, int rgId)
		{
			HiddenLeadingTagsArray.RemoveFromHidedTags(leadingTag, rgId);
			//If there is parent entry, then remove tag from parent entry
			if (ParentEntryIndex != -1)
			{
				var parentEntry = _rgStorage.Entries[ParentEntryIndex];
				for (var i = EntriesHeaderSize; i < _entriesArray.GetArrayEnd(parentEntry); i += EntriesEntrySize)
				{
					if (_entriesArray.GetEntryTag(parentEntry, i) == leadingTag &&
						_entriesArray.GetEntryLink(parentEntry, i) == rgId)
					{
						for (var j = i; j < i + EntriesEntrySize; j++)
						{
							parentEntry[j] = 0;
						}

						parentEntry[EntriesLastTagPointerIndex] -= EntriesEntrySize;
						break;
					}
				}
			}
		}

		/// <summary>
		/// Removes passed entry from the specific index. </summary>
		/// <param name="index"> number of entry in group. In other words, if adds first entry, then index equals to 0, if adds second entry, then index equals to 1 and so on. </param>
		public void RemoveEntry(int index)
		{
			var entriesCount = Count;
			if (index < 0 || index >= entriesCount)
			{
				throw new IndexOutOfRangeException("Invalid index " + index + " for entry. Current entry size " +
													entriesCount);
			}

			var repeatingGroup = _rgArray.GetRgArrayById(LeadingTag, RgId);
			var entryLink = _rgArray.GetEntryLinkByIndex(repeatingGroup, index);
			RemoveEntryByIndex(index, entryLink, repeatingGroup);
		}

		/// <summary>
		/// Removes passed entry from the specific index. </summary>
		/// <param name="index"> number of entry in group. In other words, if adds first entry, then index equals to 0, if adds second entry, then index equals to 1 and so on. </param>
		public void RemoveEntryAndClean(int index)
		{
			RemoveEntry(index);
			if (Count == 0)
			{
				Remove();
			}
		}

		/// <summary>
		/// Returns value of group's leading tag. </summary>
		/// <returns> value of group's leading tag </returns>
		public int GetLeadingTagValue()
		{
			return _rgStorage.GetLeadingTagValue(LeadingTag, RgId);
		}

		/// <summary>
		/// Returns number of entries in group. </summary>
		/// <value> number of entries in group </value>
		public int Count
		{
			get
			{
				//        int rgIndex = rgArray.findRgIndex(leadingTag, rgId);
				if (RgIndex == -1)
				{
					return 0;
				}

				var repeatingGroup = _rgArray.GetRepeatingGroup(RgIndex);
				if (repeatingGroup == null)
				{
					return 0;
				}

				return _rgArray.GetRgArrayEnd(repeatingGroup) - RgHashHeaderSize;
			}
		}

		/// <summary>
		/// Fills passed entry instance by entry data. </summary>
		/// <param name="index"> number of entry in group. In other words, if adds first entry, then index equals to 0, if adds second entry, then index equals to 1 and so on. </param>
		/// <param name="entry"> entry for fill. </param>
		public void GetEntry(int index, Entry entry)
		{
			var entriesCount = Count;
			if (index < 0 || index > entriesCount)
			{
				throw new IndexOutOfRangeException("Invalid index " + index + " for entry. Current group size " +
													entriesCount);
			}

			var repeatingGroup = _rgArray.GetRgArrayById(LeadingTag, RgId);
			var entryIndex = repeatingGroup[RgHashHeaderSize + index * RgHashEntrySize];
			entry.Init(this, entryIndex, _storage);
		}

		/// <summary>
		/// Returns entry from specific instance. </summary>
		/// <param name="index"> number of entry in group. In other words, if adds first entry, then index equals to 0, if adds second entry, then index equals to 1 and so on. </param>
		/// <returns> instance of Entry from RepeatingGroupPool, filled by entry data. There is no need to call release for this object. </returns>
		public Entry GetEntry(int index)
		{
			var entriesCount = Count;
			if (index < 0 || index > entriesCount - 1)
			{
				throw new IndexOutOfRangeException("Invalid index " + index + " for entry. Current group size " +
													entriesCount);
			}

			Entry entry = null;
			var repeatingGroup = _rgArray.GetRgArrayById(LeadingTag, RgId);
			var entryIndex = repeatingGroup[RgHashHeaderSize + index * RgHashEntrySize];
			for (var i = 0; i < _allocatedEntries.Count; i++)
			{
				entry = _allocatedEntries[i];
				if (entry.EntryIndex == entryIndex)
				{
					break;
				}
			}

			if (entry == null || entry.EntryIndex != entryIndex)
			{
				entry = RepeatingGroupPool.Entry;
				entry.ReleaseNeeded = false;
				_allocatedEntries.Add(entry);
				entry.Init(this, entryIndex, _storage);
			}

			return entry;
		}

		/// <summary>
		/// Returns group's leading tag </summary>
		/// <value> leading tag of group </value>
		public int LeadingTag { get; internal set; }

		private void RemoveEntryByIndex(int rgEntryIndex, int entryLink, int[] repeatingGroup)
		{
			var entry = _entriesArray.GetEntry(entryLink);
			//Remove all tags from entry
			for (var i = _entriesArray.GetArrayEnd(entry); i >= EntriesHeaderSize; i -= EntriesEntrySize)
			{
				//if there is some gap
				if (_entriesArray.GetEntryTag(entry, i) == 0)
				{
					continue;
				}

				var type = entry[i + EntriesType];
				if (type == LinkTypeTag)
				{
					_rgStorage.RemoveRgTagAtIndex(_entriesArray.GetEntryLink(entry, i), ParentEntryIndex);
					UpdateParentEntries(entry, _rgStorage.Entries, _entriesArray.GetEntryLink(entry, i), -1);
				}
				else if (type == LinkTypeRg)
				{
					var subGroup = RepeatingGroupPool.RepeatingGroup;
					_rgStorage.GetRepeatingGroup(_entriesArray.GetEntryTag(entry, i),
						_entriesArray.GetEntryLink(entry, i),
						subGroup);

					subGroup.Remove();
					subGroup.Release();
				}
			}

			//Remove entry link from repeating group array
			_rgArray.RemoveEntryByIndex(repeatingGroup, rgEntryIndex);
			//Remove from hidden tags all leading tags, that owned by removed entry
			HiddenLeadingTagsArray.RemoveFromHidedTagsByEntryLink(entryLink);
			_rgStorage.DecrementLeadingTag(LeadingTag, RgId, GetLeadingTagValue() - 1, ParentEntryIndex);
			if (Count == 0)
			{
				var rgIndex = _rgArray.FindRgIndex(LeadingTag, RgId);
				if (rgIndex != -1)
				{
					_rgStorage.RemoveRepeatingGroup(rgIndex);
				}
			}
		}

		public override string ToString()
		{
			var entriesCount = Count;
			if (entriesCount > 0)
			{
				return StringHelper.NewString(ToByteArray());
			}

			return "";
		}

		public string ToPrintableString()
		{
			var entriesCount = Count;
			if (entriesCount > 0)
			{
				return FixMessagePrintableFormatter.ToPrintableString(ToString());
			}

			return "";
		}

		public byte[] ToByteArray()
		{
			var entriesCount = GetLeadingTagValue();
			if (entriesCount > 0)
			{
				var length = GetIntBytesLength(LeadingTag) + GetIntBytesLength(entriesCount) + 2;
				for (var i = 0; i < Count; i++)
				{
					var repeatingGroup = _rgArray.GetRgArrayById(LeadingTag, RgId);
					var entryIndex = repeatingGroup[RgHashHeaderSize + i * RgHashEntrySize];
					var entry = _entriesArray.GetEntry(entryIndex);
					if (_entriesArray.GetArrayEnd(entry) == EntriesHeaderSize)
					{
						continue;
					}

					length += GetEntryTagsSize(_entriesArray.GetEntry(entryIndex));
				}

				var result = new byte[length];
				var offset = 0;
				offset = WriteTag(result, LeadingTag, offset);
				offset = WriteValue(result, entriesCount, offset);
				for (var i = 0; i < Count; i++)
				{
					var repeatingGroup = _rgArray.GetRgArrayById(LeadingTag, RgId);
					var entry = _rgStorage.Entries[repeatingGroup[RgHashHeaderSize + i * RgHashEntrySize]];
					if (_entriesArray.GetArrayEnd(entry) == EntriesHeaderSize)
					{
						continue;
					}

					var startIndex = GetStartTagLink(entry);
					var endIndex = entry[EntriesLastTagLinkIndex];
					for (var j = startIndex; j <= endIndex && j < _storage.Count; j++)
					{
						offset = WriteTag(result, _storage.GetTagIdAtIndex(j), offset);
						offset = WriteValue(result, _storage.GetTagValueAsBytesAtIndex(j), offset);
					}
				}

				return result;
			}

			return Array.Empty<byte>();
		}

		/// <summary>
		/// Copy entry to end of repeating group </summary>
		/// <param name="src"> entry for copy </param>
		/// <returns> copied entry </returns>
		public Entry CopyEntry(Entry src)
		{
			return CopyEntry(src, Count);
		}

		/// <summary>
		/// Copy entry at specified index of repeating group </summary>
		/// <param name="src"> entry for copy </param>
		/// <param name="index"> index at which the source entry is to be copied </param>
		/// <returns> copied entry </returns>
		public Entry CopyEntry(Entry src, int index)
		{
			var dst = AddEntry(index);
			CopyEntryImpl(src, dst);
			return dst;
		}

		/// <summary>
		/// Copy entry to end of repeating group </summary>
		/// <param name="src"> entry for copy </param>
		/// <param name="dst"> entry for hold copied entry </param>
		public void CopyEntry(Entry src, Entry dst)
		{
			CopyEntry(src, dst, Count);
		}

		/// <summary>
		/// Copy entry at specified index of repeating group </summary>
		/// <param name="src"> entry for copy </param>
		/// <param name="dst"> entry for hold copied entry </param>
		/// <param name="index"> index at which the source entry is to be copied </param>
		public void CopyEntry(Entry src, Entry dst, int index)
		{
			AddEntry(index, dst);
			CopyEntryImpl(src, dst);
		}

		private void CopyEntryImpl(Entry src, Entry dst)
		{
			var tv = new TagValue();
			for (var i = 0; i < src.Count; i++)
			{
				src.LoadTagValueByIndex(i, tv);
				dst.AddTag(tv);
			}
		}

		internal int WriteTag(byte[] result, int tag, int offset)
		{
			var tagBytesLength = GetIntBytesLength(tag);
			offset += tagBytesLength;
			do
			{
				result[--offset] = (byte)(tag % 10 + '0');
			} while ((tag /= 10) > 0);

			offset += tagBytesLength;
			result[offset++] = (byte)'=';
			return offset;
		}

		internal int WriteValue(byte[] result, byte[] value, int offset)
		{
			Array.Copy(value, 0, result, offset, value.Length);
			offset += value.Length;
			result[offset++] = (byte)'\u0001';
			return offset;
		}

		private int WriteValue(byte[] result, int value, int offset)
		{
			var valueLength = GetIntBytesLength(value);
			offset += valueLength;
			do
			{
				result[--offset] = (byte)(value % 10 + '0');
			} while ((value /= 10) > 0);

			offset += valueLength;
			result[offset++] = (byte)'\u0001';
			return offset;
		}

		internal int GetEntryTagsSize(int[] entry)
		{
			if (_entriesArray.GetArrayEnd(entry) == EntriesHeaderSize)
			{
				return 0;
			}

			var startIndex = GetStartTagLink(entry);
			var endIndex = entry[EntriesLastTagLinkIndex];
			var length = 0;
			for (var i = startIndex; i <= endIndex && i < _storage.Count; i++)
			{
				length += _storage.GetTagValueLengthAtIndex(i) + GetIntBytesLength(_storage.GetTagIdAtIndex(i)) +
						2; //2 is soh + '='
			}

			return length;
		}

		private int GetIntBytesLength(int tag)
		{
			var length = 1;
			while ((tag /= 10) > 0)
			{
				length++;
			}

			return length;
		}

		internal void UpdateParentEntries(int[] entry, int[][] entries, int insertedIndex, int offset)
		{
			var parentEntryIndex = ParentEntryIndex;
			while (parentEntryIndex != -1)
			{
				var parentEntry = entries[parentEntryIndex];
				parentEntryIndex = parentEntry[EntriesParentEntryLink];
				parentEntry[EntriesLastTagLinkIndex] += offset;

				var entryEnd = _entriesArray.GetArrayEnd(parentEntry);
				for (var j = EntriesHeaderSize; j < entryEnd; j += EntriesEntrySize)
				{
					var currentLinkIndex = j + EntriesLink;
					if (parentEntry[j + EntriesLink] >= insertedIndex &&
						parentEntry[j + EntriesType] == LinkTypeTag)
					{
						parentEntry[currentLinkIndex] += offset;
						if (parentEntry[EntriesLastTagLinkIndex] < parentEntry[currentLinkIndex])
						{
							//Update last link if needed
							parentEntry[EntriesLastTagLinkIndex] = parentEntry[currentLinkIndex];
						}
					}
				}
			}
		}

		internal int GetStartTagLink(int[] entry)
		{
			var startTagLink = int.MaxValue;
			for (var i = EntriesHeaderSize; i < _entriesArray.GetArrayEnd(entry); i += EntriesEntrySize)
			{
				int tagLink;
				if (_entriesArray.GetEntryType(entry, i) == LinkTypeTag)
				{
					tagLink = _entriesArray.GetEntryLink(entry, i);
				}
				else
				{
					var subRepeatingGroup = _rgArray.GetRgArrayById(_entriesArray.GetEntryTag(entry, i),
						_entriesArray.GetEntryLink(entry, i));
					tagLink = _rgArray.GetRgLeadingTagIndexInFixMsg(subRepeatingGroup);
				}

				if (tagLink < startTagLink)
				{
					startTagLink = tagLink;
				}
			}

			return startTagLink;
		}

		internal RepeatingGroupStorage RepeatingGroupStorage => _rgStorage;

		internal bool ReleaseNeeded { get; set; } = true;

		internal EntriesArray EntriesArray => _entriesArray;

		internal RepeatingGroupArray RepeatingGroupsArray => _rgArray;

		/// <summary>
		/// Class for work with entry data.
		/// All entry data are stored at IndexedStorage, instances of this class is only view for entry data.
		/// </summary>
		public class Entry : EntryImpl, ITagList
		{
			public virtual void ReleaseInstance()
			{
				Release();
			}

			public virtual ITagList Clone()
			{
				throw new NotSupportedException("Entry can't be cloned. It better to use RepeatingGroup.copy()");
			}

			public virtual int GetTagIdAtIndex(int index)
			{
				var size = Count;
				if (index < size && index >= 0)
				{
					var entry = EntriesArray.GetEntry(EntryIndex);
					return Storage.GetTagIdAtIndex(EntriesArray.GetTagLinkAtIndex(entry, index));
				}

				throw new IndexOutOfRangeException("Invalid index for entry with size " + size);
			}

			/// <summary>
			/// Remove entry from group
			/// </summary>
			public virtual void Remove()
			{
				Group.RemoveEntry(this);
			}

			/// <summary>
			/// Remove entry from group and clear message - remove group instance if it is empty
			/// </summary>
			public virtual void RemoveAndClean()
			{
				Remove();
				if (Group.Count == 0)
				{
					Group.Remove();
				}
			}

			/// <summary>
			/// Returns entry to pool if entry was got by <see cref="RepeatingGroupPool.GetEntry"/>.
			/// If entry was got by calling <c>Entry AddEntry()</c> method or <c>Entry GetEntry()</c>, then it does nothing.
			/// </summary>
			public virtual void Release()
			{
				if (ReleaseNeeded)
				{
					RepeatingGroupPool.ReturnObj(this);
				}
			}
		}
	}
}