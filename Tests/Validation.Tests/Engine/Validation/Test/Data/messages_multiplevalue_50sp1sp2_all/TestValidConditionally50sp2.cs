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

namespace Epam.FixAntenna.Validation.Tests.Engine.Validation.Test.Data.messages_multiplevalue_50sp1sp2_all
{
	[TestFixture]
	internal class TestValidConditionally50Sp2 : ValidValidationTestStub
	{
		private const string PackagePrefix = "resources/messages_multiplevalue_50sp1sp2_all/";

		public override FixInfo GetFixInfo()
		{
			return new FixInfo(FixVersion.Fixt11, FixVersion.Fix50Sp2);
		}

		[Test]
		public virtual void InvalidConditionalTest1()
		{
			ValidTest(FixVersion.Fix50Sp2, PackagePrefix + "valid_multipleCHARvalue_50sp1sp2.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest2()
		{
			ValidTest(FixVersion.Fix50Sp2, PackagePrefix + "valid_multipleSTRINGvalue_50sp1sp2.fix");
		}
	}
}