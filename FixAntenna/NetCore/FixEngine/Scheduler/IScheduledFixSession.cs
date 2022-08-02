// Copyright (c) 2022 EPAM Systems
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

namespace Epam.FixAntenna.NetCore.FixEngine.Scheduler
{
	/// <summary>
	/// Interface for FIX session which could have scheduled start/stop time.
	/// </summary>
	public interface IScheduledFixSession : IFixSession
	{
		/// <summary>
		/// Init scheduler with start/stop tasks for this session.
		/// </summary>
		void Schedule();

		/// <summary>
		/// Deactivate scheduled tasks for this session.
		/// </summary>
		void Deschedule();
	}
}