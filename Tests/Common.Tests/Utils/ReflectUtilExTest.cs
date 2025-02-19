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

using Epam.FixAntenna.NetCore.Common.Utils;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Common.Utils
{
	[TestFixture]
	public class ReflectUtilExTest
	{
		[Test]
		public void TestCreateWithDefaultConstructor()
		{
			var objUtil = new ReflectUtilEx(typeof(object));
			var objObj = objUtil.GetInstance(new object[] {});
			ClassicAssert.IsNotNull(objObj);
		}

		[Test]
		public void TestCreateWithExtendedConstructor()
		{
			var strUtil = new ReflectUtilEx(typeof(string));
			var strObj = strUtil.GetInstance(new object[]{"1".ToCharArray()});
			ClassicAssert.IsNotNull(strObj);
			ClassicAssert.AreEqual("1", strObj);
		}
	}
}
