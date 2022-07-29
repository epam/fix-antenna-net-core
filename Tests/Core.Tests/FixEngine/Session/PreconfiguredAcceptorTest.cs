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

using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.TestUtils;

using NUnit.Framework;

using System;
using System.IO;
using System.Threading;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	internal class PreconfiguredAcceptorTest
	{
		public const int Port = 12345;
		internal FixServer Server;
		internal IFixSession Acceptor;


		[SetUp]
		public void SetUp()
		{
			ClearLogs();
		}

		[TearDown]
		public virtual void TearDown()
		{
			FixSessionManager.DisposeAllSession();
			Server?.Stop();
		}

		public virtual bool ClearLogs()
		{
			return (new LogsCleaner()).Clean("./logs") && (new LogsCleaner()).Clean("./logs/backup");
		}

		internal class FixServerListenerAnonymousInnerClass : IFixServerListener
		{
			private readonly PreconfiguredAcceptorTest _outerInstance;

			private CountdownEvent _waitConnect;
			private CountdownEvent _waitDisconnect;

			public FixServerListenerAnonymousInnerClass(PreconfiguredAcceptorTest outerInstance, CountdownEvent waitConnect, CountdownEvent waitDisconnect)
			{
				_outerInstance = outerInstance;
				_waitConnect = waitConnect;
				_waitDisconnect = waitDisconnect;
			}

			public void NewFixSession(IFixSession session)
			{
				try
				{
					_outerInstance.Acceptor = session;
					_waitConnect.Signal();
					_waitDisconnect.Wait();
					session.Reject("TEST");
				}
				catch (ThreadInterruptedException e)
				{
					Console.WriteLine(e.ToString());
					Console.Write(e.StackTrace);
				}
				catch (IOException e)
				{
					Console.WriteLine(e.ToString());
					Console.Write(e.StackTrace);  
				}
			}
		}
	}
}