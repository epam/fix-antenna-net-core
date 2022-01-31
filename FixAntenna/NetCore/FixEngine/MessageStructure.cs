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
using System.Linq;

namespace Epam.FixAntenna.NetCore.FixEngine
{
	public sealed class MessageStructure
	{
		/// <summary>
		/// Special value for defining the size of field with variable value length
		/// </summary>
		public const int VariableLength = -1;

		public List<int> Lengths { get; } = new List<int>();
		public List<int> TagIds { get; } = new List<int>();
		public List<ValueType> Types { get; } = new List<ValueType>();

		/// <summary>
		/// Specifies tagId and size of the tag value. All tags should be reserved in order like in generated message.
		/// Use <c>MessageStructure.VariableLength</c> to define the tag value that could be increased dynamically for concrete parameter.
		/// Keep in mind, this will slow performance and spawn new Objects in runtime.
		/// Reserved field will be added to the end of the structure.
		/// </summary>
		/// <param name="tagId">  tagId </param>
		/// <param name="length"> required amount of bytes for value or <c>MessageStructure.VariableLength</c> </param>
		public void Reserve(int tagId, int length)
		{
			Reserve(tagId, length, ValueType.ByteArray);
		}

		public void ReserveString(int tagId)
		{
			ReserveString(tagId, VariableLength);
		}

		public void ReserveString(int tagId, int length)
		{
			Reserve(tagId, length, ValueType.String);
		}

		public void ReserveLong(int tagId, int length)
		{
			Reserve(tagId, length, ValueType.Long);
		}

		public void Reserve(int tagId, int length, ValueType type)
		{
			CheckLength(length);
			TagIds.Add(tagId);
			Lengths.Add(length);
			Types.Add(type);
		}

		/// <summary>
		/// Specifies tagId and size of the tag value. Reserved field will be inserted at specified position.
		/// </summary>
		/// <param name="position"> position to insent the tag </param>
		/// <param name="tagId">    tagId </param>
		/// <param name="length">   required amount of bytes for value or <see cref="VariableLength"/> </param>
		/// <seealso cref="Reserve(int,int,int,ValueType)"> </seealso>
		public void Reserve(int position, int tagId, int length)
		{
			Reserve(position, tagId, length, ValueType.ByteArray);
		}

		public void ReserveString(int position, int tagId, int length)
		{
			Reserve(position, tagId, length, ValueType.String);
		}

		public void ReserveLong(int position, int tagId, int length)
		{
			Reserve(position, tagId, length, ValueType.Long);
		}

		public void Reserve(int position, int tagId, int length, ValueType type)
		{
			CheckLength(length);
			TagIds.Insert(position, tagId);
			Lengths.Insert(position, length);
			Types.Insert(position, type);
		}

		private void CheckLength(int length)
		{
			if (length <= 0 && length != VariableLength)
			{
				throw new ArgumentException(
					"Invalid length for reserved field. Should be VARIABLE_LENGTH or positive integer.");
			}
		}

		public int GetTagId(int index)
		{
			return TagIds[index];
		}

		public int GetLength(int index)
		{
			return Lengths[index];
		}

		public ValueType GetType(int index)
		{
			return Types[index];
		}

		public int Size => TagIds.Count;

		public bool ContainsTagId(int tag)
		{
			return TagIds.Contains(tag);
		}

		public int IndexOfTag(int tag)
		{
			return IndexOf(tag);
		}

		/// <summary>
		/// Append fields to the end of current structure.
		/// </summary>
		/// <param name="struct"> </param>
		public void Append(MessageStructure @struct)
		{
			TagIds.AddRange(@struct.TagIds);
			Lengths.AddRange(@struct.Lengths);
			Types.AddRange(@struct.Types);
		}

		public void Merge(MessageStructure @struct)
		{
			var size = @struct.Size;
			var origTagSize = TagIds.Count;
			for (var i = 0; i < size; i++)
			{
				var id = @struct.GetTagId(i);
				var pos = IndexOf(id);
				if (pos < 0 || pos >= origTagSize)
				{
					TagIds.Add(id);
					Lengths.Add(@struct.GetLength(i));
					Types.Add(@struct.GetType(i));
				}
				else
				{
					SetLengthAtIndex(pos, @struct.GetLength(i));
					SetTypeAtIndex(pos, @struct.GetType(i));
				}
			}
		}

		public int IndexOf(int tagId)
		{
			return TagIds.IndexOf(tagId);
		}

		/// <summary>
		/// Change the length for specific tag.
		/// </summary>
		/// <param name="tagId"> </param>
		/// <param name="length"> required amount of bytes for value or <see cref="VariableLength"/> </param>
		public void SetLength(int tagId, int length)
		{
			SetLength(tagId, 1, length);
		}

		/// <summary>
		/// Change the length for specific tag.
		/// </summary>
		/// <param name="tagId"> </param>
		/// <param name="occurance"> </param>
		/// <param name="length">    required amount of bytes for value or <see cref="VariableLength"/> </param>
		public void SetLength(int tagId, int occurance, int length)
		{
			CheckLength(length);
			var tagIndex = 0;
			for (var i = occurance; i > 0; i--)
			{
				tagIndex = TagIds.Skip(tagIndex).Take(TagIds.Count).ToList().IndexOf(tagId);
				if (tagIndex < 0)
				{
					throw new ArgumentException("There is no reserved field for tag " + tagId);
				}
			}

			SetLengthAtIndex(tagIndex, length);
		}

		public void SetLengthAtIndex(int tagIndex, int length)
		{
			Lengths[tagIndex] = length;
		}

		public void SetType(int tagId, ValueType type)
		{
			SetType(tagId, 1, type);
		}

		public void SetType(int tagId, int occurance, ValueType type)
		{
			var tagIndex = 0;
			for (var i = occurance; i > 0; i--)
			{
				tagIndex = TagIds.Skip(tagIndex).Take(TagIds.Count).ToList().IndexOf(tagId);
				if (tagIndex < 0)
				{
					throw new ArgumentException("There is no reserved field for tag " + tagId);
				}
			}

			SetTypeAtIndex(tagIndex, type);
		}

		public void SetTypeAtIndex(int tagIndex, ValueType type)
		{
			Types[tagIndex] = type;
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

			var that = (MessageStructure) o;

			if (!Lengths.SequenceEqual(that.Lengths))
			{
				return false;
			}

			if (!TagIds.SequenceEqual(that.TagIds))
			{
				return false;
			}

			if (!Types.SequenceEqual(that.Types))
			{
				return false;
			}

			return true;
		}

		public override int GetHashCode()
		{
			var result = TagIds.GetHashCode();
			foreach (var value in Lengths)
			{
				result = 31 * result + value.GetHashCode();
			}

			result = 31 * result + Types.GetHashCode();
			return result;
		}
	}
}