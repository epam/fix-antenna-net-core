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

namespace Epam.FixAntenna.Validation.Tests.Engine.Validation.Test.Data.messages_inv_cond_req_44_all
{
	[TestFixture]
	internal class TestInvalidConditionally44 : InvalidValidationTestStub
	{
		private const string PackagePrefix = "resources/messages_inv_cond_req_44_all/";

		[Test]
		public virtual void InvalidConditionalTest1()
		{
			InvalidTest(FixVersion.Fix44, PackagePrefix + "inv_cond_req_44_01.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest10()
		{
			InvalidTest(FixVersion.Fix44, PackagePrefix + "inv_cond_req_44_10.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest11()
		{
			InvalidTest(FixVersion.Fix44, PackagePrefix + "inv_cond_req_44_11.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest12()
		{
			InvalidTest(FixVersion.Fix44, PackagePrefix + "inv_cond_req_44_12.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest13()
		{
			InvalidTest(FixVersion.Fix44, PackagePrefix + "inv_cond_req_44_13.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest14()
		{
			InvalidTest(FixVersion.Fix44, PackagePrefix + "inv_cond_req_44_14.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest15()
		{
			InvalidTest(FixVersion.Fix44, PackagePrefix + "inv_cond_req_44_15.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest2()
		{
			InvalidTest(FixVersion.Fix44, PackagePrefix + "inv_cond_req_44_02.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest3()
		{
			InvalidTest(FixVersion.Fix44, PackagePrefix + "inv_cond_req_44_03.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest4()
		{
			InvalidTest(FixVersion.Fix44, PackagePrefix + "inv_cond_req_44_04.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest5()
		{
			InvalidTest(FixVersion.Fix44, PackagePrefix + "inv_cond_req_44_05.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest6()
		{
			InvalidTest(FixVersion.Fix44, PackagePrefix + "inv_cond_req_44_06.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest7()
		{
			InvalidTest(FixVersion.Fix44, PackagePrefix + "inv_cond_req_44_07.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest8()
		{
			InvalidTest(FixVersion.Fix44, PackagePrefix + "inv_cond_req_44_08.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest9()
		{
			InvalidTest(FixVersion.Fix44, PackagePrefix + "inv_cond_req_44_09.fix");
		}

		[Test]
		public virtual void InvalidConditionalTestAll()
		{
			InvalidTest(FixVersion.Fix44, PackagePrefix + "inv_cond_req_44_all.fix");
		}

		[Test]
		public virtual void TestCustomMesssage()
		{
			InvalidMessageTest(FixVersion.Fix43,
				"8=FIX.4.3\u00019=147\u000135=s\u000149=SNDR\u000156=TRGT\u000134=18\u000152=20090401-13:22:26.824\u0001548=90001008\u0001549=1\u0001550=0\u0001552=1\u000154=1\u000111=B101010\u000178=1\u000179=00079\u0001539=1\u0001524=000524\u000138=4000\u000155=TESTA\u000121=1\u000160=20060217-10:00:00\u000140=1\u000110=177\u0001");
		}
	}
}