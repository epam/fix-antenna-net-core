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

using Epam.FixAntenna.NetCore.FixEngine.Session.IoThreads;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler
{
	/// <summary>
	/// Composite message handler.
	/// Provides ability to listen incoming messages from <see cref="MessageReader"/>.
	/// </summary>
	internal interface ICompositeMessageHandlerListener
	{
		/// <summary>
		/// Invoked when incoming message received.
		/// </summary>
		/// <param name="messageBuf"> the message </param>
		void OnMessage(MsgBuf messageBuf);
	}
}