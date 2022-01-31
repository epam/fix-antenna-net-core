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
	/// Connection listener interface.
	/// </summary>
	/// <seealso cref="IServer"></seealso>
	internal interface IConnectionListener
	{
		/// <summary>
		/// Invoked when a new connection occurred.
		/// </summary>
		/// <param name="transport"> the transport </param>
		void OnConnect(ITransport transport);
	}
}