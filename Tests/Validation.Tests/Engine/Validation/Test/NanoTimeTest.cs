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
using System.IO;
using System.Text;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Helpers;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Validation.Tests.Engine.Validation.Test
{
	[TestFixture]
	internal class NanoTimeTest : ValidValidationTestStub
	{
		public override FixInfo GetFixInfo()
		{
			return new FixInfo(FixVersion.Fix44);
		}

		[Test]
		public virtual void TestCustomMesssageOk()
		{
			var message = "8=FIX.4.4 | 9=113 | 35=B | 34=2 | 49=senderId | 56=targetId | 52=20170601-15:46:32.674123456"
						+ " | 148=Hello there | 33=3 | 58=line1 | 58=line2 | 58=line3 | 10=031 | ";
			message = message.Replace(" | ", "\u0001");
			Console.WriteLine(message);
			using (var stream = new MemoryStream(message.AsByteArray(Encoding.UTF8)))
			{
				ValidTest(FixVersion.Fix44, stream);
			}
		}
	}
}