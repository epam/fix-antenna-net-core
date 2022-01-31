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
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.FixEngine.Session;

namespace Epam.FixAntenna.Tester.Task.FixEngine
{
	public sealed class EngineDown : ITask
	{
		private static readonly ILog _log = LogFactory.GetLog(typeof(EngineDown));
		private const string NAME_PARAM = "name";

		private FixServer _server;

		public void Init(IDictionary<string, string> @params, CustomConcurrentDictionary<string, object> session)
		{
			_server = (FixServer) session[@params[NAME_PARAM]];
		}

		public void DoTask()
		{
			_log.Debug("Attempt to shutdown engine");
			
			var manager = FixSessionManager.Instance;
			IList<IExtendedFixSession> list = manager.SessionListCopy;
			foreach (var session in list)
			{
				_log.Debug("shutdown - " + session);
				session.MarkShutdownAsGraceful();
				session.Shutdown(DisconnectReason.UserRequest, true);
				session.Dispose();
			}
			_log.Info("all sessions terminated");
			if (_server != null)
			{
				try
				{
					_server.Stop();
					_log.Info("Stopped test server");
				}
				catch (IOException e)
				{
					throw new TaskException(e.Message, e);
				}
			}
			_log.Info("Success");
		}

		public void Dispose()
		{
		}
	}
}