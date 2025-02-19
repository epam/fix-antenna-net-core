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
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.FixEngine
{
	[TestFixture]
	internal class SessionIdTest
	{
		private readonly string _target = "TARGET";
		private readonly string _sender = "SENDER";
		private readonly string _qualifier = "qID";

		[Test]
		public void TestSenderTarget()
		{
			Verify(_sender + "-" + _target, _sender, _target, null, new SessionId(_sender, _target));
		}

		[Test]
		public void TestSenderTargetQualifier()
		{
			Verify(_sender + "-" + _target + "-" + _qualifier, _sender, _target, _qualifier, new SessionId(_sender, _target, _qualifier));
		}

		[Test]
		public void TestCustomId()
		{
			var customId = "MyID";
			VerifyCustomId(customId, _sender, _target, new SessionId(_sender, _target, null, customId));
		}

		[Test]
		public void TestEqualsSenderTarget()
		{
			var id1 = new SessionId(_sender, _target);
			var id2 = new SessionId(_sender, _target);
			ClassicAssert.AreEqual(id1, id2);
			ClassicAssert.AreEqual(id1.GetHashCode(), id2.GetHashCode());
		}

		[Test]
		public void TestEqualsSenderTargetQualifier()
		{
			var id1 = new SessionId(_sender, _target, _qualifier);
			var id2 = new SessionId(_sender, _target, _qualifier);
			ClassicAssert.AreEqual(id1, id2);
			ClassicAssert.AreEqual(id1.GetHashCode(), id2.GetHashCode());
		}

		[Test]
		public void TestNotEquals()
		{
			var id1 = new SessionId(_sender, _target);
			var idQ2 = new SessionId(_sender, _target, _qualifier);
			var idQ3 = new SessionId(_sender, _target, _qualifier + "2");

			ClassicAssert.IsFalse(id1.Equals(idQ2));
			ClassicAssert.IsFalse(idQ2.Equals(idQ3));
			ClassicAssert.IsFalse(id1.GetHashCode() == idQ2.GetHashCode());
			ClassicAssert.IsFalse(idQ2.GetHashCode() == idQ3.GetHashCode());
		}

		[Test]
		public void TestEqualsCustomId()
		{
			var customId = _sender + "-" + _target;
			var idCustom = new SessionId(_sender, _target, null, customId);
			var id2 = new SessionId(_sender, _target);
			ClassicAssert.IsTrue(idCustom.Equals(id2));
		}

		[Test]
		public void TestNotEqualsCustomId()
		{
			var customId = "MyID";
			var idCustom = new SessionId(_sender, _target, null, customId);
			var id2 = new SessionId(_sender, _target);
			ClassicAssert.IsFalse(idCustom.Equals(id2));
		}

		private static void Verify(string expectedId, string sender, string target, string qualifier, SessionId sessionId)
		{
			ClassicAssert.AreEqual(expectedId, sessionId.ToString());
			ClassicAssert.AreEqual(sender, sessionId.Sender);
			ClassicAssert.AreEqual(target, sessionId.Target);
			ClassicAssert.AreEqual(qualifier, sessionId.Qualifier);
			ClassicAssert.IsNull(sessionId.CustomSessionId);
		}

		private static void VerifyCustomId(string expectedId, string sender, string target, SessionId sessionId)
		{
			ClassicAssert.AreEqual(expectedId, sessionId.ToString());
			ClassicAssert.AreEqual(expectedId, sessionId.CustomSessionId);
			ClassicAssert.AreEqual(sender, sessionId.Sender);
			ClassicAssert.AreEqual(target, sessionId.Target);
		}
	}
}