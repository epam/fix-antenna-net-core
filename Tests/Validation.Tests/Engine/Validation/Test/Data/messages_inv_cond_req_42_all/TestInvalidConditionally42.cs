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

namespace Epam.FixAntenna.Validation.Tests.Engine.Validation.Test.Data.messages_inv_cond_req_42_all
{
	[TestFixture]
	internal class TestInvalidConditionally42 : InvalidValidationTestStub
	{
		private const string PackagePrefix = "resources/messages_inv_cond_req_42_all/";

		[Test]
		public virtual void InvalidConditionalTest1()
		{
			InvalidTest(FixVersion.Fix42, PackagePrefix + "inv_cond_req_42_01.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest2()
		{
			InvalidTest(FixVersion.Fix42, PackagePrefix + "inv_cond_req_42_02.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest3()
		{
			InvalidTest(FixVersion.Fix42, PackagePrefix + "inv_cond_req_42_03.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest4()
		{
			InvalidTest(FixVersion.Fix42, PackagePrefix + "inv_cond_req_42_04.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest5()
		{
			InvalidTest(FixVersion.Fix42, PackagePrefix + "inv_cond_req_42_05.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest6()
		{
			InvalidTest(FixVersion.Fix42, PackagePrefix + "inv_cond_req_42_06.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest7()
		{
			InvalidTest(FixVersion.Fix42, PackagePrefix + "inv_cond_req_42_07.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest8()
		{
			InvalidTest(FixVersion.Fix42, PackagePrefix + "inv_cond_req_42_08.fix");
		}

		[Test]
		public virtual void InvalidConditionalTest9()
		{
			InvalidTest(FixVersion.Fix42, PackagePrefix + "inv_cond_req_42_09.fix");
		}

		[Test]
		public virtual void InvalidConditionalTestAll()
		{
			InvalidTest(FixVersion.Fix42, PackagePrefix + "inv_cond_req_42_all.fix");
		}

		[Test]
		public virtual void InvalidConditionalTestQuick()
		{
			InvalidMessageTest(FixVersion.Fix42,
				"8=FIX.4.2\u00019=92\u000135=k\u000149=SNDR\u000156=TRGT\u000134=2\u000152=20030204-09:07:50\u0001391=20012411\u0001374=N\u0001393=2\u0001394=1\u0001398=1\u0001418=A\u0001419=4\u000110=045\u0001");
		}
	}
}