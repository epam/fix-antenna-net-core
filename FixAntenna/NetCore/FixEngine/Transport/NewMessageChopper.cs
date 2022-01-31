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
using System.IO;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Transport
{
	/// <summary>
	/// Slices and parse FIX messages from the <c>Transport</c> implementations.
	/// </summary>
	internal sealed class NewMessageChopper : IMessageChopper
	{
		private static ILog _log = LogFactory.GetLog(typeof(NewMessageChopper));

		private readonly ITransport _transport;
		private readonly bool _checkMessageSize;
		private readonly int _maxMessageSize;
		private int _readOffset;
		private int _messageStartOffset;
		private int _parsedOffset;
		private GarbledMessageError _error;
		private byte[] _buffer;
		private ByteBuffer _bufferObj;
		private readonly TagValue _tempValue = new TagValue();

		private readonly bool _markReadingTime;

		public const int SocketReadSize = SocketTransport.SocketReadSize;

		// TBD! retrieve it from Session object
		private IFixParserListener _parserListener;
		private bool _isAdvancedParseControl = false;

		/// <summary>
		/// Creates <c>NewMessageChopper</c> with specified message size limit.
		/// </summary>
		/// <param name="transport">           the transport implementstion to read from. </param>
		/// <param name="maxMessageSize">      the message size limit. </param>
		/// <param name="optimalBufferLength"> the optimal length of internal buffer </param>
		public NewMessageChopper(ITransport transport, int maxMessageSize, int optimalBufferLength, bool markInMessageTime) : this(transport, maxMessageSize, optimalBufferLength, markInMessageTime, null)
		{
		}

		/// <summary>
		/// Creates <c>NewMessageChopper</c> with specified message size limit.
		/// </summary>
		/// <param name="transport">           the transport implementstion to read from. </param>
		/// <param name="maxMessageSize">      the message size limit. </param>
		/// <param name="optimalBufferLength"> the optimal length of internal buffer </param>
		/// <param name="parserListener"> </param>
		public NewMessageChopper(ITransport transport, int maxMessageSize, int optimalBufferLength, bool markInMessageTime, IFixParserListener parserListener)
		{
			_transport = transport;
			_maxMessageSize = maxMessageSize;
			// TBD! add check of message size. The rest must be defined as garbled.
			_checkMessageSize = maxMessageSize > 0;
			_bufferObj = new ByteBuffer(SocketReadSize);
			_buffer = _bufferObj.GetByteArray();
			SetUserParserListener(parserListener);
			if (_log.IsDebugEnabled)
			{
				_log.Debug("Initialize new message chopper " + (markInMessageTime ? "with" : "without") + " marking incoming message time");
			}
			_markReadingTime = markInMessageTime;
		}

		/// <inheritdoc />
		public RawFixUtil.IRawTags RawTags { get; set; } = new DefaultRawTags();

		/// <summary>
		/// Returns true if last read message is garbled.
		/// </summary>
		/// <value> true if last read message is garbled. </value>
		public bool IsMessageGarbled
		{
			get { return _error != null; }
		}

		/// <summary>
		/// Gets buffer
		/// </summary>
		/// <returns> buffer </returns>
		public byte[] GetBuffer()
		{
			return _buffer;
		}

		/// <summary>
		/// Returns error of last read message if message is garbled or null otherwise.
		/// </summary>
		/// <value> the instance of error enum. </value>
		public GarbledMessageError Error
		{
			get { return _error; }
		}

		/// <summary>
		/// Returns error position of last read message if message is garbled or -1 otherwise.
		/// </summary>
		/// <value> the error message string. </value>
		public int ErrorPosition
		{
			get
			{
				return -1;
			}
		}

		public void ReadMessage(MsgBuf buf)
		{

			var message = buf.FixMessage;
			if (message == null)
			{
				message = new FixMessage();
				buf.FixMessage = message;
			}

			if (_parsedOffset == _readOffset)
			{
				_parsedOffset = _readOffset = 0; // no data left in buffer, roll back to the beginning
			}
			else
			{
				if (_readOffset > SocketReadSize / 2)
				{
					var availData = _readOffset - _parsedOffset;
					var tail = SocketReadSize - _readOffset;

					if (availData < 256 || tail < 1024) // preventive data move ?
					{
						Array.Copy(_buffer, _parsedOffset, _buffer, 0, availData);
						_readOffset -= _parsedOffset;
						_parsedOffset = 0;
					}
				}
			}

			_messageStartOffset = _parsedOffset;

			while (_parsedOffset + 20 > _readOffset)
			{
				ReadAvailableBytesToBuffer(buf);
			}


			message.SetBuffer(_buffer, _messageStartOffset, _readOffset);

			var valueStartIndex = 0;
			var tag = 0;
			var isTagParsing = true;
			var readSomeBytes = false;

			var userStopParse = false;
			var isHeaderTag = true;
			var isAdminMsg = false;
			var stopParse = false;

			OnMessageStart();
			while (true)
			{

				while (_parsedOffset >= _readOffset)
				{
					ReadAvailableBytesToBuffer(buf);
					readSomeBytes = true;
				}

				if (readSomeBytes)
				{
					readSomeBytes = false;

					if (_readOffset <= SocketReadSize || _messageStartOffset == 0)
					{
						message.SetBuffer(_buffer, _messageStartOffset, _readOffset);
					}
					else
					{
						// end of buffer reached, wrap around now and move remaining data to buffer's head
						Array.Copy(_buffer, _messageStartOffset, _buffer, 0, _readOffset - _messageStartOffset);
						_readOffset -= _messageStartOffset;
						_parsedOffset -= _messageStartOffset;
						valueStartIndex -= _messageStartOffset;
						_messageStartOffset = 0;

						message.ShiftBuffer(_buffer, _messageStartOffset, _readOffset);
					}
				}

				var b = _buffer[_parsedOffset];

				if (isTagParsing)
				{
					if (b >= (byte)'0' && b <= (byte)'9')
					{
						tag = tag * 10 + (b - '0');
					}
					else if (b == (byte)'=')
					{
						if (RawTags.IsWithinRawTags(tag))
						{
							var rawLength = 0;
							if (message.IsMessageIncomplete)
							{
								byte bv;
								for (var i = valueStartIndex; i < _parsedOffset; i++)
								{
									bv = _buffer[i];
									if (bv >= (byte)'0' && bv <= (byte)'9')
									{
										rawLength = rawLength * 10 + (bv - '0');
									}
									else
									{
										break;
									}
								}
							}
							else
							{
								rawLength = RawFixUtil.GetRawTagLengthFromPreviousField(message);
							}
							if (rawLength > _maxMessageSize)
							{
								throw new GarbledMessageException(MessageChopperFields.RawDataLengthIsTooBigError + " (" + rawLength + ")");
							}
							valueStartIndex = _parsedOffset + 1;
							_parsedOffset += rawLength;
						}
						else
						{
							valueStartIndex = _parsedOffset + 1;
						}
						isTagParsing = false;
					}
					else
					{
						GetMessage(buf);
						throw new GarbledMessageException("Invalid tag number");
					}
				}
				else
				{
					if (b == (byte)'\x0001')
					{
						if (_isAdvancedParseControl)
						{
							if (userStopParse)
							{
								if (isHeaderTag)
								{
									if (ParseRequiredTags.IsHeader(tag))
									{
										message.Add(tag, valueStartIndex, _parsedOffset - valueStartIndex);
									}
									else
									{
										message.LoadTagValue(35, _tempValue);
										isAdminMsg = ParseRequiredTags.IsAdminMsg(_tempValue);
										isHeaderTag = false;
										if (isAdminMsg)
										{
											message.Add(tag, valueStartIndex, _parsedOffset - valueStartIndex);
										}
									}
								}
								else if (isAdminMsg)
								{
									message.Add(tag, valueStartIndex, _parsedOffset - valueStartIndex);
								}
								else if (!stopParse)
								{
									// can be stop
									message.LoadTagValue(9, _tempValue);
									var msgLength = (int) _tempValue.LongValue;
									//            | length considered from this point      |  length   |
									_parsedOffset = _tempValue.Offset + _tempValue.Length + 1 + msgLength - 1;
									stopParse = true;
								}
								else if (tag == 10)
								{
									message.Add(tag, valueStartIndex, _parsedOffset - valueStartIndex);
								}
							}
							else
							{
								var control = _parserListener.OnTag(tag, _buffer, valueStartIndex, _parsedOffset - valueStartIndex);
								if (control == FixParserListenerParseControl.Continue)
								{
									message.Add(tag, valueStartIndex, _parsedOffset - valueStartIndex);
								}
								else
								{
									// IGNORE or STOP
									if (ParseRequiredTags.IsRequired(tag))
									{
										message.Add(tag, valueStartIndex, _parsedOffset - valueStartIndex);
									}
									else
									{
										message.IsMessageIncomplete = true;
									}
									if (control == FixParserListenerParseControl.StopParse)
									{
										userStopParse = true;
									}
								}
							}
						}
						else
						{
							message.Add(tag, valueStartIndex, _parsedOffset - valueStartIndex);
						}
						if (tag == 10)
						{
							_parsedOffset++;
							message.SetBuffer(_buffer, _messageStartOffset, _parsedOffset - _messageStartOffset);
							break;
						}
						tag = 0;
						isTagParsing = true;
					}
				}
				_parsedOffset++;
			}

			OnMessageEnd();
			GetMessage(buf);
		}

		public void SetUserParserListener(IFixParserListener parserListener)
		{
			_isAdvancedParseControl = parserListener != null;
			_parserListener = parserListener;
		}

		public void OnMessageStart()
		{
			if (_isAdvancedParseControl)
			{
				_parserListener.OnMessageStart();
			}
		}

		public void OnMessageEnd()
		{
			if (_isAdvancedParseControl)
			{
				_parserListener.OnMessageEnd();
			}
		}

		private void GetMessage(MsgBuf buf)
		{
			var messageLength = _parsedOffset - _messageStartOffset;
			if (messageLength == 0)
			{
				throw new IOException(MessageChopperFields.EofReadError);
			}

			if (messageLength < 0)
			{
				throw new IOException(MessageChopperFields.ReadError);
			}

			buf.Buffer = _buffer;
			buf.Offset = _messageStartOffset;
			buf.Length = messageLength;
			buf.MessageReadTimeInTicks = MessageReadTimeInTicks;
		}

		private bool AreThereParsedDataInBuffer()
		{
			return _parsedOffset != _messageStartOffset;
		}

		/// <summary>
		/// Reads array of bytes from transport.
		/// </summary>
		/// <returns> true if method reads some byets </returns>
		/// <exception cref="IOException"> if transport returns -1 (EOF) </exception>
		private bool ReadAvailableBytesToBuffer(MsgBuf buf)
		{

			int length;

			if (_readOffset < SocketReadSize)
			{
				length = SocketReadSize - _readOffset;
			}
			else
			{
				length = _buffer.Length - _readOffset;
				if (length >= SocketReadSize)
				{
					length = SocketReadSize;
				}
				else
				{
					if (_readOffset - _messageStartOffset > _maxMessageSize)
					{
						//TODO: mask here
						var message = StringHelper.NewString(_buffer, _messageStartOffset, _parsedOffset - _messageStartOffset);
						GetMessage(buf);
						throw new GarbledMessageException(MessageChopperFields.MessageIsTooLongError + " (maxMessageSize=" + _maxMessageSize + "): " + message);
					}

					_bufferObj.Offset = _readOffset;
					_bufferObj.IncreaseBuffer(_bufferObj.Length);
					_buffer = _bufferObj.GetByteArray();
					length = SocketReadSize;
				}
			}

			var n = _transport.Read(_bufferObj, _readOffset, length);
			SetReadingStartTime();

			if (n == 0)
			{
				throw new IOException(MessageChopperFields.EofReadError);
			}

			_readOffset += n;
			return n > 0;
		}

		private void SetReadingStartTime()
		{
			if (_markReadingTime)
			{
				MessageReadTimeInTicks = DateTimeHelper.CurrentTicks;
			}
		}

		public long MessageReadTimeInTicks { get; private set; } = -1;

		public void Reset()
		{
			_messageStartOffset = 0;
			_readOffset = 0;
			_parsedOffset = 0;
			_bufferObj.ResetBuffer();
			_buffer = _bufferObj.GetByteArray();
			_error = null;
			MessageReadTimeInTicks = -1;
		}
	}
}