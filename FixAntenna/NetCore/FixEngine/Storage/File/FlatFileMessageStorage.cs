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
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Message.SpecialTags;

namespace Epam.FixAntenna.NetCore.FixEngine.Storage.File
{
	/// <summary>
	/// File message storage implementation.
	/// </summary>
	internal class FlatFileMessageStorage : AbstractFileMessageStorage
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(FlatFileMessageStorage));

		protected internal const long Delta = 1000;
		private const int DefaultGrowSize = 1 * 1000 * 1024;
		private const int OverlapLength = 20;
		private const int ReadBufferLength = 1024;
		protected internal const int WriteBufferLengthDef = 8 * 1024;
		protected internal static readonly byte[] NewLine = { (byte)'\n' };

		protected internal long ChannelLength;
		protected internal long ChannelPosition;

		protected internal byte[] DateFormattedBuffer;
		//protected internal int MaxBufferSize = 1024 * 1024;
		protected internal long MaxStorageGrowSize = DefaultGrowSize;
		protected internal bool StorageGrowSize;
		protected internal long TimestampTicks;
		protected internal bool IsTimestampEnabled;

		protected internal RawFixUtil.IRawTags RawTags;
		protected internal IMaskedTags MaskedTags;

		private byte[] _buffer;
		private byte[] _rentedArray;

		/// <summary>
		/// Creates <c>FlatFileMessageStorage</c>.
		/// </summary>
		public FlatFileMessageStorage(Config config) : base(config)
		{
			IsTimestampEnabled = Configuration.GetPropertyAsBoolean(Config.TimestampsInLogs, true);
			StorageGrowSize = Configuration.GetPropertyAsBoolean(Config.StorageGrowSize, false);
			MaxStorageGrowSize = Configuration.GetPropertyAsBytesLength(Config.MaxStorageGrowSize);
			if (StorageGrowSize && MaxStorageGrowSize <= 0)
			{
				Log.Warn($"Parameter \"{Config.MaxStorageGrowSize}\" must be integer and not negative");
				MaxStorageGrowSize = DefaultGrowSize;
			}

			RawTags = RawFixUtil.CreateRawTags(Configuration.GetProperty(Config.RawTags));
			MaskedTags = CustomMaskedTags.Create(Configuration.GetProperty(Config.MaskedTags));

			_buffer = new byte[WriteBufferLengthDef];
			_rentedArray = null;
		}

		/// <inheritdoc />
		protected override void OpenStorageFile()
		{
			base.OpenStorageFile();
			ChannelLength = AccessFile.Length;
			if (ChannelLength > 0)
			{
				ChannelPosition = GetLastChannelPosition(FileName);
				AccessFile.Position = ChannelPosition;
			}
			else
			{
				ChannelPosition = 0;
			}

			if (Log.IsTraceEnabled)
			{
				Log.Trace($"Storage {FileName}: Channel position:{ChannelPosition}, channelLength:{ChannelLength}");
			}
		}

		/// <summary>
		/// The method <c>RetrieveMessages</c> is not supported in this instance.
		/// </summary>
		/// <exception cref="IOException"> Message retrieval is not possible for flat files </exception>
		/// <seealso cref="AbstractFileMessageStorage.RetrieveMessages"> </seealso>
		protected override void RetrieveMessagesImplementation(long from, long to, IMessageStorageListener listener, bool blocking)
		{
			throw new IOException("Message retrieval is not possible for flat files!");
		}

		/// <inheritdoc />
		protected override long GetNextSequenceNumber()
		{
			return RetrieveSequenceNumber(FileName) + 1;
		}

		/// <inheritdoc />
		public override void AppendMessage(byte[] timestampFormatted, byte[] message, int offset, int length)
		{
			AppendMessageInternal(timestampFormatted, message, offset, length);
		}

		/// <inheritdoc />
		public override void AppendMessage(byte[] timestampFormatted, byte[] message)
		{
			AppendMessage(timestampFormatted, message, 0, message.Length);
		}

		protected virtual long AppendMessageInternal(byte[] timestampFormatted, byte[] message, int offset, int length)
		{
			var tsLength = 0;
			int recordLength;

			if (IsTimestampEnabled)
			{
				tsLength = timestampFormatted.Length;
				recordLength = timestampFormatted.Length + length + 1;
			}
			else
			{
				recordLength = length + 1;
			}

			if (StorageGrowSize && ChannelLength - (ChannelPosition + length) < Delta)
			{
				AccessFile.SetLength(ChannelPosition + MaxStorageGrowSize);
				ChannelLength = ChannelPosition + MaxStorageGrowSize;
				AccessFile.Position = ChannelPosition;
			}

			if (recordLength > _buffer.Length)
			{
				GrowBuffer(recordLength);
			}

			var written = 0;
			var msgSpan = message.AsSpan(offset, length);
			var bufSpan = _buffer.AsSpan();

			if (IsTimestampEnabled)
			{
				timestampFormatted.CopyTo(bufSpan);
				written += tsLength;
			}

			msgSpan.CopyTo(bufSpan.Slice(written));
			written += length;

			SpecialFixUtil.MaskFields(bufSpan.Slice(tsLength), RawTags, MaskedTags);

			NewLine.CopyTo(bufSpan.Slice(written++));

			AccessFile.Write(_buffer, 0, written);
			AccessFile.Flush();

			ChannelPosition += recordLength;
			return recordLength;
		}

		/// <summary>
		/// Appends message to storage
		/// </summary>
		/// <param name="ticks"> the timestamp parameter, in .Net Tick units.</param>
		/// <param name="message">   the array of bytes </param>
		/// <param name="offset"> </param>
		/// <param name="length">  </param>
		/// <exception cref="IOException"> if I/O error occurred </exception>
		protected internal override long AppendMessageInternal(long ticks, byte[] message, int offset, int length)
		{
			if (IsTimestampEnabled)
			{
				if (ticks != TimestampTicks)
				{
					TimestampTicks = ticks;
					DateFormattedBuffer = GetPrefixFormat(ticks);
				}
			}

			return AppendMessageInternal(DateFormattedBuffer, message, offset, length);
		}

		/// <summary>
		/// Retrieves the last sequence number.
		/// </summary>
		/// <param name="fileName"> the file name </param>
		/// <exception cref="IOException"> if I/O error occurred </exception>
		public static long RetrieveSequenceNumber(string fileName)
		{
			long sequenceNumber = 0;

			try
			{
				using (var file = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					var bufferEndPosition = file.Length;
					var attempt = 0;
					do
					{
						var seekPosition = Math.Max(0, bufferEndPosition - ReadBufferLength);
						file.Seek(seekPosition, SeekOrigin.Begin);
						var buffer = new byte[ReadBufferLength];
						var numberOfBytesRead = file.Read(buffer, 0, buffer.Length);
						var value = RawFixUtil.GetRawValue(buffer, 0, numberOfBytesRead, 34, false);

						if (value == null)
						{
							continue;
						}

						sequenceNumber = FixTypes.ParseInt(value);
					} while ((bufferEndPosition = file.Length - (ReadBufferLength - OverlapLength) * ++attempt) > 0 &&
							sequenceNumber == 0);
				}
			}
			catch (Exception e)
			{
				if (Log.IsDebugEnabled)
				{
					Log.Debug($"Error on retrieveSequenceNumber: {e.Message}");
				}

				return sequenceNumber;
			}

			if (Log.IsDebugEnabled)
			{
				Log.Debug($"retrieveSequenceNumber read sequenceNumber: {sequenceNumber}");
			}

			return sequenceNumber;
		}

		private long GetLastChannelPosition(string fileName)
		{
			long position = 0;
			try
			{
				using (var file = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
				{
					var bufferEndPosition = file.Length;
					var attempt = 0;
					var found = false;
					do
					{
						var seekPosition = Math.Max(0, bufferEndPosition - ReadBufferLength);
						file.Seek(seekPosition, SeekOrigin.Begin);

						var buffer = new byte[ReadBufferLength];
						var numberOfBytesRead = file.Read(buffer, 0, buffer.Length);
						var index = numberOfBytesRead - 1;
						while (index > 0)
						{
							if (buffer[index] != 0)
							{
								position = seekPosition + index + 1;
								found = true;
								break;
							}

							index--;
						}
					} while (!found && (bufferEndPosition = file.Length - (ReadBufferLength - OverlapLength) * ++attempt) > 0);
				}
			}
			catch (Exception)
			{
				return position;
			}

			return position;
		}

		public override void Close()
		{
			if (_rentedArray != null)
			{
				ArrayPool<byte>.Shared.Return(_rentedArray);
				_rentedArray = null;
				_buffer = Array.Empty<byte>();
			}
			base.Close();
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private void GrowBuffer(int newLength)
		{
			var toReturn = _rentedArray;
			var newBuffer = ArrayPool<byte>.Shared.Rent(newLength);

			_buffer.CopyTo(newBuffer.AsSpan());
			_buffer = _rentedArray = newBuffer;

			if (toReturn != null)
			{
				ArrayPool<byte>.Shared.Return(toReturn);
			}
		}
	}
}