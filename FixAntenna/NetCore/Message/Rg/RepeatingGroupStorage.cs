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
using System.Collections.Generic;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Message.Rg.Exceptions;

namespace Epam.FixAntenna.NetCore.Message.Rg
{
	internal sealed class RepeatingGroupStorage
	{
		//32 is maximum value, 8 is minimum value
		public const int InitialSize = 8;

		public const int RgHashTag = 0; //leading tag of group
		public const int RgHashId = 1; //unique id, used to distinguish groups with the same leading tag
		public const int RgHashTagLink = 2; //link to leading tag at indexed storage
		public const int RgHashLastEntryPointer = 3; //pointer at end of repeating group
		public const int RgHashParentEntryLink = 4; //link to parent entry
		public const int RgHashVirtualTagLink = 5; //virtual tag link from hidden leading tags array
		public const int RgHashHeaderSize = 6;
		public const int RgHashEntryLinkIndex = 0; //pointer at entry in entries array
		public const int RgHashEntrySize = 1;

		public const int EntriesLastTagPointerIndex = 0; //pointer at last tag at entry array
		public const int EntriesLastTagLinkIndex = 1; //link to last tag at storage
		public const int EntriesParentEntryLink = 2;
		public const int EntriesHeaderSize = 3;

		public const int EntriesTag = 0;
		public const int EntriesLink = 1;
		public const int EntriesType = 2;
		public const int EntriesEntrySize = 3;

		public const int HidedHeaderArrayEnd = 0; //link to last entry in hidden leading tags arrays
		public const int HidedHeaderSize = 1;

		public const int HidedTag = 0; //hidden leading tag value

		public const int
			HidedTagLinkIndex =
				1; //hidden leading tag index in fix message. Leading tag will be added at this index.

		public const int HidedRgId = 2; //unique id for leading tag for distinguish few tags with same leading tag
		public const int HidedEntry = 3; //entry, who owns group's leading tag, -1 if group at the top level

		public const int
			HidedTagLinkVirtual = 4; //virtual leading tag link. Used when few groups added at same index.

		public const int HidedEntrySize = 5;

		public const int LinkTypeTag = 0;
		public const int LinkTypeRg = 1;

		private readonly IList<RepeatingGroup> _allocatedGroups = new List<RepeatingGroup>();
		private int _currentEntry;

		/// <summary>
		/// Contains info about entries in format:
		/// Header:
		/// 1. Pointer at end of entry
		/// 2. Maximum tag index in fieldIndex for current entry
		/// Entry:
		/// 1. Tag number
		/// 2. Tag index in fieldIndex or in repeating group array
		/// 3. Type of Link:
		/// 1 = Tag in field index
		/// 2 = Tag in Repeating Group array
		/// </summary>
		private int[][] _entries;

		private EntriesArray _entriesArray;
		private bool _entryCreating;

		/// <summary>
		/// Array with leading tags that doesn't inserted into fix message because have value=0.
		/// It is possible in cases when group just created and entries not yet added and when all entries deleted from the group, but the group is not deleted.
		/// Tags should be removed from this array when leading tag inserts into message or when group is deleted.
		/// </summary>
		private int[] _hiddenLeadingTags;

		private HiddenLeadingTagsArray _hiddenLeadingTagsArray;

		private string _msgType;

		/// <summary>
		/// Contains info about Repeating Groups in format:
		/// Header:
		/// 1. Leading tag
		/// 2. Unique id for distinguish groups with same leading tag
		/// 3. Link to leading tag at indexed storage
		/// 4. Pointer at end of Repeating Group
		/// 5. Link to parent entry
		/// Entry:
		/// 1. Pointer to entry in entries array
		/// </summary>
		private int[][] _rgArray;

		private RepeatingGroupArray _rgArrayManager;

		private int _rgCount, _lastEntryPointer;

		private bool _rgCreating;
		private int _rgId; //Source of unique id

		private Stash _stash = new Stash();

		private IndexedStorage _storage;
		private bool _validation;
		private FixVersionContainer _version;

		public RepeatingGroupStorage(IndexedStorage storage, FixVersion version, string msgType, bool validation) :
			this(storage, FixVersionContainer.GetFixVersionContainer(version), msgType, validation)
		{
		}

		public RepeatingGroupStorage(IndexedStorage storage, FixVersionContainer version, string msgType,
			bool validation)
		{
			_storage = storage;
			_version = version;
			_msgType = msgType;
			_validation = validation;
			_rgArray = RepeatingGroupStorageIntArrayPool.GetTwoDimIntArrayFromPool(InitialSize);
			_entries = RepeatingGroupStorageIntArrayPool.GetTwoDimIntArrayFromPool(InitialSize);
			_hiddenLeadingTags = RepeatingGroupStorageIntArrayPool.GetIntArrayFromPool(InitialSize);
			_hiddenLeadingTags[HidedHeaderArrayEnd] = HidedHeaderSize;
			_currentEntry = -1;
			_lastEntryPointer = -1;
			_rgArrayManager = new RepeatingGroupArray(_rgArray);
			_entriesArray = new EntriesArray(_entries);
			//todo - prevent allocation
			_hiddenLeadingTagsArray = new HiddenLeadingTagsArray(_hiddenLeadingTags);
		}

		private RepeatingGroupStorage()
		{
		}

		public void Init(bool validation)
		{
			if (_version != null && !ReferenceEquals(_msgType, null))
			{
				Init(_version, _msgType, validation);
			}

			throw new InvalidOperationException(
				"Repeating Group storage is not initialized properly. You should call RawFixUtil.indexRepeatingGroup() method before first call addRepeatingGroupAtIndex");
		}

		public void Init(FixVersion version, string msgType, bool validation)
		{
			Init(FixVersionContainer.GetFixVersionContainer(version), msgType, validation);
		}

		public void Init(FixVersionContainer version, string msgType, bool validation)
		{
			_rgArray = RepeatingGroupStorageIntArrayPool.GetTwoDimIntArrayFromPool(InitialSize);
			_entries = RepeatingGroupStorageIntArrayPool.GetTwoDimIntArrayFromPool(InitialSize);
			_hiddenLeadingTags = RepeatingGroupStorageIntArrayPool.GetIntArrayFromPool(InitialSize);
			_hiddenLeadingTags[HidedHeaderArrayEnd] = HidedHeaderSize;
			_currentEntry = -1;
			_lastEntryPointer = -1;
			_version = version;
			_msgType = msgType;
			_validation = validation;
			IsInvalidated = false;
			_rgArrayManager = new RepeatingGroupArray(_rgArray);
			_entriesArray = new EntriesArray(_entries);
			//todo - prevent allocation
			_hiddenLeadingTagsArray = new HiddenLeadingTagsArray(_hiddenLeadingTags);
		}

		public void ClearRepeatingGroupStorage()
		{
			if (!IsInvalidated)
			{
				if (_rgArray != null)
				{
					for (var i = 0; i < _rgArray.Length; i++)
					{
						if (_rgArray[i] != null)
						{
							RepeatingGroupStorageIntArrayPool.ReturnObj(_rgArray[i]);
						}
					}

					RepeatingGroupStorageIntArrayPool.ReturnObj(_rgArray);
					_rgArray = null;

					if (_rgArrayManager != null)
					{
						_rgArrayManager.SetRgArray(null);
					}
				}

				if (_entries != null)
				{
					int[] entry;
					for (var i = 0; i < _entries.Length && (entry = _entries[i]) != null; i++)
					{
						RepeatingGroupStorageIntArrayPool.ReturnObj(entry);
					}

					RepeatingGroupStorageIntArrayPool.ReturnObj(_entries);
					if (_entriesArray != null)
					{
						_entriesArray.SetEntries(null);
					}
				}

				for (var i = 0; i < _allocatedGroups.Count; i++)
				{
					RepeatingGroupPool.ReturnObj(_allocatedGroups[i]);
				}

				if (_hiddenLeadingTags != null)
				{
					RepeatingGroupStorageIntArrayPool.ReturnObj(_hiddenLeadingTags);
					_hiddenLeadingTags = null;
					_hiddenLeadingTagsArray.SetHiddenLeadingTags(null);
				}

				_allocatedGroups.Clear();
				_entries = null;
				_stash.Clear();
				_rgCount = 0;
				_lastEntryPointer = -1;
				_entryCreating = false;
				_rgId = 0;
				_currentEntry = -1;
				_rgCreating = false;
				IsInvalidated = true;
			}
		}

		//Methods for first indexing
		public void StartCreateRg(int leadingTag, int leadingTagIndex, int size, int delimTag)
		{
			EnsureEntriesCapacityAndEnlarge();
			var startEntry = _lastEntryPointer + 1;
			var endEntry = startEntry + size - 1;
			int rgIndex;
			if (_rgCreating)
			{
				rgIndex = AddRgToArray(leadingTagIndex, leadingTag, -1, _currentEntry);
				AddTagToEntry(leadingTag, _rgArrayManager.GetRgId(rgIndex), LinkTypeRg);
				_currentEntry = _lastEntryPointer;
			}
			else
			{
				if (_validation)
				{
					ValidateGroupDuplicate(leadingTag);
				}

				rgIndex = AddRgToArray(leadingTagIndex, leadingTag, -1, -1);
			}

			_rgArrayManager.SetRgLeadingTagIndexInFixMsg(rgIndex, leadingTagIndex);
			_rgArrayManager.SetZeroSize(rgIndex);

			_stash.StashValue(delimTag, rgIndex);

			//Create all entries for group
			for (var i = startEntry; i <= endEntry; i++)
			{
				EnsureRgCapacityAndEnlarge(rgIndex);
				EnsureEntriesCapacityAndEnlarge();
				_rgArrayManager.AddEntry(rgIndex, i);
				_entries[i] = RepeatingGroupStorageIntArrayPool.GetIntArrayFromPool(InitialSize);
				var entry = _entries[i];
				_entriesArray.SetZeroSize(entry);
				//mark entry as unfinished
				entry[_entriesArray.GetArrayEnd(_entries[i])] = -1;
				if (_rgCreating)
				{
					_entriesArray.SetParentEntryLink(entry, _rgArrayManager.GetParentEntryIndex(rgIndex));
				}
				else
				{
					_entriesArray.SetParentEntryLink(entry, -1);
				}
			}

			_lastEntryPointer += size;

			_entryCreating = false;
			_rgCreating = true;
		}

		public void StopCreateRg()
		{
			if (_stash.HasRgStash())
			{
				FinishEntry();
				ValidateRgEntries();
				var repeatingGroup = _rgArray[_stash.GetRgPointer()];
				var lastEntry = _entries[_rgArrayManager.GetLastEntryLink(repeatingGroup)];
				var lastTagLink = _entriesArray.GetLastTagIndexInFixMessage(lastEntry);
				_stash.Unstash();

				var currentRgPointer = _stash.GetRgPointer();
				//Find next unfinished entry
				repeatingGroup = _rgArray[currentRgPointer];
				for (var i = RgHashHeaderSize;
					i < _rgArrayManager.GetRgArrayEnd(repeatingGroup);
					i += RgHashEntrySize)
				{
					var entryPointer = _rgArrayManager.GetEntryLink(repeatingGroup, i);
					var entry = _entries[entryPointer];
					var lastTagPointer = _entriesArray.GetArrayEnd(entry);
					if (entry[lastTagPointer] == -1)
					{
						_currentEntry = entryPointer;
						if (_entriesArray.GetLastTagIndexInFixMessage(entry) < lastTagLink)
						{
							_entriesArray.SetLastTagIndexInFixMessage(entry, lastTagLink);
						}

						break;
					}
				}
			}
			else
			{
				_rgCreating = false;
				FinishEntry();
				ValidateRgEntries();
				_stash.Clear();
				_currentEntry = _lastEntryPointer;
			}
		}

		public void AddTag(int tag, int tagIndex, int counterTag)
		{
			if (tag == _stash.GetDelimTag())
			{
				if (_entryCreating)
				{
					FinishEntry();
				}

				StartEntry(counterTag);
			}

			AddTagToEntry(tag, tagIndex, LinkTypeTag);
		}

		private void AddTagToEntry(int tag, int tagIndex, int type)
		{
			var entry = EnsureEntryCapacityAndEnlarge(_currentEntry);
			ValidateEntry(tag, entry);
			if (type == LinkTypeTag)
			{
				if (tagIndex > _entriesArray.GetLastTagIndexInFixMessage(entry))
				{
					_entriesArray.SetLastTagIndexInFixMessage(entry, tagIndex);
				}
			}

			_entriesArray.AddEntry(entry, tag, tagIndex, type);

			//Mark last entry as unfinished
			entry[_entriesArray.GetArrayEnd(entry)] = -1;
		}

		/// <summary>
		/// Start creating entry.
		/// </summary>
		/// <param name="counterTag"> counter tag </param>
		/// <exception cref="InvalidLeadingTagValueException"> if there is invalid counter tag value in the specified message. </exception>
		private void StartEntry(int counterTag)
		{
			EnsureEntriesCapacityAndEnlarge();
			_entryCreating = true;
			_currentEntry++;
			var entry = _entries[_currentEntry];
			if (entry == null)
			{
				throw new InvalidLeadingTagValueException(counterTag, false, _version, _msgType);
			}

			ValidateLeadingTagValueLess(entry);
			_entriesArray.SetZeroSize(entry);
		}

		private void FinishEntry()
		{
			var endEntryPointer = _entriesArray.GetArrayEnd(_entries[_currentEntry]);
			_entries[_currentEntry][endEntryPointer] = 0;
		}
		//END methods for first indexing

		//Start methods for validation
		public void ValidateLeadingTag(int leadingTag)
		{
			if (!DictionaryHolder.GetDictionary(_version).GetMessageDict(_msgType).GetOuterLeadingTags()
				.Contains(leadingTag))
			{
				throw new InvalidLeadingTagException(leadingTag, _version, _msgType);
			}
		}

		public void ValidateGroupDuplicate(int leadingTag)
		{
			var indexInHided = _hiddenLeadingTagsArray.FindInHidedLeadingTags(leadingTag, -1);
			var indexInRgArray = _rgArrayManager.FindRgIndex(leadingTag);
			if (indexInHided != -1 || indexInRgArray != -1)
			{
				throw new DuplicateGroupException(leadingTag, _version, _msgType);
			}
		}

		private void ValidateRgEntries()
		{
			if (_validation)
			{
				var repeatingGroup = _rgArray[_stash.GetRgPointer()];
				for (var i = RgHashHeaderSize;
					i < _rgArrayManager.GetRgArrayEnd(repeatingGroup);
					i += RgHashEntrySize)
				{
					var entry = _entries[_rgArrayManager.GetEntryLink(repeatingGroup, i)];
					if (entry[_entriesArray.GetArrayEnd(entry)] == -1)
					{
						throw new InvalidLeadingTagValueException(_rgArrayManager.GetRgLeadingTag(repeatingGroup), true,
							_version, _msgType);
					}
				}
			}
		}

		private void ValidateLeadingTagValueLess(int[] entry)
		{
			if (_validation)
			{
				if (entry == null || entry[EntriesHeaderSize] != -1)
				{
					var repeatingGroup = _rgArray[_stash.GetRgPointer()];
					throw new InvalidLeadingTagValueException(_rgArrayManager.GetRgLeadingTag(repeatingGroup), false,
						_version, _msgType);
				}
			}
		}

		private void ValidateEntry(int tag, int[] entry)
		{
			if (_validation)
			{
				for (var i = EntriesHeaderSize; i < _entriesArray.GetArrayEnd(entry); i += EntriesEntrySize)
				{
					if (_entriesArray.GetEntryTag(entry, i) == tag)
					{
						throw new DuplicateTagException(_rgArrayManager.GetRgLeadingTag(_stash.GetRgPointer()), tag,
							_version, _msgType);
					}
				}
			}
		}

		//End methods for validation
		internal void IncrementLeadingTag(int newEntriesCount, int leadingTag, int rgId, int parentEntryIndex,
			int updatedEntryIndex, int rgIndex)
		{
			if (newEntriesCount == 1)
			{
				var indexAtHidedTag = _hiddenLeadingTagsArray.FindInHidedLeadingTags(leadingTag, rgId);

				var leadingTagIndexInFixMsg = FindRealLeadingTagIndex(
					_hiddenLeadingTagsArray.GetTagLink(indexAtHidedTag),
					_hiddenLeadingTagsArray.GetTagLinkVirtual(indexAtHidedTag));
				var parentEntry = _hiddenLeadingTagsArray.GetEntryLink(indexAtHidedTag);

				_storage.AddTagAtIndex(leadingTagIndexInFixMsg, leadingTag, newEntriesCount, false);
				Shift(leadingTagIndexInFixMsg, 1, parentEntryIndex, updatedEntryIndex, false);
				if (parentEntry != -1)
				{
					//If there is parent entry, should insert leading tag in this entry
					var entry = EnsureEntryCapacityAndEnlarge(parentEntry);

					var indexAtEntryArray = FindIndexAtEntryArray(leadingTagIndexInFixMsg, entry);
					_entriesArray.AddEntryAtIndex(entry, indexAtEntryArray, leadingTag, rgId, LinkTypeRg);
					UpdateParentEntries(parentEntry, leadingTagIndexInFixMsg, 1);
				}

				FillRg(rgIndex, leadingTagIndexInFixMsg, leadingTag,
					_hiddenLeadingTagsArray.GetTagLinkVirtual(indexAtHidedTag), rgId, parentEntry);
				_hiddenLeadingTagsArray.RemoveFromHidedTags(leadingTag, rgId);
			}
			else
			{
				_storage.UpdateValueAtIndex(_rgArrayManager.GetRgLeadingTagIndexById(leadingTag, rgId),
					newEntriesCount);
			}
		}

		private int FindIndexAtEntryArray(int leadingTagIndexInFixMsg, int[] entry)
		{
			var indexAtEntryArray = EntriesHeaderSize;
			for (; indexAtEntryArray < _entriesArray.GetArrayEnd(entry); indexAtEntryArray += EntriesEntrySize)
			{
				int tagIndexInFixMsg;
				if (_entriesArray.GetEntryType(entry, indexAtEntryArray) == LinkTypeTag)
				{
					tagIndexInFixMsg = _entriesArray.GetEntryLink(entry, indexAtEntryArray);
				}
				else
				{
					var subRgIndex = _entriesArray.GetEntryLink(entry, indexAtEntryArray);
					tagIndexInFixMsg = _rgArrayManager.GetRgLeadingTagIndexInFixMsg(subRgIndex);
				}

				if (tagIndexInFixMsg >= leadingTagIndexInFixMsg)
				{
					break;
				}
			}

			return indexAtEntryArray;
		}

		internal void DecrementLeadingTag(int leadingTag, int rgId, int newEntriesCount, int parentEntryIndex)
		{
			if (newEntriesCount == 0)
			{
				var rgIndex = _rgArrayManager.FindRgIndex(leadingTag, rgId);
				var repeatingGroup = RepeatingGroups[rgIndex];
				var leadingTagIndexInFixMsg = _rgArrayManager.GetRgLeadingTagIndexInFixMsg(repeatingGroup);
				//Move tag from repeating group array to hidden tags
				AddToHidedTags(leadingTag, leadingTagIndexInFixMsg, rgId, parentEntryIndex,
					_rgArrayManager.GetVirtualLeadingTagIndex(repeatingGroup));
				RemoveRepeatingGroup(rgIndex);
				//Remove leading tag from message
				Shift(leadingTagIndexInFixMsg, -1, parentEntryIndex, -1, false);
				_storage.RemoveTagAtIndex(leadingTagIndexInFixMsg, false);

				//if there is parent entry remove leading tag from this entry
				if (parentEntryIndex != -1)
				{
					var entry = Entries[parentEntryIndex];
					for (var i = EntriesHeaderSize; i < _entriesArray.GetArrayEnd(entry); i += EntriesEntrySize)
					{
						if (_entriesArray.GetEntryTag(entry, i) == leadingTag &&
							_entriesArray.GetEntryLink(entry, i) == rgId)
						{
							_entriesArray.RemoveTagAtIndex(entry, i);
							break;
						}
					}

					UpdateParentEntries(parentEntryIndex, leadingTagIndexInFixMsg, -1);

					if (_entriesArray.IsEmpty(parentEntryIndex))
					{
						var parentRepeatingGroup = _rgArrayManager.GetRgArrayByEntryIndex(parentEntryIndex);

						var parentLeadingTag = _rgArrayManager.GetRgLeadingTag(parentRepeatingGroup);
						var parentRgId = _rgArrayManager.GetRgId(parentRepeatingGroup);
						var parentEntry = _rgArrayManager.GetParentEntryIndex(parentRepeatingGroup);
						var newSize = GetLeadingTagValue(parentLeadingTag, parentRgId) - 1;
						DecrementLeadingTag(parentLeadingTag, parentRgId, newSize, parentEntry);
					}
				}
			}
			else if (newEntriesCount > 0)
			{
				_storage.UpdateValueAtIndex(_rgArrayManager.GetRgLeadingTagIndexById(leadingTag, rgId),
					newEntriesCount);
			}
		}

		//Update links in all entries, starts from parentEntryIndex, if tagIndex
		internal void UpdateParentEntries(int parentEntryIndex, int insertedIndex, int offset)
		{
			while (parentEntryIndex != -1)
			{
				var parentEntry = _entriesArray.GetEntry(parentEntryIndex);
				_entriesArray.ShiftLastTagIndexInFixMessage(parentEntry, offset);

				var entryEnd = _entriesArray.GetArrayEnd(parentEntry);
				for (var j = EntriesHeaderSize; j < entryEnd; j += EntriesEntrySize)
				{
					if (_entriesArray.GetEntryLink(parentEntry, j) >= insertedIndex &&
						_entriesArray.GetEntryType(parentEntry, j) == LinkTypeTag)
					{
						//don't update nested repeating groups, because they will be updated in shift method
						_entriesArray.ShiftEntryLink(parentEntry, j, offset);
						if (_entriesArray.GetLastTagIndexInFixMessage(parentEntry) <
							_entriesArray.GetEntryLink(parentEntry, j))
						{
							//Update last link if needed
							_entriesArray.SetLastTagIndexInFixMessage(parentEntry,
								_entriesArray.GetEntryLink(parentEntry, j));
						}
					}
				}

				//Move to next parent
				parentEntryIndex = _entriesArray.GetParentEntryLink(parentEntry);
			}
		}

		public void Shift(int index, int offset, int parentEntryIndex, int updatedEntry,
			bool shouldUpdatedHided)
		{
			int[] entry;
			for (var entryIndex = 0;
				entryIndex < _entries.Length && (entry = _entries[entryIndex]) != null;
				entryIndex++)
			{
				if (_entriesArray.GetLastTagIndexInFixMessage(entry) >= index)
				{
					//Shift the only ones right
					if (!IsNeedUpdate(parentEntryIndex, entryIndex))
					{
						continue;
					}

					var entryEnd = _entriesArray.GetArrayEnd(entry);
					var updated = false;
					for (var j = EntriesHeaderSize; j < entryEnd; j += EntriesEntrySize)
					{
						if (_entriesArray.GetEntryLink(entry, j) >= index &&
							_entriesArray.GetEntryType(entry, j) == LinkTypeTag)
						{
							_entriesArray.ShiftEntryLink(entry, j, offset);
							if (_entriesArray.GetLastTagIndexInFixMessage(entry) < _entriesArray.GetEntryLink(entry, j))
							{
								//Update last link if needed
								_entriesArray.SetLastTagIndexInFixMessage(entry, _entriesArray.GetEntryLink(entry, j));
								updated = true;
							}
						}
					}

					//Not updated but last tag index > index means that there is nested repeating group that affects last tag index, so index should be updated
					if (!updated && _entriesArray.GetLastTagIndexInFixMessage(entry) > index)
					{
						_entriesArray.ShiftLastTagIndexInFixMessage(entry, offset);
					}
				}
			}

			//update repeating group tags link
			for (var rgIndex = 0; rgIndex < _rgArray.Length; rgIndex++)
			{
				var rg = _rgArray[rgIndex];
				if (rg != null && _rgArrayManager.GetRgId(rg) != -1)
				{
					if (_rgArrayManager.GetRgLeadingTagIndexInFixMsg(rg) >= index)
					{
						_rgArrayManager.AddRgLeadingTagIndexInFixMsg(rg, offset);
					}
				}
			}

			if (shouldUpdatedHided)
			{
				if (offset > 0)
				{
					for (var indexInHidedRg = HidedHeaderSize;
						indexInHidedRg < _hiddenLeadingTagsArray.ArrayEnd;
						indexInHidedRg += HidedEntrySize)
					{
						var needUpdate = true;
						var entryIndex = _hiddenLeadingTagsArray.GetEntryLink(indexInHidedRg);
						//if hidden group is nested
						if (entryIndex != -1)
						{
							//There is no need to update hidden leading tag index if hidden leading tag belongs to updated entry (or to any parent of updated entry)
							//It's because we don't support addRepeatingGroupAtIndex in Entry. So group should stay at place, where it was added
							needUpdate = IsNeedUpdateHidedTags(entryIndex, updatedEntry);
						}

						if (_hiddenLeadingTagsArray.GetTagLink(indexInHidedRg) >= index &&
							_hiddenLeadingTagsArray.GetEntryLink(indexInHidedRg) != updatedEntry && needUpdate)
						{
							_hiddenLeadingTagsArray.ShiftHidedTagLink(indexInHidedRg, offset);
						}
					}
				}
				else
				{
					for (var indexInHidedRg = HidedHeaderSize;
						indexInHidedRg < _hiddenLeadingTagsArray.ArrayEnd;
						indexInHidedRg += HidedEntrySize)
					{
						if (_hiddenLeadingTagsArray.GetTagLink(indexInHidedRg) > index)
						{
							_hiddenLeadingTagsArray.ShiftHidedTagLink(indexInHidedRg, offset);
						}
					}
				}
			}
		}

		private bool IsNeedUpdate(int parentEntryIndex, int currentIndex)
		{
			//Don't update parent entries, because they will be updated in updateParentEntries
			var needUpdate = true;
			while (parentEntryIndex != -1)
			{
				if (parentEntryIndex == currentIndex)
				{
					needUpdate = false;
					break;
				}

				var parentEntry = _entries[parentEntryIndex];
				parentEntryIndex = _entriesArray.GetParentEntryLink(parentEntry);
			}

			return needUpdate;
		}

		private bool IsNeedUpdateHidedTags(int hiddenEntryIndex, int updatedEntry)
		{
			var needUpdate = true;
			if (updatedEntry == -1)
			{
				return IsNeedUpdate(hiddenEntryIndex, updatedEntry);
			}

			var commonRgIndex = FindCommonAncestorGroup(hiddenEntryIndex, updatedEntry);
			//If entry, which owns hidden group, belongs to same group as updated entry
			if (commonRgIndex != -1)
			{
				//Find entry number in repeating group
				var hiddenEntryIndexInCommonGroup = FindEntryIndexInCommonGroup(hiddenEntryIndex, commonRgIndex);
				var updatedEntryIndexInCommonGroup = FindEntryIndexInCommonGroup(updatedEntry, commonRgIndex);
				//If hidden group leading tag in the same entry as updated tag or in entry, that lefter then updated entry (include all parents), then it should't updated
				if (hiddenEntryIndexInCommonGroup <= updatedEntryIndexInCommonGroup)
				{
					needUpdate = false;
				}
			}

			return needUpdate;
		}

		private int FindEntryIndexInCommonGroup(int entryIndex, int commonRgIndex)
		{
			var rgIndex = _rgArrayManager.GetRgIndexByEntryIndex(entryIndex);
			while (rgIndex != commonRgIndex)
			{
				entryIndex = _entriesArray.GetParentEntryLink(_entriesArray.GetEntry(entryIndex));
				rgIndex = _rgArrayManager.GetRgIndexByEntryIndex(entryIndex);
			}

			return _rgArrayManager.GetIndexByEntryIndex(_rgArrayManager.GetRepeatingGroup(rgIndex), entryIndex);
		}

		private int FindCommonAncestorGroup(int entry1, int entry2)
		{
			var level1 = FindLevel(entry1);
			var level2 = FindLevel(entry2);

			if (level1 > level2)
			{
				entry1 = GoToLevel(entry1, level1, level2);
			}
			else if (level2 > level1)
			{
				entry2 = GoToLevel(entry2, level2, level1);
			}

			var entry1RgIndex = _rgArrayManager.GetRgIndexByEntryIndex(entry1);
			var entry2RgIndex = _rgArrayManager.GetRgIndexByEntryIndex(entry2);
			while (level1 >= 0)
			{
				if (entry1RgIndex == entry2RgIndex)
				{
					return entry1RgIndex;
				}

				level1--;
				entry1 = _entriesArray.GetParentEntryLink(_entriesArray.GetEntry(entry1));
				entry2 = _entriesArray.GetParentEntryLink(_entriesArray.GetEntry(entry2));
				entry1RgIndex = _rgArrayManager.GetRgIndexByEntryIndex(entry1);
				entry2RgIndex = _rgArrayManager.GetRgIndexByEntryIndex(entry2);
			}

			return -1;
		}

		private int GoToLevel(int entry, int startLevel, int targetLevel)
		{
			while (startLevel > targetLevel)
			{
				startLevel--;
				entry = _entriesArray.GetParentEntryLink(_entriesArray.GetEntry(entry));
			}

			return entry;
		}

		private int FindLevel(int entryLink)
		{
			var level = 0;

			entryLink = _entriesArray.GetParentEntryLink(_entriesArray.GetEntry(entryLink));
			while (entryLink != -1)
			{
				level++;
				entryLink = _entriesArray.GetParentEntryLink(_entriesArray.GetEntry(entryLink));
			}

			return level;
		}

		public bool IsRepeatingGroupExists(int leadingTag)
		{
			var indexInHided = _hiddenLeadingTagsArray.FindInHidedLeadingTags(leadingTag, -1);
			if (indexInHided == -1)
			{
				return _rgArrayManager.FindRgIndex(leadingTag) != -1;
			}

			return true;
		}

		public void GetRepeatingGroup(int tag, RepeatingGroup group)
		{
			group = InitRepeatingGroup(tag, group);
			if (group == null)
			{
				throw new FieldNotFoundException("There is no repeating group with tag " + tag);
			}
		}

		public RepeatingGroup GetRepeatingGroup(int tag)
		{
			RepeatingGroup group = null;
			for (var i = 0; i < _allocatedGroups.Count; i++)
			{
				group = _allocatedGroups[i];
				if (@group.LeadingTag == tag)
				{
					break;
				}
			}

			if (group == null || @group.LeadingTag != tag)
			{
				group = RepeatingGroupPool.RepeatingGroup;
				@group.ReleaseNeeded = false;
				_allocatedGroups.Add(group);
			}

			return InitRepeatingGroup(tag, group);
		}

		private RepeatingGroup InitRepeatingGroup(int tag, RepeatingGroup group)
		{
			var hidedRgIndex = _hiddenLeadingTagsArray.FindInHidedLeadingTags(tag, -1);
			int rgIndex, rgId, parentEntry;

			if (hidedRgIndex == -1)
			{
				rgIndex = _rgArrayManager.FindRgIndex(tag);
				if (rgIndex == -1 || _rgArray[rgIndex] == null)
				{
					return null;
				}

				rgId = _rgArrayManager.GetRgId(rgIndex);
				parentEntry = _rgArrayManager.GetParentEntryIndex(rgIndex);
			}
			else
			{
				rgId = _hiddenLeadingTagsArray.GetRgId(hidedRgIndex);
				rgIndex = _rgArrayManager.FindRgIndex(tag, rgId);
				parentEntry = _hiddenLeadingTagsArray.GetEntryLink(hidedRgIndex);
			}

			group.Init(tag, rgId, rgIndex, _storage, this, parentEntry, _version, _msgType);
			return group;
		}

		public void GetRepeatingGroup(int tag, int rgId, RepeatingGroup rg)
		{
			var rgIndex = _rgArrayManager.FindRgIndex(tag, rgId);
			int parentEntry;

			if (rgIndex == -1 || _rgArray[rgIndex] == null)
			{
				rgIndex = _hiddenLeadingTagsArray.FindInHidedLeadingTags(tag, rgId);
				if (rgIndex == -1)
				{
					throw new Exception("There is no rg with tag " + tag);
				}

				parentEntry = _hiddenLeadingTagsArray.GetEntryLink(rgIndex);
			}
			else
			{
				parentEntry = _rgArrayManager.GetParentEntryIndex(rgIndex);
			}

			rg.Init(tag, rgId, rgIndex, _storage, this, parentEntry, _version, _msgType);
		}

		public RepeatingGroupStorage Copy(IndexedStorage newStorage)
		{
			var copy = new RepeatingGroupStorage();
			copy._rgCount = _rgCount;
			copy._lastEntryPointer = _lastEntryPointer;
			copy._entryCreating = _entryCreating;
			copy._rgId = _rgId;
			copy._currentEntry = _currentEntry;
			copy._rgCreating = _rgCreating;
			copy.IsInvalidated = IsInvalidated;
			copy._version = _version;
			copy._msgType = _msgType;
			copy._validation = _validation;
			copy._stash = new Stash();
			copy._stash.Copy(_stash);
			copy._storage = newStorage;
			copy._rgArray = RepeatingGroupStorageIntArrayPool.GetTwoDimIntArrayFromPool(_rgArray.Length);
			copy._entries = RepeatingGroupStorageIntArrayPool.GetTwoDimIntArrayFromPool(_entries.Length);
			copy._hiddenLeadingTags = RepeatingGroupStorageIntArrayPool.GetIntArrayFromPool(_hiddenLeadingTags.Length);

			for (var rgIndex = 0; rgIndex < _rgArray.Length; rgIndex++)
			{
				if (_rgArray[rgIndex] != null)
				{
					copy._rgArray[rgIndex] =
						RepeatingGroupStorageIntArrayPool.GetIntArrayFromPool(_rgArray[rgIndex].Length);
					Array.Copy(_rgArray[rgIndex], 0, copy._rgArray[rgIndex], 0, _rgArray[rgIndex].Length);
				}
			}

			for (var entryIndex = 0; entryIndex < _entries.Length; entryIndex++)
			{
				if (_entries[entryIndex] != null)
				{
					copy._entries[entryIndex] =
						RepeatingGroupStorageIntArrayPool.GetIntArrayFromPool(_entries[entryIndex].Length);
					Array.Copy(_entries[entryIndex], 0, copy._entries[entryIndex], 0, _entries[entryIndex].Length);
				}
			}

			Array.Copy(_hiddenLeadingTags, 0, copy._hiddenLeadingTags, 0, _hiddenLeadingTags.Length);

			copy._hiddenLeadingTagsArray = new HiddenLeadingTagsArray(copy._hiddenLeadingTags);
			copy._rgArrayManager = new RepeatingGroupArray(copy._rgArray);
			copy._entriesArray = new EntriesArray(copy._entries);

			return copy;
		}

		internal int[][] RepeatingGroups => _rgArray;

		internal void RemoveRepeatingGroup(int rgIndex)
		{
			_rgArrayManager.SetRgId(rgIndex, -1);
			_rgArrayManager.SetRgLeadingTagIndexInFixMsg(rgIndex, -1);
		}

		internal int PrepareAdd(int index, int tagId, int parentEntry, int updatedEntry)
		{
			var fieldCount = _storage.ReserveTagAtIndex(index, tagId, false);
			//        if (index != fieldCount - 1) {
			Shift(index, 1, parentEntry, updatedEntry, true);
			//        }
			return fieldCount;
		}

		public RepeatingGroup AddRepeatingGroup(int indexInFixMessage, int leadingTag, bool validation,
			RepeatingGroup group)
		{
			EnsureRepeatingGroupArrayCapacityAndEnlarge();
			EnsureEntriesCapacityAndEnlarge();
			if (_currentEntry == -1)
			{
				_currentEntry = 0;
			}

			if (_entries[_currentEntry] != null)
			{
				_currentEntry++;
			}

			var parentEntryLink = -1;

			var rgId = _rgId++;
			if (indexInFixMessage == _storage.Count)
			{
				ShiftHidedLeadingTagsLeft(indexInFixMessage);
			}
			else
			{
				ShiftHidedLeadingTagsRight(indexInFixMessage);
			}

			AddToHidedTags(leadingTag, indexInFixMessage, rgId, -1);
			var rgIndex = AddRgToArray(-1, leadingTag, -1, -1, parentEntryLink);

			group.Init(leadingTag, rgId, rgIndex, _storage, this, parentEntryLink, _version, _msgType);
			@group.Validation = validation;
			@group.ReleaseNeeded = false;
			_allocatedGroups.Add(group);
			return group;
		}

		private void ShiftHidedLeadingTagsRight(int indexInFixMessage)
		{
			for (var i = HidedHeaderSize; i < _hiddenLeadingTagsArray.ArrayEnd; i += HidedEntrySize)
			{
				if (_hiddenLeadingTagsArray.GetTagLinkVirtual(i) >= indexInFixMessage)
				{
					_hiddenLeadingTagsArray.ShiftHidedVirtualTag(i, 1);
				}
			}
		}

		private void ShiftHidedLeadingTagsLeft(int indexInFixMessage)
		{
			for (var i = HidedHeaderSize; i < _hiddenLeadingTagsArray.ArrayEnd; i += HidedEntrySize)
			{
				if (_hiddenLeadingTagsArray.GetTagLinkVirtual(i) <= indexInFixMessage)
				{
					_hiddenLeadingTagsArray.ShiftHidedVirtualTag(i, -1);
				}
			}
		}

		internal int FindRealLeadingTagIndex(int desiredLeadingTagIndex, int virtualLeadingTagIndex)
		{
			/*
			We want insert group at desiredLeadingTagIndex. But some other groups already can be inserted at the same index.
			For example:
				msg.addRepeatingGroupAtIndex(1, 123);
				msg.addRepeatingGroupAtIndex(1, 456);

			In message 456 should be before 123. When we add group 456, leading tag index of group 123 was incremented.
			*/
			if (desiredLeadingTagIndex < _storage.Count)
			{
				var tagAtDesiredLeadingTagIndex = _storage.GetTagIdAtIndex(desiredLeadingTagIndex);
				var rgIndex = _rgArrayManager.FindRgIndex(tagAtDesiredLeadingTagIndex);
				var insertedVirtualLeadingTagIndex = -1;
				if (rgIndex != -1)
				{
					insertedVirtualLeadingTagIndex = _rgArrayManager.GetVirtualLeadingTagIndex(rgIndex);
				}

				//Iterate over groups, breaks when leading tag of inserted group became greater then leading tag of group for insert
				while (rgIndex != -1 && insertedVirtualLeadingTagIndex != -1 &&
						insertedVirtualLeadingTagIndex < virtualLeadingTagIndex &&
						desiredLeadingTagIndex < _storage.Count)
				{
					tagAtDesiredLeadingTagIndex = _storage.GetTagIdAtIndex(desiredLeadingTagIndex);
					rgIndex = _rgArrayManager.FindRgIndex(tagAtDesiredLeadingTagIndex);
					if (rgIndex != -1)
					{
						desiredLeadingTagIndex =
							_entriesArray.GetLastTagIndexInFixMessage(
								_entriesArray.GetEntry(_rgArrayManager.GetLastEntryLink(rgIndex))) + 1;
						insertedVirtualLeadingTagIndex = _rgArrayManager.GetVirtualLeadingTagIndex(rgIndex);
					}
				}
			}

			return desiredLeadingTagIndex;
		}

		public RepeatingGroup AddSubGroup(int indexInFixMessage, int leadingTag, int parentEntryIndex,
			RepeatingGroup group)
		{
			EnsureRepeatingGroupArrayCapacityAndEnlarge();
			EnsureEntriesCapacityAndEnlarge();
			if (_entries[_currentEntry] != null)
			{
				_currentEntry++;
			}

			var parentEntry = _entries[parentEntryIndex];
			var rgId = _rgId++;
			ShiftHidedLeadingTagsLeft(indexInFixMessage);
			AddToHidedTags(leadingTag, indexInFixMessage, rgId, parentEntryIndex);
			var rgIndex = AddRgToArray(-1, leadingTag, -1, -1, parentEntryIndex);

			_entriesArray.SetEntryLink(parentEntry, _entriesArray.GetArrayEnd(parentEntry), rgId);
			group.Init(leadingTag, rgId, rgIndex, _storage, this, parentEntryIndex, _version, _msgType);
			return group;
		}

		internal int AddRgToArray(int indexInFixMessage, int leadingTag, int virtualLink, int parentEntryLink)
		{
			var rgIndex = _rgCount++;
			EnsureRepeatingGroupArrayCapacityAndEnlarge();
			_rgArray[rgIndex] = RepeatingGroupStorageIntArrayPool.GetIntArrayFromPool(InitialSize);
			_rgArrayManager.SetRgLeadingTag(rgIndex, leadingTag);
			_rgArrayManager.SetZeroSize(rgIndex);
			_rgArrayManager.SetRgLeadingTagIndexInFixMsg(rgIndex, indexInFixMessage);
			_rgArrayManager.SetParentEntryIndex(rgIndex, parentEntryLink);
			_rgArrayManager.SetRgId(rgIndex, _rgId++);
			_rgArrayManager.SetVirtualLeadingTagIndex(rgIndex, virtualLink);
			return rgIndex;
		}

		internal int AddRgToArray(int indexInFixMessage, int leadingTag, int virtualLink, int rgId,
			int parentEntryLink)
		{
			var rgIndex = _rgCount++;
			EnsureRepeatingGroupArrayCapacityAndEnlarge();
			_rgArray[rgIndex] = RepeatingGroupStorageIntArrayPool.GetIntArrayFromPool(InitialSize);
			_rgArrayManager.SetRgLeadingTag(rgIndex, leadingTag);
			_rgArrayManager.SetZeroSize(rgIndex);
			_rgArrayManager.SetRgLeadingTagIndexInFixMsg(rgIndex, indexInFixMessage);
			_rgArrayManager.SetParentEntryIndex(rgIndex, parentEntryLink);
			_rgArrayManager.SetRgId(rgIndex, rgId);
			_rgArrayManager.SetVirtualLeadingTagIndex(rgIndex, virtualLink);
			return rgIndex;
		}

		internal void ReAddRgToArray(int rgIndex, int rgId)
		{
			_rgArrayManager.SetRgId(rgIndex, rgId);
		}

		internal void FillRg(int rgIndex, int indexInFixMessage, int leadingTag, int virtualLink, int rgId,
			int parentEntryLink)
		{
			_rgArrayManager.SetRgLeadingTag(rgIndex, leadingTag);
			_rgArrayManager.SetRgLeadingTagIndexInFixMsg(rgIndex, indexInFixMessage);
			_rgArrayManager.SetParentEntryIndex(rgIndex, parentEntryLink);
			_rgArrayManager.SetRgId(rgIndex, rgId);
			_rgArrayManager.SetVirtualLeadingTagIndex(rgIndex, virtualLink);
		}

		internal void AddToHidedTags(int leadingTag, int indexInFixMessage, int rgId, int entryIndex)
		{
			EnsureHidedLeadingTagArrayCapacityAndEnlarge();
			_hiddenLeadingTagsArray.AddEntry(leadingTag, indexInFixMessage, rgId, entryIndex, indexInFixMessage);
		}

		internal void AddToHidedTags(int leadingTag, int indexInFixMessage, int rgId, int entryIndex,
			int virtualTagIndex)
		{
			EnsureHidedLeadingTagArrayCapacityAndEnlarge();
			_hiddenLeadingTagsArray.AddEntry(leadingTag, indexInFixMessage, rgId, entryIndex, virtualTagIndex);
		}

		public bool RemoveRgTagAtIndex(int tagIndex, int parentEntryIndex)
		{
			Shift(tagIndex, -1, parentEntryIndex, -1, true);
			return _storage.RemoveTagAtIndex(tagIndex, false);
		}

		public int GetEntryForCreate(int rgIndex, int parentEntryIndex, int index)
		{
			//Add entry at some index of repeating group
			EnsureEntriesCapacityAndEnlarge();
			if (_entries[_currentEntry] != null)
			{
				_currentEntry++;
			}

			_entries[_currentEntry] = RepeatingGroupStorageIntArrayPool.GetIntArrayFromPool(InitialSize);
			var entry = _entries[_currentEntry];
			EnsureRgCapacityAndEnlarge(rgIndex);
			var repeatingGroup = _rgArray[rgIndex];

			_rgArrayManager.AddEntryAtIndex(repeatingGroup, index, _currentEntry);

			_entriesArray.SetZeroSize(entry);
			_entriesArray.SetParentEntryLink(entry, parentEntryIndex);
			return _currentEntry++;
		}

		public int GetEntryForCreate(int rgIndex, int parentEntryIndex)
		{
			//Add entry at end of repeating group
			EnsureEntriesCapacityAndEnlarge();
			if (_entries[_currentEntry] != null)
			{
				_currentEntry++;
			}

			_entries[_currentEntry] = RepeatingGroupStorageIntArrayPool.GetIntArrayFromPool(InitialSize);
			var entry = _entries[_currentEntry];
			EnsureRgCapacityAndEnlarge(rgIndex);
			var repeatingGroup = _rgArray[rgIndex];

			_rgArrayManager.AddEntry(repeatingGroup, _currentEntry);

			_entriesArray.SetZeroSize(entry);
			_entriesArray.SetParentEntryLink(entry, parentEntryIndex);
			return _currentEntry++;
		}

		private void EnsureHidedLeadingTagArrayCapacityAndEnlarge()
		{
			var lastIndex = _hiddenLeadingTagsArray.ArrayEnd;
			if (lastIndex + HidedEntrySize * 2 >= _hiddenLeadingTags.Length)
			{
				var newArr = RepeatingGroupStorageIntArrayPool.GetIntArrayFromPool(_hiddenLeadingTags.Length * 2);
				Array.Copy(_hiddenLeadingTags, 0, newArr, 0, _hiddenLeadingTags.Length);
				RepeatingGroupStorageIntArrayPool.ReturnObj(_hiddenLeadingTags);
				_hiddenLeadingTags = newArr;
				_hiddenLeadingTagsArray.SetHiddenLeadingTags(_hiddenLeadingTags);
			}
		}

		private void EnsureRepeatingGroupArrayCapacityAndEnlarge()
		{
			if (_rgArray[_rgArray.Length - 1] != null)
			{
				var newArr = RepeatingGroupStorageIntArrayPool.GetTwoDimIntArrayFromPool(_rgArray.Length * 2);
				Array.Copy(_rgArray, 0, newArr, 0, _rgArray.Length);
				RepeatingGroupStorageIntArrayPool.ReturnObj(_rgArray);
				_rgArray = newArr;
				_rgArrayManager.SetRgArray(_rgArray);
			}
		}

		internal int[] EnsureEntryCapacityAndEnlarge(int entryIndex)
		{
			var entry = _entries[entryIndex];
			var currentSize = _entriesArray.GetArrayEnd(entry) + EntriesEntrySize * 2;
			if (currentSize >= entry.Length - 1)
			{
				var newArr = RepeatingGroupStorageIntArrayPool.GetIntArrayFromPool(entry.Length * 2);
				Array.Copy(entry, 0, newArr, 0, entry.Length);
				RepeatingGroupStorageIntArrayPool.ReturnObj(entry);
				_entries[entryIndex] = newArr;
			}

			return _entries[entryIndex];
		}

		internal void EnsureEntriesCapacityAndEnlarge()
		{
			if (_entries[_entries.Length - 1] != null)
			{
				var newArr = RepeatingGroupStorageIntArrayPool.GetTwoDimIntArrayFromPool(_entries.Length * 2);
				Array.Copy(_entries, 0, newArr, 0, _entries.Length);
				RepeatingGroupStorageIntArrayPool.ReturnObj(_entries);
				_entries = newArr;
				_entriesArray.SetEntries(_entries);
			}
		}

		internal void EnsureRgCapacityAndEnlarge(int rgIndex)
		{
			var repeatingGroup = _rgArray[rgIndex];
			if (_rgArrayManager.GetRgArrayEnd(repeatingGroup) + RgHashEntrySize * 2 >= repeatingGroup.Length - 1)
			{
				var newArr = RepeatingGroupStorageIntArrayPool.GetIntArrayFromPool(repeatingGroup.Length * 2);
				Array.Copy(repeatingGroup, 0, newArr, 0, repeatingGroup.Length);
				RepeatingGroupStorageIntArrayPool.ReturnObj(repeatingGroup);
				_rgArray[rgIndex] = newArr;
			}
		}

		public int[][] Entries => _entries;

		public bool IsInvalidated { get; private set; }

		public void AddSubGroup(int index, int leadingTag, int parentEntryLink, RepeatingGroup group,
			bool validation)
		{
			AddSubGroup(index, leadingTag, parentEntryLink, group);
			@group.Validation = validation;
		}

		public RepeatingGroupArray RgArrayManager
		{
			get { return _rgArrayManager; }
		}

		public EntriesArray EntriesArray
		{
			get { return _entriesArray; }
		}

		public HiddenLeadingTagsArray HiddenLeadingTagsArray
		{
			get { return _hiddenLeadingTagsArray; }
		}

		public int GetLeadingTagValue(int leadingTag, int rgId)
		{
			var indexInHided = _hiddenLeadingTagsArray.FindInHidedLeadingTags(leadingTag, rgId);
			if (indexInHided == -1)
			{
				var repeatingGroup = _rgArrayManager.GetRgArrayById(leadingTag, rgId);
				if (repeatingGroup == null || _rgArrayManager.GetRgId(repeatingGroup) == -1)
				{
					throw new InvalidOperationException("Can't get leading tag value of nonexistent group");
				}

				var leadingTagIndexInFixMsg = _rgArrayManager.GetRgLeadingTagIndexInFixMsg(repeatingGroup);
				if (leadingTagIndexInFixMsg == -1)
				{
					throw new InvalidOperationException("Can't get leading tag value of removed group");
				}

				return (int)_storage.GetTagValueAsLongAtIndex(leadingTagIndexInFixMsg);
			}

			return 0;
		}

		public void ShiftIndexes(int startIndex, int offset)
		{
			//shift(startIndex, offset, -1, -1, false);
			if (offset < 0)
			{
				for (var rgIndex = 0; rgIndex < _rgArray.Length; rgIndex++)
				{
					var rg = _rgArray[rgIndex];
					if (rg != null && _rgArrayManager.GetRgId(rg) != -1)
					{
						var rgStart = _rgArrayManager.GetRgLeadingTagIndexInFixMsg(rg);
						var rgEnd = GetRgLastTagIndexInFixMsg(rg);
						if (rgStart <= startIndex && startIndex <= rgEnd)
						{
							//tag is a part of RG
							return;
						}
					}
				}
			}
			else
			{
				for (var rgIndex = 0; rgIndex < _rgArray.Length; rgIndex++)
				{
					var rg = _rgArray[rgIndex];
					if (rg != null && _rgArrayManager.GetRgId(rg) != -1)
					{
						var rgStart = _rgArrayManager.GetRgLeadingTagIndexInFixMsg(rg);
						var rgEnd = GetRgLastTagIndexInFixMsg(rg);
						Console.WriteLine(_rgArrayManager.GetRgLeadingTag(rg) + " [" + rgStart + ":" + rgEnd + "]");
						if (rgStart < startIndex && startIndex <= rgEnd)
						{
							//tag is a part of RG
							return;
						}
					}
				}
			}

			Shift(startIndex, offset, -1, -1, false);
		}

		private int GetRgLastTagIndexInFixMsg(int[] repeatingGroup)
		{
			var lastEntryLink = _rgArrayManager.GetLastEntryLink(repeatingGroup);
			return _entriesArray.GetLastTagIndexInFixMessage(lastEntryLink);
		}
	}
}