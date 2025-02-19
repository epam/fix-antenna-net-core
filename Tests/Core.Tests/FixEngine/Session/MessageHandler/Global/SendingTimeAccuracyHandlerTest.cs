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
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation.Error;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global
{
	[TestFixture]
	internal class SendingTimeAccuracyHandlerTest : AbstractDataLengthCheckHandlerTst
	{
		private static readonly int? DefaultAccuracy = 100; // ms

		private SendingTimeAccuracyHandler _handler;

		private FixMessage _message;

		private TestFixSession _testFixSession;

		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			_testFixSession = new TestFixSession();
			_testFixSession.Parameters.Configuration.SetProperty(Config.Accuracy, DefaultAccuracy.ToString());

			_handler = new SendingTimeAccuracyHandler();
			_handler.Session = _testFixSession;
			_handler.NextHandler = this;

			_message = new FixMessage();
			_message.AddTag(8, "FIX.4.4");
			_message.AddTag(35, "B");
			_message.AddTag(Tags.SendingTime, FixTypes.FormatUtcTimestamp(DateTime.Now, TimestampPrecision.Milli));
		}

		/// <summary>
		/// check valid time
		/// </summary>
		[Test]
		public virtual void TestValidSendingTime()
		{
			_handler.OnNewMessage(_message);
			ClassicAssertThatMessagePassedToNextHandlerIs(_message);
			base.Reset();

			var timestamp = DateTimeHelper.CurrentMilliseconds - DefaultAccuracy.Value;
			var calendar = DateTimeHelper.FromMilliseconds(timestamp);
			_message.Set(Tags.SendingTime, FixTypes.FormatUtcTimestamp(calendar, TimestampPrecision.Milli));

			_handler.OnNewMessage(_message);
			ClassicAssertThatMessagePassedToNextHandlerIs(_message);
			base.Reset();

			timestamp = DateTimeHelper.CurrentMilliseconds + DefaultAccuracy.Value;
			calendar = DateTimeHelper.FromMilliseconds(timestamp);

			_message.Set(Tags.SendingTime, FixTypes.FormatUtcTimestamp(calendar, TimestampPrecision.Milli));

			_handler.OnNewMessage(_message);
			ClassicAssertThatMessagePassedToNextHandlerIs(_message);
		}

		/// <summary>
		/// check valid time
		/// </summary>
		[Test]
		public virtual void TestValidSendingTimeWithFractions()
		{
			var timestamp = DateTimeHelper.CurrentMilliseconds - DefaultAccuracy.Value;
			var calendar = DateTimeHelper.FromMilliseconds(timestamp);

			var value = FixTypes.FormatUtcTimestamp(calendar, TimestampPrecision.Milli);
			var fractinalTimestamp = new byte[value.Length + 2];
			fractinalTimestamp[fractinalTimestamp.Length - 2] = (byte)'5';
			fractinalTimestamp[fractinalTimestamp.Length - 1] = (byte)'3';
			Array.Copy(value, 0, fractinalTimestamp, 0, value.Length);
			_message.Set(Tags.SendingTime, fractinalTimestamp);

			_handler.OnNewMessage(_message);
			ClassicAssertThatMessagePassedToNextHandlerIs(_message);
			base.Reset();
		}


		/// <summary>
		/// check if (SendingTime - DEFAULT_ACCURACY - 2 * DEFAULT_REASONABLE_DELAY) less than current system time
		/// </summary>
		[Test]
		public virtual void TestInvalidSendingTime()
		{
			var timestamp = DateTimeHelper.CurrentMilliseconds - SendingTimeAccuracyHandler.DefaultAccuracy - 2 * SendingTimeAccuracyHandler.DefaultReasonableDelay;
			var calendar = DateTimeHelper.FromMilliseconds(timestamp);
			_message.Set(Tags.SendingTime, FixTypes.FormatUtcTimestamp(calendar, TimestampPrecision.Milli));

			var ex = ClassicAssert.Throws<MessageValidationException>(() =>
			{
				_handler.OnNewMessage(_message);
			});

			ClassicAssert.IsTrue(ex.IsCritical());
			ClassicAssert.AreEqual(Tags.SendingTime, ex.GetProblemField().TagId);
			ClassicAssert.AreEqual(FixErrorCode.SendingTimeAccuracyProblem, ex.GetFixErrorCode());

			ClassicAssertNoMessagePassedForNext();
			ClassicAssert.IsTrue(_testFixSession.Messages.Count > 0);
			var responseMessage = _testFixSession.Messages[0];
			ClassicAssert.AreEqual(3, responseMessage.GetTagAsInt(35));
			ClassicAssert.AreEqual("SendingTime accuracy problem", responseMessage.GetTagValueAsString(58));

			ClassicAssert.AreEqual("SendingTime accuracy problem", _testFixSession.DisconnectReason);
			ClassicAssert.AreEqual(SessionState.WaitingForLogoff, _testFixSession.SessionState);
			ClassicAssert.AreEqual(DisconnectReason.InvalidMessage, _testFixSession.LastDisconnectReason);
		}

		/// <summary>
		/// check if
		/// (SendingTime + DEFAULT_ACCURACY + 2 * DEFAULT_REASONABLE_DELAY) > current system time
		/// </summary>
		[Test]
		public virtual void CheckIfSendingTimeIsToHigher()
		{
			var timestamp = DateTimeHelper.CurrentMilliseconds + 2 * SendingTimeAccuracyHandler.DefaultReasonableDelay;
			var calendar = DateTimeHelper.FromMilliseconds(timestamp);

			_message.Set(Tags.SendingTime, FixTypes.FormatUtcTimestamp(calendar, TimestampPrecision.Milli));

			var ex = ClassicAssert.Throws<MessageValidationException>(() =>
			{
				_handler.OnNewMessage(_message);
			});

			ClassicAssert.IsTrue(ex.IsCritical());
			ClassicAssert.AreEqual(Tags.SendingTime, ex.GetProblemField().TagId);
			ClassicAssert.AreEqual(FixErrorCode.SendingTimeAccuracyProblem, ex.GetFixErrorCode());

			ClassicAssertNoMessagePassedForNext();
			ClassicAssert.IsTrue(_testFixSession.Messages.Count > 0);
			var responseMessage = _testFixSession.Messages[0];
			ClassicAssert.AreEqual(3, responseMessage.GetTagAsInt(35));
			ClassicAssert.AreEqual("SendingTime accuracy problem", responseMessage.GetTagValueAsString(58));

			ClassicAssert.AreEqual("SendingTime accuracy problem", _testFixSession.DisconnectReason);
			ClassicAssert.AreEqual(SessionState.WaitingForLogoff, _testFixSession.SessionState);
			ClassicAssert.AreEqual(DisconnectReason.InvalidMessage, _testFixSession.LastDisconnectReason);
		}

		/// <summary>
		/// this test used for check if
		/// (SendingTime - DEFAULT_REASONABLE_DELAY) approximately eq to current system time
		/// </summary>
		[Test]
		public virtual void CheckIfSendingTimeIsABitLessCurrent()
		{
			var timestamp = DateTimeHelper.CurrentMilliseconds - SendingTimeAccuracyHandler.DefaultReasonableDelay;
			var calendar = DateTimeHelper.FromMilliseconds(timestamp);

			_message.Set(Tags.SendingTime, FixTypes.FormatUtcTimestamp(calendar, TimestampPrecision.Milli));

			_handler.OnNewMessage(_message);
			ClassicAssertThatMessagePassedToNextHandlerIs(_message);
		}

		/// <summary>
		/// this test used for check if
		/// (SendingTime + DEFAULT_REASONABLE_DELAY) approximately eq to current system time
		/// </summary>
		[Test]
		public virtual void CheckIfSendingTimeIsABitHighCurrent()
		{
			var timestamp = DateTimeHelper.CurrentMilliseconds + SendingTimeAccuracyHandler.DefaultReasonableDelay;

			var calendar = DateTimeHelper.FromMilliseconds(timestamp);
			_message.Set(Tags.SendingTime, FixTypes.FormatUtcTimestamp(calendar, TimestampPrecision.Milli));

			_handler.OnNewMessage(_message);
			ClassicAssertThatMessagePassedToNextHandlerIs(_message);
		}

		[Test]
		public virtual void CheckIfSendingTimeIsAbsent()
		{
			_message.RemoveTag(Tags.SendingTime);

			var ex = ClassicAssert.Throws<MessageValidationException>(() => { _handler.OnNewMessage(_message); });

			ClassicAssert.IsTrue(ex.IsCritical());
			ClassicAssert.AreEqual(Tags.SendingTime, ex.GetProblemField().TagId);
			ClassicAssert.AreEqual(FixErrorCode.RequiredTagMissing, ex.GetFixErrorCode());

			ClassicAssertNoMessagePassedForNext();
			ClassicAssert.IsTrue(_testFixSession.Messages.Count > 0);
			var responseMessage = _testFixSession.Messages[0];
			ClassicAssert.AreEqual(3, responseMessage.GetTagAsInt(35));
			ClassicAssert.AreEqual("Missed sending time", responseMessage.GetTagValueAsString(58));

			ClassicAssert.AreEqual("Missed sending time", _testFixSession.DisconnectReason);
			ClassicAssert.AreEqual(SessionState.WaitingForLogoff, _testFixSession.SessionState);
			ClassicAssert.AreEqual(DisconnectReason.InvalidMessage, _testFixSession.LastDisconnectReason);
		}

		[Test]
		public virtual void CheckIfSendingTimeIsInvalid()
		{
			_message.Set(Tags.SendingTime, "a");

			var ex = ClassicAssert.Throws<MessageValidationException>(() =>
			{
				_handler.OnNewMessage(_message);
			});

			ClassicAssert.IsTrue(ex.IsCritical());
			ClassicAssert.AreEqual(Tags.SendingTime, ex.GetProblemField().TagId);
			ClassicAssert.AreEqual(FixErrorCode.IncorrectDataFormatForValue, ex.GetFixErrorCode());

			ClassicAssertNoMessagePassedForNext();
			ClassicAssert.IsTrue(_testFixSession.Messages.Count > 0);
			var responseMessage = _testFixSession.Messages[0];
			ClassicAssert.AreEqual(3, responseMessage.GetTagAsInt(35));
			ClassicAssert.AreEqual("Invalid sending time", responseMessage.GetTagValueAsString(58));

			ClassicAssert.AreEqual("Invalid sending time", _testFixSession.DisconnectReason);
			ClassicAssert.AreEqual(SessionState.WaitingForLogoff, _testFixSession.SessionState);
			ClassicAssert.AreEqual(DisconnectReason.InvalidMessage, _testFixSession.LastDisconnectReason);
		}

		[Test]
		public virtual void CheckTimeInBoundMethod()
		{
			_handler.Session.Parameters.Configuration.SetProperty(Config.Accuracy, "1");
			_handler.Session.Parameters.Configuration.SetProperty(Config.Delay, "10");
			_handler.Session = _handler.Session;

			long msgTime = 100;
			long currTime = 104;
			var inOutBorder = _handler.IsTimeOutOfBorder(msgTime, currTime);
			ClassicAssert.IsFalse(inOutBorder);

			msgTime = 90;
			currTime = 104;
			inOutBorder = _handler.IsTimeOutOfBorder(msgTime, currTime);
			ClassicAssert.IsTrue(inOutBorder);

			msgTime = 95;
			currTime = 104;
			inOutBorder = _handler.IsTimeOutOfBorder(msgTime, currTime);
			ClassicAssert.IsFalse(inOutBorder);

			msgTime = 114;
			currTime = 104;
			inOutBorder = _handler.IsTimeOutOfBorder(msgTime, currTime);
			ClassicAssert.IsFalse(inOutBorder);

			msgTime = 115;
			currTime = 104;
			inOutBorder = _handler.IsTimeOutOfBorder(msgTime, currTime);
			ClassicAssert.IsTrue(inOutBorder);
		}
	}

}