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
using System.Net;
using System.Net.Security;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Transport
{
	/// <summary>
	/// Slices FIX messages from the <c>InputStream</c> or <c>Transport</c> implementations.
	/// </summary>
	internal sealed class FixMessageChopper : IMessageChopper
	{
		private static ILog _log = LogFactory.GetLog(typeof(FixMessageChopper));

		private ITransport _transport;
		private readonly bool _checkMessageSize;
		private readonly bool _validateCheckSum;
		private readonly int _maxMessageSize;
		private int _readOffset;
		private int _messageStartOffset;
		private int _resetOffset;
		private int _parsedOffset;
		private int _stateChangeOffset;
		private readonly int _optimalBufferLength;
		private int _state;
		private GarbledMessageError _error;
		private byte[] _buffer;

		private ByteBuffer _optimalBufferObj;
		private ByteBuffer _bufferObj;
		private TagValue _tempValue = new TagValue();

		private readonly bool _markReadingTime;
		private bool _isGarbled = false;

		// TBD! retrieve it from Session object
		private IFixParserListener _parserListener;
		private bool _isAdvancedParseControl = false;

		/// <summary>
		/// Creates <c>FIXMessageChopper</c> with specified message size limit.
		/// </summary>
		/// <param name="inputStream">         the input stream to read from. </param>
		/// <param name="maxMessageSize">      the message size limit. </param>
		/// <param name="optimalBufferLength"> the optimal length of internal buffer </param>
		public FixMessageChopper(Stream inputStream, int maxMessageSize, int optimalBufferLength) : this(new ReadOnlyTransport(inputStream), maxMessageSize, optimalBufferLength, true, false)
		{
		}

		/// <summary>
		/// Creates <c>FIXMessageChopper</c> with specified message size limit.
		/// </summary>
		/// <param name="inputStream">         the input stream to read from. </param>
		/// <param name="maxMessageSize">      the message size limit. </param>
		/// <param name="optimalBufferLength"> the optimal length of internal buffer </param>
		/// <param name="validateCheckSum">    do not validate CheckSum(10) if this flag is set to false </param>
		public FixMessageChopper(Stream inputStream, int maxMessageSize, int optimalBufferLength, bool validateCheckSum) : this(new ReadOnlyTransport(inputStream), maxMessageSize, optimalBufferLength, validateCheckSum, false)
		{
		}

		/// <summary>
		/// Creates <c>FIXMessageChopper</c> with specified message size limit.
		/// </summary>
		/// <param name="transport">           the transport implementation to read from. </param>
		/// <param name="maxMessageSize">      the message size limit. </param>
		/// <param name="optimalBufferLength"> the optimal length of internal buffer </param>
		public FixMessageChopper(ITransport transport, int maxMessageSize, int optimalBufferLength) : this(transport, maxMessageSize, optimalBufferLength, true, false)
		{
		}

		/// <summary>
		/// Creates <c>FIXMessageChopper</c> with specified message size limit.
		/// </summary>
		/// <param name="transport">           the transport implementation to read from. </param>
		/// <param name="maxMessageSize">      the message size limit. </param>
		/// <param name="optimalBufferLength"> the optimal length of internal buffer </param>
		/// <param name="validateCheckSum">    do not validate CheckSum(10) if this flag is set to false </param>
		public FixMessageChopper(ITransport transport, int maxMessageSize, int optimalBufferLength, bool validateCheckSum, bool markInMessageTime)
		{
			_transport = transport;
			_maxMessageSize = maxMessageSize;
			_checkMessageSize = maxMessageSize > 0;
			_validateCheckSum = validateCheckSum;
			_optimalBufferLength = optimalBufferLength;
			_optimalBufferObj = new ByteBuffer(optimalBufferLength);
			_bufferObj = _optimalBufferObj;

			_buffer = _bufferObj.GetByteArray();
			if (_log.IsDebugEnabled)
			{
				_log.Debug("Initialize old message chopper " + (markInMessageTime ? "with" : "without") + " marking incoming message time");
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
				if (_error == null)
				{
					return -1;
				}

				if (GarbledMessageError.Field10InvalidChecksum == _error)
				{
					return _stateChangeOffset - _messageStartOffset - 4;
				}

				if (GarbledMessageError.Field8TagExpected == _error)
				{
					return _stateChangeOffset - _messageStartOffset;
				}

				return _stateChangeOffset - _messageStartOffset + 1;
			}
		}

		/// <summary>
		/// Read next message (garbled or non garbled) from
		/// the <c>InputStream</c> or <c>Transport</c> implementations.
		/// </summary>
		/// <returns> the byte representation of FIX message. </returns>
		/// <exception cref="IOException"> if some I/O error occurs, or end of file read, or message is too long. </exception>
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
				if (_readOffset <= _optimalBufferObj.Length)
				{
					_buffer = _optimalBufferObj.GetByteArray();
					_bufferObj = _optimalBufferObj;
				}
				_parsedOffset = _readOffset = 0;
			}
			_messageStartOffset = _parsedOffset;

			var offsetLimit = _messageStartOffset + _maxMessageSize;
			var parsedBodyLength = 0;
			var checksum = 0;
			var parsedChecksum = 0;
			var parsedChecksumBytes = 0;
			_isGarbled = false;

			var valueStartIndex = 0;
			var valueOffset = 0;
			var tag = 0;
			var isTagParsing = true;

			var userStopParse = false;
			var isHeaderTag = true;
			var isAdminMsg = false;

			message.SetBuffer(_buffer, _messageStartOffset, _readOffset);

			MoveToNextStepAndSetResetOffset(GarbledMessageError.Field8TagExpected);
			ResetState();
			OnMessageStart();
			do
			{
				if (_messageStartOffset != _parsedOffset)
				{ // if we read something from transport
					if (_readOffset <= _optimalBufferLength || _messageStartOffset == 0)
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
						valueOffset -= _messageStartOffset;

						_messageStartOffset = 0;

						message.ShiftBuffer(_buffer, _messageStartOffset, _readOffset);
					}
					offsetLimit = _messageStartOffset + _maxMessageSize;
				}
				while (_parsedOffset < _readOffset)
				{
					// Check message size
					if (_checkMessageSize && _parsedOffset >= offsetLimit)
					{
						GetMessage(buf);
						throw new GarbledMessageException(MessageChopperFields.MessageIsTooLongError + " (maxMessageSize=" + _maxMessageSize + ")");
					}
					var ch = (char) _buffer[_parsedOffset];
					if (!_isGarbled)
					{
						if (isTagParsing)
						{
							if (ch >= '0' && ch <= '9')
							{
								tag = tag * 10 + (ch - '0');
							}
							else if (ch == '=')
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
											if (bv >= '0' && bv <= '9')
											{
												rawLength = rawLength * 10 + (bv - '0');
											}
											else if (bv == '\u0001')
											{
												break;
											}
										}
									}
									else
									{
										rawLength = RawFixUtil.GetRawTagLengthFromPreviousField(message);
									}
									if (rawLength > offsetLimit)
									{
										throw new GarbledMessageException(MessageChopperFields.RawDataLengthIsTooBigError + " (" + rawLength + ")");
									}
									valueOffset = _parsedOffset + rawLength;
								}
								valueStartIndex = _parsedOffset + 1;
								isTagParsing = false;
							}
							else
							{
								ResetStateAndGetResetOffset();
							}
						}
						else
						{
							if (ch == '\x0001')
							{
								// check if not in raw data
								if (_parsedOffset > valueOffset)
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
											else if (ParseRequiredTags.IsTrailer(tag))
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
									tag = 0;
									isTagParsing = true;
								}
							}
						}
					}
					switch (_state)
					{
						case 0:
							checksum += ch;
							if (ch == '8')
							{
								if (AreThereParsedDataInBuffer())
								{
									// Before analise new message flush the buffered data (garbled message)
									goto loopBreak;
								}
								else
								{
									MoveToNextStepAndSetResetOffset(GarbledMessageError.Field8TagValueDelimiterExpected);
								}
							}
							else
							{
								_isGarbled = true;
								ResetState();
							}
							break;
						case 1:
							checksum += ch;
							if (ch == '=')
							{
								MoveToNextStepAndSetResetOffset(GarbledMessageError.Field8FieldDelimiterExpected);
							}
							else
							{
								ResetStateAndGetResetOffset();
							}
							break;
						case 2:
							checksum += ch;
							if (ch == '\x0001')
							{
								MoveToNextStepAndSetResetOffset(GarbledMessageError.Field9TagExpected);
							}
							else if (ch == '8')
							{
								ResetStateAndGetResetOffset();
							}
							break;
						case 3:
							checksum += ch;
							if (ch == '9')
							{
								MoveToNextStepAndSetResetOffset(GarbledMessageError.Field9TagValueDelimiterExpected);
							}
							else
							{
								ResetStateAndGetResetOffset();
							}
							break;
						case 4:
							checksum += ch;
							if (ch == '=')
							{
								MoveToNextStepAndSetResetOffset(GarbledMessageError.Field9DecimalValueExpected);
							}
							else
							{
								ResetStateAndGetResetOffset();
							}
							break;
						case 5:
							checksum += ch;
							if (ch >= '0' && ch <= '9')
							{
								parsedBodyLength = parsedBodyLength * 10 + (ch - '0');
							}
							else if (ch == '\x0001' && parsedBodyLength != 0)
							{
								MoveToNextStepAndSetResetOffset(GarbledMessageError.Field35TagExpected);
							}
							else
							{
								ResetStateAndGetResetOffset();
							}
							break;
						case 6:
							checksum += ch;
							--parsedBodyLength;
							if (ch == '3')
							{
								MoveToNextState(GarbledMessageError.Field35TagExpected);
							}
							else
							{
								ResetStateAndGetResetOffset();
							}
							break;
						case 7:
							checksum += ch;
							--parsedBodyLength;
							if (ch == '5')
							{
								MoveToNextState(GarbledMessageError.Field35TagValueDelimiterExpected);
							}
							else
							{
								ResetStateAndGetResetOffset();
							}
							break;
						case 8:
							checksum += ch;
							--parsedBodyLength;
							if (ch == '=')
							{
								MoveToNextState(GarbledMessageError.Field35FieldDelimiterExpected);
							}
							else
							{
								ResetStateAndGetResetOffset();
							}
							break;
						case 9:
							checksum += ch;
							--parsedBodyLength;
							if (ch == '\x0001')
							{
								MoveToNextState(GarbledMessageError.InvalidTagNumber);
								if (parsedBodyLength <= 0)
								{
									MoveToNextState(GarbledMessageError.Field10TagExpected);
								}
							}
							break;
						case 10:
							checksum += ch;
							--parsedBodyLength;
							if (parsedBodyLength <= 0)
							{
								MoveToNextState(GarbledMessageError.Field10TagExpected);
							}
							break;
						case 11:
							if (ch == '1')
							{
								checksum &= 255;
								MoveToNextState(GarbledMessageError.Field10TagExpected);
							}
							else
							{
								ResetStateAndGetResetOffset();
							}
							break;
						case 12:
							if (ch == '0')
							{
								MoveToNextState(GarbledMessageError.Field10TagValueDelimiterExpected);
							}
							else
							{
								ResetStateAndGetResetOffset();
							}
							break;
						case 13:
							if (ch == '=')
							{
								MoveToNextState(GarbledMessageError.Field10DecimalValueExpected);
							}
							else
							{
								ResetStateAndGetResetOffset();
							}
							break;
						case 14:
							if (ch >= '0' && ch <= '9')
							{
								parsedChecksum = parsedChecksum * 10 + (ch - '0');
								if (++parsedChecksumBytes == 3)
								{
									MoveToNextState(GarbledMessageError.Field10FieldDelimiterExpected);
								}
							}
							else
							{
								ResetStateAndGetResetOffset();
							}
							break;
						case 15:
							if (ch == '\x0001')
							{
								_parsedOffset++;
								MoveToNextState(GarbledMessageError.Field10InvalidChecksum);
								if (!_validateCheckSum || checksum == parsedChecksum)
								{
									MoveToNextState(null);
								}
								else
								{
									_isGarbled = true;
									ResetState();
								}
								goto loopBreak;
							}
							else
							{
								ResetStateAndGetResetOffset();
							}
							break;
					}
					_parsedOffset++;
				}
			} while (ReadAvailableBytesToBuffer());

			loopBreak:
			if (_isGarbled && !message.IsMessageIncomplete)
			{
				message.IsMessageIncomplete = true;
			}
			OnMessageEnd();
			GetMessage(buf);
		}

		/// <inheritdoc />
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
			//byte[] message = new byte[messageLength];

			//byte[] message = getByteArrayFromPool(messageLength);
			//System.arraycopy(buffer, messageStartOffset, message, 0, messageLength);
			//return message;

			buf.Buffer = _buffer;
			buf.Offset = _messageStartOffset;
			buf.Length = messageLength;
			buf.MessageReadTimeInTicks = MessageReadTimeInTicks;
			var fixMessage = buf.FixMessage;
			if (fixMessage != null)
			{
				fixMessage.SetBuffer(_buffer, _messageStartOffset, messageLength);
			}
		}

		private bool AreThereParsedDataInBuffer()
		{
			return _parsedOffset != _messageStartOffset;
		}

		/// <summary>
		/// Reads array of bytes from transport.
		/// </summary>
		/// <returns> true if method reads some byets </returns>
		/// <exception cref="IOException"> if transport returns 0 (EOF) </exception>
		private bool ReadAvailableBytesToBuffer()
		{
			int length;

			if (_readOffset < _optimalBufferLength)
			{
				length = _optimalBufferLength - _readOffset;
			}
			else
			{
				length = _buffer.Length - _readOffset;
				if (length >= _optimalBufferLength)
				{
					length = _optimalBufferLength;
				}
				else
				{
					_bufferObj.Offset = _readOffset;
					_bufferObj.IncreaseBuffer(_bufferObj.Length);
					_buffer = _bufferObj.GetByteArray();
					length = _optimalBufferLength;
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

		private void ResetStateAndGetResetOffset()
		{
			ResetState();
			_isGarbled = true;
			_parsedOffset = _resetOffset;
		}

		private void MoveToNextStepAndSetResetOffset(GarbledMessageError errorMessage)
		{
			_resetOffset = _parsedOffset;
			MoveToNextState(errorMessage);
		}

		private void MoveToNextState(GarbledMessageError errorMessage)
		{
			_stateChangeOffset = _parsedOffset;
			_state++;
			_error = errorMessage;
		}

		private void ResetState()
		{
			SetReadingStartTime();
			_state = 0;
		}

		private void SetReadingStartTime()
		{
			if (_markReadingTime)
			{
				MessageReadTimeInTicks = DateTimeHelper.CurrentTicks;
			}
		}

		internal class ReadOnlyTransport : ITransport
		{
			internal Stream InputStream;

			/// <inheritdoc />
			public virtual bool IsBlockingSocket => true;

			/// <inheritdoc />
			public virtual bool IsSecured => InputStream is SslStream sslStream && sslStream.IsAuthenticated;

			/// <inheritdoc />
			public ReadOnlyTransport(Stream inputStream)
			{
				InputStream = inputStream;
			}

			/// <inheritdoc />
			public virtual int Write(byte[] message, int offset, int length)
			{
				throw new NotSupportedException();
			}

			/// <inheritdoc />
			public virtual void Write(byte[] message)
			{
				throw new NotSupportedException();
			}

			/// <inheritdoc />
			public virtual int Read(byte[] buffer, int offset, int length)
			{
				return InputStream.Read(buffer, offset, length);
			}

			/// <inheritdoc />
			public virtual int Read(byte[] buffer)
			{
				return InputStream.Read(buffer, 0, buffer.Length);
			}

			/// <inheritdoc />
			public virtual int Write(ByteBuffer buffer)
			{
				throw new NotSupportedException();
			}

			/// <inheritdoc />
			public virtual int Write(ByteBuffer buffer, int offset, int length)
			{
				throw new NotSupportedException();
			}

			/// <inheritdoc />
			public virtual int Read(ByteBuffer buffer, int offset, int length)
			{
				return Read(buffer.GetByteArray(), offset, length);
			}

			/// <inheritdoc />
			public virtual int Read(ByteBuffer buffer)
			{
				return Read(buffer.GetByteArray());
			}

			/// <inheritdoc />
			public virtual void WaitUntilReadyToWrite()
			{
			}

			/// <inheritdoc />
			public IPEndPoint LocalEndPoint => throw new NotSupportedException();

			/// <inheritdoc />
			public IPEndPoint RemoteEndPoint => throw new NotSupportedException();

			/// <inheritdoc />
			public virtual void Open()
			{
				throw new NotSupportedException();
			}

			/// <inheritdoc />
			public virtual void Close()
			{
				throw new NotSupportedException();
			}

			/// <inheritdoc />
			public virtual bool IsOpen => throw new NotSupportedException();
		}

		/// <inheritdoc />
		public long MessageReadTimeInTicks { get; private set; } = -1;

		/// <inheritdoc />
		public void Reset()
		{
			_optimalBufferObj.ResetBuffer();
			_bufferObj.ResetBuffer();
			_buffer = _bufferObj.GetByteArray();
			_readOffset = 0;
			_parsedOffset = 0;
			_resetOffset = 0;
			_messageStartOffset = 0;
			_stateChangeOffset = 0;
			_state = 0;
			_error = null;
			MessageReadTimeInTicks = -1;
			_isGarbled = false;
		}
	}
}