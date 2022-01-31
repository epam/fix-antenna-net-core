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

namespace Epam.FixAntenna.Validation.Tests.Engine.Validation.Test.Data.messages_inv_cond_req_43_all
{
	[TestFixture]
	internal class TestInvalidConditionally43 : InvalidValidationTestStub
	{
		private const string PackagePrefix = "resources/messages_inv_cond_req_43_all/";

		[Test]
		public virtual void InvalidConditionalTest1()
		{
			InvalidTest(FixVersion.Fix43, PackagePrefix + "inv_cond_req_43_01.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest10()
		{
			InvalidTest(FixVersion.Fix43, PackagePrefix + "inv_cond_req_43_10.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest2()
		{
			InvalidTest(FixVersion.Fix43, PackagePrefix + "inv_cond_req_43_02.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest3()
		{
			InvalidTest(FixVersion.Fix43, PackagePrefix + "inv_cond_req_43_03.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest4()
		{
			InvalidTest(FixVersion.Fix43, PackagePrefix + "inv_cond_req_43_04.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest5()
		{
			InvalidTest(FixVersion.Fix43, PackagePrefix + "inv_cond_req_43_05.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest6()
		{
			InvalidTest(FixVersion.Fix43, PackagePrefix + "inv_cond_req_43_06.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest7()
		{
			InvalidTest(FixVersion.Fix43, PackagePrefix + "inv_cond_req_43_07.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest8()
		{
			InvalidTest(FixVersion.Fix43, PackagePrefix + "inv_cond_req_43_08.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest9()
		{
			InvalidTest(FixVersion.Fix43, PackagePrefix + "inv_cond_req_43_09.fix");
		}

		[Test]
		public virtual void InvalidConditionalTestAll()
		{
			InvalidTest(FixVersion.Fix43, PackagePrefix + "inv_cond_req_43_all.fix");
		}

		[Test]
		public virtual void TestQuick()
		{
			InvalidMessageTest(FixVersion.Fix43,
				"8=FIX.4.3\u00019=147\u000135=s\u000149=SNDR\u000156=TRGT\u000134=18\u000152=20090401-13:22:26.824\u0001548=90001008\u0001549=1\u0001550=0\u0001552=1\u000154=1\u000111=B101010\u000178=1\u000179=00079\u0001539=1\u0001524=000524\u000138=4000\u000155=TESTA\u000121=1\u000160=20060217-10:00:00\u000140=1\u000110=177\u0001");
		}
	}
}