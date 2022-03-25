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

namespace Epam.FixAntenna.Validation.Tests.Engine.Validation.Test.Data.messages_repeat_group_44
{
	[TestFixture]
	internal class InvalidGroupTest : InvalidValidationTestStub
	{
		private const string PackagePrefix = "resources/messages_repeat_group_44/";

		[Test]
		public virtual void InvalidGroupTest1()
		{
			InvalidTest(FixVersion.Fix44, PackagePrefix + "132-133_invalid.fix");
		}

		[Test]
		public virtual void InvalidGroupTest2()
		{
			InvalidTest(FixVersion.Fix44, PackagePrefix + "453-802_invalid.fix");
		}

		[Test]
		public virtual void InvalidGroupTest3()
		{
			InvalidTest(FixVersion.Fix44, PackagePrefix + "fix44_8_invalid_01.fix");
		}

		[Test]
		public virtual void InvalidGroupTest4()
		{
			InvalidTest(FixVersion.Fix44, PackagePrefix + "fix44_8_invalid_02.fix");
		}

		[Test]
		public virtual void InvalidGroupTest5()
		{
			InvalidTest(FixVersion.Fix44, PackagePrefix + "fix44_8_invalid_03.fix");
		}

		[Test]
		public virtual void InvalidGroupTest6()
		{
			InvalidMessageTest(FixVersion.Fix44,
				"8=FIX.4.4\u00019=180\u000135=J\u000149=SNDR\u000156=TRGT\u000134=9\u000152=20090331-09:17:04.228\u000170=1116226\u000171=0\u0001626=1\u0001857=1\u000154=1\u000155=TESTB\u000153=1004000\u00016=30\u000175=20030821\u000178=3\u000179=1000\u000180=500000\u000179=2000\u000180=400000\u000179=3000\u000180=104000\u000110=061\u0001");
		}
	}
}