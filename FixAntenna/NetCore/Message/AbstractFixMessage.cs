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
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message.SpecialTags;

namespace Epam.FixAntenna.NetCore.Message
{
	public abstract class AbstractFixMessage : HpExtendedIndexedStorage, IEnumerable<TagValue>
	{
		//Must be power of two
		internal const int InitMaxFields = 32;

		protected TagValueIterator Iterator;
		internal bool Standalone = true;

		public bool IsOriginatingFromPool { get; set; }

		/// <summary>
		/// Creates an empty message that is engine owned
		/// TBD: protect(hide) the constructor from user access. May bee need to have default constructor for user
		/// and special - for internal usage
		/// TBD: make with default modificator - all other should use NewInstanceFromPool
		/// </summary>
		/// <seealso cref="IsUserOwned"/>
		protected AbstractFixMessage() : this(false)
		{
		}

		/// <summary>
		/// Creates an empty message
		/// TBD: make with default modificator - all other should use NewInstanceFromPool
		/// </summary>
		/// <seealso cref="IsUserOwned"> </seealso>
		protected AbstractFixMessage(bool isUserOwned) : base(InitMaxFields)
		{
			Iterator = new TagValueIterator(this);
			IsUserOwned = isUserOwned;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public abstract IEnumerator<TagValue> GetEnumerator();

		/// <summary>
		/// Controls how the message object ownership is handled after the Send.
		/// <p/>
		/// The message objects that are not owned by user
		/// should not be accessed again after FIXSession.sendMessage() call is made
		/// <p/>
		/// Engine will take a copy of the user owned objects when enqueing them before sending.
		/// <p/>
		/// The engine owned messages that originate from pool will be freed by
		/// the engine automatically after the send. But before sendMessage was called,
		/// this is the user's responsibility to return it back using FixMessage.returnInstance()
		/// <p/>
		/// By default the heap allocated objects (new FixMessage()) are engine owned.
		/// <p/>
		/// The ownership of objects allocated from pool is explicitely requested in call:
		/// FixMessage.NewInstanceFromPool(boolean isUserOwned)
		/// </summary>
		public bool IsUserOwned { set; get; }

		protected internal bool IsPreparedMessage { get; set; }

		protected internal bool IsMessageIncomplete { get; set; }

		internal bool NeedCloneOnSend => IsUserOwned || ForceCloneOnSend;

		internal bool NeedReleaseAfterSend => !IsUserOwned && IsOriginatingFromPool;

		internal bool ForceCloneOnSend { get; set; }

		protected internal bool IsFree { get; set; } = true;

		//TBD! hide from user
		internal void SetBuffer(byte[] buf, int offset, int length)
		{
			Standalone = false;

			base.SetOriginalBuffer(buf, offset, length);
		}

		// usage in NewMessageChopper
		//TBD! try to remove external call to this method or hide it from public API
		internal sealed override void ShiftBuffer(byte[] buf, int offset, int length)
		{
			Standalone = false;
			base.ShiftBuffer(buf, offset, length);
		}

		protected internal abstract FixMessage MakeStandalone();

		protected internal virtual void SwitchToStandalone()
		{
			base.TransferDataToArena();
			Standalone = true;
		}

		public int Add(TagValue tagValue)
		{
			return AddTag(tagValue.TagId, tagValue.Buffer, tagValue.Offset, tagValue.Length);
		}

		internal int Add(int tag, int offset, int length)
		{
			return base.MapTagInOrigStorage(tag, offset, length);
		}

		internal int AddPrepared(int tag, int offset, int length)
		{
			return base.MapPreparedTagInOrigStorage(tag, offset, length);
		}

		protected bool AddAll(FixMessage list)
		{
			var iterator = list.GetTagValueIterator();
			while (iterator.MoveNext())
			{
				var next = iterator.Current;
				Add(next);
			}

			return true;
		}

		public override void Clear()
		{
			Standalone = true;
			IsMessageIncomplete = false;
			IsPreparedMessage = false;
			base.Clear(); // TBD! get rid of this
		}

		public FixMessage DeepCopyTo(FixMessage cloned)
		{
			cloned.IsPreparedMessage = IsPreparedMessage;
			cloned.IsMessageIncomplete = IsMessageIncomplete;
			cloned.Standalone = false;

			cloned.DeepCopy(this);
			cloned.MakeStandalone();
			return cloned;
		}

		protected internal override bool CanCopyInPlace(int index, int oldLen, int newLen)
		{
			var canCopyInPlace = base.CanCopyInPlace(index, oldLen, newLen);
			if (IsPreparedMessage)
			{
				return canCopyInPlace;
			}

			return canCopyInPlace && oldLen == newLen;
		}

		protected internal override bool CanCopyInPlaceNumber(int index, int oldLen, int newLen)
		{
			var canCopyInPlace = base.CanCopyInPlaceNumber(index, oldLen, newLen);
			if (IsPreparedMessage)
			{
				return canCopyInPlace;
			}

			return canCopyInPlace && oldLen == newLen;
		}

		public int GetTagNumAtIndex(int index)
		{
			return base.GetTagIdAtIndex(index);
		}

		public int GetTagLength(int tag)
		{
			return base.GetTagValueLength(tag);
		}

		// TBD!   Add all the UTC timestamp get/set methods for FIX time types

		/// <summary>
		/// Calculates body length for collection.
		/// </summary>
		/// <returns> body length </returns>
		public int CalculateBodyLength()
		{
			var length = 0;
			var iterator = GetTagValueIterator();
			while (iterator.MoveNext())
			{
				var nextTag = iterator.Current;
				var tag = nextTag.TagId;
				if (tag > Tags.CheckSum || tag < Tags.BeginString)
				{
					// do not count tags 8, 9 and 10
					length += GetTagBytesLength(tag) + nextTag.Length + 1 + 1; // + '=' + 'SOH'
				}
			}

			return length;
		}

		/// <summary>
		/// Calculates checksum.
		/// </summary>
		/// <returns> checksum </returns>
		//TBD Deprecated
		public int CalculateChecksum()
		{
			long sum = 0;
			var iterator = GetTagValueIterator();
			while (iterator.MoveNext())
			{
				var nextTag = iterator.Current;
				if (nextTag.TagId != Tags.CheckSum)
				{
					sum += nextTag.CalculateChecksum() + FieldSeparator;
				}
			}

			return (int)(sum % 256);
		}

		/// <summary>
		/// Converts collection of fix fields to string.
		/// </summary>
		public sealed override string ToString()
		{
			return StringHelper.NewString(AsByteArray(DefaultMaskedTags.Instance));
		}

		/// <summary>
		/// Converts collection of fix fields to string, not masking fields 554, 925.
		/// </summary>
		/// <returns></returns>
		public string ToUnmaskedString()
		{
			return StringHelper.NewString(AsByteArray());
		}

		/// <summary>
		/// Converts collection of FIX fields to string. Some fields (by default 554, 925) masked with asterisks.
		/// </summary>
		/// <returns>Returns string, where some fields masked with asterisks.</returns>
		public string ToPrintableString()
		{
			return ToPrintableString(DefaultMaskedTags.Instance);
		}

		/// <summary>
		/// Converts collection of FIX fields to string. Some fields (by default 554, 925) masked with asterisks.
		/// </summary>
		/// <returns>Returns string, where some fields masked with asterisks.</returns>
		/// <param name="maskedTags">IMaskedTags instance or null (DefaultMaskedTags will be used).</param>
		internal string ToPrintableString(IMaskedTags maskedTags)
		{
			var tags = maskedTags ?? DefaultMaskedTags.Instance;
			var maskedStr = StringHelper.NewString(AsByteArray(tags));
			return FixMessagePrintableFormatter.ToPrintableString(maskedStr);
		}

		/// <summary>
		/// Writes field list to array of bytes.
		/// </summary>
		/// <returns> byte origBuffer </returns>
		public byte[] AsByteArray()
		{
			var result = new byte[RawLength];
			ToByteArrayAndReturnNextPosition(result, 0);
			return result;
		}

		/// <summary>
		/// Writes field list to array of bytes.
		/// </summary>
		/// <returns> byte origBuffer </returns>
		internal byte[] AsByteArray(IMaskedTags maskedTags)
		{
			var result = new byte[RawLength];
			ToByteArrayAndReturnNextPosition(result, 0, maskedTags);
			return result;
		}

		private int GetTagBytesLength(int tag)
		{
			var length = 1;
			while ((tag /= 10) > 0)
			{
				length++;
			}

			return length;
		}

		public bool IsMessageBufferContinuous
		{
			get
			{
				if (!IsPreparedMessage || IsMessageIncomplete)
				{
					return false;
				}

				return base.IsAllTagsInOneBuffer;
			}
		}

		/// <summary>
		/// Writes the list of field to the <c>origBuffer</c>, and returns the next index.
		/// The <c>SOH</c> symbol is added after each field.
		/// </summary>
		/// <param name="dst">    the origBuffer </param>
		/// <param name="offset"> the offset in origBuffer </param>
		public int ToByteArrayAndReturnNextPosition(byte[] dst, int offset)
		{
			return ToByteArrayAndReturnNextPosition(dst, offset, null);
		}

		internal int ToByteArrayAndReturnNextPosition(byte[] dst, int offset, IMaskedTags maskedTags)
		{
			if (IsMessageIncomplete)
			{
				throw new Exception(
					"cannot send incomplete message that contains tags skipped by the FIXParseListener user's callback");
			}

			return IsPreparedMessage
				? base.PreparedToByteArrayAndReturnNextPosition(dst, offset, maskedTags)
				: base.GenericMessageToByteArrayAndReturnNextPosition(dst, offset, maskedTags);
		}

		/// <summary>
		/// Utility method that splits current message into the repeating
		/// groups based on first mandatory tag in the repeating
		/// group (always first tag in the repeating group).
		/// </summary>
		/// <param name="tag"> the tag number </param>
		/// <returns> List of repeating groups (each one is separate <see cref="FixMessage"/>) </returns>
		public IList<FixMessage> Split(int tag)
		{
			var append = false;
			IList<FixMessage> result = new List<FixMessage>();
			FixMessage list = null;
			var iterator = GetTagValueIterator();
			while (iterator.MoveNext())
			{
				var nextTag = iterator.Current;
				var currTagId = nextTag.TagId;
				if (currTagId == tag)
				{
					if (list != null)
					{
						result.Add(list);
					}

					list = IsOriginatingFromPool
						? FixMessageFactory.NewInstanceFromPool(false)
						: new FixMessage(false);
					append = true;
				}

				if (append)
				{
					list.Add(nextTag);
				}
			}

			if (list != null)
			{
				result.Add(list);
			}

			return result;
		}

		protected abstract IList<IDictionary<int, TagValue>> NotifyInvalidMessage(int rgTag, int rgFirstTag);

		public sealed override int GetTagIndex(int tag)
		{
			return base.GetTagIndex(tag);
		}

		/// <summary>
		/// Removes a fix field with specified tag from collection.
		/// The methods removes the first occurrence of the specified tag.
		/// </summary>
		/// <param name="tag"> the fix tag. </param>
		/// <returns> <tt>true</tt> if the element was removed. </returns>
		public sealed override bool RemoveTag(int tag)
		{
			var tagIndex = base.GetTagIndex(tag);
			return tagIndex != NotFound && RemoveTagAtIndex(tagIndex);
		}

		public virtual bool IsEmpty
		{
			get { return Count <= 0; }
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}

			if (o == null || GetType() != o.GetType())
			{
				return false;
			}

			var that = (AbstractFixMessage)o;

			if (Count != that.Count)
			{
				return false;
			}

			var value1 = new TagValue();
			var value2 = new TagValue();
			try
			{
				for (var i = 0; i < Count; i++)
				{
					LoadTagValueByIndex(i, value1);
					that.LoadTagValueByIndex(i, value2);
					if (!value1.Equals(value2))
					{
						return false;
					}
				}
			}
			catch (FieldNotFoundException)
			{
				return false;
			}

			return true;
		}

		protected internal virtual IEnumerator<TagValue> GetTagValueIterator()
		{
			Iterator.Reset();
			return Iterator;
		}

		protected sealed class TagValueIterator : IEnumerator<TagValue>
		{
			private TagValue _instance = new TagValue();
			private readonly AbstractFixMessage _message;
			private int _iteratorIdx = -1;

			public TagValueIterator(AbstractFixMessage message)
			{
				_message = message;
			}

			public TagValue Current
			{
				get
				{
					_message.LoadTagValueByIndex(_iteratorIdx, _instance);
					return _instance;
				}
			}

			object IEnumerator.Current => Current;

			public void Reset()
			{
				_iteratorIdx = -1;
			}

			public bool MoveNext()
			{
				if (_iteratorIdx + 1 >= _message.Count)
				{
					return false;
				}

				_iteratorIdx++;
				return true;
			}

			public void Dispose()
			{
			}
		}
	}
}