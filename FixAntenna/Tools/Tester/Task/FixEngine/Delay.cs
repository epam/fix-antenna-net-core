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
using System.Threading;
using Epam.FixAntenna.NetCore.Common.Logging;

namespace Epam.FixAntenna.Tester.Task.FixEngine
{
	public sealed class Delay : ITask
	{
		private static readonly ILog _log = LogFactory.GetLog(typeof(Delay));
		private const string TIME_PARAM = "timeInMilis";

		private int _delayInMilis;
		private CustomConcurrentDictionary<string, object> _session;

		public void Init(IDictionary<string, string> @params, CustomConcurrentDictionary<string, object> session)
		{
			this._session = session;
			string delay = @params[TIME_PARAM];
			_delayInMilis = int.Parse(delay);
		}

		public void DoTask()
		{
			_log.Info("Do delay " + _delayInMilis + "ms");

			Thread.Sleep(_delayInMilis);
		}

		public void Dispose()
		{
		}
	}
}