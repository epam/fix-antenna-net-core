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

namespace Epam.FixAntenna.Tester.Transport
{

	public class FIXMessageReader
	{

		/// <summary>
		/// Length of the check sum footer.
		/// Always constant: tag num - 2 bytes, equal sign - 1 byte, check sum value - 3 bytes, SOH - 1 byte
		/// </summary>
		private const int CHKSUM_LEN = 7;

		/// <summary>
		/// Optimal buffer length.
		/// </summary>
		private const int OPTIMAL_BUF_LEN = 16384;

		/// <summary>
		/// Buffer for incoming bytes.
		/// </summary>
		private byte[] _buf;

		/// <summary>
		/// Writing offset.
		/// </summary>
		private int _woff = 0;

		/// <summary>
		/// Message reading offset.
		/// </summary>
		private int _mroff = 0;

		/// <summary>
		/// Source input stream.
		/// </summary>
		private Stream @in;

		/// <summary>
		/// Max allowed size of messages.
		/// </summary>
		private int _maxMsgSize;

		private readonly object _sync = new object();

		public FIXMessageReader(Stream @in, int maxMsgSize)
		{
			this.@in = @in;
			this._maxMsgSize = maxMsgSize;
			_buf = new byte[OPTIMAL_BUF_LEN];
		}

		public virtual byte[] ReadMessage()
		{
			lock (_sync)
			{
				// State of reader FSM
				int state = 0;
				// Reading offset
				int roff = _mroff;
				// Reading offset limit
				int maxroff = _maxMsgSize + roff;
				// Expected body length
				int bodylen = 0;
				for (; ;)
				{
					while (roff < _woff)
					{
						switch (state)
						{
							case 0:
								if (_buf[roff] != (byte) '8')
								{
									state = 0;
								}
								else if (roff > _mroff)
								{
									// Before analise new message flush the buffered data
									// which will recognized as a garbled message
									goto loopBreak;
								}
								else
								{
									state = 1;
								}
								break;
							case 1:
								if (_buf[roff] == (byte) '=')
								{
									state = 2;
								}
								else
								{
									state = 0;
								}
								break;
							case 2:
								if (_buf[roff] == (byte) '\x0001')
								{
									state = 3;
								}
								break;
							case 3:
								if (_buf[roff] == (byte) '9')
								{
									state = 4;
								}
								else
								{
									state = 0;
								}
								break;
							case 4:
								if (_buf[roff] == (byte) '=')
								{
									state = 5;
									// Clear an accumulator of expected body length
									bodylen = 0;
								}
								else
								{
									state = 0;
								}
								break;
							case 5:
								byte b = _buf[roff];
								if (b == (byte) '\x0001')
								{
									goto loopBreak;
								}
								else if (b >= (byte) '0' && b <= (byte) '9')
								{
									bodylen = bodylen * 10 + b - (byte) '0';
								}
								else
								{
									state = 0;
								}
								break;
						}
						roff++;
						// Check message size
						if (roff > maxroff)
						{
							throw new IOException("Too long message. Check <maxMessageSize> in configuration file.");
						}
					}
					// Try to read some bytes from a stream
					int n = @in.Read(_buf, _woff, _buf.Length - _woff);
					if (n <= 0)
					{
						goto loopBreak;
					}
					else
					{
						_woff += n;
						if (_woff == _buf.Length)
						{
							byte[] nbuf = new byte[_woff * 2];
							Array.Copy(_buf, 0, nbuf, 0, _woff);
							_buf = nbuf;
						}
					}
				}
				loopBreak:
				if (state == 5)
				{
					// Calculate an expected message size
					roff += bodylen + CHKSUM_LEN + 1;
					if (roff > maxroff)
					{
						throw new IOException("Too long message. Check <maxMessageSize> in configuration file.");
					}
					if (roff > _buf.Length)
					{
						byte[] nbuf = new byte[roff];
						Array.Copy(_buf, 0, nbuf, 0, _woff);
						_buf = nbuf;
					}
					// Read a rest of the message
					while (_woff < roff)
					{
						int n = @in.Read(_buf, _woff, _buf.Length - _woff);
						if (n <= 0)
						{
							break;
						}
						_woff += n;
					}
				}
				int msglen = roff - _mroff;
				if (msglen == 0)
				{
					throw new IOException("End of File read.");
				}
				// Prepare output message.
				byte[] msg = new byte[msglen];
				Array.Copy(_buf, _mroff, msg, 0, msglen);
				// If buffer is empty...
				if (roff == _woff)
				{
					if (_woff <= OPTIMAL_BUF_LEN && _buf.Length > OPTIMAL_BUF_LEN)
					{
						_buf = new byte[OPTIMAL_BUF_LEN];
					}
					_woff = 0;
					_mroff = 0;
				}
				else
				{
					_mroff = roff;
				}
				return msg;
			}
		}

		public virtual void close()
		{
			@in.Close();
		}
	}

}