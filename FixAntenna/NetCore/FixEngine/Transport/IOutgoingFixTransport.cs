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

using System.Threading.Tasks;

namespace Epam.FixAntenna.NetCore.FixEngine.Transport
{
	/// <summary>
	/// Outgoing FIX transport.
	/// </summary>
	internal interface IOutgoingFixTransport : IFixTransport
	{
		/// <summary>
		/// Open transport method.
		/// </summary>
		/// <exception cref="System.IO.IOException"> if error occurred
		///  </exception>
		void Open();

		/// <summary>
		/// Open transport method. Async version.
		/// </summary>
		/// <exception cref="System.IO.IOException"> if error occurred
		///  </exception>
		Task OpenAsync();
	}
}