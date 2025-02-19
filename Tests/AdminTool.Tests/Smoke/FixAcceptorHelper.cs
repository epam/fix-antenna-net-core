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
using System.Threading;
using Epam.FixAntenna.AdminTool.Tests.Smoke.Util;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.AdminTool.Tests.Smoke
{
	internal sealed class FixAcceptorHelper : IFixServerListener, IDisposable
	{
		private FixServer _server;
		private IFixSession _session;
		private CountdownEvent _condition;
		private readonly string _sender;
		private readonly string _target;
		private readonly int _port;

		public FixAcceptorHelper(string target, string sender, int port)
		{
			_sender = target;
			_target = sender;
			_port = port;
		}

		public IFixSession Session => _session;

		public void Start()
		{
			_condition = new CountdownEvent(2);
			SmokeUtil.CreateTask(() =>
			{
				_server = new FixServer();
				_server.SetListener(this);
				_server.SetPort(_port);
				_server.Start();
				_condition.Signal();
			});
		}

		public void Stop()
		{
			_server.Stop();
		}

		public void NewFixSession(IFixSession session)
		{
			try
			{
				_session = session;
				_session.Connect();
				_condition.Signal();
			}
			catch (IOException e)
			{
				ClassicAssert.Fail("error on start server:" + e.Message);
			}
		}

		public void WaitForStartup()
		{
			SmokeUtil.CreateTask(() =>
			{
				var i = 0;
				while (i++ < 5 && _session == null)
				{
					_session = FixSessionManager.Instance.LocateFirst(_sender, _target);
					Thread.Sleep(500);
				}
				_condition.Signal();
			});

			try
			{
				_condition.Wait(3000);
			}
			catch (Exception e)
			{
				ClassicAssert.Fail(e.Message);
			}
		}

		private bool _disposed = false;

		private void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					_condition.Dispose();
				}
				_disposed = true;
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}