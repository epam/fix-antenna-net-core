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
	/// This is interface for receiving messages rejected from internal queue.
	/// Sometimes occurs serious problem in session and it couldn't send anymore messages.
	/// Messages, which were stored in internal queue and waiting for delivering will be removed.
	/// This listener can notify that message was removed from sending queue.
	/// </summary>
	public interface IRejectMessageListener
	{
		/// <summary>
		/// On remove message from sending queue.
		/// </summary>
		/// <param name="message"> the rejected message </param>
		void OnRejectMessage(FixMessage message);
	}
}