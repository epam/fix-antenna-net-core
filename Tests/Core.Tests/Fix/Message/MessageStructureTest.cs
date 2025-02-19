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
using Epam.FixAntenna.NetCore.FixEngine;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Fix.Message
{
	internal class MessageStructureTest
	{
		internal MessageStructure Ms;

		[SetUp]
		public virtual void Initialize()
		{
			Ms = new MessageStructure();
			Ms.Reserve(75, 5);
			Ms.Reserve(80, 10);
			Ms.Reserve(3, 30);
		}

		[Test]
		public virtual void TestAdd()
		{
			ClassicAssert.AreEqual(75, Ms.GetTagId(0));
			ClassicAssert.AreEqual(3, Ms.GetTagId(2));
			ClassicAssert.AreEqual(10, Ms.GetLength(1));
			ClassicAssert.AreEqual(30, Ms.GetLength(2));
		}

		[Test]
		public virtual void TestAppend()
		{
			var ms2 = new MessageStructure();
			ms2.Reserve(35, 256);
			Ms.Append(ms2);
			ClassicAssert.AreEqual(35, Ms.GetTagId(3));
			ClassicAssert.AreEqual(256, Ms.GetLength(3));
		}

		[Test]
		public virtual void ReservWrongZeroLength()
		{
			ClassicAssert.Throws<ArgumentException>(() => { Ms.Reserve(75, 0); });
		}

		[Test]
		public virtual void ReservWrongNegativeLength()
		{
			ClassicAssert.Throws<ArgumentException>(() => { Ms.Reserve(75, -2); });
		}

		[Test]
		public virtual void TestSetInvalidNegativeLength()
		{
			ClassicAssert.Throws<ArgumentException>(() => { Ms.SetLength(3, -2); });
		}

		[Test]
		public virtual void TestSetInvalidZeroLength()
		{
			ClassicAssert.Throws<ArgumentException>(() => { Ms.SetLength(3, 0); });
		}

		[Test]
		public virtual void TestSetInvalidLength()
		{
			ClassicAssert.AreEqual(5, Ms.GetLength(0));
			Ms.SetLength(75, MessageStructure.VariableLength);
			ClassicAssert.AreEqual(MessageStructure.VariableLength, Ms.GetLength(0));
		}

		[Test]
		public virtual void TestMerge()
		{
			var m1 = new MessageStructure();
			m1.ReserveLong(1, 2);
			m1.ReserveLong(2, 3);

			var m2 = new MessageStructure();
			m2.ReserveString(3);
			m2.ReserveString(3);
			m2.ReserveString(4);
			m2.ReserveString(1);

			m1.Merge(m2);

			var res = new MessageStructure();
			res.ReserveString(1);
			res.ReserveLong(2, 3);
			res.ReserveString(3);
			res.ReserveString(3);
			res.ReserveString(4);

			ClassicAssert.AreEqual(res, m1);
		}
	}

}