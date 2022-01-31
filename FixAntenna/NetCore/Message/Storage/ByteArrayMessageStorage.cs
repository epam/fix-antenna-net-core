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
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Message.Format;

namespace Epam.FixAntenna.NetCore.Message.Storage
{
	internal sealed class ByteArrayMessageStorage : MessageStorage, IContinuousMessageStorage
	{
		/// <summary>
		/// link to the original buffer which is external for messages.
		/// </summary>
		private byte[] _origBuffer;

		// Length of the data in original buffer
		private int _origBufLength;

		// Offset of the data in original buffer
		private int _origBufStartOffset;

		public byte[] Buffer
		{
			get { return _origBuffer; }
		}

		public int Offset
		{
			get { return _origBufStartOffset; }
		}

		public int Length
		{
			get { return _origBufLength; }
		}

		public override byte[] GetByteArray(int index)
		{
			return _origBuffer;
		}

		public override void ClearAll()
		{
			_origBuffer = null;
			_origBufStartOffset = 0;
			_origBufLength = 0;
		}

		public override void Add(int tagIndex, byte[] value, int offset, int length)
		{
			Array.Copy(value, 0, _origBuffer, offset, length);
		}

		public override void SetValue(int tagIndex, long value, int length)
		{
			throw new NotSupportedException("getByteBuffer not supported for ByteArrayMessageStorage");
		}

		public override void SetPaddedValue(int tagIndex, long value, int length)
		{
			throw new NotSupportedException("getByteBuffer not supported for ByteArrayMessageStorage");
		}

		public override int SetValue(int tagIndex, double value, int precision, int length)
		{
			throw new NotSupportedException("getByteBuffer not supported for ByteArrayMessageStorage");
		}

		public override void SetValue(int tagIndex, string value, int length)
		{
			throw new NotSupportedException("getByteBuffer not supported for ByteArrayMessageStorage");
		}

		public override void SetCalendarValue(int index, IFixDateFormatter fixDateFormatter, DateTimeOffset value,
			int length)
		{
			throw new NotSupportedException("getByteBuffer not supported for ByteArrayMessageStorage");
		}

		public override void SetTimeValue(int index, DateTime value, TimestampPrecision precision, int length)
		{
			throw new NotSupportedException("getByteBuffer not supported for ByteArrayMessageStorage");
		}

		public override void SetTimeValue(int index, DateTimeOffset value, TimestampPrecision precision, int length)
		{
			throw new NotSupportedException("getByteBuffer not supported for ByteArrayMessageStorage");
		}

		public override void SetDateTimeValue(int index, DateTime value, TimestampPrecision precision, int length)
		{
			throw new NotSupportedException("getByteBuffer not supported for ByteArrayMessageStorage");
		}

		public override void SetDateTimeValue(int index, DateTimeOffset value, TimestampPrecision precision, int length)
		{
			throw new NotSupportedException("getByteBuffer not supported for ByteArrayMessageStorage");
		}

		public override void SetValue(int tagIndex, string value, int offset, int length)
		{
			for (var j = 0; j < value.Length; j++)
			{
				_origBuffer[offset + j] = (byte)value[j];
			}

			for (var j = value.Length; j < length; j++)
			{
				_origBuffer[offset + j] = (byte)' ';
			}
		}

		public void SetBuffer(byte[] buf, int offset, int length)
		{
			_origBuffer = buf;
			_origBufStartOffset = offset;
			_origBufLength = length;
		}

		public bool IsActive => _origBuffer != null;

		public override bool IsEmpty => _origBuffer == null || _origBufLength == 0;

		public void Copy(ByteArrayMessageStorage srcStorage)
		{
			_origBuffer = srcStorage._origBuffer;
			_origBufStartOffset = srcStorage._origBufStartOffset;
			_origBufLength = srcStorage._origBufLength;
		}
	}
}