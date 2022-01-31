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

using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler
{
	/// <summary>
	/// The abstract post process message handler
	/// </summary>
	internal abstract class AbstractGlobalPostProcessSessionMessageHandler : AbstractGlobalMessageHandler, IPostProcessMessageHandler
	{
		protected internal new ILog Log;

		public AbstractGlobalPostProcessSessionMessageHandler()
		{
			Log = LogFactory.GetLog(GetType());
		}

		private IPostProcessMessageHandler _next;

		/// <summary>
		/// Sets the next message handler.
		/// </summary>
		/// <param name="handler"> the next message handler </param>
		public virtual void SetNext(IPostProcessMessageHandler handler)
		{
			_next = handler;
		}

		/// <inheritdoc />
		public override IFixMessageListener NextHandler
		{
			get { return _next; }
		}

		/// <summary>
		/// Invokes the next message handler.
		/// </summary>
		/// <param name="message"> the message </param>
		public virtual void CallNextHandler(MsgBuf message)
		{
			if (NextHandler != null)
			{
				_next.OnPostProcessMessage(message);
			}
		}

		/// <inheritdoc />
		public override void OnNewMessage(FixMessage message)
		{
			if (NextHandler != null)
			{
				_next.OnNewMessage(message);
			}
		}

		/// <inheritdoc />
		public virtual void OnPostProcessMessage(MsgBuf message)
		{
			if (HandleMessage(message))
			{
				CallNextHandler(message);
			}
		}

		/// <summary>
		/// Handle message, user should override this method.
		/// </summary>
		/// <param name="message"> the message </param>
		/// <returns> if true - call next handler </returns>
		public abstract bool HandleMessage(MsgBuf message);
	}
}