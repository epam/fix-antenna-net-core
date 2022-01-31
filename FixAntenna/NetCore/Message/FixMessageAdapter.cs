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
using Epam.FixAntenna.NetCore.Message.Format;
using Epam.FixAntenna.NetCore.Message.Storage;

namespace Epam.FixAntenna.NetCore.Message
{
	public abstract class FixMessageAdapter : AbstractFixMessage, IList<TagValue>
	{
		private readonly TagValueStorage _fields = new TagValueStorage(InitMaxFields);

		#region ctor
		protected internal FixMessageAdapter()
		{
		}

		protected internal FixMessageAdapter(bool isUserOwned) : base(isUserOwned)
		{
		}
		#endregion

		#region IList implementation
		/// <summary>
		/// Gets or sets <see cref="TagValue"/> field.
		/// </summary>
		/// <param name="index">Index of the field.</param>
		/// <returns></returns>
		public TagValue this[int index]
		{
			get => GetByIndex(index);
			set => SetByIndex(index, value);
		}

		public int IndexOf(TagValue item)
		{
			return GetTagIndex(item.TagId);
		}

		public void Insert(int index, TagValue item)
		{
			AddAtIndex(index, item);
		}

		public void RemoveAt(int index)
		{
			RemoveTagAtIndex(index);
		}
		#endregion

		#region ICollection implementation
		public bool IsReadOnly => false;

		void ICollection<TagValue>.Add(TagValue item)
		{
			AddTag(item);
		}

		public override void Clear()
		{
			ReleaseFixFieldCache();
			base.Clear();
		}

		public bool Contains(TagValue item)
		{
			return item != null && IsTagExists(item.TagId);
		}

		public void CopyTo(TagValue[] array, int arrayIndex)
		{
			if (array == null)
			{
				throw new ArgumentNullException(nameof(array));
			}

			if (arrayIndex < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(arrayIndex));
			}

			var size = Count;

			if (size > array.Length - arrayIndex)
			{
				throw new ArgumentException();
			}

			for (var i = 0; i < size; i++)
			{
				array[i + arrayIndex] = this[i];
			}
		}

		public bool Remove(TagValue item)
		{
			return item != null && RemoveTag(item.TagId);
		}
		#endregion

		public override IEnumerator<TagValue> GetEnumerator()
		{
			return base.GetTagValueIterator();
		}

		protected internal override void OnEnlarge(int ratio, int newSize)
		{
			_fields.Enlarge(ratio);
		}

		#region UpdateValue staff ...
		public override void UpdateValueAtIndex(int index, byte[] value)
		{
			base.UpdateValueAtIndex(index, value);
			_fields.InvalidateCachedField(index);
		}

		public override void UpdateCalendarValueAtIndex(int index, DateTimeOffset value,
			FixDateFormatterFactory.FixDateType type)
		{
			base.UpdateCalendarValueAtIndex(index, value, type);
			_fields.InvalidateCachedField(index);
		}

		public override void UpdateValueAtIndex(int index, double value, int precision)
		{
			base.UpdateValueAtIndex(index, value, precision);
			_fields.InvalidateCachedField(index);
		}

		public override void UpdateValueAtIndex(int index, long value)
		{
			base.UpdateValueAtIndex(index, value);
			_fields.InvalidateCachedField(index);
		}

		public override void UpdateValueAtIndex(int index, string str)
		{
			base.UpdateValueAtIndex(index, str);
			_fields.InvalidateCachedField(index);
		}

		public override void UpdateValueAtIndex(int index, bool value)
		{
			base.UpdateValueAtIndex(index, value);
			_fields.InvalidateCachedField(index);
		}

		public override void UpdateValueAtIndex(int index, byte[] value, int offset, int length)
		{
			base.UpdateValueAtIndex(index, value, offset, length);
			_fields.InvalidateCachedField(index);
		}

		public override void UpdateValueAtIndex(int index, TagValue value)
		{
			base.UpdateValueAtIndex(index, value);
			_fields.InvalidateCachedField(index);
		}
		#endregion

		public override bool RemoveTagAtIndex(int tagIndex, bool shiftRg)
		{
			InvalidateCachedFieldByIndex(tagIndex);
			return base.RemoveTagAtIndex(tagIndex, shiftRg);
		}

		public override int ReserveTagAtIndex(int addAtIndex, int tagId)
		{
			var fieldCount = base.ReserveTagAtIndex(addAtIndex, tagId);
			_fields.Shift(addAtIndex, 1, fieldCount);
			return fieldCount;
		}

		protected internal override void DeepCopy(IndexedStorage source)
		{
			base.DeepCopy(source);
			//TODO: in general FIXFields count could be less then overall fields count
			_fields.EnlargeTo(source.GetIndexCapacity());
		}
		
		/// <summary>
		/// Use addAllTags() instead of this
		/// </summary>
		/// <param name="c">
		/// @return </param>
		public bool AddAll(ICollection<TagValue> c)
		{
			if (c is FixMessage list)
			{
				return base.AddAll(list);
			}

			foreach (var tagValue in c)
			{
				Add(tagValue);
			}

			return true;
		}

		public virtual bool AddAll(int index, ICollection<TagValue> c)
		{
			//TODO: optimize
			foreach (var tagValue in c)
			{
				Add(index++, tagValue);
			}

			return true;
		}

		public virtual void Add(FixMessage list)
		{
			base.AddAll(list);
		}

		public virtual void Add(int index, TagValue element)
		{
			AddTagAtIndex(index, element);
		}

		public void AddAtIndex(int addAtIndex, TagValue field)
		{
			AddTagAtIndex(addAtIndex, field.TagId, field.Buffer, field.Offset, field.Length);
		}

		public int Length => Count;

		//TODO: move to field storage
		private void ReleaseFixFieldCache()
		{
			for (var i = 0; i < Count; i++)
			{
				_fields.InvalidateCachedField(i);
			}
		}

		/// <summary>
		/// Gets <see cref="TagValue"/> field by provided TagId value.
		/// </summary>
		/// <remarks>Seems to be main method to get field.</remarks>
		/// <param name="tagId"></param>
		/// <returns>Returns <see cref="TagValue"/> with specified TagId or null, if TagId was not found.</returns>

		public TagValue GetTag(int tagId)
		{
			return GetByTagId(tagId);
		}

		public TagValue GetTag(int tagId, int occurrence)
		{
			return GetByTagId(tagId, occurrence);
		}

		/// <summary>
		/// Gets <see cref="TagValue"/> field by provided TagId value.
		/// </summary>
		/// <remarks>Seems to be main method to get field.</remarks>
		/// <param name="tagId"></param>
		/// <returns>Returns <see cref="TagValue"/> with specified TagId or null, if TagId was not found.</returns>
		private TagValue GetByTagId(int tagId)
		{
			var index = GetTagIndex(tagId);
			return index == IndexedStorage.NotFound ? null : GetByIndex(index);
		}

		private TagValue GetByTagId(int tagId, int occurrence)
		{
			var index = GetTagIndex(tagId, occurrence);
			return index == IndexedStorage.NotFound ? null : GetByIndex(index);
		}

		private TagValue GetByIndex(int index)
		{
			TagValue field = null;

			if (index >= 0 && Count >= index)
			{
				field = _fields[index];
				if (field == null)
				{
					field = GetByIndex(index, IsOriginatingFromPool);
					_fields[index] = field;
				}
			}

			return field;
		}

		public bool TryGetLongByIndex(int index, out long value)
		{
			value = 0;
			var tv = GetByIndex(index);
			return tv != null && FixTypes.TryParseLong(tv.Buffer, tv.Offset, tv.Length, out value);
		}

		private TagValue GetByIndex(int index, bool isFromPool)
		{
			var storage = GetStorage(index);
			var fieldData = storage.GetByteArray(index);
			var fieldDataLength = GetTagValueLengthAtIndex(index);
			var fieldDataOffset = GetTagValueOffsetAtIndex(index);
			var tagId = GetTagIdAtIndex(index);

			if (isFromPool)
			{
				var data = RawFixUtil.CopyValueUsePool(fieldData, fieldDataOffset, fieldDataLength);
				var field = RawFixUtil.GetFieldFromPool();
				field.Reload(tagId, data, true);
				return field;
			}

			return new TagValue(tagId, RawFixUtil.CopyValue(fieldData, fieldDataOffset, fieldDataLength), true);
		}

		private void SetByIndex(int index, TagValue newValue)
		{
			Remove(index);
			Add(index, newValue);
		}

		public virtual TagValue Remove(int index)
		{
			var tagValue = this[index];
			RemoveTagAtIndex(index);
			return tagValue;
		}

		/// <summary>
		/// Utility method that splits current message into the repeating
		/// groups based on first mandatory tag in the repeating
		/// group (always first tag in the repeating group).
		/// </summary>
		/// <param name="tag"> the tag number </param>
		/// <returns> List of repeating groups (each one is separate FixMessage) </returns>
		public IList<List<TagValue>> SplitAsList(int tag)
		{
			var append = false;
			var result = new List<List<TagValue>>();
			List<TagValue> list = null;
			//TBD! optimize
			for (var i = 0; i < Count; i++)
			{
				if (base.GetTagIdAtIndex(i) == tag)
				{
					if (list != null)
					{
						result.Add(list);
					}

					list = new List<TagValue>();
					append = true;
				}

				if (append)
				{
					list.Add(GetTag(i));
				}
			}

			if (list != null)
			{
				result.Add(list);
			}

			return result;
		}

		/// <summary>
		/// Parse repeating group in FIX message
		/// </summary>
		/// <param name="rgTag">      Group amount tag </param>
		/// <param name="rgFirstTag"> The first tag. Tag just after size tag. </param>
		/// <param name="tagList">    List of expected tags </param>
		/// <returns> Repeating group </returns>
		public IList<IDictionary<int, TagValue>> ExtractGroup(int rgTag, int rgFirstTag, int[] tagList)
		{
			var rgTagIndex = GetTagIndex(rgTag);
			if (rgTagIndex < 0)
			{
				return new List<IDictionary<int, TagValue>>();
			}

			var noGrp = (int)GetByIndex(rgTagIndex).LongValue;
			var lst = new List<IDictionary<int, TagValue>>(noGrp);
			for (var i = 0; i < noGrp; i++)
			{
				lst.Add(new Dictionary<int, TagValue>(tagList.Length));
			}

			Array.Sort(tagList);
			var rgCounter = -1;

			for (var i = rgTagIndex + 1; i < Count; i++)
			{
				var field = this[i];
				var tag = field.TagId;
				if (tag == rgFirstTag)
				{
					rgCounter++;
					if (rgCounter >= noGrp)
					{
						break; // we was read all RG entries of this RG
					}
				}

				if (Array.BinarySearch(tagList, tag) != -1)
				{
					if (rgCounter == -1)
					{
						return NotifyInvalidMessage(rgTag, rgFirstTag);
					}

					lst[rgCounter][tag] = field;
				}
			}

			return lst;
		}

		public int GetTagAsInt(int tag)
		{
			return (int)GetTagValueAsLong(tag);
		}

		public int GetTagAsInt(int tag, int occurrence)
		{
			return (int)GetTagValueAsLong(tag, occurrence);
		}

		public int GetTagAsIntAtIndex(int index)
		{
			return (int)GetTagValueAsLongAtIndex(index);
		}

		private void InvalidateCachedFieldByIndex(int tagIndex)
		{
			_fields.InvalidateCachedField(tagIndex);
			var size = base.Count;
			if (tagIndex < size - 1)
			{
				_fields.ShiftBack(tagIndex, 1, size);
			}
		}
	}
}