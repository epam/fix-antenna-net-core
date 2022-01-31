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

using System.Threading;

namespace Epam.FixAntenna.Tester
{
	public sealed class ResultCounter
	{
		private volatile int _success;
		private volatile int _failed;

		public void Add(ResultCounter counter)
		{
			Interlocked.Add(ref _success, counter._success);
			Interlocked.Add(ref _failed, counter._failed);
		}

		public void IncSuccess()
		{
			Interlocked.Increment(ref _success);
		}
		
		public void IncFailed()
		{
			Interlocked.Increment(ref _failed);
		}

		public bool HasFaults => _failed > 0;

		public override string ToString()
		{
			return "Total cases:" + (_failed + _success) + " (success:" + _success + ", failed:" + _failed + ')';
		}
	}
}