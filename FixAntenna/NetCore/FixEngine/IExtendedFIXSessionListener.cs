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

using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine
{
	/// <summary>
	/// Extended fix session listener
	/// </summary>
	public interface IExtendedFixSessionListener : IFixSessionListener
	{
		/// <summary>
		/// Method fired when message received.
		/// </summary>
		/// <param name="msgBuf"> incoming message
		///  </param>
		void OnMessageReceived(MsgBuf msgBuf);

		/// <summary>
		/// Method fired when message will send.
		/// </summary>
		/// <param name="bytes"> outgoing message
		/// </param>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		void OnMessageSent(byte[] bytes, int offset, int length);
	}
}