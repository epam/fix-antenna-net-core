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
using System.Collections.Generic;
using System.IO;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;

namespace Epam.FixAntenna.Tester.Task.FixEngine
{
	public sealed class EngineUp : ITask
	{
		private static readonly ILog _log = LogFactory.GetLog(typeof(EngineUp));
		private const string PORT_PARAM = "port";
		private const string NAME_PARAM = "name";

		private string _name;
		private CustomConcurrentDictionary<string, object> _session;
		private string _port;

		public void Init(IDictionary<string, string> @params, CustomConcurrentDictionary<string, object> session)
		{
			this._session = session;
			_port = @params[PORT_PARAM];
			_name = @params[NAME_PARAM];

			IEnumerator<string> it = @params.Keys.GetEnumerator();
			while (it.MoveNext())
			{
				string keyName = it.Current;
				if (keyName.StartsWith("global_", StringComparison.Ordinal))
				{
					string paramName = keyName.Substring("global_".Length);
					Config.GlobalConfiguration.SetProperty(paramName, @params[keyName]);
				}
			}

		}

		public void DoTask()
		{
			_log.Info("Starting test server on port " + _port);

			var server = new FixServer();

			server.SetPort(int.Parse(_port));
			server.SetListener(new ConnectAllFIXServerListener());
			try
			{
				server.Start();
			}
			catch (IOException e)
			{
				throw new TaskException(e.Message, e);
			}
			_session[_name] = server;
		}

		public void Dispose()
		{
			try
			{
				using (var engineDown = new EngineDown())
				{
					engineDown.Init(new Dictionary<string, string>() { { NAME_PARAM, _name } }, _session );
					engineDown.DoTask();
					_session.TryRemove(_name, out var server);
				}
			}
			catch (Exception e)
			{
				_log.Error(e, e);
			}
		}

		internal class ConnectAllFIXServerListener : IFixServerListener
		{
			public void NewFixSession(IFixSession session)
			{
				try
				{
					session.Connect();
				}
				catch (IOException e)
				{
					_log.Warn(e, e);
				}
			}
		}
	}
}