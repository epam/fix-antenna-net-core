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
using Epam.FixAntenna.NetCore.FixEngine.Transport;

namespace Epam.FixAntenna.NetCore.FixEngine
{
	/// <summary>
	/// <c>IFixSessionFactory</c> defines session level behaviour and other FIXEngine parameters.
	/// </summary>
	/// <seealso cref="StandardFixSessionFactory"></seealso>
	public interface IFixSessionFactory
	{
		/// <summary>
		/// Creates the initiator session.
		/// </summary>
		/// <param name="sessionParameters"> the session configuration </param>
		/// <returns> the fix session </returns>
		/// <exception cref="IOException"> if the session cannot be created </exception>
		IFixSession CreateInitiatorSession(SessionParameters sessionParameters);

		/// <summary>
		/// Creates the acceptor session.
		/// </summary>
		/// <param name="sessionParameters"> the session configuration </param>
		/// <returns> the fix session </returns>
		/// <exception cref="IOException"> if the session cannot be created </exception>
		IFixSession CreateAcceptorSession(SessionParameters sessionParameters);

		/// <summary>
		/// Creates the acceptor session.
		/// </summary>
		/// <param name="sessionParameters"> the session configuration </param>
		/// <param name="transport"> the fix transport </param>
		/// <returns> the fix session </returns>
		/// <exception cref="IOException"> if the session cannot be created </exception>
		IFixSession CreateAcceptorSession(SessionParameters sessionParameters, IFixTransport transport);
	}
}