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
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.TestUtils;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.User;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Message.Format;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.User
{
	[TestFixture]
	internal class DeliverToCompIdMessageHandlerTest
	{
		protected internal static readonly ILog Log = LogFactory.GetLog(typeof(DeliverToCompIdMessageHandlerTest));

		private NextHandler _nextHandler;

		[SetUp]
		public virtual void SetUp()
		{
			_nextHandler = new NextHandler();
		}

		[TearDown]
		public virtual void TearDown()
		{
			ClearLogs();
			FixSessionManager.Instance.RemoveAllSessions();
			ClassicAssert.IsTrue(ClearLogs(), "Can't Clean logs after tests");
		}

		public virtual bool ClearLogs()
		{
			var logsCleaner = new LogsCleaner();
			return logsCleaner.Clean("./logs") && logsCleaner.Clean("./logs/backup");
		}

		[Test]
		public virtual void CheckInvalidMessageType()
		{
			var handler = GetHandler("A", null, null, "B", null, null);
			var message = new FixMessage();
			message.AddTag(8, "FIX.4.4");
			message.AddTag(34, (long)1);
			message.AddCalendarTag(52, DateTime.Now, FixDateFormatterFactory.FixDateType.UtcTimestampShort);
			message.AddTag(10, (long)20);

			handler.OnNewMessage(message);
			ClassicAssert.IsNotNull(_nextHandler.Message);
			handler.Session.Dispose();
		}

		[Test]
		public virtual void CheckMessageType()
		{
			var handler = GetHandler("A", null, null, "B", null, null);
			var message = new FixMessage();
			message.AddTag(8, "FIX.4.4");
			message.AddTag(34, (long)1);
			message.AddCalendarTag(52, DateTime.Now, FixDateFormatterFactory.FixDateType.UtcTimestampShort);
			message.AddTag(35, "B");
			message.AddTag(Tags.DeliverToCompID, "B");
			message.AddTag(10, (long)20);

			handler.OnNewMessage(message);
			ClassicAssert.IsNotNull(_nextHandler.Message);
			handler.Session.Dispose();
		}

		[Test]
		public virtual void CheckDeliveringWithAllFields()
		{
			CheckDelivering("A", "ASub", "ALoc", "B", "BSub", "BLoc", "C", "CSub", "CLoc");
		}

		[Test]
		public virtual void CheckDeliveringWithMinimumFields()
		{
			CheckDelivering("A", null, null, "B", null, null, "C", null, null);
		}

		[Test]
		public virtual void CheckDeliveringWithFullSender()
		{
			CheckDelivering("A", "ASub", "ALoc", "B", null, null, "C", null, null);
		}

		[Test]
		public virtual void CheckDeliveringWithFullTarget()
		{
			CheckDelivering("A", null, null, "B", "BSub", "BLoc", "C", null, null);
		}

		[Test]
		public virtual void CheckDeliveringWithFullDeliverToFields()
		{
			CheckDelivering("A", null, null, "B", null, null, "C", "CSub", "CLoc");
		}

		public virtual void CheckDelivering(string senderCompId, string senderSubId, string senderLocationId, string targetCompId, string targetSubId, string targetLocationId, string deliverCompId, string deliverSubId, string deliverLocationId)
		{
			var handler = GetHandler(targetCompId, targetSubId, targetLocationId, senderCompId, senderSubId, senderLocationId);

			var targetSession = new TestFixSession();
			try
			{
				var origSessionParameters = targetSession.Parameters;
				origSessionParameters.SenderCompId = targetCompId;
				origSessionParameters.SenderSubId = targetSubId;
				origSessionParameters.SenderLocationId = targetLocationId;
				origSessionParameters.TargetCompId = deliverCompId;
				origSessionParameters.TargetSubId = deliverSubId;
				origSessionParameters.TargetLocationId = deliverLocationId;
				FixSessionManager.Instance.RegisterFixSession(targetSession);

				var sentMessage = PrepareMessage(senderCompId, senderSubId, senderLocationId, targetCompId, targetSubId, targetLocationId, deliverCompId, deliverSubId, deliverLocationId);

				handler.OnNewMessage(sentMessage);
				ClassicAssert.IsNull(_nextHandler.Message);
				ClassicAssert.AreEqual(1, targetSession.Messages.Count);
				var fixMessage = targetSession.Messages[0];

				Log.Info("Delivered message: " + fixMessage.ToString());

				ClassicAssert.AreEqual(targetCompId, fixMessage.GetTagValueAsString(Tags.SenderCompID));
				ClassicAssert.AreEqual(targetSubId, fixMessage.GetTagValueAsString(Tags.SenderSubID));
				ClassicAssert.AreEqual(targetLocationId, fixMessage.GetTagValueAsString(Tags.SenderLocationID));

				ClassicAssert.AreEqual(deliverCompId, fixMessage.GetTagValueAsString(Tags.TargetCompID));
				ClassicAssert.AreEqual(deliverSubId, fixMessage.GetTagValueAsString(Tags.TargetSubID));
				ClassicAssert.AreEqual(deliverLocationId, fixMessage.GetTagValueAsString(Tags.TargetLocationID));

				ClassicAssert.AreEqual(senderCompId, fixMessage.GetTagValueAsString(Tags.OnBehalfOfCompID));
				ClassicAssert.AreEqual(senderSubId, fixMessage.GetTagValueAsString(Tags.OnBehalfOfSubID));
				ClassicAssert.AreEqual(senderLocationId, fixMessage.GetTagValueAsString(Tags.OnBehalfOfLocationID));

				ClassicAssert.AreEqual("Header End", fixMessage.GetTagValueAsString(53));
				ClassicAssert.IsNull(fixMessage.GetTagValueAsString(53, 2));
			}
			finally
			{
				targetSession.Dispose();
			}
		}

		private FixMessage PrepareMessage(string senderCompId, string senderSubId, string senderLocationId, string targetCompId, string targetSubId, string targetLocationId, string deliverCompId, string deliverSubId, string deliverLocationId)
		{
			var message = new FixMessage();
			message.AddTag(8, "FIX.4.4");
			message.AddTag(34, (long)1);
			message.AddTag(35, "B");
			message.AddCalendarTag(52, DateTime.Now, FixDateFormatterFactory.FixDateType.UtcTimestampShort);

			message.AddTag(Tags.SenderCompID, senderCompId);
			if (!string.IsNullOrEmpty(senderSubId))
			{
				message.AddTag(Tags.SenderSubID, senderSubId);
			}

			if (!string.IsNullOrEmpty(senderLocationId))
			{
				message.AddTag(Tags.SenderLocationID, senderLocationId);
			}

			message.AddTag(Tags.TargetCompID, targetCompId);
			if (!string.IsNullOrEmpty(targetSubId))
			{
				message.AddTag(Tags.TargetSubID, targetSubId);
			}

			if (!string.IsNullOrEmpty(targetLocationId))
			{
				message.AddTag(Tags.TargetLocationID, targetLocationId);
			}

			message.AddTag(Tags.DeliverToCompID, deliverCompId);
			if (!string.IsNullOrEmpty(deliverSubId))
			{
				message.AddTag(Tags.DeliverToSubID, deliverSubId);
			}

			if (!string.IsNullOrEmpty(deliverLocationId))
			{
				message.AddTag(Tags.DeliverToLocationID, deliverLocationId);
			}

			message.AddTag(53, "Header End");
			message.AddTag(10, "000");

			//rebuild message from buffer, like in real handlers chain
			return RawFixUtil.GetFixMessage(message.AsByteArray());
		}

		public virtual DeliverToCompIdMessageHandler GetHandler(string senderCompId, string senderSubId, string senderLocationId, string targetCompId, string targetSubId, string targetLocationId)
		{
			var handler = new DeliverToCompIdMessageHandler();
			handler.NextHandler = _nextHandler;
			var fixSession = new TestFixSession();
			var origParameters = fixSession.Parameters;
			origParameters.SenderCompId = senderCompId;
			origParameters.SenderSubId = senderSubId;
			origParameters.SenderLocationId = senderLocationId;
			origParameters.TargetCompId = targetCompId;
			origParameters.TargetSubId = targetSubId;
			origParameters.TargetLocationId = targetLocationId;

			handler.Session = fixSession;
			handler.Session.Parameters.Configuration.SetProperty("user.messagehandler.global.0", typeof(DeliverToCompIdMessageHandler).FullName);
			return handler;
		}


		internal class NextHandler : AbstractGlobalMessageHandler
		{
			public FixMessage Message { get; private set; }

			public override void OnNewMessage(FixMessage message)
			{
				Message = message;
			}
		}
	}

}