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
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.FixEngine.Session;

namespace Epam.FixAntenna.Tester.Task
{
	public sealed class ChangeProcessedIncomingSeqNumber : ITask
	{
		private static readonly ILog _log = LogFactory.GetLog(typeof(ChangeProcessedIncomingSeqNumber));

		private int _inSeqNum;
		private CustomConcurrentDictionary<string, object> _sessions;

		public void Init(IDictionary<string, string> @params, CustomConcurrentDictionary<string, object> session)
		{
			_inSeqNum = int.Parse(@params[TesterParams.SEQ_NUM_PARAM]);
			_sessions = session;
		}

		public void DoTask()
		{
			if (_log.IsDebugEnabled)
			{
				_log.Debug("Change processed incoming seq num to:" + _inSeqNum);
			}

			var fixSession = (IExtendedFixSession) _sessions[TesterParams.SESSION_NAME_PARAM];
			fixSession.RuntimeState.InSeqNum = _inSeqNum;
		}

		public void Dispose()
		{
		}
	}
}