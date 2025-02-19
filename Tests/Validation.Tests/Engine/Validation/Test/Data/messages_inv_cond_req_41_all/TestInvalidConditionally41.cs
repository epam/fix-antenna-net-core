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

namespace Epam.FixAntenna.Validation.Tests.Engine.Validation.Test.Data.messages_inv_cond_req_41_all
{
	[TestFixture]
	internal class TestInvalidConditionally41 : InvalidValidationTestStub
	{
		private const string PackagePrefix = "resources/messages_inv_cond_req_41_all/";

		[Test]
		public virtual void InvalidConditionalTest1()
		{
			InvalidTest(FixVersion.Fix41, PackagePrefix + "inv_cond_req_41_01.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest2()
		{
			InvalidTest(FixVersion.Fix41, PackagePrefix + "inv_cond_req_41_02.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest3()
		{
			InvalidTest(FixVersion.Fix41, PackagePrefix + "inv_cond_req_41_03.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest4()
		{
			InvalidTest(FixVersion.Fix41, PackagePrefix + "inv_cond_req_41_04.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest5()
		{
			InvalidTest(FixVersion.Fix41, PackagePrefix + "inv_cond_req_41_05.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest6()
		{
			InvalidTest(FixVersion.Fix41, PackagePrefix + "inv_cond_req_41_06.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest7()
		{
			InvalidTest(FixVersion.Fix41, PackagePrefix + "inv_cond_req_41_07.fix");
		}

		[Test]
		public virtual void InvalidConditionalTestAll()
		{
			InvalidTest(FixVersion.Fix41, PackagePrefix + "inv_cond_req_41_all.fix");
		}
	}
}