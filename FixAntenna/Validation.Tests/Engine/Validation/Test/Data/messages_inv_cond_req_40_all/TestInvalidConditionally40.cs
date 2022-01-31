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

namespace Epam.FixAntenna.Validation.Tests.Engine.Validation.Test.Data.messages_inv_cond_req_40_all
{
	[TestFixture]
	internal class TestInvalidConditionally40 : InvalidValidationTestStub
	{
		private const string PackagePrefix = "resources/messages_inv_cond_req_40_all/";

		[Test]
		public virtual void InvalidConditionalTest1()
		{
			InvalidTest(FixVersion.Fix40, PackagePrefix + "inv_cond_req_40_01.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest2()
		{
			InvalidTest(FixVersion.Fix40, PackagePrefix + "inv_cond_req_40_02.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest3()
		{
			InvalidTest(FixVersion.Fix40, PackagePrefix + "inv_cond_req_40_03.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest4()
		{
			InvalidTest(FixVersion.Fix40, PackagePrefix + "inv_cond_req_40_04.fix");
		}

		[Test]
		public virtual void InvalidConditionalTestAll()
		{
			InvalidTest(FixVersion.Fix40, PackagePrefix + "inv_cond_req_40_all.fix");
		}
	}
}