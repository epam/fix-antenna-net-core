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
using Epam.FixAntenna.NetCore.Common.Pool;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Helpers;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.Util
{
	[TestFixture]
	internal class ByteBufferTest
	{
		private ByteBuffer _buffer;
		private const int Length = 32;
		private const string TestString = "0123456789012345678901234567890";

		[SetUp]
		public void Before()
		{
			_buffer = new ByteBuffer(Length);
		}

		[Test]
		public virtual void CheckAvailableBytes()
		{
			Assert.IsTrue(_buffer.IsAvailable(32));
			Assert.IsFalse(_buffer.IsAvailable(33));
		}

		[Test]
		public virtual void CopyBytesFromOtherBuffer()
		{
			_buffer.Add("1".AsByteArray());
			_buffer.Add(TestString.AsByteArray());
			Assert.That(_buffer.GetByteArray(), Is.EqualTo(("1" + TestString).AsByteArray()));
			Assert.IsTrue(_buffer.Offset == ("1" + TestString).Length);
		}

		[Test]
		public virtual void CheckBigBufferWithAddOneByte()
		{
			_buffer = new ByteBuffer(3);
			for (var i = 0; i < 3; i++)
			{
				_buffer.Add(Convert.ToString(i).AsByteArray());
			}
			_buffer.Add("0".AsByteArray()[0]);
			Assert.AreEqual("0120", StringHelper.NewString(_buffer.GetBulk()));
		}

		[Test]
		public virtual void CheckBigBufferWithAddOneChar()
		{
			_buffer = new ByteBuffer(3);
			for (var i = 0; i < 3; i++)
			{
				_buffer.Add(Convert.ToString(i).AsByteArray());
			}
			_buffer.Add('0');
			Assert.AreEqual("0120", StringHelper.NewString(_buffer.GetBulk()));
		}

		[Test]
		public virtual void CheckByteBufferWithAddOneCharThatWritesOneByte()
		{
			const char NonASCIIGlyphs = '€';
			const int BytebufferLength = 1;
			_buffer = new ByteBuffer(BytebufferLength);

			_buffer.Add(NonASCIIGlyphs);

			Assert.AreEqual(BytebufferLength, _buffer.Length);
		}

		[Test]
		public virtual void CheckBigBufferWithAddTheSameArray()
		{
			_buffer = new ByteBuffer(3);
			for (var i = 0; i < 3; i++)
			{
				_buffer.Add(Convert.ToString(i).AsByteArray());
			}
			_buffer.Add(new byte [0]);
			Assert.AreEqual("012", StringHelper.NewString(_buffer.GetBulk()));
		}

		[Test]
		public virtual void CheckAddedWithOffset()
		{
			var world = "Hello World!!!".AsByteArray();
			_buffer = new ByteBuffer(3);
			_buffer.Add(world, 6, world.Length - 6);
			Assert.AreEqual("World!!!", StringHelper.NewString(_buffer.GetBulk()));
		}

		[Test]
		public virtual void CheckTryAllocateMoreThenDefaultSize()
		{
			ByteBuffer.Demand(ByteBufferPool.DefaultLength + 1);
		}
	}
}