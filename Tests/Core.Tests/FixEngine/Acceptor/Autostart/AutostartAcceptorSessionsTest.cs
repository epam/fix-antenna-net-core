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
using System.Collections.Generic;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Acceptor.Autostart;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Acceptor.AutoStart
{
	[TestFixture]
	internal class AutostartAcceptorSessionsTest
	{
		[Test]
		public void TestParsingIpFilters()
		{
			IDictionary<string, string> props = new Dictionary<string, string>();
			props[AutostartAcceptorSessions.Prefix + ".targetIds"] = "t";
			props[AutostartAcceptorSessions.Prefix + ".t.ip"] = "192.168.0.100, 74.24.0.0/16";
			props[AutostartAcceptorSessions.Prefix + ".t.fixServerListener"] = "System.String";

			var conf = new Config(props);
			var details = new AutostartAcceptorSessions(conf, null);
			var t = details.Map["t"];
			ClassicAssert.IsNotNull(t);
			ClassicAssert.IsTrue(t.AllowedIp("192.168.0.100"));
			ClassicAssert.IsTrue(t.AllowedIp("74.24.8.100"));
			ClassicAssert.IsFalse(t.AllowedIp("74.24.0.0"));
			ClassicAssert.IsFalse(t.AllowedIp("74.24.255.255"));
			ClassicAssert.IsFalse(t.AllowedIp("168.24.0.100"));
		}

		[Test]
		public void TestParsingWildcardIpFilters()
		{
			IDictionary<string, string> props = new Dictionary<string, string>();
			props[AutostartAcceptorSessions.Prefix + ".targetIds"] = "t";
			props[AutostartAcceptorSessions.Prefix + ".t.ip"] = "*";
			props[AutostartAcceptorSessions.Prefix + ".t.fixServerListener"] = "System.String";

			var conf = new Config(props);
			var details = new AutostartAcceptorSessions(conf, null);
			var t = details.Map["t"];
			ClassicAssert.IsNotNull(t);
			ClassicAssert.IsTrue(t.AllowedIp("192.168.0.100"));
			ClassicAssert.IsTrue(t.AllowedIp("74.24.8.100"));
			ClassicAssert.IsTrue(t.AllowedIp("74.24.0.0"));
			ClassicAssert.IsTrue(t.AllowedIp("74.24.255.255"));
			ClassicAssert.IsTrue(t.AllowedIp("168.24.0.100"));
			ClassicAssert.IsTrue(t.AllowedIp("aaa"));
		}


		[Test]
		public void TestInvalidParsingIpFilters()
		{
			IDictionary<string, string> props = new Dictionary<string, string>();
			props[AutostartAcceptorSessions.Prefix + ".targetIds"] = "t";
			props[AutostartAcceptorSessions.Prefix + ".t.ip"] = "aaa";
			props[AutostartAcceptorSessions.Prefix + ".t.fixServerListener"] = "System.String";

			var conf = new Config(props);
			ClassicAssert.Throws<ArgumentException>(() => new AutostartAcceptorSessions(conf, null));
		}
	}
}