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

using Epam.FixAntenna.NetCore.Common.Utils;

namespace Epam.FixAntenna.NetCore.FixEngine.Storage.Queue
{
	/// <summary>
	/// The common queueable interface, supported by <seealso cref="IQueue{T}"/> .
	/// </summary>
	/// <seealso cref="IQueueableFactory{T}"> </seealso>
	/// <seealso cref="IQueue{T}">  </seealso>
	internal interface IQueueable
	{
		/// <summary>
		/// The <c>AsByteArray</c> method is responsible for
		/// write object to buffer of bytes.
		/// </summary>
		/// <param name="buffer"> of bytes </param>
		void SerializeTo(ByteBuffer buffer);

		/// <summary>
		/// The <c>FromBytes</c> method is responsible for reading object from buffer of bytes.
		/// </summary>
		/// <param name="bytes"> buffer of bytes </param>
		/// <param name="offset"> offset in buffer </param>
		/// <param name="length"> number of bytes to read
		/// </param>
		void FromBytes(byte[] bytes, int offset, int length);
	}
}