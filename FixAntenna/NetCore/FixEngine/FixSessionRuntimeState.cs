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
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine
{
	internal sealed class FixSessionRuntimeState
	{
		public const long InitSeqNum = -1;
		private const string StateInSeqNumProperty = "state.inSeqNum";
		private const string StateOutSeqNumProperty = "state.outSeqNum";
		private const string StateLastProcessedSeqNumProperty = "state.lastProcessedSeqNum";

		private static readonly ILog Log = LogFactory.GetLog(typeof(FixSessionRuntimeState));

		private volatile SessionState _sessionState = SessionState.Disconnected;

		public long InSeqNum { get; set; } = InitSeqNum;

		public long OutSeqNum { get; set; } = InitSeqNum;

		public long LastProcessedSeqNum { get; set; }

		public SessionState SessionState
		{
			get { return _sessionState; }
			set { _sessionState = value; }
		}

		public void DecrementInSeqNum()
		{
			InSeqNum--;
			LastProcessedSeqNum = InSeqNum > 0 ? InSeqNum - 1 : 0;
		}

		public IDictionary<string, string> ToProperties()
		{
			var properties = new Dictionary<string, string>
			{
				{ StateInSeqNumProperty, InSeqNum.ToString() },
				{ StateOutSeqNumProperty, OutSeqNum.ToString() },
				{ StateLastProcessedSeqNumProperty, LastProcessedSeqNum.ToString() }
			};
			return properties;
		}

		public void FromProperties(IDictionary<string, string> properties)
		{
			if (properties.ContainsKey(StateInSeqNumProperty))
			{
				InSeqNum = Convert.ToInt64(properties.GetValueOrDefault(StateInSeqNumProperty, "0"));
			}

			if (properties.ContainsKey(StateOutSeqNumProperty))
			{
				OutSeqNum = Convert.ToInt64(properties.GetValueOrDefault(StateOutSeqNumProperty, "0"));
			}

			if (properties.ContainsKey(StateLastProcessedSeqNumProperty))
			{
				LastProcessedSeqNum = Convert.ToInt64(properties.GetValueOrDefault(StateLastProcessedSeqNumProperty, "0"));
			}
		}

		public void IncrementInSeqNum()
		{
			InSeqNum++;
		}

		public void IncrementOutSeqNum()
		{
			OutSeqNum++;
			if (Log.IsTraceEnabled)
			{
				Log.Trace("Increment outgoing seq num: " + OutSeqNum);
			}
		}

		public FixMessage OutgoingLogon { get; set; } = new FixMessage();
	}
}