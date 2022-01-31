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

using System.Net;
using Epam.FixAntenna.NetCore.Common.Utils;

namespace Epam.FixAntenna.NetCore.FixEngine.Transport
{
	/// <summary>
	/// Common transport interface.
	/// Provides ability to read and write the messages.
	/// </summary>
	internal interface ITransport
	{
		bool IsBlockingSocket { get; }

		/// <summary>
		/// Returns true, if connected through secured connection.
		/// </summary>
		bool IsSecured { get; }

		/// <summary>
		/// Transport dependent write method.
		/// </summary>
		/// <param name="message"> actual message </param>
		/// <exception cref="System.IO.IOException"> if unable to write </exception>
		void Write(byte[] message);

		int Write(ByteBuffer message);

		/// <summary>
		/// Transport dependent write method.
		/// </summary>
		/// <param name="message"> actual message </param>
		/// <param name="offset">  the offset in buffer </param>
		/// <param name="length">  the num of bytes to write </param>
		/// <exception cref="System.IO.IOException"> if unable to write </exception>
		int Write(byte[] message, int offset, int length);

		int Write(ByteBuffer message, int offset, int length);

		void WaitUntilReadyToWrite();

		/// <summary>
		/// Transport dependent read method.
		/// </summary>
		/// <param name="buffer"> holder for read bytes </param>
		/// <param name="offset"> in buffer </param>
		/// <param name="length"> maximum bytes to be read </param>
		/// <returns> number of byte actually read </returns>
		/// <exception cref="System.IO.IOException"> if unable to read </exception>
		int Read(byte[] buffer, int offset, int length);

		int Read(ByteBuffer buffer, int offset, int length);

		/// <summary>
		/// Transport dependent read method.
		/// </summary>
		/// <param name="buffer"> holder for read bytes </param>
		/// <returns> number of byte actually read </returns>
		/// <exception cref="System.IO.IOException"> if unable to read </exception>
		int Read(byte[] buffer);

		int Read(ByteBuffer buffer);

		/// <summary>
		/// Gets local IPEndPint after the transport connected.
		/// </summary>
		IPEndPoint LocalEndPoint { get; }

		/// <summary>
		/// Gets the remote IPEndPoint after the transport connected.
		/// </summary>
		IPEndPoint RemoteEndPoint { get; }

		/// <summary>
		/// Open the transport.
		/// </summary>
		/// <exception cref="System.IO.IOException"> - throws if error occurred </exception>
		void Open();

		/// <summary>
		/// Close the transport.
		/// </summary>
		/// <exception cref="System.IO.IOException"> - throws if error occurred </exception>
		void Close();

		/// <summary>
		/// Returns <tt>true</tt> if transport is open.
		/// </summary>
		bool IsOpen { get; }
	}
}