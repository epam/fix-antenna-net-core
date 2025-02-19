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

namespace Epam.FixAntenna.Validation.Tests.Engine.Validation.Test.Data.messages_inv_cond_req
{
	[TestFixture]
	internal class TestInvalidConditionalyRequired : InvalidValidationTestStub
	{
		private readonly string _packagePrefix = "resources/messages_inv_cond_req/";

		[Test]
		public virtual void InvalidConditionalTest1()
		{
			InvalidTest(FixVersion.Fix40, _packagePrefix + "40_E_63.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest10()
		{
			InvalidTest(FixVersion.Fix44, _packagePrefix + "44_T_160.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest11()
		{
			InvalidTest(FixVersion.Fix50, _packagePrefix + "50_G_40.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest12()
		{
			InvalidTest(FixVersion.Fix50, _packagePrefix + "50_T_160.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest2()
		{
			InvalidTest(FixVersion.Fix40, _packagePrefix + "40_G_40.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest3()
		{
			InvalidTest(FixVersion.Fix41, _packagePrefix + "41_E_63.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest4()
		{
			InvalidTest(FixVersion.Fix41, _packagePrefix + "41_G_40.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest5()
		{
			InvalidTest(FixVersion.Fix42, _packagePrefix + "42_G_40.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest6()
		{
			InvalidTest(FixVersion.Fix42, _packagePrefix + "42_T_160.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest7()
		{
			InvalidTest(FixVersion.Fix43, _packagePrefix + "43_G_40.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest8()
		{
			InvalidTest(FixVersion.Fix43, _packagePrefix + "43_T_160.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest9()
		{
			InvalidTest(FixVersion.Fix44, _packagePrefix + "44_G_40.fix");
		}
	}
}