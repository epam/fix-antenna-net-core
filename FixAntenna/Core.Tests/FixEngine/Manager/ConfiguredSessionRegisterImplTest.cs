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

using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Manager
{
	[TestFixture]
	internal class ConfiguredSessionRegisterImplTest
	{
		private ConfiguredSessionRegisterImpl _register;
		private ConfiguredSessionListenerChecker _eventChecker;

		[SetUp]
		public void SetUp()
		{
			_eventChecker = new ConfiguredSessionListenerChecker();
			_register = new ConfiguredSessionRegisterImpl();
			_register.AddSessionManagerListener(_eventChecker);
		}

		[Test]
		public virtual void TestRegisterNoIdSession()
		{
			var @params = BuildParams("s", "ss", "sl", "t", "ts", "tl");
			_register.RegisterSession(@params);
			_eventChecker.CheckAddSessionEvent(@params);
			var sessions = _register.RegisteredSessions;
			Assert.AreEqual(1, sessions.Count);
			Assert.AreEqual(@params, sessions[0]);
		}

		[Test]
		public virtual void TestRegisterIdSession()
		{
			var @params = BuildParams("s", "ss", "sl", "t", "ts", "tl");
			@params.SetSessionId("SessionId");

			_register.RegisterSession(@params.SessionId, @params);
			_eventChecker.CheckAddSessionEvent(@params);
			var sessions = _register.RegisteredSessions;
			Assert.AreEqual(1, sessions.Count);
			Assert.AreEqual(@params, sessions[0]);
		}

		[Test]
		public virtual void TestRegisterDuplicateSession()
		{
			var @params = BuildParams("s", "ss", "sl", "t", "ts", "tl");

			_register.RegisterSession(@params);

			var duplicate = BuildParams("S", "SS", "SL", "T", "TS", "TL");
			//duplicate
			Assert.Throws<DuplicateSessionException>(() => _register.RegisterSession(duplicate));
		}

		[Test]
		public virtual void TestIsSessionRegistered()
		{
			var @params = BuildParams("s", "ss", "sl", "t", "ts", "tl");
			_register.RegisterSession(@params);
			Assert.AreEqual(1, _register.RegisteredSessions.Count);

			Assert.IsTrue(_register.IsSessionRegistered(@params.SessionId));
			Assert.IsTrue(_register.IsSessionRegistered("s", "t"));
			Assert.IsTrue(_register.IsSessionRegistered("S", "T"));
			Assert.IsFalse(_register.IsSessionRegistered("s", "A"));
			Assert.IsFalse(_register.IsSessionRegistered("t", "t"));

			Assert.IsTrue(_register.IsSessionRegistered(new SessionId("s", "t", null, "someId")));
			Assert.IsTrue(_register.IsSessionRegistered(new SessionId("S", "T", null, "someId")));
			Assert.IsFalse(_register.IsSessionRegistered(new SessionId("A", "t", null, "someId")));
			Assert.IsFalse(_register.IsSessionRegistered(new SessionId("s", "A", null, "someId")));
		}


		[Test]
		public virtual void TestGetSessionByParams()
		{
			var @params = BuildParams("s", "ss", "sl", "t", "ts", "tl");
			@params.SetSessionId("CustomSessionID");
			_register.RegisterSession(@params.SessionId, @params);

			Assert.AreEqual(@params, _register.GetSessionParams("s", "t"));
			Assert.AreEqual(@params, _register.GetSessionParams("S", "T"));
			Assert.AreEqual(@params, _register.GetSessionParams(new SessionId("s", "t", null, "CustomSessionID")));
			Assert.AreEqual(@params, _register.GetSessionParams(new SessionId("S", "T", null, "CUSTOMSESSIONID")));
			Assert.AreEqual(@params, _register.GetSessionParams(new SessionId("S", "T", null, "ANY")));
			Assert.IsNull(_register.GetSessionParams("S", "A"));
			Assert.IsNull(_register.GetSessionParams("A", "T"));
		}

		[Test]
		public virtual void TestUnregisterSession()
		{
			var params1 = BuildParams("s", "ss", "sl", "t", "ts", "tl");
			_register.RegisterSession(params1);
			Assert.AreEqual(1, _register.RegisteredSessions.Count);
			_eventChecker.ClearEvents();

			var params2 = BuildParams("S", "SS", "SL", "T", "TS", "TL");
			_register.UnregisterSession(params2);
			Assert.AreEqual(0, _register.RegisteredSessions.Count);
			_eventChecker.CheckRemoveSessionEvent(params1);
		}


		[Test]
		public virtual void TestDeleteAllSession()
		{
			//init
			var params1 = BuildParams("s", "ss", "sl", "t", "ts", "tl");
			_register.RegisterSession(params1);
			var params2 = BuildParams("s2", "ss2", "sl2", "t2", "ts2", "tl2");
			_register.RegisterSession(params2);
			Assert.AreEqual(2, _register.RegisteredSessions.Count);
			_eventChecker.ClearEvents();


			_register.DeleteAll();
			Assert.AreEqual(0, _register.RegisteredSessions.Count);
			if (_eventChecker.Events.First.Value.GetParams().Equals(params1))
			{
				_eventChecker.CheckRemoveSessionEvent(params1);
				_eventChecker.CheckRemoveSessionEvent(params2);
			}
			else
			{
				_eventChecker.CheckRemoveSessionEvent(params2);
				_eventChecker.CheckRemoveSessionEvent(params1);
			}
		}

		private SessionParameters BuildParams(string senderCompId, string senderSubId, string senderLocationId, string targetComptId, string targetSubId, string targetLocationId)
		{
			var parameters = new SessionParameters();
			parameters.SenderCompId = senderCompId;
			parameters.TargetCompId = targetComptId;
			parameters.SenderSubId = senderSubId;
			parameters.TargetSubId = targetSubId;
			parameters.SenderLocationId = senderLocationId;
			parameters.TargetLocationId = targetLocationId;
			return parameters;
		}
	}
}