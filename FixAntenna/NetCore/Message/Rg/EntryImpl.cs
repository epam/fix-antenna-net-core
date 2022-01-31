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
using System.Text;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message.Format;
using Epam.FixAntenna.NetCore.Message.Rg.Exceptions;
using static Epam.FixAntenna.NetCore.Message.Rg.RepeatingGroupStorage;

namespace Epam.FixAntenna.NetCore.Message.Rg
{
	public class EntryImpl
	{
		private HiddenLeadingTagsArray _hiddenLeadingTagsArray;
		protected internal List<RepeatingGroup> AllocatedSubGroups = new List<RepeatingGroup>();
		protected internal bool Deleted;
		/*protected*/ internal EntriesArray EntriesArray;
		protected internal int EntryIndex;
		protected internal RepeatingGroup Group;
		protected internal HashSet<int> GroupTags;
		protected internal HashSet<int> NestedLeadingTags;
		protected internal HashSet<int> OuterLeadingTags;
		protected internal bool ReleaseNeeded = true;
		/*protected*/ internal RepeatingGroupArray RgArray;
		/*protected*/ internal RepeatingGroupStorage RgStorage;
		protected internal IndexedStorage Storage;

		/// <summary>
		/// Adds sub group to entry. Validation for this group is turned off. </summary>
		/// <param name="leadingTag"> leading tag for repeating group. </param>
		/// <returns> instance of RepeatingGroup from RepeatingGroupPool. There is no need to call release for this object. </returns>
		public virtual RepeatingGroup AddRepeatingGroup(int leadingTag)
		{
			return AddRepeatingGroup(leadingTag, Group.Validation);
		}

		public virtual void AddRepeatingGroup(int leadingTag, RepeatingGroup group)
		{
			AddRepeatingGroup(leadingTag, false, group);
		}

		/// <summary>
		/// Adds sub group to entry. </summary>
		/// <param name="leadingTag"> leading tag for repeating group. </param>
		/// <param name="validation"> turn on/off validation </param>
		/// <returns> instance of RepeatingGroup from RepeatingGroupPool. There is no need to call release for this object. </returns>
		public virtual RepeatingGroup AddRepeatingGroup(int leadingTag, bool validation)
		{
			var subGroup = RepeatingGroupPool.RepeatingGroup;
			subGroup.ReleaseNeeded = false;
			AllocatedSubGroups.Add(subGroup);
			AddRepeatingGroup(leadingTag, validation, subGroup);
			return subGroup;
		}

		/// <summary>
		/// Adds sub group to entry. </summary>
		/// <param name="leadingTag"> leading tag for repeating group. </param>
		/// <param name="validation"> turn on/off validation. </param>
		/// <param name="subGroup"> group for further work. </param>
		public virtual void AddRepeatingGroup(int leadingTag, bool validation, RepeatingGroup subGroup)
		{
			if (Deleted)
			{
				throw new InvalidOperationException("Entry was deleted. You should create new entry");
			}

			if (Group.Validation)
			{
				IsTagValid(leadingTag);
				IsLeadingTagValid(leadingTag);
			}

			IsDuplicateGroup(leadingTag);
			var entry = RgStorage.EnsureEntryCapacityAndEnlarge(EntryIndex);
			var repeatingGroup = RgArray.GetRepeatingGroup(Group.RgIndex);
			int lastIndex;
			if (EntriesArray.IsEmpty(entry))
			{
				if (RgArray.GetRgLeadingTagIndexInFixMsg(repeatingGroup) == -1)
				{
					var indexInHiddenLeadingTags =
						_hiddenLeadingTagsArray.FindInHidedLeadingTags(Group.LeadingTag, Group.RgId);
					lastIndex = _hiddenLeadingTagsArray.GetTagLinkVirtual(indexInHiddenLeadingTags) + 1;
				}
				else if (EntriesArray.HasParent(entry))
				{
					var parentEntry = EntriesArray.GetParentEntry(entry);
					lastIndex = EntriesArray.GetLastTagIndexInFixMessage(parentEntry);
				}
				else
				{
					var prevEntry = RgArray.GetPrevEntryLink(repeatingGroup, EntryIndex);
					lastIndex = EntriesArray.GetLastTagIndexInFixMessage(EntriesArray.GetEntry(prevEntry)) + 1;
					while (EntriesArray.IsEmpty(prevEntry))
					{
						prevEntry = RgArray.GetPrevEntryLink(repeatingGroup, prevEntry);
						lastIndex = EntriesArray.GetLastTagIndexInFixMessage(EntriesArray.GetEntry(prevEntry)) + 1;
					}
				}
			}
			else
			{
				lastIndex = 1 + EntriesArray.GetLastTagIndexInFixMessage(entry);
			}

			RgStorage.AddSubGroup(lastIndex, leadingTag, EntryIndex, subGroup, validation);
		}

		/// <summary>
		/// Adds tagValue to entry. </summary>
		/// <param name="tagValue"> value for add. </param>
		/// <returns> index in message (not in group or entry) in which value was added </returns>
		public virtual int AddTag(TagValue tagValue)
		{
			var index = PrepareAdd(tagValue.TagId);
			Storage.UpdateValueAtIndex(index, tagValue);
			return index;
		}

		/// <summary>
		/// Adds tag to entry. </summary>
		/// <param name="tag"> tag for add </param>
		/// <param name="value"> value for add </param>
		/// <returns> index in message (not in group or entry) in which value was added </returns>
		public virtual int AddTag(int tag, byte[] value)
		{
			var index = PrepareAdd(tag);
			Storage.UpdateValueAtIndex(index, value);
			return index;
		}

		/// <summary>
		/// Adds tag to entry. </summary>
		/// <param name="tag"> tag for add </param>
		/// <param name="value"> value for add </param>
		/// <returns> index in message (not in group or entry) in which value was added </returns>
		public virtual int AddTag(int tag, bool value)
		{
			var index = PrepareAdd(tag);
			Storage.UpdateValueAtIndex(index, value);
			return index;
		}

		/// <summary>
		/// Adds tag to entry. </summary>
		/// <param name="tag"> </param>
		/// <param name="value"> byte array for add </param>
		/// <param name="offset"> offset in passed array </param>
		/// <param name="length"> length of array that will be added </param>
		/// <returns> index in message (not in group or entry) in which value was added </returns>
		public virtual int AddTag(int tag, byte[] value, int offset, int length)
		{
			var index = PrepareAdd(tag);
			Storage.UpdateValueAtIndex(index, value, offset, length);
			return index;
		}

		/// <summary>
		/// Adds tag to entry. </summary>
		/// <param name="tag"> tag for add </param>
		/// <param name="value"> value for add </param>
		/// <param name="precision"> precision of value rounding </param>
		/// <returns> index in message (not in group or entry) in which value was added </returns>
		public virtual int AddTag(int tag, double value, int precision)
		{
			var index = PrepareAdd(tag);
			Storage.UpdateValueAtIndex(index, value, precision);
			return index;
		}

		/// <summary>
		/// Adds tag to entry. </summary>
		/// <param name="tag"> tag for add </param>
		/// <param name="value"> value for add </param>
		/// <returns> index in message (not in group or entry) in which value was added </returns>
		public virtual int AddTag(int tag, long value)
		{
			var index = PrepareAdd(tag);
			Storage.UpdateValueAtIndex(index, value);
			return index;
		}

		/// <summary>
		/// Adds tag to entry. </summary>
		/// <param name="tag"> tag for add </param>
		/// <param name="value"> value for add </param>
		/// <returns> index in message (not in group or entry) in which value was added </returns>
		public virtual int AddTag(int tag, string value)
		{
			var index = PrepareAdd(tag);
			Storage.UpdateValueAtIndex(index, value);
			return index;
		}

		/// <summary>
		/// Adds tag to entry. </summary>
		/// <param name="tag"> tag for add </param>
		/// <param name="value"> value for add </param>
		/// <param name="type"> type of date </param>
		/// <returns> index in message (not in group or entry) in which value was added </returns>
		public virtual int AddCalendarTag(int tag, DateTimeOffset value, FixDateFormatterFactory.FixDateType type)
		{
			var index = PrepareAdd(tag);
			Storage.UpdateCalendarValueAtIndex(index, value, type);
			return index;
		}

		private int PrepareAdd(int tag)
		{
			if (Deleted)
			{
				throw new InvalidOperationException("Entry was deleted. You should create new entry");
			}

			if (Group.Validation)
			{
				IsTagValid(tag);
				IsDuplicateTag(tag);
			}

			var entry = RgStorage.EnsureEntryCapacityAndEnlarge(EntryIndex);
			var entries = RgStorage.Entries;
			CreateRepeatingGroupIfNotExists(entry, Group.RgIndex, EntryIndex);

			EntriesArray.IncrementLastTagIndexInFixMessage(entry);
			var lastIndex = EntriesArray.GetLastTagIndexInFixMessage(entry);

			RgStorage.PrepareAdd(lastIndex, tag, Group.ParentEntryIndex, EntryIndex);

			EntriesArray.AddEntry(entry, tag, lastIndex, LinkTypeTag);

			Group.UpdateParentEntries(entry, entries, lastIndex, 1);

			return lastIndex;
		}

		private int[] CreateRepeatingGroupIfNotExists(int[] entry, int rgIndex, int entryIndex)
		{
			if (EntriesArray.HasParent(entry) && EntriesArray.IsEmpty(EntriesArray.GetParentEntry(entry)))
			{
				//Should create all uncreated parent repeating group
				var parentEntryLink = EntriesArray.GetParentEntryLink(entry);
				var parentEntry = EntriesArray.GetParentEntry(entry);
				var parentRgArrayIndex = RgArray.GetRgIndexByEntryIndex(parentEntryLink);
				CreateRepeatingGroupIfNotExists(parentEntry, parentRgArrayIndex, parentEntryLink);
			}

			int[] repeatingGroup;

			if (rgIndex == -1)
			{
				throw new InvalidOperationException(
					"Entry, which owned repeating group, was deleted. You should create new entry with new nested group.");
			}

			repeatingGroup = RgStorage.RepeatingGroups[rgIndex];

			var leadingTag = RgArray.GetRgLeadingTag(repeatingGroup);
			var rgId = RgArray.GetRgId(repeatingGroup);
			var parentEntryIndex = RgArray.GetParentEntryIndex(repeatingGroup);

			//If group is not created, then recreate this group
			if (rgId == -1)
			{
				var hiddenArrayIndex = _hiddenLeadingTagsArray.FindInHidedLeadingTags(leadingTag, rgId);
				rgId = _hiddenLeadingTagsArray.GetRgId(hiddenArrayIndex);
				RgStorage.ReAddRgToArray(rgIndex, rgId);
				//            rgArray.addEntry(repeatingGroup, entryIndex);
			}

			//if first tag in entry then should update leading tag and set last tag link in new entry
			if (EntriesArray.IsEmpty(entryIndex))
			{
				RgStorage.IncrementLeadingTag(RgStorage.GetLeadingTagValue(leadingTag, rgId) + 1, leadingTag, rgId,
					parentEntryIndex, parentEntryIndex, rgIndex);
				var lastTagLink = RgArray.GetRgLeadingTagIndexById(leadingTag, rgId);
				if (RgArray.GetRgLeadingTagIndexInFixMsg(repeatingGroup) != -1)
				{
					var prevEntry = RgArray.GetPrevEntryLink(repeatingGroup, entryIndex);
					while (prevEntry != -1 && EntriesArray.IsEmpty(prevEntry))
					{
						prevEntry = RgArray.GetPrevEntryLink(repeatingGroup, prevEntry);
					}

					if (prevEntry != -1 && !EntriesArray.IsEmpty(prevEntry))
					{
						lastTagLink = EntriesArray.GetLastTagIndexInFixMessage(EntriesArray.GetEntry(prevEntry));
					}
				}

				EntriesArray.SetLastTagIndexInFixMessage(entry, lastTagLink);
			}

			return repeatingGroup;
		}

		/// <summary>
		/// Updates tagValue </summary>
		/// <param name="tagValue"> tagValue for update </param>
		/// <param name="addIfNotExists"> determines the behavior in case if tag is not exists </param>
		/// <returns> index of updated value in fix message (not in group or entry) </returns>
		public virtual int UpdateValue(TagValue tagValue, IndexedStorage.MissingTagHandling addIfNotExists)
		{
			var tag = tagValue.TagId;
			ValidateUpdateLeadingTag(tag);
			switch (addIfNotExists)
			{
				case IndexedStorage.MissingTagHandling.DontAddIfNotExists:
					var tagLink = FindTagLink(tag);
					if (tagLink != -1)
					{
						Storage.UpdateValueAtIndex(tagLink, tagValue);
					}

					return tagLink;
				case IndexedStorage.MissingTagHandling.AddIfNotExists:
					var link = FindTagLink(tag);
					if (link == -1)
					{
						return AddTag(tagValue);
					}
					else
					{
						Storage.UpdateValueAtIndex(link, tagValue);
						return link;
					}
				case IndexedStorage.MissingTagHandling.AlwaysAdd:
					return AddTag(tagValue);
				default:
					throw new NotSupportedException("Unsupported tag handling " + addIfNotExists);
			}
		}

		/// <summary>
		/// Updates tag value </summary>
		/// <param name="tag"> tag for update </param>
		/// <param name="value"> new value for tag </param>
		/// <param name="offset"> offset in passed array </param>
		/// <param name="length"> length of array that will be added </param>
		/// <param name="addIfNotExists"> determines the behavior in case if tag is not exists </param>
		/// <returns> index of updated value in fix message (not in group or entry) </returns>
		public virtual int UpdateValue(int tag, byte[] value, int offset, int length,
			IndexedStorage.MissingTagHandling addIfNotExists)
		{
			ValidateUpdateLeadingTag(tag);
			switch (addIfNotExists)
			{
				case IndexedStorage.MissingTagHandling.DontAddIfNotExists:
					var linkTag = FindTagLink(tag);
					if (linkTag != -1)
					{
						Storage.UpdateValueAtIndex(linkTag, value, offset, length);
					}

					return linkTag;
				case IndexedStorage.MissingTagHandling.AddIfNotExists:
					var link = FindTagLink(tag);
					if (link == -1)
					{
						return AddTag(tag, value, offset, length);
					}
					else
					{
						Storage.UpdateValueAtIndex(link, value, offset, length);
						return link;
					}
				case IndexedStorage.MissingTagHandling.AlwaysAdd:
					return AddTag(tag, value, offset, length);
				default:
					throw new NotSupportedException("Unsupported tag handling " + addIfNotExists);
			}
		}

		/// <summary>
		/// Updates tag value </summary>
		/// <param name="tag"> tag for update </param>
		/// <param name="value"> new value for tag </param>
		/// <param name="addIfNotExists"> determines the behavior in case if tag is not exists </param>
		/// <returns> index of updated value in fix message (not in group or entry) </returns>
		public virtual int UpdateValue(int tag, byte[] value, IndexedStorage.MissingTagHandling addIfNotExists)
		{
			ValidateUpdateLeadingTag(tag);
			switch (addIfNotExists)
			{
				case IndexedStorage.MissingTagHandling.DontAddIfNotExists:
					var linkTag = FindTagLink(tag);
					if (linkTag != -1)
					{
						Storage.UpdateValueAtIndex(linkTag, value);
					}

					return linkTag;
				case IndexedStorage.MissingTagHandling.AddIfNotExists:
					var link = FindTagLink(tag);
					if (link == -1)
					{
						return AddTag(tag, value);
					}
					else
					{
						Storage.UpdateValueAtIndex(link, value);
						return link;
					}
				case IndexedStorage.MissingTagHandling.AlwaysAdd:
					return AddTag(tag, value);
				default:
					throw new NotSupportedException("Unsupported tag handling " + addIfNotExists);
			}
		}

		/// <summary>
		/// Updates tag value </summary>
		/// <param name="tag"> tag for update </param>
		/// <param name="value"> new value for tag </param>
		/// <param name="addIfNotExists"> determines the behavior in case if tag is not exists </param>
		/// <returns> index of updated value in fix message (not in group or entry) </returns>
		public virtual int UpdateValue(int tag, long value, IndexedStorage.MissingTagHandling addIfNotExists)
		{
			ValidateUpdateLeadingTag(tag);
			switch (addIfNotExists)
			{
				case IndexedStorage.MissingTagHandling.DontAddIfNotExists:
					var linkTag = FindTagLink(tag);
					if (linkTag != -1)
					{
						Storage.UpdateValueAtIndex(linkTag, value);
					}

					return linkTag;
				case IndexedStorage.MissingTagHandling.AddIfNotExists:
					var link = FindTagLink(tag);
					if (link == -1)
					{
						return AddTag(tag, value);
					}
					else
					{
						Storage.UpdateValueAtIndex(link, value);
						return link;
					}
				case IndexedStorage.MissingTagHandling.AlwaysAdd:
					return AddTag(tag, value);
				default:
					throw new NotSupportedException("Unsupported tag handling " + addIfNotExists);
			}
		}

		/// <summary>
		/// Updates tag value </summary>
		/// <param name="tag"> tag for update </param>
		/// <param name="value"> new value for tag </param>
		/// <param name="precision"> precision of value rounding </param>
		/// <param name="addIfNotExists"> determines the behavior in case if tag is not exists </param>
		/// <returns> index of updated value in fix message (not in group or entry) </returns>
		public virtual int UpdateValue(int tag, double value, int precision,
			IndexedStorage.MissingTagHandling addIfNotExists)
		{
			ValidateUpdateLeadingTag(tag);
			switch (addIfNotExists)
			{
				case IndexedStorage.MissingTagHandling.DontAddIfNotExists:
					var linkTag = FindTagLink(tag);
					if (linkTag != -1)
					{
						Storage.UpdateValueAtIndex(linkTag, value, precision);
					}

					return linkTag;
				case IndexedStorage.MissingTagHandling.AddIfNotExists:
					var link = FindTagLink(tag);
					if (link == -1)
					{
						return AddTag(tag, value, precision);
					}
					else
					{
						Storage.UpdateValueAtIndex(link, value, precision);
						return link;
					}
				case IndexedStorage.MissingTagHandling.AlwaysAdd:
					return AddTag(tag, value, precision);
				default:
					throw new NotSupportedException("Unsupported tag handling " + addIfNotExists);
			}
		}

		/// <summary>
		/// Updates tag value </summary>
		/// <param name="tag"> tag for update </param>
		/// <param name="strBuffer"> new value for tag </param>
		/// <param name="addIfNotExists"> determines the behavior in case if tag is not exists </param>
		/// <returns> index of updated value in fix message (not in group or entry) </returns>
		public virtual int UpdateValue(int tag, string strBuffer, IndexedStorage.MissingTagHandling addIfNotExists)
		{
			ValidateUpdateLeadingTag(tag);
			switch (addIfNotExists)
			{
				case IndexedStorage.MissingTagHandling.DontAddIfNotExists:
					var linkTag = FindTagLink(tag);
					if (linkTag != -1)
					{
						Storage.UpdateValueAtIndex(linkTag, strBuffer);
					}

					return linkTag;
				case IndexedStorage.MissingTagHandling.AddIfNotExists:
					var link = FindTagLink(tag);
					if (link == -1)
					{
						return AddTag(tag, strBuffer);
					}
					else
					{
						Storage.UpdateValueAtIndex(link, strBuffer);
						return link;
					}
				case IndexedStorage.MissingTagHandling.AlwaysAdd:
					return AddTag(tag, strBuffer);
				default:
					throw new NotSupportedException("Unsupported tag handling " + addIfNotExists);
			}
		}

		/// <summary>
		/// Updates tag value </summary>
		/// <param name="tag"> tag for update </param>
		/// <param name="value"> new value for tag </param>
		/// <param name="addIfNotExists"> determines the behavior in case if tag is not exists </param>
		/// <returns> index of updated value in fix message (not in group or entry) </returns>
		public virtual int UpdateValue(int tag, bool value, IndexedStorage.MissingTagHandling addIfNotExists)
		{
			ValidateUpdateLeadingTag(tag);
			switch (addIfNotExists)
			{
				case IndexedStorage.MissingTagHandling.DontAddIfNotExists:
					var linkTag = FindTagLink(tag);
					if (linkTag != -1)
					{
						Storage.UpdateValueAtIndex(linkTag, value);
					}

					return linkTag;
				case IndexedStorage.MissingTagHandling.AddIfNotExists:
					var link = FindTagLink(tag);
					if (link == -1)
					{
						return AddTag(tag, value);
					}
					else
					{
						Storage.UpdateValueAtIndex(link, value);
						return link;
					}
				case IndexedStorage.MissingTagHandling.AlwaysAdd:
					return AddTag(tag, value);
				default:
					throw new NotSupportedException("Unsupported tag handling " + addIfNotExists);
			}
		}

		/// <summary>
		/// Updates tag value </summary>
		/// <param name="tag"> tag for update </param>
		/// <param name="value"> new value for tag </param>
		/// <param name="type"> type of date </param>
		/// <param name="addIfNotExists"> determines the behavior in case if tag is not exists </param>
		/// <returns> index of updated value in fix message (not in group or entry) </returns>
		public virtual int UpdateCalendarValue(int tag, DateTimeOffset value,
			FixDateFormatterFactory.FixDateType type, IndexedStorage.MissingTagHandling addIfNotExists)
		{
			ValidateUpdateLeadingTag(tag);
			switch (addIfNotExists)
			{
				case IndexedStorage.MissingTagHandling.DontAddIfNotExists:
					var linkTag = FindTagLink(tag);
					if (linkTag != -1)
					{
						Storage.UpdateCalendarValueAtIndex(linkTag, value, type);
					}

					return linkTag;
				case IndexedStorage.MissingTagHandling.AddIfNotExists:
					var link = FindTagLink(tag);
					if (link == -1)
					{
						return AddCalendarTag(tag, value, type);
					}
					else
					{
						Storage.UpdateCalendarValueAtIndex(link, value, type);
						return link;
					}

				case IndexedStorage.MissingTagHandling.AlwaysAdd:
					return AddCalendarTag(tag, value, type);
				default:
					throw new NotSupportedException("Unsupported tag handling " + addIfNotExists);
			}
		}

		private void ValidateUpdateLeadingTag(int tag)
		{
			if (IsGroupTag(tag))
			{
				throw new ArgumentException(
					"Trying to update leading tag value. It's impossible because leading tags are self-maintaining.");
			}
		}

		/// <summary>
		/// Returns true if tag exists in entry </summary>
		/// <param name="tag"> tag for check </param>
		/// <returns> is tag exists </returns>
		public virtual bool IsTagExists(int tag)
		{
			return FindTagLink(tag) != -1;
		}

		/// <summary>
		/// Fills passed TagValue object by tag data </summary>
		/// <param name="tag"> tag for find </param>
		/// <param name="dest"> object for fill </param>
		public virtual void LoadTagValue(int tag, TagValue dest)
		{
			var tagLink = FindTagLink(tag);
			if (tagLink != -1)
			{
				Storage.LoadTagValueByIndex(tagLink, dest);
			}
			else
			{
				throw new FieldNotFoundException("Entry doesn't contains tag " + tag);
			}
		}

		/// <summary>
		/// Returns tag value as string </summary>
		/// <param name="tag"> tag for find </param>
		/// <returns> tag value </returns>
		public virtual string GetTagValueAsString(int tag)
		{
			var tagLink = FindTagLink(tag);
			if (tagLink != -1)
			{
				return Storage.GetTagValueAsStringAtIndex(tagLink);
			}

			return null;
		}

		/// <summary>
		/// Returns tag value as byte array </summary>
		/// <param name="tag"> tag for find </param>
		/// <returns> tag value </returns>
		public virtual byte[] GetTagValueAsBytes(int tag)
		{
			var tagLink = FindTagLink(tag);
			if (tagLink != -1)
			{
				return Storage.GetTagValueAsBytesAtIndex(tagLink);
			}

			return null;
		}

		/// <summary>
		/// Returns tag value as byte array </summary>
		/// <param name="tag"> tag for find </param>
		/// <param name="dest"> array for fill </param>
		/// <param name="offset"> start index in passed array from which array will filled </param>
		/// <returns> length of wrote data </returns>
		public virtual int GetTagValueAsBytes(int tag, byte[] dest, int offset)
		{
			var tagLink = FindTagLink(tag);
			if (tagLink != -1)
			{
				return Storage.GetTagValueAsBytesAtIndex(tagLink, dest, offset);
			}

			throw new FieldNotFoundException("Entry doesn't contains tag " + tag);
		}

		/// <summary>
		/// Returns first byte of tag value </summary>
		/// <param name="tag"> tag for find </param>
		/// <returns> first byte of tag value </returns>
		public virtual byte GetTagValueAsByte(int tag)
		{
			var tagLink = FindTagLink(tag);
			if (tagLink != -1)
			{
				return Storage.GetTagValueAsByteAtIndex(tagLink, 0);
			}

			throw new FieldNotFoundException("Entry doesn't contains tag " + tag);
		}

		/// <summary>
		/// Returns byte of tag value at offset </summary>
		/// <param name="tag"> tag for find </param>
		/// <returns> byte of tag value </returns>
		public virtual byte GetTagValueAsByte(int tag, int offset)
		{
			var tagLink = FindTagLink(tag);
			if (tagLink != -1)
			{
				return Storage.GetTagValueAsByteAtIndex(tagLink, offset);
			}

			throw new FieldNotFoundException("Entry doesn't contains tag " + tag);
		}

		/// <summary>
		/// Returns tag value as boolean </summary>
		/// <param name="tag"> tag for find </param>
		/// <returns> tag value </returns>
		public virtual bool GetTagValueAsBool(int tag)
		{
			var tagLink = FindTagLink(tag);
			if (tagLink != -1)
			{
				return Storage.GetTagValueAsBoolAtIndex(tagLink);
			}

			throw new FieldNotFoundException("Entry doesn't contains tag " + tag);
		}

		/// <summary>
		/// Returns tag value as double </summary>
		/// <param name="tag"> tag for find </param>
		/// <returns> tag value </returns>
		public virtual double GetTagValueAsDouble(int tag)
		{
			var tagLink = FindTagLink(tag);
			if (tagLink != -1)
			{
				return Storage.GetTagValueAsDoubleAtIndex(tagLink);
			}

			throw new FieldNotFoundException("Entry doesn't contains tag " + tag);
		}

		/// <summary>
		/// Returns tag value as long </summary>
		/// <param name="tag"> tag for find </param>
		/// <returns> tag value </returns>
		public virtual long GetTagValueAsLong(int tag)
		{
			var tagLink = FindTagLink(tag);
			if (tagLink != -1)
			{
				return Storage.GetTagValueAsLongAtIndex(tagLink);
			}

			throw new FieldNotFoundException("Entry doesn't contains tag " + tag);
		}

		/// <summary>
		/// Fills passed StringBuffer by tag value </summary>
		/// <param name="tag"> tag for find </param>
		/// <param name="str"> buffer for filled </param>
		public virtual void GetTagValueAsStringBuff(int tag, StringBuilder str)
		{
			var tagLink = FindTagLink(tag);
			if (tagLink != -1)
			{
				Storage.GetTagValueAsStringBuffAtIndex(tagLink, str);
			}
			else
			{
				throw new FieldNotFoundException("Entry doesn't have field " + tag);
			}
		}

		/// <summary>
		/// Fills passed TagValue object by tag data from index </summary>
		/// <param name="index"> number of tag in entry (not in entire FIX message) </param>
		/// <param name="dest"> object for fill </param>
		public virtual void LoadTagValueByIndex(int index, TagValue dest)
		{
			var size = Count;
			if (index < size && index >= 0)
			{
				var entry = EntriesArray.GetEntry(EntryIndex);
				Storage.LoadTagValueByIndex(EntriesArray.GetTagLinkAtIndex(entry, index), dest);
				return;
			}

			throw new IndexOutOfRangeException("Invalid index for entry with size " + size);
		}

		/// <summary>
		/// Returns tag value as string </summary>
		/// <param name="index"> number of tag in entry (not in entire FIX message) </param>
		/// <returns> tag value </returns>
		public virtual string GetTagValueAsStringAtIndex(int index)
		{
			var size = Count;
			if (index < size && index >= 0)
			{
				var entry = EntriesArray.GetEntry(EntryIndex);
				return Storage.GetTagValueAsStringAtIndex(EntriesArray.GetTagLinkAtIndex(entry, index));
			}

			throw new IndexOutOfRangeException("Invalid index for entry with size " + size);
		}

		/// <summary>
		/// Returns tag value as byte array </summary>
		/// <param name="index"> number of tag in entry (not in entire FIX message) </param>
		/// <returns> tag value </returns>
		public virtual byte[] GetTagValueAsBytesAtIndex(int index)
		{
			var size = Count;
			if (index < size && index >= 0)
			{
				var entry = EntriesArray.GetEntry(EntryIndex);
				return Storage.GetTagValueAsBytesAtIndex(EntriesArray.GetTagLinkAtIndex(entry, index));
			}

			throw new IndexOutOfRangeException("Invalid index for entry with size " + size);
		}

		/// <summary>
		/// Returns tag value as byte array </summary>
		/// <param name="index"> number of tag in entry (not in entire FIX message) </param>
		/// <param name="dest"> array for fill </param>
		/// <param name="offset"> start index in passed array from which array will filled </param>
		/// <returns> length of wrote data </returns>
		public virtual int GetTagValueAsBytesAtIndex(int index, byte[] dest, int offset)
		{
			var size = Count;
			if (index < size && index >= 0)
			{
				var entry = EntriesArray.GetEntry(EntryIndex);
				return Storage.GetTagValueAsBytesAtIndex(EntriesArray.GetTagLinkAtIndex(entry, index), dest, offset);
			}

			throw new IndexOutOfRangeException("Invalid index for entry with size " + size);
		}

		/// <summary>
		/// Returns first byte of tag value </summary>
		/// <param name="index"> number of tag in entry (not in entire FIX message) </param>
		/// <returns> first byte of tag value </returns>
		public virtual byte GetTagValueAsByteAtIndex(int index)
		{
			var size = Count;
			if (index < size && index >= 0)
			{
				var entry = EntriesArray.GetEntry(EntryIndex);
				return Storage.GetTagValueAsByteAtIndex(EntriesArray.GetTagLinkAtIndex(entry, index), 0);
			}

			throw new IndexOutOfRangeException("Invalid index for entry with size " + size);
		}

		/// <summary>
		/// Returns byte of tag value at offset </summary>
		/// <param name="index"> number of tag in entry (not in entire FIX message) </param>
		/// <returns> byte of tag value </returns>
		public virtual byte GetTagValueAsByteAtIndex(int index, int offset)
		{
			var size = Count;
			if (index < size && index >= 0)
			{
				var entry = EntriesArray.GetEntry(EntryIndex);
				return Storage.GetTagValueAsByteAtIndex(EntriesArray.GetTagLinkAtIndex(entry, index), offset);
			}

			throw new IndexOutOfRangeException("Invalid index for entry with size " + size);
		}

		/// <summary>
		/// Returns tag value as boolean </summary>
		/// <param name="index"> number of tag in entry (not in entire FIX message) </param>
		/// <returns> tag value </returns>
		public virtual bool GetTagValueAsBoolAtIndex(int index)
		{
			var size = Count;
			if (index < size && index >= 0)
			{
				var entry = EntriesArray.GetEntry(EntryIndex);
				return Storage.GetTagValueAsBoolAtIndex(EntriesArray.GetTagLinkAtIndex(entry, index));
			}

			throw new IndexOutOfRangeException("Invalid index for entry with size " + size);
		}

		/// <summary>
		/// Returns tag value as double </summary>
		/// <param name="index"> number of tag in entry (not in entire FIX message) </param>
		/// <returns> tag value </returns>
		public virtual double GetTagValueAsDoubleAtIndex(int index)
		{
			var size = Count;
			if (index < size && index >= 0)
			{
				var entry = EntriesArray.GetEntry(EntryIndex);
				return Storage.GetTagValueAsDoubleAtIndex(EntriesArray.GetTagLinkAtIndex(entry, index));
			}

			throw new IndexOutOfRangeException("Invalid index for entry with size " + size);
		}

		/// <summary>
		/// Returns tag value as long </summary>
		/// <param name="index"> number of tag in entry (not in entire FIX message) </param>
		/// <returns> tag value </returns>
		public virtual long GetTagValueAsLongAtIndex(int index)
		{
			var size = Count;
			if (index < size && index >= 0)
			{
				var entry = EntriesArray.GetEntry(EntryIndex);
				return Storage.GetTagValueAsLongAtIndex(EntriesArray.GetTagLinkAtIndex(entry, index));
			}

			throw new IndexOutOfRangeException("Invalid index for entry with size " + size);
		}

		/// <summary>
		/// Fills passed StringBuffer by tag value </summary>
		/// <param name="index"> number of tag in entry (not in entire FIX message) </param>
		/// <param name="str"> buffer for filled </param>
		public virtual void GetTagValueAsStringBuffAtIndex(int index, StringBuilder str)
		{
			var size = Count;
			if (index < size && index >= 0)
			{
				var entry = EntriesArray.GetEntry(EntryIndex);
				Storage.GetTagValueAsStringBuffAtIndex(EntriesArray.GetTagLinkAtIndex(entry, index), str);
				return;
			}

			throw new IndexOutOfRangeException("Invalid index for entry with size " + size);
		}

		/// <summary>
		/// Returns repeating group </summary>
		/// <param name="index"> number of leading tag of group in entry (not in entire FIX message) </param>
		/// <returns> instance of RepeatingGroup from RepeatingGroupPool. There is no need to call release for this object. </returns>
		public virtual RepeatingGroup GetRepeatingGroupAtIndex(int index)
		{
			var group = RepeatingGroupPool.RepeatingGroup;
			GetRepeatingGroupAtIndex(index, group);
			return group;
		}

		/// <summary>
		/// Fills repeating group by repeating group data </summary>
		/// <param name="index"> number of leading tag of group in entry (not in entire FIX message) </param>
		/// <param name="group"> repeating group object for fill </param>
		public virtual void GetRepeatingGroupAtIndex(int index, RepeatingGroup group)
		{
			var size = Count;
			if (index < size && index >= 0)
			{
				var entry = EntriesArray.GetEntry(EntryIndex);
				var tag = EntriesArray.GetTagAtIndex(entry, index);
				var rgId = EntriesArray.GetTagLinkAtIndex(entry, index);
				RgStorage.GetRepeatingGroup(tag, rgId, group);
				return;
			}

			throw new IndexOutOfRangeException("Invalid index for entry with size " + size);
		}

		/// <summary>
		/// Removes tag from entry </summary>
		/// <param name="tag"> tag for remove </param>
		/// <returns> is tag was removes </returns>
		public virtual bool RemoveTag(int tag)
		{
			var entry = EntriesArray.GetEntry(EntryIndex);
			for (var i = EntriesHeaderSize; i < EntriesArray.GetArrayEnd(entry); i += EntriesEntrySize)
			{
				if (entry[i + EntriesTag] == tag)
				{
					RemoveTag(entry, i);
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Removes tag from entry by index </summary>
		/// <param name="index"> number of tag in entry (not in entire FIX message) </param>
		/// <returns> is tag was removes </returns>
		public virtual bool RemoveTagAtIndex(int index)
		{
			var size = Count;
			if (index < size && index >= 0)
			{
				var entry = EntriesArray.GetEntry(EntryIndex);
				var indexInEntry = EntriesHeaderSize + index * EntriesEntrySize;
				RemoveTag(entry, indexInEntry);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Removes repeating group from entry </summary>
		/// <param name="leadingTag"> leading tag of repeating group </param>
		/// <returns> true if repeating group was deleted </returns>
		public virtual bool RemoveRepeatingGroup(int leadingTag)
		{
			if (IsRepeatingGroupExists(leadingTag))
			{
				if (IsGroupTag(leadingTag))
				{
					return RemoveTag(leadingTag);
				}

				return RemoveHidedRepeatingGroup(leadingTag);
			}

			return false;
		}

		private bool RemoveHidedRepeatingGroup(int leadingTag)
		{
			return _hiddenLeadingTagsArray.RemoveFromHidedTagsByLeadingTagAndEntry(leadingTag, EntryIndex);
		}

		/// <summary>
		/// Removes repeating group from entry </summary>
		/// <param name="index"> index of leading tag group in entry (not in entire FIX message) </param>
		/// <returns> true if repeating group was deleted </returns>
		public virtual bool RemoveRepeatingGroupAtIndex(int index)
		{
			return RemoveTagAtIndex(Storage.GetTagIdAtIndex(index));
		}

		private void RemoveTag(int[] entry, int index)
		{
			if (EntriesArray.GetEntryType(entry, index) == LinkTypeTag)
			{
				RgStorage.RemoveRgTagAtIndex(EntriesArray.GetEntryLink(entry, index),
					EntriesArray.GetParentEntryLink(entry));
				RemoveTagFromEntry(entry, index);

				if (EntriesArray.IsEmpty(entry))
				{
					RgStorage.DecrementLeadingTag(Group.LeadingTag, Group.RgId, 0,
						EntriesArray.GetParentEntryLink(entry)); //remove repeating group from fix message
				}
			}
			else if (EntriesArray.GetEntryType(entry, index) == LinkTypeRg)
			{
				var group = RepeatingGroupPool.RepeatingGroup;
				RgStorage.GetRepeatingGroup(EntriesArray.GetEntryTag(entry, index),
					EntriesArray.GetEntryLink(entry, index), group);
				group.Remove();
				group.Release();
			}
		}

		private void RemoveTagFromEntry(int[] entry, int index)
		{
			EntriesArray.RemoveTagAtIndex(entry, index);
			Group.UpdateParentEntries(entry, RgStorage.Entries,
				EntriesArray.GetLastTagIndexInFixMessage(entry) + 1, -1);
		}

		/// <summary>
		/// Returns repeating group from entry by leading tag. If group doesn't exist, it will be added. </summary>
		/// <param name="leadingTag"> leading tag for repeating group </param>
		/// <returns> instance of RepeatingGroup from RepeatingGroupPool. There is no need to call release for this object. </returns>
		public virtual RepeatingGroup GetOrAddRepeatingGroup(int leadingTag)
		{
			if (IsRepeatingGroupExists(leadingTag))
			{
				return GetRepeatingGroup(leadingTag);
			}

			return AddRepeatingGroup(leadingTag);
		}

		/// <summary>
		/// Fills passed repeating group instance by data from entry. If group doesn't exist, it will be added. </summary>
		/// <param name="leadingTag"> leading tag for repeating group </param>
		/// <param name="group"> repeating group object for fill </param>
		public virtual void GetOrAddRepeatingGroup(int leadingTag, RepeatingGroup group)
		{
			if (IsRepeatingGroupExists(leadingTag))
			{
				GetRepeatingGroup(leadingTag, group);
			}
			else
			{
				AddRepeatingGroup(leadingTag, group);
			}
		}

		/// <summary>
		/// Checks is entry contains nested group with passed leading tag.
		/// Note that empty groups, that doesn't appear in the message, also considered existing </summary>
		/// <param name="leadingTag"> leading tag for check </param>
		/// <returns> true if repeating group with passed leading tag exists. </returns>
		public virtual bool IsRepeatingGroupExists(int leadingTag)
		{
			var isShowedGroup = IsGroupTag(leadingTag);
			if (!isShowedGroup)
			{
				return _hiddenLeadingTagsArray.FindInHidedLeadingTagsByEntryOwner(leadingTag, EntryIndex) != -1;
			}

			return isShowedGroup;
		}

		/// <summary>
		/// Checks whether tag is leading tag of group </summary>
		/// <param name="tag"> tag for check </param>
		/// <returns> true if tag is leading tag of group, false if tag doesn't exists or is not leading tag of group </returns>
		public virtual bool IsGroupTag(int tag)
		{
			var entry = EntriesArray.GetEntry(EntryIndex);
			for (var i = EntriesHeaderSize; i < EntriesArray.GetArrayEnd(entry); i += EntriesEntrySize)
			{
				if (EntriesArray.GetEntryTag(entry, i) == tag)
				{
					return EntriesArray.GetEntryType(entry, i) == LinkTypeRg;
				}
			}

			return false;
		}

		/// <summary>
		/// Checks whether tag is group tag at index </summary>
		/// <param name="index"> number of tag in entry (not in entire FIX message) </param>
		/// <returns> true if tag is group tag </returns>
		public virtual bool IsGroupTagAtIndex(int index)
		{
			var size = Count;
			if (index < size && index >= 0)
			{
				var entry = EntriesArray.GetEntry(EntryIndex);
				return EntriesArray.GetTagTypeAtIndex(entry, index) == LinkTypeRg;
			}

			throw new IndexOutOfRangeException("Invalid index for entry with size " + size);
		}

		public virtual int GetTagIndex(int tag)
		{
			return FindTagLink(tag);
		}

		private int FindTagLink(int tag)
		{
			if (Deleted)
			{
				throw new InvalidOperationException("Entry was deleted. You should create new entry");
			}

			var entry = EntriesArray.GetEntry(EntryIndex);
			for (var i = EntriesHeaderSize; i < EntriesArray.GetArrayEnd(entry); i += EntriesEntrySize)
			{
				if (EntriesArray.GetEntryTag(entry, i) == tag)
				{
					if (EntriesArray.GetEntryType(entry, i) == LinkTypeTag)
					{
						return EntriesArray.GetEntryLink(entry, i);
					}

					var repeatingGroup = RgArray.GetRgArrayById(tag, EntriesArray.GetEntryLink(entry, i));
					if (repeatingGroup == null)
					{
						return -1;
					}

					return RgArray.GetRgLeadingTagIndexInFixMsg(repeatingGroup);
				}
			}

			return -1;
		}

		public virtual void Clear()
		{
			for (var i = 0; i < AllocatedSubGroups.Count; i++)
			{
				RepeatingGroupPool.ReturnObj(AllocatedSubGroups[i]);
			}

			AllocatedSubGroups.Clear();
			ReleaseNeeded = true;
			Storage = null;
			Group = null;
			EntryIndex = -1;
			Deleted = false;
			GroupTags = null;
			_hiddenLeadingTagsArray = null;
			EntriesArray = null;
			RgArray = null;
			OuterLeadingTags = null;
		}

		/// <summary>
		/// Returns repeating group from entry by leading tag </summary>
		/// <param name="leadingTag"> leading tag for repeating group </param>
		/// <returns> instance of RepeatingGroup from RepeatingGroupPool. There is no need to call release for this object. </returns>
		public virtual RepeatingGroup GetRepeatingGroup(int leadingTag)
		{
			RepeatingGroup group = null;
			for (var i = 0; i < AllocatedSubGroups.Count; i++)
			{
				group = AllocatedSubGroups[i];
				if (group.LeadingTag == leadingTag)
				{
					break;
				}
			}

			if (group == null || group.LeadingTag != leadingTag)
			{
				group = RepeatingGroupPool.RepeatingGroup;
				@group.ReleaseNeeded = false;
				AllocatedSubGroups.Add(group);
			}

			return InitRepeatingGroup(leadingTag, group);
		}

		/// <summary>
		/// Fills passed repeating group instance by data from entry </summary>
		/// <param name="leadingTag"> leading tag for repeating group </param>
		/// <param name="group"> repeating group object for fill </param>
		public virtual void GetRepeatingGroup(int leadingTag, RepeatingGroup group)
		{
			group = InitRepeatingGroup(leadingTag, group);
			if (group == null)
			{
				throw new FieldNotFoundException("There is no Repeating Group with tag = " + leadingTag);
			}
		}

		private RepeatingGroup InitRepeatingGroup(int leadingTag, RepeatingGroup group)
		{
			var rgIndex = _hiddenLeadingTagsArray.FindInHidedLeadingTagsByEntryOwner(leadingTag, EntryIndex);
			if (rgIndex == -1)
			{
				var entry = RgStorage.Entries[EntryIndex];
				for (var i = EntriesHeaderSize; i < EntriesArray.GetArrayEnd(entry); i += EntriesEntrySize)
				{
					if (EntriesArray.GetEntryTag(entry, i) == leadingTag &&
						EntriesArray.GetEntryType(entry, i) == LinkTypeRg)
					{
						RgStorage.GetRepeatingGroup(leadingTag, EntriesArray.GetEntryLink(entry, i), group);
						return group;
					}
				}

				return null;
			}

			var rgId = _hiddenLeadingTagsArray.GetRgId(rgIndex);
			RgStorage.GetRepeatingGroup(leadingTag, rgId, group);
			return group;
		}

		private void IsTagValid(int tag)
		{
			if (!GroupTags.Contains(tag))
			{
				throw new UnresolvedGroupTagException(tag, Group.LeadingTag, Group.Version, Group.MsgType);
			}
		}

		private void IsLeadingTagValid(int leadingTag)
		{
			if (!OuterLeadingTags.Contains(leadingTag) && !NestedLeadingTags.Contains(leadingTag))
			{
				throw new InvalidLeadingTagException(leadingTag, Group.Version, Group.MsgType);
			}
		}

		private void IsDuplicateTag(int tag)
		{
			var entry = EntriesArray.GetEntry(EntryIndex);
			for (var i = EntriesHeaderSize; i < EntriesArray.GetArrayEnd(entry); i += EntriesEntrySize)
			{
				if (EntriesArray.GetEntryTag(entry, i) == tag)
				{
					throw new DuplicateTagException(Group.LeadingTag, tag, Group.Version, Group.MsgType);
				}
			}
		}

		private void IsDuplicateGroup(int leadingTag)
		{
			var entry = EntriesArray.GetEntry(EntryIndex);
			for (var i = EntriesHeaderSize; i < EntriesArray.GetArrayEnd(entry); i += EntriesEntrySize)
			{
				if (EntriesArray.GetEntryTag(entry, i) == leadingTag)
				{
					throw new DuplicateGroupException(leadingTag, Group.Version, Group.MsgType);
				}
			}

			for (var i = HidedHeaderSize; i < _hiddenLeadingTagsArray.ArrayEnd; i += HidedEntrySize)
			{
				if (_hiddenLeadingTagsArray.GetTag(i) == leadingTag &&
					_hiddenLeadingTagsArray.GetEntryLink(i) == EntryIndex)
				{
					throw new DuplicateGroupException(leadingTag, Group.Version, Group.MsgType);
				}
			}
		}

		internal virtual void Init(RepeatingGroup group, int entryIndex, IndexedStorage storage)
		{
			Group = group;
			Storage = storage;
			EntryIndex = entryIndex;
			RgStorage = @group.RepeatingGroupStorage;
			RgArray = @group.RepeatingGroupsArray;
			EntriesArray = @group.EntriesArray;
			GroupTags = group.Dict.GetGroupTags(group.LeadingTag);
			OuterLeadingTags = group.Dict.GetOuterLeadingTags();
			NestedLeadingTags = group.Dict.GetNestedLeadingTags();
			_hiddenLeadingTagsArray = group.HiddenLeadingTagsArray;
			Deleted = false;
		}

		public override string ToString()
		{
			var entry = EntriesArray.GetEntry(EntryIndex);
			if (entry != null)
			{
				return StringHelper.NewString(AsByteArray());
			}

			return "";
		}

		public virtual string ToPrintableString()
		{
			var entry = EntriesArray.GetEntry(EntryIndex);
			if (entry != null)
			{
				return FixMessagePrintableFormatter.ToPrintableString(ToString());
			}

			return "";
		}

		/// <summary>
		/// Returns number of added tag in entry.
		/// </summary>
		public virtual int Count
		{
			get
			{
				if (Deleted)
				{
					return 0;
				}

				var entry = EntriesArray.GetEntry(EntryIndex);
				return (EntriesArray.GetArrayEnd(entry) - EntriesHeaderSize) / EntriesEntrySize;
			}
		}

		/// <summary>
		/// Returns group which owns entry </summary>
		/// <returns> group which owns entry </returns>
		public virtual RepeatingGroup GetGroup()
		{
			return Group;
		}

		public virtual bool IsEmpty => Count <= 0;

		/// <summary>
		/// Returns entry index in FIX message </summary>
		/// <returns> entry index in FIX message </returns>
		public virtual int GetEntryIndex()
		{
			return Group.GetStartTagLink(EntriesArray.GetEntry(EntryIndex));
		}

		public virtual byte[] AsByteArray()
		{
			var entry = EntriesArray.GetEntry(EntryIndex);
			var entryTagsSize = Group.GetEntryTagsSize(entry);
			var result = new byte[entryTagsSize];
			if (result.Length == 0)
			{
				return result;
			}

			var startIndex = Group.GetStartTagLink(entry);
			var endIndex = EntriesArray.GetLastTagIndexInFixMessage(entry);
			var offset = 0;
			var msgSize = Storage.Count;
			for (var i = startIndex; i <= endIndex && i < msgSize; i++)
			{
				offset = Group.WriteTag(result, Storage.GetTagIdAtIndex(i), offset);
				offset = Group.WriteValue(result, Storage.GetTagValueAsBytesAtIndex(i), offset);
			}

			return result;
		}

		/// <summary>
		/// Copy repeating group to entry </summary>
		/// <param name="source"> repeating group for copy </param>
		/// <returns> copied repeating group </returns>
		public virtual RepeatingGroup CopyRepeatingGroup(RepeatingGroup source)
		{
			var dst = AddRepeatingGroup(source.LeadingTag, source.Validation);
			for (var i = 0; i < source.Count; i++)
			{
				dst.CopyEntry(source.GetEntry(i));
			}

			return dst;
		}

		/// <summary>
		/// Copy repeating group to entry </summary>
		/// <param name="source"> repeating group for copy </param>
		/// <param name="dst"> entry for hold copied repeating group </param>
		public virtual void CopyRepeatingGroup(RepeatingGroup source, RepeatingGroup dst)
		{
			AddRepeatingGroup(source.LeadingTag, source.Validation, dst);
			for (var i = 0; i < source.Count; i++)
			{
				dst.CopyEntry(source.GetEntry(i));
			}
		}
	}
}