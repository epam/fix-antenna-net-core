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

using System.IO;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Transport
{
	/// <summary>
	/// IFIXTransport interface describe base functionality for transport implementation.
	/// </summary>
	public interface IFixTransport
	{
		bool IsBlockingSocket { get; }

		/// <summary>
		/// Read message method.
		/// </summary>
		/// <returns> message the message </returns>
		/// <exception cref="IOException"> if error occurred </exception>
		void ReadMessage(MsgBuf buf);

		/// <summary>
		/// Write message method.
		/// </summary>
		/// <param name="message"> the message </param>
		/// <exception cref="IOException"> if error occurred </exception>
		void Write(byte[] message);

		/// <summary>
		/// Write message method.
		/// </summary>
		/// <param name="message"> the message </param>
		/// <param name="offset">  the start buffer position </param>
		/// <param name="length">  the length </param>
		/// <exception cref="IOException"> if error occurred </exception>
		int Write(byte[] message, int offset, int length);

		int Write(Common.Utils.ByteBuffer buf, int offset, int length);

		void WaitUntilReadyToWrite();


		/// <summary>
		/// Close transport method.
		/// </summary>
		/// <exception cref="IOException"> if error occurred </exception>
		void Close();

		/// <summary>
		/// Gets optimal the size of buffer.
		/// </summary>
		/// <value> the buffer size in bytes </value>
		int OptimalBufferSize { get; }

		/// <summary>
		/// Gets remote host.
		/// </summary>
		string RemoteHost { get; }
	}
}