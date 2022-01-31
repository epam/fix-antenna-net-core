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

namespace Epam.FixAntenna.NetCore.FixEngine.Storage
{
	/// <summary>
	/// Located the full file name.
	/// </summary>
	internal interface ILogFileLocator
	{
		/// <summary>
		/// Gets file name from session parameters.
		/// </summary>
		/// <param name="sessionParameters"> the session parameters </param>
		/// <returns> the file name for session parameters, the result file name consists of sender and target parameters </returns>
		string GetFileName(SessionParameters sessionParameters);
	}
}