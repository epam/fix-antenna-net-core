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

using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.PerType
{
	/// <summary>
	/// The test request message handler.
	/// </summary>
	internal class TestRequestMessageHandler : AbstractSessionMessageHandler
	{
		/// <summary>
		/// Sends the heartbeat message response.
		/// If received message contents the 112 tag, the field will be add to response.
		/// </summary>
		/// <seealso cref="IFixMessageListener.OnNewMessage"/>
		public override void OnNewMessage(FixMessage message)
		{
			var testRequestId = message.GetTagValueAsString(Tags.TestReqID);
			if (Log.IsDebugEnabled)
			{
				Log.Debug("TestRequest message handler: send answer for request with ID '" + testRequestId + "'");
			}
			var content = FixMessageFactory.NewInstanceFromPool();
			if (!string.IsNullOrEmpty(testRequestId))
			{
				content.AddTag(Tags.TestReqID, testRequestId);
				Session.SendMessageOutOfTurn("0", content);
			}
			else
			{
				Log.Warn("Incoming TestRequest does't have TestReqID(112) tag: " + message.ToPrintableString());
			}
		}
	}
}