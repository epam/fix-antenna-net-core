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

using System.Collections.Generic;
using System.Collections.Immutable;
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global;
using Epam.FixAntenna.NetCore.Message;
using Moq;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	[TestFixture]
	internal class ThrottlingMockTest
	{
		private ThrottleCheckingHandler _handler;

		private ThrottleCheckingHandler GetHandler(bool isThrottlingEnabled, long period, IDictionary<string, long> thresholds)
		{
			ThrottleCheckingHandler handler = new ThrottleCheckingHandlerAnonymousInnerClass(this, isThrottlingEnabled, period, thresholds);
			handler.Session = null;
			return handler;
		}

		internal class ThrottleCheckingHandlerAnonymousInnerClass : ThrottleCheckingHandler
		{
			private readonly ThrottlingMockTest _outerInstance;

			private bool _isThrottlingEnabled;
			private long _period;
			private IDictionary<string, long> _thresholds;

			public ThrottleCheckingHandlerAnonymousInnerClass(ThrottlingMockTest outerInstance, bool isThrottlingEnabled, long period, IDictionary<string, long> thresholds)
			{
				this._outerInstance = outerInstance;
				this._isThrottlingEnabled = isThrottlingEnabled;
				this._period = period;
				this._thresholds = thresholds;
			}

			public override bool GetThrottlingPropertyValue()
			{
				return _isThrottlingEnabled;
			}

			public override long GetThrottlingPeriod()
			{
				return _period;
			}

			public override long GetThreshold(string msgType)
			{
				return _thresholds[msgType];
			}
		}

		[Test]
		public void ThrottlingDisabledTest()
		{
			_handler = new ThrottleCheckingHandlerAnonymousInnerClass2();
			_handler.OnNewMessage(new FixMessage());
		}

		internal class ThrottleCheckingHandlerAnonymousInnerClass2 : ThrottleCheckingHandler
		{
			public override bool GetThrottlingPropertyValue()
			{
				return false;
			}

			public override long GetThrottlingPeriod()
			{
				return 1;
			}

			public override void CheckThrottling(FixMessage message)
			{
				ClassicAssert.Fail("method should not be called with disabled throttling");
			}
		}

		[Test]
		public void ThrottlingEnabledTest()
		{
			var latch = new System.Threading.CountdownEvent(1);
			_handler = new ThrottleCheckingHandlerAnonymousInnerClass3(latch);
			_handler.Session = null;
			_handler.OnNewMessage(new FixMessage());
			ClassicAssert.AreEqual(0L, latch.CurrentCount);
		}

		internal class ThrottleCheckingHandlerAnonymousInnerClass3 : ThrottleCheckingHandler
		{
			private System.Threading.CountdownEvent _latch;

			public ThrottleCheckingHandlerAnonymousInnerClass3(System.Threading.CountdownEvent latch)
			{
				this._latch = latch;
			}

			public override bool GetThrottlingPropertyValue()
			{
				return true;
			}

			public override long GetThrottlingPeriod()
			{
				return 1;
			}

			public override void CheckThrottling(FixMessage message)
			{
				_latch.Signal();
			}
		}

		[Test]
		public void ThrottlingDisconnectTest()
		{
			var fixSession = new Mock<IExtendedFixSession>();
			var threshold = 5L;
			var messageType = "B";

			var message = new FixMessage();
			message.Set(Tags.MsgType, messageType);

			_handler = GetHandler(true, 1000, new Dictionary<string, long> { { messageType, threshold } }.ToImmutableDictionary());
			_handler.Session = fixSession.Object;
			for (var index = 0; index < (threshold + 1); index++)
			{
				_handler.OnNewMessage(message);
			}

			fixSession.Verify(x => x.Disconnect(DisconnectReason.Throttling, It.IsAny<string>()), Times.Once);
		}

		[Test]
		public virtual void ThrottlingLongPeriodDisconnectTest()
		{
			var fixSession = new Mock<IExtendedFixSession>();
			var threshold = 5L;
			var messageType = "B";

			var message = new FixMessage();
			message.Set(35, messageType);

			_handler = GetHandler(true, 60 * 60 * 1000, new Dictionary<string, long> { { messageType, threshold } }.ToImmutableDictionary());
			_handler.Session = fixSession.Object;
			for (var index = 0; index < threshold + 1; index++)
			{
				_handler.OnNewMessage(message);
			}

			fixSession.Verify(x => x.Disconnect(DisconnectReason.Throttling, It.IsAny<string>()), Times.Once);
		}

		[Test]
		public virtual void ThrottlingTwoMessageTypesTestNoDisconnect()
		{
			var fixSession = new Mock<IExtendedFixSession>();
			long? threshold = 5L;
			var messageType = "B";

			IDictionary<string, long> thresholds = new Dictionary<string, long>();
			thresholds[messageType] = threshold.Value;
			thresholds["D"] = 1000000L;

			var message = new FixMessage();
			message.Set(35, "D");

			_handler = GetHandler(true, 60 * 60 * 1000, thresholds);
			_handler.Session = fixSession.Object;
			for (var index = 0; index < threshold.Value + 1; index++)
			{
				_handler.OnNewMessage(message);
			}

			fixSession.Verify(x => x.Disconnect(DisconnectReason.Throttling, It.IsAny<string>()), Times.Never);
		}
	}
}