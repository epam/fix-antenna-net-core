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
using Epam.FixAntenna.NetCore.Common.Threading.Runnable;
using Epam.FixAntenna.NetCore.FixEngine.Session.IoThreads.Bean;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.IoThreads
{
	internal interface IMessagePumper : IWorkerThread, IDisposable
	{
		long Init();

		bool GracefulShutdown { get; set; }

		void RejectQueueMessages();

		void RejectFirstQueueMessage();

		bool IsStatisticEnabled { get; }

		MessageStatistic Statistic { get; }

		long MessageProcessedTimestamp { get; }

		bool SendOutOfTurn(string msgType, FixMessage content);

		void Start();

		void Shutdown();

		void Join();

		int Send(string type, FixMessage content, FixSessionSendingType optionMask);

		int Send(FixMessage content, ChangesType? allowedChangesType);

		int Send(FixMessage content, ChangesType? allowedChangesType, FixSessionSendingType optionMask);

		int Send(string s, FixMessage message);

		void SendMessages(int messageCount);
	}
}