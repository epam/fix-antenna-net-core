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
using System.Threading.Tasks;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.Tester.Task.FixEngine
{
	internal sealed class SessionUpTask : ITask, IFixSessionListener
	{

		private const string HOST_PARAM = "host";
		private const string PORT_PARAM = "port";
		private const string SENDER_COMP_ID = "SenderCompID";
		private const string TARGET_COMP_ID = "TargetCompID";
		private const string SESSION_ID_NAME_PARAM = "sessionIdName";
		private const string FIX_VERSION = "FixVersion";
		private const string FIX_HEARTBEAT = "hbtInterval";
		private const string IN_SEQ = "inSequence";
		private const string OUT_SEQ = "outSequence";

		private FixVersion _fixString;
		private string _senderCompID;
		private string _targetCompID;
		private string _sessionIdAttributeName;
		private int _sessionHbt;
		private CustomConcurrentDictionary<string, object> _session;
		private string _host;
		private int _port;
		private int _inSeq = -1;
		private int _outSeq = -1;

		private System.Threading.Tasks.Task _connector = null;

		public void OnSessionStateChange(SessionState sessionState)
		{
		}

		public void OnNewMessage(FixMessage message)
		{
		}

		public void Init(IDictionary<string, string> @params, CustomConcurrentDictionary<string, object> session)
		{
			_fixString = FixVersion.GetInstanceByMessageVersion(@params[FIX_VERSION]);
			_host = @params[HOST_PARAM];
			_port = int.Parse(@params[PORT_PARAM]);
			_senderCompID = @params[SENDER_COMP_ID];
			_targetCompID = @params[TARGET_COMP_ID];
			_sessionIdAttributeName = @params[SESSION_ID_NAME_PARAM];
			_sessionHbt = int.Parse(@params[FIX_HEARTBEAT]);
			if (@params.ContainsKey(IN_SEQ))
			{
				_inSeq = int.Parse(@params[IN_SEQ]);
			}
			if (@params.ContainsKey(OUT_SEQ))
			{
				_outSeq = int.Parse(@params[OUT_SEQ]);
			}
			this._session = session;
		}

		public void DoTask()
		{
			_connector = System.Threading.Tasks.Task.Factory.StartNew(TaskRun, TaskCreationOptions.LongRunning);
		}

		private void TaskRun()
		{
			try
			{
				var activeSession = (IFixSession)_session[_sessionIdAttributeName];
				if (activeSession != null)
				{
					activeSession.Connect();
				}
				else
				{
					SessionParameters details = new SessionParameters();
					details.FixVersion = _fixString;
					details.Host = _host;
					details.Port = _port;
					details.HeartbeatInterval = _sessionHbt;
					details.SenderCompId = _senderCompID;
					details.TargetCompId = _targetCompID;

					if (_inSeq >= 0)
					{
						details.IncomingSequenceNumber = _inSeq;
					}

					if (_outSeq >= 0)
					{
						details.OutgoingSequenceNumber = _outSeq;
					}

					// create session we intend to work with
					IFixSession fixSession = details.CreateNewFixSession();
					fixSession.SetFixSessionListener(this);
					fixSession.Connect();
					_session[_sessionIdAttributeName] = fixSession;
				}
			}
			catch (IOException e)
			{
				LogFactory.GetLog(this.GetType()).Error(e, e);
			}
		}

		public void Dispose()
		{
			try
			{
				if (_connector != null)
				{
					if (!_connector.Wait(TimeSpan.FromSeconds(10)))
						LogFactory.GetLog(this.GetType()).Error("Session connect async execution timeout expired");
				}

				using (var sessionDown = new SessionDownTask())
				{
					sessionDown.Init(new Dictionary<string, string>() { { SESSION_ID_NAME_PARAM, _sessionIdAttributeName } }, _session);
					sessionDown.DoTask();
					_session.TryRemove(_sessionIdAttributeName, out var session);
				}
			}
			catch (Exception e)
			{
				LogFactory.GetLog(this.GetType()).Error(e, e);
			}
		}
	}
}