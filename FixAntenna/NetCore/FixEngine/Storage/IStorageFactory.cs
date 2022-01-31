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
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.FixEngine.Storage.Queue;

namespace Epam.FixAntenna.NetCore.FixEngine.Storage
{
	/// <summary>
	/// Base storage factory interface. Provides ability to create incoming and outgoing storage.
	/// To replace the standard implementation, use a <c>storageFactory</c> parameter in properties file.
	/// </summary>
	internal interface IStorageFactory
	{
		/// <summary>
		/// Get incoming message storage instance.
		/// </summary>
		/// <param name="sessionParameters"> session parameters </param>
		/// <returns> the incoming message storage </returns>
		IMessageStorage CreateIncomingMessageStorage(SessionParameters sessionParameters);

		/// <summary>
		/// Get outgoing message storage instance.
		/// </summary>
		/// <param name="sessionParameters"> session parameters </param>
		/// <returns> the outgoing message storage </returns>
		IMessageStorage CreateOutgoingMessageStorage(SessionParameters sessionParameters);

		/// <summary>
		/// Save session parameters.
		/// </summary>
		/// <param name="sessionParameters"> session parameters </param>
		/// <exception cref="IOException"> if error occurred </exception>
		void SaveSessionParameters(SessionParameters sessionParameters, FixSessionRuntimeState state);

		/// <summary>
		/// Save session parameters.
		/// </summary>
		/// <param name="sessionParameters"> session parameters </param>
		/// <returns> true if parameters loaded </returns>
		bool LoadSessionParameters(SessionParameters sessionParameters, FixSessionRuntimeState state);

		/// <summary>
		/// Get outgoing message queue.
		/// </summary>
		/// <param name="sessionParameters"> session parameters </param>
		/// <returns> the outgoing queue of messages </returns>
		IQueue<FixMessageWithType> CreateQueue(SessionParameters sessionParameters);
	}
}