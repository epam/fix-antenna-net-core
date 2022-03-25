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

using Epam.FixAntenna.TestUtils.Transport;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Session.Impl;

namespace Epam.FixAntenna.NetCore.FixEngine.ResetLogon.Util
{
	internal class AcceptorSessionEmulator : FixSessionEmulator
	{
		public AcceptorSessionEmulator(SessionParameters sessionParameters)
			: base(sessionParameters, new AcceptorSocketTransport(), CreateMessageFactory(sessionParameters))
		{
		}

		private static StandardMessageFactory CreateMessageFactory(SessionParameters sessionParameters)
		{
			var fixVersion = sessionParameters.FixVersion;
			if (fixVersion.IsFixt && fixVersion.FixtVersion == FixVersion.Fixt11.FixtVersion)
			{
				return new Fixt11MessageFactory();
			}
			else
			{
			  return new Fix44MessageFactory();
			}
		}
	}
}