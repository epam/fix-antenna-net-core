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

using System;
using Epam.FixAntenna.NetCore.Common.Utils;
using NUnit.Framework;

namespace Epam.FixAntenna.Common.Utils
{
	[TestFixture]
	public class ReflectUtilTest
	{
		[SetUp]
		public virtual void Before()
		{
			_reflectUtil = new ReflectUtil<string>();
			_reflectUtilObj = new ReflectUtil<object>();
		}

		private ReflectUtil<string> _reflectUtil;
		private ReflectUtil<object> _reflectUtilObj;

		[Test]
		public virtual void TestCreateWithDefaultConstructor()
		{
			var emptyObj = _reflectUtilObj.GetInstance(new Type[] { }, new object[] { });
			Assert.IsNotNull(emptyObj);
		}

		[Test]
		public virtual void TestCreateWithExtendedConstructor()
		{
			var emptyString = _reflectUtil.GetInstance(new[] { typeof(char[]) }, new object[] { "1".ToCharArray() });
			Assert.IsNotNull(emptyString);
			Assert.AreEqual("1", emptyString);
		}
	}
}