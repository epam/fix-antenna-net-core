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

using Epam.FixAntenna.NetCore.Common.Threading.Runnable;
using Epam.FixAntenna.NetCore.FixEngine.Session.IoThreads.Bean;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.FixEngine.Storage;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.IoThreads
{
	internal interface IMessageReader : IWorkerThread
	{
		long Init(ConfigurationAdapter configurationAdapter);

		void Shutdown();

		long MessageProcessedTimestamp { get; set; }

		bool IsStatisticEnabled { get; }

		MessageStatistic MessageStatistic { get; }

		IMessageStorage IncomingMessageStorage { get; }

		bool GracefulShutdown { get; set; }

		void Start();

		void Join();
	}
}