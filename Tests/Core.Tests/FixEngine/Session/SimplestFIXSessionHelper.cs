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

using System.Collections.Generic;
using System.IO;
using System.Threading;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.FixEngine.Transport;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	internal class SimplestFixSessionHelper
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(SimplestFixSessionHelper));
		protected internal readonly IList<FixMessage> Messages = new List<FixMessage>();

		internal InitiatorFixTransport Transport;
		internal MsgReader Reader;

		internal CountdownEvent TransportDisconnectWaiter;
		internal CountdownEvent MessagesWaiter = new CountdownEvent(0);
		internal int MsgCount = 0;

		public SimplestFixSessionHelper(string localhost, int port)
		{
			Transport = new InitiatorFixTransport(localhost, port);
			Reader = new MsgReader(this, Transport);
		}

		public virtual void Connect()
		{
			TransportDisconnectWaiter = new CountdownEvent(1);
			Transport.Open();
			var thread = new Thread(Reader.Run);
			thread.Start();
		}

		public virtual void Disconnect()
		{
			Reader.Stop = true;
			Transport.Close();
		}

		public virtual void SendMessage(byte[] msg)
		{
			var message = new ByteBuffer(msg.Length);
			message.Add(msg);
			Transport.Write(msg, 0, msg.Length);
		}

		public virtual IList<FixMessage> GetMessages()
		{
			lock (Messages)
			{
				return Messages;
			}
		}

		public virtual void WaitTransportDown()
		{
			TransportDisconnectWaiter.Wait();
		}

		public virtual void PrepareToReceiveMessages(int msgCount)
		{
			lock (Messages)
			{
				Messages.Clear();
				MsgCount = msgCount;
				MessagesWaiter = new CountdownEvent(msgCount);
			}
		}

		public virtual void WaitForMessages(int timeout)
		{
			MessagesWaiter.Wait(timeout);
			Assert.IsTrue(MsgCount <= Messages.Count, "Wrong amount of messages received during last " + timeout + " msec. Expected " + MsgCount + " but received " + Messages.Count);
		}

		internal sealed class MsgReader
		{
			private readonly SimplestFixSessionHelper _sessionHelper;

			internal bool Stop = false;
			internal MsgBuf Buf = new MsgBuf();
			internal IFixTransport Transport;
			internal Thread Thread;

			public MsgReader(SimplestFixSessionHelper sessionHelper, IFixTransport transport)
			{
				_sessionHelper = sessionHelper;
				Transport = transport;
			}

			public void Run()
			{
				try
				{
					while (!Stop)
					{
						Transport.ReadMessage(Buf);
						var fieldList = RawFixUtil.GetFixMessage(Buf);
						var msgStr = fieldList.ToPrintableString();
						Log.Debug($"Message '{msgStr}' was received by test.");

						lock (_sessionHelper.Messages)
						{
							if (Stop)
							{
								continue;
							}

							_sessionHelper.Messages.Add((FixMessage)fieldList.Clone());
							Log.Debug($"Message '{msgStr}' was added into the list.");
							if (!_sessionHelper.MessagesWaiter.IsSet)
							{
								_sessionHelper.MessagesWaiter.Signal();
							}
						}
					}
				}
				catch (IOException e)
				{
					Log.Warn(e);
				}
				finally
				{
					_sessionHelper.TransportDisconnectWaiter.Signal();
				}
			}
		}
	}
}