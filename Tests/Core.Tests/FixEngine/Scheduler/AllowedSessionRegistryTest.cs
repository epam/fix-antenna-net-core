// Copyright (c) 2022 EPAM Systems
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
using Epam.FixAntenna.NetCore.FixEngine.Scheduler;

using NUnit.Framework;

namespace Epam.FixAntenna.Core.Tests.FixEngine.Scheduler
{
	public class AllowedSessionRegistryTest
	{
		[Test]
		public void AllowAndDenySessionTest()
		{
			var registry = new AllowedSessionRegistry();
			var parameters = GetDefaultParams();

			registry.DenySessionForConnect(parameters);
			Assert.IsFalse(registry.IsSessionAllowed(parameters));

			registry.AllowSessionForConnect(parameters);
			Assert.IsTrue(registry.IsSessionAllowed(parameters));

			registry.DenySessionForConnect(parameters);
			Assert.IsFalse(registry.IsSessionAllowed(parameters));
		}

		[Test]
		public void CleanTestAfterAllowWhenDefaultAllow()
		{
			var registry = new AllowedSessionRegistry();
			var @params = GetDefaultParams();

			Assert.IsTrue(registry.IsSessionAllowed(@params));

			registry.AllowSessionForConnect(@params);
			Assert.IsTrue(registry.IsSessionAllowed(@params));

			registry.Clean(@params);
			Assert.IsTrue(registry.IsSessionAllowed(@params));
		}

		[Test]
		public void CleanTestAfterAllowWhenDefaultDeny()
		{
			var registry = new AllowedSessionRegistry(false);
			var @params = GetDefaultParams();

			Assert.IsFalse(registry.IsSessionAllowed(@params));

			registry.AllowSessionForConnect(@params);
			Assert.IsTrue(registry.IsSessionAllowed(@params));

			registry.Clean(@params);
			Assert.IsFalse(registry.IsSessionAllowed(@params));
		}

		[Test]
		public void CleanTestAfterDenyWhenDefaultAllow()
		{
			var registry = new AllowedSessionRegistry();
			var @params = GetDefaultParams();

			Assert.IsTrue(registry.IsSessionAllowed(@params));

			registry.DenySessionForConnect(@params);
			Assert.IsFalse(registry.IsSessionAllowed(@params));

			registry.Clean(@params);
			Assert.IsTrue(registry.IsSessionAllowed(@params));
		}

		[Test]
		public void CleanTestAfterDenyWhenDefaultDeny()
		{
			var registry = new AllowedSessionRegistry(false);
			var @params = GetDefaultParams();

			Assert.IsFalse(registry.IsSessionAllowed(@params));

			registry.DenySessionForConnect(@params);
			Assert.IsFalse(registry.IsSessionAllowed(@params));

			registry.Clean(@params);
			Assert.IsFalse(registry.IsSessionAllowed(@params));
		}

		[Test]
		public void ChangeDefaultPolicy()
		{
			var registry = new AllowedSessionRegistry(true);
			var paramsDefault = GetParams("s", "t");
			var paramsAllow = GetParams("sa", "ta");
			var paramsDeny = GetParams("sd", "td");

			Assert.IsTrue(registry.IsSessionAllowed(paramsDefault));
			Assert.IsTrue(registry.IsSessionAllowed(paramsAllow));
			Assert.IsTrue(registry.IsSessionAllowed(paramsDeny));

			registry.AllowSessionForConnect(paramsAllow);
			registry.DenySessionForConnect(paramsDeny);

			Assert.IsTrue(registry.IsSessionAllowed(paramsDefault));
			Assert.IsTrue(registry.IsSessionAllowed(paramsAllow));
			Assert.IsFalse(registry.IsSessionAllowed(paramsDeny));

			registry.DenyByDefault();

			Assert.IsFalse(registry.IsSessionAllowed(paramsDefault));
			Assert.IsTrue(registry.IsSessionAllowed(paramsAllow));
			Assert.IsFalse(registry.IsSessionAllowed(paramsDeny));

			registry.AllowByDefault();

			Assert.IsTrue(registry.IsSessionAllowed(paramsDefault));
			Assert.IsTrue(registry.IsSessionAllowed(paramsAllow));
			Assert.IsFalse(registry.IsSessionAllowed(paramsDeny));

		}

		private SessionParameters GetDefaultParams()
		{
			return GetParams("s", "t");
		}

		private static SessionParameters GetParams(string sender, string target)
		{
			var sessionParameters = new SessionParameters
			{
				SenderCompId = sender,
				TargetCompId = target
			};
			return sessionParameters;
		}
	}
}