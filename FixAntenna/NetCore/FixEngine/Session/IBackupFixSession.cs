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

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	/// <summary>
	/// The <c>IBackupFIXSession</c> provides ability to switch the connection from
	/// the primary host to backup and vice versa.
	/// The backup session should configured by engine.properties and default.properties configuration files.
	/// </summary>
	internal interface IBackupFixSession
	{
		/// <summary>
		/// The method, switch current connection to backup.
		/// </summary>
		void SwitchToBackUp();

		/// <summary>
		/// The method, switch current connection to primary.
		/// </summary>
		void SwitchToPrimary();

		/// <summary>
		/// Returns true, if session connected to backup host.
		/// </summary>
		bool IsRunningOnBackup { get; }
	}
}