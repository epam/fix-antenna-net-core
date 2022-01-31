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

using Epam.FixAntenna.Constants.Fixt11;
using System;
using System.Collections.Generic;
using System.IO;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.Tester.Task.FixEngine
{
	public sealed class EngineUpWithEchoApp : ITask
	{
		private bool InstanceFieldsInitialized = false;

		public EngineUpWithEchoApp()
		{
			if (!InstanceFieldsInitialized)
			{
				InitializeInstanceFields();
				InstanceFieldsInitialized = true;
			}
		}

		private void InitializeInstanceFields()
		{
			_log = LogFactory.GetLog(this.GetType());
		}

		private ILog _log;
		private string _port;
		private string _name;
		private bool _reset = false;
		private CustomConcurrentDictionary<string, object> _sessions;


		public void Init(IDictionary<string, string> @params, CustomConcurrentDictionary<string, object> sessions)
		{
			_port = @params["port"];
			_name = @params["name"];
			_reset = @params.ContainsKey("reset") ? bool.Parse(@params["reset"]) : false;
			this._sessions = sessions;

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
			_log.Debug("Starting test server on port " + _port);

			var server = new FixServer();

			server.SetPort(int.Parse(_port));
			server.SetListener(new EchoFIXServerListener(this));
			try
			{
				server.Start();
			}
			catch (IOException e)
			{
				throw new TaskException(e.Message, e);
			}
			_sessions[_name] = server;
		}

		public void Dispose()
		{
			try
			{
				using (var engineDown = new EngineDown())
				{
					engineDown.Init(new Dictionary<string, string>() { { "name", _name } }, _sessions);
					engineDown.DoTask();
					_sessions.TryRemove(_name, out var server);
				}
			}
			catch (Exception e)
			{
				_log.Error(e, e);
			}
		}

		internal class EchoFIXServerListener : IFixServerListener
		{
			private readonly EngineUpWithEchoApp _outerInstance;

			public EchoFIXServerListener(EngineUpWithEchoApp outerInstance)
			{
				this._outerInstance = outerInstance;
			}

			public void NewFixSession(IFixSession session)
			{
				try
				{
					session.SetFixSessionListener(new EchoSessionListener(this, session));
					if (_outerInstance._reset)
					{
						session.ResetSequenceNumbers();
					}
					session.Connect();
					_outerInstance._sessions[session.Parameters.SessionId.ToString()] = session;
				}
				catch (IOException e)
				{
					_outerInstance._log.Warn(e, e);
				}
			}

			public class EchoSessionListener : IFixSessionListener
			{
				private readonly EchoFIXServerListener _outerInstance;

				private IFixSession _session;

				public EchoSessionListener(EchoFIXServerListener outerInstance, IFixSession session)
				{
					this._outerInstance = outerInstance;
					this._session = session;
				}

				public void OnSessionStateChange(SessionState sessionState)
				{
				}

				public void OnNewMessage(FixMessage origMessage)
				{
					FixMessage message = origMessage.DeepClone(true, true);
					TagValue targetTv = message.GetTag(Tags.TargetCompID);
					TagValue senderTv = message.GetTag(Tags.SenderCompID);

					targetTv.TagId = Tags.SenderCompID;
					senderTv.TagId = Tags.TargetCompID;
					message.Set(targetTv);
					message.Set(senderTv);

					message.Set(Tags.MsgSeqNum, ((IExtendedFixSession)_session).RuntimeState.OutSeqNum);
					message.Set(Tags.BodyLength, message.CalculateBodyLength());
					_session.SendMessage("", message);
					message.ReleaseInstance();
				}
			}
		}

	}
}