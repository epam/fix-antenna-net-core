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

using System;
using System.IO;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.IoThreads
{
	internal class TransportMessagesNotSentException : IOException
	{
		private readonly string[] _messages;

		public TransportMessagesNotSentException(Exception cause, string[] messages) : base(null, cause)
		{
			this._messages = messages;
		}

		public virtual string[] GetMessages()
		{
			return this._messages;
		}

		public override string ToString()
		{
			var messageStr = "";
			for (var i = 0; i < _messages.Length; i++)
			{
				messageStr += _messages[i] + Environment.NewLine;
			}

			return base.ToString() + Environment.NewLine + "lost messages:" + Environment.NewLine + messageStr;
		}
	}
}