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

using Epam.FixAntenna.NetCore.Common;

using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Validation.Tests.Engine.Validation.Test
{
	internal class InvalidValidationTestStub : GenericValidationTestStub
	{
		[TearDown]
		public virtual void After()
		{
			ClassicAssert.IsTrue(!Errors.IsEmpty, Errors.Errors.ToReadableString());
		}

		private FixInfo _fixInfo;
		private FixVersion _currentVersion;

		public virtual void InvalidTest(FixVersion version, string pathToTestMessage)
		{
			_currentVersion = version;
			Validate(pathToTestMessage, false, true);
		}

		public virtual void InvalidMessageTest(FixVersion version, string message)
		{
			_currentVersion = version;
			Validate(message);
		}

		public override FixInfo GetFixInfo()
		{
			if (_currentVersion != null)
			{
				if (FixVersion.Fix50.CompareTo(_currentVersion) == 0)
				{
					_fixInfo = new FixInfo(FixVersion.Fixt11, _currentVersion);
				}
				else
				{
					_fixInfo = new FixInfo(_currentVersion);
				}
			}

			return _fixInfo;
		}
	}
}