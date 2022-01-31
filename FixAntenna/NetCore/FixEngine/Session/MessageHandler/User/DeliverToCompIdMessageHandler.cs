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
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.User
{
	/// <summary>
	/// This handler is used for Third Party Message routing.
	/// Session massages are skipped.
	/// <p/>
	/// </summary>
	internal class DeliverToCompIdMessageHandler : AbstractUserGlobalMessageHandler
	{
		private TagValue _copiedValue = new TagValue();

		/// <inheritdoc />
		public override bool ProcessMessage(FixMessage message)
		{
			return !HandleMessage(message);
		}

		private bool HandleMessage(FixMessage message)
		{
			var sessionParameters = Session.Parameters;
			var msgType = message.MsgType; // fixed bug 15319: Message without tag 35 caused NullPointerException and session termination
			if (msgType != null && !RawFixUtil.IsSessionLevelType(msgType) && IsDeliverMessage(message))
			{
				if (ProcessDeliverMessage(message, sessionParameters))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Checks if message has deliver tag.
		/// </summary>
		/// <param name="message"> the fix message </param>
		private bool IsDeliverMessage(FixMessage message)
		{
			var deliverTagIndex = message.GetTagIndex(Tags.DeliverToCompID);
			if (deliverTagIndex == FixMessage.NotFound)
			{
				return false;
			}

			return message.GetTagValueLengthAtIndex(deliverTagIndex) > 0;
		}

		/// <summary>
		/// Deliver message handler.
		/// </summary>
		/// <param name="message"> the deliver message </param>
		private bool ProcessDeliverMessage(FixMessage message, SessionParameters sessionParameters)
		{
			//Smh.DeliverToCompId(128)
			var targetComIdField = message.GetTagValueAsString(Tags.DeliverToCompID);
			//Smh.DeliverToSubId(129)
			var targetSubIdField = message.GetTagValueAsString(Tags.DeliverToSubID);
			//Smh.DeliverToLocationId(145)
			var targetLocationIdField = message.GetTagValueAsString(Tags.DeliverToLocationID);
			var deliverSession = Lookup(sessionParameters.SenderCompId, sessionParameters.SenderSubId, sessionParameters.SenderLocationId, targetComIdField, targetSubIdField, targetLocationIdField);
			if (deliverSession != null)
			{
				message = PrepareMessageForDelivering(message, sessionParameters);

				deliverSession.SendMessage(message);
				return true;
			}
			else
			{
				if (Log.IsWarnEnabled)
				{
					Log.Warn("No delivery session found: sender-" + sessionParameters.SenderCompId + ", " + "target-" + targetComIdField + ".");
				}
			}
			return false;
		}

		private FixMessage PrepareMessageForDelivering(FixMessage message, SessionParameters sessionParameters)
		{
			//replace sender and target for message
			var index = message.GetTagIndex(Tags.SenderCompID);
			message.SetAtIndex(index, sessionParameters.SenderCompId);
			if (sessionParameters.SenderSubId != null)
			{
				var tagIndex = message.GetTagIndex(Tags.SenderSubID);
				if (tagIndex == FixMessage.NotFound)
				{
					message.AddTagAtIndex(index++, Tags.SenderSubID, sessionParameters.SenderSubId);
				}
				else
				{
					message.SetAtIndex(tagIndex, sessionParameters.SenderSubId);
				}
			}
			else
			{
				message.RemoveTag(Tags.SenderSubID);
			}

			if (sessionParameters.SenderLocationId != null)
			{
				var tagIndex = message.GetTagIndex(Tags.SenderLocationID);
				if (tagIndex == FixMessage.NotFound)
				{
					message.AddTagAtIndex(index++, Tags.SenderLocationID, sessionParameters.SenderLocationId);
				}
				else
				{
					message.SetAtIndex(tagIndex, sessionParameters.SenderLocationId);
				}
			}
			else
			{
				message.RemoveTag(Tags.SenderLocationID);
			}

			//set values from DeliverToXXX as a target
			message.LoadTagValue(Tags.DeliverToCompID, _copiedValue);
			_copiedValue.TagId = Tags.TargetCompID;
			message.Set(_copiedValue);

			index = message.GetTagIndex(Tags.DeliverToSubID);
			if (index != FixMessage.NotFound)
			{
				message.LoadTagValueByIndex(index, _copiedValue);
				_copiedValue.TagId = Tags.TargetSubID;

				var tagIndex = message.GetTagIndex(Tags.TargetSubID);
				if (tagIndex == FixMessage.NotFound)
				{
					message.AddTagAtIndex(index++, _copiedValue);
				}
				else
				{
					message.Set(_copiedValue);
				}
			}
			else
			{
				message.RemoveTag(Tags.TargetSubID);
			}

			index = message.GetTagIndex(Tags.DeliverToLocationID);
			if (index != FixMessage.NotFound)
			{
				message.LoadTagValueByIndex(index, _copiedValue);
				_copiedValue.TagId = Tags.TargetLocationID;

				var tagIndex = message.GetTagIndex(Tags.TargetLocationID);
				if (tagIndex == FixMessage.NotFound)
				{
					message.AddTagAtIndex(index++, _copiedValue);
				}
				else
				{
					message.Set(_copiedValue);
				}
			}
			else
			{
				message.RemoveTag(Tags.TargetLocationID);
			}

			//replace tags DelivetToXXX with OnBehaltOfXXX
			index = message.GetTagIndex(Tags.DeliverToCompID); //replace tag
			message.RemoveTagAtIndex(index);
			message.AddTagAtIndex(index++, Tags.OnBehalfOfCompID, sessionParameters.TargetCompId);
			if (sessionParameters.TargetSubId != null)
			{
				message.AddTagAtIndex(index++, Tags.OnBehalfOfSubID, sessionParameters.TargetSubId);
			}
			if (sessionParameters.TargetLocationId != null)
			{
				message.AddTagAtIndex(index++, Tags.OnBehalfOfLocationID, sessionParameters.TargetLocationId);
			}
			message.RemoveTag(Tags.DeliverToSubID);
			message.RemoveTag(Tags.DeliverToLocationID);
			return message;
		}

		/// <summary>
		/// Lookup fix session.
		/// </summary>
		/// <param name="senderComId">  the sender name </param>
		/// <param name="targetCompId"> the target name </param>
		private static IExtendedFixSession Lookup(string senderComId, string senderSubId, string senderLocationId, string targetCompId, string targetSubId, string targetLocationId)
		{
			return FixSessionManager.Instance.Locate(senderComId, senderSubId, senderLocationId, targetCompId, targetSubId, targetLocationId);
		}
	}
}