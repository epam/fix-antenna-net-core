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

using System.Text;
using Epam.FixAntenna.NetCore.Common.Utils;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Common.Utils
{
	[TestFixture]
	public class ByteBufferTest
	{
		[Test]
		public virtual void AddLikeStringTest()
		{
			var buffer = ByteBuffer.Demand(1);
			buffer.AddLikeString(-100L);
			buffer.Flip();
			ClassicAssert.AreEqual("-100", Encoding.UTF8.GetString(buffer.GetByteArray(0, buffer.Limit())));
		}

		[Test]
		public virtual void AddLikeStringMinLenTest6()
		{
			var buffer = ByteBuffer.Demand(1);
			buffer.AddLikeString(-100L, 6);
			buffer.Flip();
			ClassicAssert.AreEqual("-000100", Encoding.UTF8.GetString(buffer.GetByteArray(0, buffer.Limit())));
		}

		[Test]
		public virtual void AddLikeStringMinLenTest2()
		{
			var buffer = ByteBuffer.Demand(1);
			buffer.AddLikeString(-100L, 2);
			buffer.Flip();
			ClassicAssert.AreEqual("-100", Encoding.UTF8.GetString(buffer.GetByteArray(0, buffer.Limit())));
		}
	}
}