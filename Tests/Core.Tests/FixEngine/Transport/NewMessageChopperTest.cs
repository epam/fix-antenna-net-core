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
using System.Reflection;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Transport
{
    [TestFixture]
	internal class NewMessageChopperTest : AbstractMessageChopperTest
	{

		public override IMessageChopper GetInstanceChopper(byte[] messages, int[] parts, int maxMessageSize, int optimalBufferLength, bool validateCheckSum)
		{
			return new NewMessageChopper(new ReadStreamTransport(new MemoryStream(messages), parts), maxMessageSize, optimalBufferLength, false);
		}

		public override IMessageChopper GetInstanceChopper(byte[] messages, int maxMessageSize, int optimalBufferLength, bool validateCheckSum, bool markInMessageTime, int milliseconds)
		{
			var inputStreamMock = GetDelayedInputStreamMock(milliseconds, messages);
			return new NewMessageChopper(new ReadStreamTransport(inputStreamMock, new int[]{ messages.Length }), maxMessageSize, optimalBufferLength, true, null);

		}

		[Test]
		[Ignore("Fixed max message size in new chopper")]
		public override void CheckMaxMessageSize()
		{
			Assert.Throws<IOException>(() => { });
		}

		[Test]
		[Ignore("Fixed max message size in new chopper")]
		public override void TooLongMessage()
		{

		}

		[Test]
		public virtual void TestRemapInternalBuffer()
		{
			byte[] data;
			using (var ms = new MemoryStream())
			{
				Assembly.GetExecutingAssembly().GetManifestResourceStream("Epam.FixAntenna.Core.Tests.FixEngine.Transport.remap_buffer_data.fix").CopyTo(ms);
				data = ms.ToArray();
			}

			var messageChopper = GetInstanceChopper(data, new int[]{ 31000, SocketTransport.SocketReadSize });

			var buf = new MsgBuf();
			var countOfMessagesInFile = 20;
			for (var i = 0; i < countOfMessagesInFile; i++)
			{
				messageChopper.ReadMessage(buf);
				Console.WriteLine(buf.FixMessage);
				buf.FixMessage.Clear();
			}

			AssertEndOfFile(messageChopper);
		}
	}
}