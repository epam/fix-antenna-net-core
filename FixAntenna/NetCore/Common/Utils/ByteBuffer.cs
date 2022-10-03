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
using System.Buffers.Binary;
using System.IO;
using Epam.FixAntenna.NetCore.Common.Pool;

namespace Epam.FixAntenna.NetCore.Common.Utils
{
	/// <summary>
	/// The byte buffer helper class.
	/// </summary>
	public sealed class ByteBuffer
	{
		private const int DefaultBufferSize = 1024;

		private readonly BinaryReader _reader;
		private readonly BinaryWriter _writer;

		/// <summary>
		/// Creates the <c>ByteBuffer</c> with allocated byte buffer.
		/// The default buffer length if 1024 bytes.
		/// </summary>
		/// <param name="size">buffer size in bytes</param>
		public ByteBuffer(int size = DefaultBufferSize)
		{
			Buffer = AllocateBuffer(size);
			_writer = new BinaryWriter(Buffer);
			_reader = new BinaryReader(Buffer);
		}

		private ByteBuffer(MemoryStream buffer)
		{
			Buffer = buffer;
		}

		private MemoryStream AllocateBuffer(int capacity)
		{
			var stream = new MemoryStream(capacity);
			stream.SetLength(capacity);
			return stream;
		}

		public ByteBuffer Clear()
		{
			Buffer.Position = 0;
			return this;
		}

		public MemoryStream Buffer { get; }

		public ByteBuffer AddLikeString(long value, int minLen = 1)
		{
			var isNegative = false;
			long length = minLen;
			if (value < 0)
			{
				isNegative = true;
				value = -value;
				length++;
			}

			var valueCopy = value;
			var valueLength = 1;
			while ((valueCopy /= 10L) > 0L)
			{
				valueLength++;
			}

			length = Math.Max(length, isNegative ? valueLength + 1 : valueLength);

			CheckAndExtend(length);
			if (isNegative)
			{
				Buffer.WriteByte((byte)'-');
				length--;
			}

			var position = Buffer.Position;
			var last = position + length;
			for (var i = last - 1L; i >= position; i--)
			{
				Buffer.Position = i;
				Buffer.WriteByte((byte)(value % 10L + '0'));
				value /= 10L;
			}

			Buffer.Position = last;
			return this;
		}

		public void CheckAndExtend(long length)
		{
			var size = Buffer.Position + length;

			if (size > Buffer.Capacity)
			{
				IncreaseBuffer(size - Buffer.Capacity);
			}
		}

		public ByteBuffer Add(string s)
		{
			CheckAndExtend(s.Length);
			return Put(s);
		}

		public ByteBuffer Put(string s)
		{
			var length = s.Length;
			for (var i = 0; i < length; i++)
			{
				_writer.Write((byte)s[i]);
			}

			return this;
		}

		/// <summary>
		/// Appends the array argument to internal buffer.
		/// </summary>
		/// <returns> a reference to this object. </returns>
		public ByteBuffer Add(byte[] array)
		{
			CheckAndExtend(array.Length);
			_writer.Write(array);
			return this;
		}

		/// <summary>
		/// Adds byte to buffer.
		/// </summary>
		/// <param name="b"> a byte </param>
		public ByteBuffer Add(byte b)
		{
			if (Buffer.Length == Buffer.Position)
			{
				IncreaseBuffer(64);
			}

			_writer.Write(b);
			return this;
		}

		/// <summary>
		/// Adds char to buffer.
		/// </summary>
		/// <param name="b"> a char </param>
		public ByteBuffer Add(char b)
		{
			if (Buffer.Position == Buffer.Length)
			{
				IncreaseBuffer(64);
			}

			_writer.Write((byte)b);

			return this;
		}

		/// <summary>
		/// Appends the array argument to internal buffer.
		/// </summary>
		/// <returns> a reference to this object. </returns>
		public ByteBuffer Add(byte[] array, int start, int len)
		{
			CheckAndExtend(len);
			_writer.Write(array, start, len);
			return this;
		}

		public ByteBuffer Add(ByteBuffer buffer)
		{
			return Add(buffer.Buffer);
		}

		public ByteBuffer Add(MemoryStream buffer)
		{
			Add(buffer.GetBuffer(), (int)buffer.Position, (int)(buffer.Length - buffer.Position));
			return this;
		}

		/// <summary>
		/// Checks if buffer has a numOfBytes.
		/// </summary>
		/// <param name="length"> the requested space </param>
		/// <returns> boolean true if has </returns>
		public bool IsAvailable(int length)
		{
			return Buffer.Position + length <= Buffer.Length;
		}

		/// <summary>
		/// Gets or sets offset of buffer.
		/// </summary>
		/// <value> the new offset </value>
		public int Offset
		{
			set
			{
				if (Buffer.Length < value)
				{
					Buffer.SetLength(value);
				}

				Buffer.Position = value;
			}
			get => Position;
		}

		/// <summary>
		/// Increases the buffer.
		/// </summary>
		/// <param name="increase"> the number of bytes </param>
		public void IncreaseBuffer(long increase)
		{
			if (increase <= 0)
			{
				throw new ArgumentException();
			}

			var targetCapacity = Buffer.Length + increase;
			Buffer.SetLength(targetCapacity);
		}

		public byte[] ToArray()
		{
			return Buffer.ToArray();
		}

		/// <summary>
		/// Gets internal byte buffer (buffer itself, NOT a copy)
		/// </summary>
		public byte[] GetByteArray()
		{
			//TODO make sure it returns own array
			return Buffer.GetBuffer();
		}

		/// <summary>
		/// Gets internal byte buffer.
		/// </summary>
		public byte[] GetByteArray(int position, int length)
		{
			var currentPosition = Buffer.Position;
			Buffer.Position = position;
			var result = new byte[length];
			_reader.Read(result, 0, length);
			Buffer.Position = currentPosition;
			return result;
		}

		public void Release()
		{
			ByteBufferPool.Instance.Release(this);
		}

		public int Capacity()
		{
			return Buffer.Capacity;
		}

		public int Limit()
		{
			return (int)Buffer.Length;
		}

		public ByteBuffer Limit(long limit)
		{
			Buffer.SetLength(limit);
			return this;
		}

		public int Position
		{ 
			get =>(int)Buffer.Position;
			set => Buffer.Position = value;
		}

		public ByteBuffer Put(byte[] src)
		{
			_writer.Write(src);
			return this;
		}

		public ByteBuffer Put(byte b)
		{
			_writer.Write(b);
			return this;
		}

		public byte Get()
		{
			return _reader.ReadByte();
		}

		public byte Get(long index)
		{
			var position = Buffer.Position;
			Buffer.Position = index;
			var result = _reader.ReadByte();
			Buffer.Position = position;
			return result;
		}

		public ByteBuffer Put(long index, byte b)
		{
			var position = Buffer.Position;
			Buffer.Position = index;
			_writer.Write(b);
			Buffer.Position = position;
			return this;
		}

		public ByteBuffer Put(byte[] src, int offset, int length)
		{
			_writer.Write(src, offset, length);
			return this;
		}

		public ByteBuffer Get(byte[] dst)
		{
			_reader.Read(dst, 0, dst.Length);
			return this;
		}

		public ByteBuffer Get(byte[] dst, int offset, int length)
		{
			_reader.Read(dst, offset, length);
			return this;
		}

		public ByteBuffer PutChar(char value)
		{
			_writer.Write(Convert.ToByte(value));
			return this;
		}

		public ByteBuffer PutChar(long index, char value)
		{
			var position = Buffer.Position;
			Buffer.Position = index;
			_writer.Write(value);
			Buffer.Position = position;
			return this;
		}

		public char GetChar()
		{
			return _reader.ReadChar();
		}

		public char GetChar(long index)
		{
			var position = Buffer.Position;
			Buffer.Position = index;
			var result = _reader.ReadChar();
			Buffer.Position = position;
			return result;
		}

		public ByteBuffer PutInt(int value)
		{
			_writer.Write(value);
			return this;
		}

		public ByteBuffer PutIntBe(int value)
		{
			_writer.Write(BinaryPrimitives.ReverseEndianness(value));
			return this;
		}

		public ByteBuffer PutInt(long index, int value)
		{
			var position = Buffer.Position;
			Buffer.Position = index;
			_writer.Write(value);
			Buffer.Position = position;
			return this;
		}

		public int GetInt()
		{
			return _reader.ReadInt32();
		}

		public int GetIntBe()
		{
			return BinaryPrimitives.ReadInt32BigEndian(_reader.ReadBytes(4).AsSpan());
		}

		public int GetInt(long index)
		{
			var position = Buffer.Position;
			Buffer.Position = index;
			var result = _reader.ReadInt32();
			Buffer.Position = position;
			return result;
		}

		public ByteBuffer PutLong(long value)
		{
			_writer.Write(value);
			return this;
		}

		public ByteBuffer PutLongBe(long value)
		{
			_writer.Write(BinaryPrimitives.ReverseEndianness(value));
			return this;
		}

		public ByteBuffer PutLong(long index, long value)
		{
			var position = Buffer.Position;
			Buffer.Position = index;
			_writer.Write(value);
			Buffer.Position = position;
			return this;
		}

		public ByteBuffer PutLongBe(long index, long value)
		{
			var position = Buffer.Position;
			Buffer.Position = index;
			_writer.Write(BinaryPrimitives.ReverseEndianness(value));
			Buffer.Position = position;
			return this;
		}

		public long GetLong()
		{
			return _reader.ReadInt64();
		}

		public long GetLongBe()
		{
			return BinaryPrimitives.ReadInt64BigEndian(_reader.ReadBytes(8).AsSpan());
		}

		public long GetLong(long index)
		{
			var position = Buffer.Position;
			Buffer.Position = index;
			var result = _reader.ReadInt64();
			Buffer.Position = position;
			return result;
		}

		public long GetLongBe(long index)
		{
			var position = Buffer.Position;
			Buffer.Position = index;
			var result = BinaryPrimitives.ReadInt64BigEndian(_reader.ReadBytes(8).AsSpan());
			Buffer.Position = position;
			return result;
		}

		public ByteBuffer PutDouble(double value)
		{
			_writer.Write(value);
			return this;
		}

		public ByteBuffer PutDouble(long index, double value)
		{
			var position = Buffer.Position;
			Buffer.Position = index;
			_writer.Write(value);
			Buffer.Position = position;
			return this;
		}

		public double GetDouble()
		{
			return _reader.ReadDouble();
		}

		public double GetDouble(long index)
		{
			var position = Buffer.Position;
			Buffer.Position = index;
			var result = _reader.ReadDouble();
			Buffer.Position = position;
			return result;
		}

		public ByteBuffer PutInAll(long start, long end, byte value)
		{
			var position = Buffer.Position;
			Buffer.Position = start;
			for (var i = start; i < end; i++)
			{
				_writer.Write(value);
			}

			Buffer.Position = position;
			return this;
		}

		public int Remaining()
		{
			return (int)(Buffer.Length - Buffer.Position);
		}

		public ByteBuffer Flip()
		{
			Buffer.SetLength(Buffer.Position);
			Buffer.Position = 0;
			return this;
		}

		public static ByteBuffer Demand(int size)
		{
			return ByteBufferPool.Instance.Demand(size);
		}

		public static void Release(ByteBuffer buffer)
		{
			ByteBufferPool.Instance.Release(buffer);
		}

		/// <summary>
		/// Resets the buffer.
		/// </summary>
		public void ResetBuffer()
		{
			Position = 0;
		}

		/// <summary>
		/// Gets buffer length.
		/// </summary>
		public int Length => (int)Buffer.Length;

		/// <summary>
		/// Gets this ByteBuffer as byte array.
		/// </summary>
		/// <returns>Byte array of this ByteBuffer.</returns>
		public byte[] GetBulk()
		{
			return GetByteArray(0, Position);
		}

		/// <summary>
		/// Returns true if buffer is empty.
		/// </summary>
		public bool IsEmpty => Position == 0;

		/// <summary>
		/// Creates new ByteBuffer from byte[].
		/// </summary>
		/// <param name="buffer">Array of bytes to wrap.</param>
		/// <returns>New instance of ByteByffer.</returns>
		public static ByteBuffer WrapBuffer(byte[] buffer)
		{
			var wrap = new MemoryStream(buffer);
			return new ByteBuffer(wrap);
		}

		/// <summary>
		/// Get part of ByteBuffer as array of bytes.
		/// </summary>
		/// <param name="start">Offset of part.</param>
		/// <param name="length">Length of part.</param>
		/// <returns>Byte array with part of ByteBuffer.</returns>
		public byte[] GetSubArray(in int start, in int length)
		{
			return GetByteArray(start, length);
		}
	}
}
