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

using Epam.FixAntenna.NetCore.Validation.Entities;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Validation.Tests.Engine
{
	[TestFixture]
	public class FiXdicTest
	{
		[SetUp]
		public void SetUp()
		{
			_fixdic = new Fixdic();
		}

		private Fixdic _fixdic;

		[Test]
		public void TestClone()
		{
			_fixdic.Date = "test string";
			_fixdic.Msgdic = new Msgdic();
			_fixdic.Msgdic.Msgdef.Add(new Msgdef());

			var clonedFixDic = (Fixdic)_fixdic.Clone();

			// change msgdic element
			_fixdic.Msgdic.Msgdef[0].Msgtype = "hi";

			ClassicAssert.AreEqual(clonedFixDic.Date, _fixdic.Date);
			ClassicAssert.AreEqual(clonedFixDic.Msgdic, _fixdic.Msgdic);
		}
	}
}