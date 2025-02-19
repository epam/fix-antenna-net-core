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

namespace Epam.FixAntenna.Validation.Tests.Engine.Validation.Test.Data.messages_repeat_group_44
{
	[TestFixture]
	internal class ValidGroupTest : GenericValidationTestStub
	{
		private const bool ErrorShouldOccur = false;
		private const string PackagePrefix = "resources/messages_repeat_group_44/";

		public override FixInfo GetFixInfo()
		{
			return new FixInfo(FixVersion.Fix44);
		}

		[Test]
		public virtual void ValidTest1()
		{
			Validate(PackagePrefix + "132-133_valid.fix", false, ErrorShouldOccur);
		}

		[Test]
		public virtual void ValidTest2()
		{
			Validate(PackagePrefix + "600_valid.fix", false, ErrorShouldOccur);
		}

		[Test]
		public virtual void ValidTest3()
		{
			Validate(PackagePrefix + "fix44_8_valid_01.fix", false, ErrorShouldOccur);
		}

		[Test]
		public virtual void ValidTest4()
		{
			Validate(PackagePrefix + "fix44_8_valid_02.fix", false, ErrorShouldOccur);
		}
	}
}