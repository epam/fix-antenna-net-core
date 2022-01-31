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

using System.IO;
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.PerType
{
	/// <summary>
	/// The heartbeat message handler.
	/// </summary>
	internal class HeartbeatMessageHandler : AbstractSessionMessageHandler
	{
		private readonly TagValue _copyTestReqId = new TagValue();
		private readonly TagValue _tempTagValue = new TagValue();

		/// <summary>
		/// Creates the <c>HeartbeatMessageHandler</c>.
		/// </summary>
		public HeartbeatMessageHandler()
		{
		}

		/// <summary>
		/// If the message contains the 112 tag,
		/// the value of 112 tag will be save as session attribute
		/// <c>LastReceivedTestReqID</c>.
		/// </summary>
		/// <seealso cref="IFixMessageListener.OnNewMessage(FixMessage)"> </seealso>
		public override void OnNewMessage(FixMessage message)
		{
			Log.Debug("Heartbeat message handler");
			if (message.IsTagExists(Tags.TestReqID))
			{
				try
				{
					message.LoadTagValue(Tags.TestReqID, _tempTagValue);
				}
				catch (FieldNotFoundException)
				{
					// we have done the check
				}
				_copyTestReqId.TagId = _tempTagValue.TagId;
				_copyTestReqId.Value = _tempTagValue.Value;
				if (Log.IsDebugEnabled)
				{
					Log.Debug("Heartbeat ID:" + _copyTestReqId.StringValue);
				}
				var fixSession = Session;
				fixSession.SetAttribute(ExtendedFixSessionAttribute.LastReceivedTestReqId.Name, _copyTestReqId);

				if (fixSession.GetAttribute(ExtendedFixSessionAttribute.TestRequestIsSentForSeqReset.Name) != null)
				{
					fixSession.RemoveAttribute(ExtendedFixSessionAttribute.TestRequestIsSentForSeqReset.Name);
					ResetSequence(fixSession);
				}
			}
		}

		private void ResetSequence(IExtendedFixSession fixSession)
		{
			try
			{
				fixSession.ResetSequenceNumbers(false);
			}
			catch (IOException e)
			{
				Log.Error("Error on send intraday logon. Cause: " + e.Message, e);
			}
		}
	}
}