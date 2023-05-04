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

namespace Epam.FixAntenna.TestUtils.Transport
{
	internal class FixMessageReader
	{
		/// <summary>
		/// Length of the check sum footer.
		/// Always constant: tag num - 2 bytes, equal sign - 1 byte, check sum value - 3 bytes, SOH - 1 byte
		/// </summary>
		private const int CheckSumLen = 7;

		/// <summary>
		/// Optimal buffer length.
		/// </summary>
		private const int OptimalBufLen = 16384;

		/// <summary>
		/// Source input stream.
		/// </summary>
		private readonly Stream _inputStream;

		/// <summary>
		/// Max allowed size of messages.
		/// </summary>
		private readonly int _maxMsgSize;

		/// <summary>
		/// Buffer for incoming bytes.
		/// </summary>
		private byte[] _buf;

		/// <summary>
		/// Message reading offset.
		/// </summary>
		private int _messageReadingOffset;

		/// <summary>
		/// Writing offset.
		/// </summary>
		private int _writingOffset;

		private readonly object _sync = new object();

		public FixMessageReader(Stream inputStream, int maxMsgSize)
		{
			_inputStream = inputStream;
			_maxMsgSize = maxMsgSize;
			_buf = new byte[OptimalBufLen];
		}

		public virtual byte[] ReadMessage()
		{
			lock (_sync)
			{
				// State of reader FSM
				var state = 0;
				// Reading offset
				var readingOffset = _messageReadingOffset;
				// Reading offset limit
				var maxReadingOffset = _maxMsgSize + readingOffset;
				// Expected body length
				var bodyLength = 0;
				for (;;)
				{
					while (readingOffset < _writingOffset)
					{
						switch (state)
						{
							case 0:
								if (_buf[readingOffset] != (byte)'8')
								{
									state = 0;
								}
								else if (readingOffset > _messageReadingOffset)
								{
									// Before analyze new message flush the buffered data
									// which will recognized as a garbled message
									goto loopBreak;
								}
								else
								{
									state = 1;
								}

								break;
							case 1:
								if (_buf[readingOffset] == (byte)'=')
								{
									state = 2;
								}
								else
								{
									state = 0;
								}

								break;
							case 2:
								if (_buf[readingOffset] == (byte)'\x0001')
								{
									state = 3;
								}

								break;
							case 3:
								if (_buf[readingOffset] == (byte)'9')
								{
									state = 4;
								}
								else
								{
									state = 0;
								}

								break;
							case 4:
								if (_buf[readingOffset] == (byte)'=')
								{
									state = 5;
									// Clear an accumulator of expected body length
									bodyLength = 0;
								}
								else
								{
									state = 0;
								}

								break;
							case 5:
								var b = _buf[readingOffset];
								if (b == (byte)'\x0001')
								{
									goto loopBreak;
								}
								else if (b >= (byte)'0' && b <= (byte)'9')
								{
									bodyLength = bodyLength * 10 + b - (byte)'0';
								}
								else
								{
									state = 0;
								}

								break;
						}

						readingOffset++;
						// Check message size
						if (readingOffset > maxReadingOffset)
						{
							throw new IOException("Too long message. Check <maxMessageSize> in configuration file.");
						}
					}

					// Try to read some bytes from a stream
					var n = _inputStream.Read(_buf, _writingOffset, _buf.Length - _writingOffset);
					if (n <= 0)
					{
						goto loopBreak;
					}

					_writingOffset += n;
					if (_writingOffset == _buf.Length)
					{
						var nbuf = new byte[_writingOffset * 2];
						Array.Copy(_buf, 0, nbuf, 0, _writingOffset);
						_buf = nbuf;
					}
				}

				loopBreak:
				if (state == 5)
				{
					// Calculate an expected message size
					readingOffset += bodyLength + CheckSumLen + 1;
					if (readingOffset > maxReadingOffset)
					{
						throw new IOException("Too long message. Check <maxMessageSize> in configuration file.");
					}

					if (readingOffset > _buf.Length)
					{
						var nbuf = new byte[readingOffset];
						Array.Copy(_buf, 0, nbuf, 0, _writingOffset);
						_buf = nbuf;
					}

					// Read a rest of the message
					while (_writingOffset < readingOffset)
					{
						var n = _inputStream.Read(_buf, _writingOffset, _buf.Length - _writingOffset);
						if (n <= 0)
						{
							break;
						}

						_writingOffset += n;
					}
				}

				var msgLen = readingOffset - _messageReadingOffset;
				if (msgLen == 0)
				{
					throw new IOException("End of File read.");
				}

				// Prepare output message.
				var msg = new byte[msgLen];
				Array.Copy(_buf, _messageReadingOffset, msg, 0, msgLen);
				// If buffer is empty...
				if (readingOffset == _writingOffset)
				{
					if (_writingOffset <= OptimalBufLen && _buf.Length > OptimalBufLen)
					{
						_buf = new byte[OptimalBufLen];
					}

					_writingOffset = 0;
					_messageReadingOffset = 0;
				}
				else
				{
					_messageReadingOffset = readingOffset;
				}

				return msg;
			}
		}

		public virtual void Close()
		{
			_inputStream.Close();
		}
	}
}