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

namespace Epam.FixAntenna.NetCore.FixEngine.Transport.Server
{
	/// <summary>
	/// The common server interface.
	/// Provides ability to start and stop operation and listen the incoming connections.
	/// </summary>
	internal interface IServer
	{
		/// <summary>
		/// Sets the connection listener.
		/// </summary>
		/// <param name="value"> the connection listener </param>
		void SetIncomingConnectionListener(IConnectionListener value);

		/// <summary>
		/// Start the server.
		/// </summary>
		/// <exception cref="System.IO.IOException"> - if error occurred </exception>
		void Start();

		/// <summary>
		/// Stop server.
		/// </summary>
		/// <exception cref="System.IO.IOException"> - if error occurred </exception>
		void Stop();
	}
}