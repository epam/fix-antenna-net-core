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

using Epam.FixAntenna.TestUtils;
using NUnit.Framework; 
using NUnit.Framework.Legacy;
using System;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.FixEngine.Manager;

namespace Epam.FixAntenna.Tester.Tests
{
	public class AbstractTesterCase
	{
		private static ILog Log = LogFactory.GetLog(typeof(AbstractTesterCase));

		[SetUp]
		public void SetUp()
		{
			Log.Info("Save global config before test");
			ConfigurationHelper.StoreGlobalConfig();
		}

		[TearDown]
		public void TearDown()
		{
			var sessionList = FixSessionManager.Instance.SessionListCopy;

			foreach (var session in sessionList) {
				try {
					session.Dispose();
				} catch (Exception e) {
					var sessionParameters = session.Parameters;
					Log.Warn("Session can't be disposed after test: " + sessionParameters.SessionId, e);
				}
			}

			Log.Info("Restore global config after test");
			ConfigurationHelper.RestoreGlobalConfig();
		}
	}
}