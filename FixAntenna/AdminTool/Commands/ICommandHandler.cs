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

namespace Epam.FixAntenna.AdminTool.Commands
{

	/// <summary>
	/// The base command handler interface.
	/// Provides ability to find and create the command instance.
	/// </summary>
	internal interface ICommandHandler
	{
		/// <summary>
		/// Gets command.
		/// </summary>
		/// <param name="xmlContext">      the xml context </param>
		/// <param name="externalPackage"> the session parameters
		/// </param>
		/// <returns> Command if command found or null otherwise </returns>
		Command GetCommand(string xmlContext, string externalPackage);
	}
}