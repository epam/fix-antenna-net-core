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

namespace Epam.FixAntenna.NetCore.FixEngine.Storage
{
	/// <summary>
	/// The common message storage interface.
	/// </summary>
	internal interface IMessageStorage : IDisposable
	{
		/// <summary>
		/// Initialize the storage.
		/// </summary>
		/// <returns> the next sequence number </returns>
		/// <exception cref="System.IO.IOException"> if error occurred. </exception>
		long Initialize();

		/// <summary>
		/// Appends message to storage.
		/// </summary>
		/// <param name="message"> the message </param>
		/// <param name="offset">  the initial offset </param>
		/// <param name="length">  the length </param>
		/// <exception cref="System.IO.IOException"> if error occurred. </exception>
		void AppendMessage(byte[] message, int offset, int length);

		/// <summary>
		/// Appends message to storage.
		/// </summary>
		/// <param name="message"> the message </param>
		/// <exception cref="System.IO.IOException"> if error occurred. </exception>
		void AppendMessage(byte[] message);

		/// <summary>
		/// Appends message to storage with the given formatted timestamp.
		/// </summary>
		/// <param name="timestampFormatted"> the message timestamp </param>
		/// <param name="message"> the message </param>
		/// <param name="offset">  the initial offset </param>
		/// <param name="length">  the length </param>
		/// <exception cref="System.IO.IOException"> if error occurred. </exception>
		void AppendMessage(byte[] timestampFormatted, byte[] message, int offset, int length);

		/// <summary>
		/// Appends message to storage with the given formatted timestampFormatted.
		/// </summary>
		/// <param name="timestampFormatted"> the message timestampFormatted </param>
		/// <param name="message"> the message </param>
		/// <exception cref="System.IO.IOException"> if error occurred. </exception>
		void AppendMessage(byte[] timestampFormatted, byte[] message);

		/// <summary>
		/// Retrieves message from storage.
		/// </summary>
		/// <param name="seqNumber"> the sequence number of message </param>
		/// <returns> the retrieved message </returns>
		/// <exception cref="System.IO.IOException"> if error occurred. </exception>
		byte[] RetrieveMessage(long seqNumber);

		/// <summary>
		/// Retrieves message from storage.
		/// </summary>
		/// <param name="fromSeqNum"> the from sequence number </param>
		/// <param name="toSeqNun">   the to sequence number </param>
		/// <param name="listener">   the callback listener </param>
		/// <param name="blocking">   if parameter is true, the execution start in current thread context, otherwise in the new thread context. </param>
		/// <exception cref="System.IO.IOException"> if error occurred. </exception>
		void RetrieveMessages(long fromSeqNum, long toSeqNun, IMessageStorageListener listener, bool blocking);

		/// <summary>
		/// Close the storage.
		/// </summary>
		/// <exception cref="System.IO.IOException"> if error occurred. </exception>
		void Close();

		/// <summary>
		/// Backup the storage.
		/// </summary>
		/// <param name="sessionParameters"> the session parameters
		///  </param>
		void BackupStorage(SessionParameters sessionParameters);
	}
}