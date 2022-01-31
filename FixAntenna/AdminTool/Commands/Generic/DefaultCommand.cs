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
using System.Text;
using Epam.FixAntenna.Fixicc.Message;

namespace Epam.FixAntenna.AdminTool.Commands.Generic
{
	/// <summary>
	/// Default command, invoked by RIA when no other implementation not found.
	/// </summary>
	internal class DefaultCommand : Command
	{
		private readonly string _commandName;
		private readonly ISet<string> _supportedCommands;

		public DefaultCommand(string commandName, IEnumerable<string> supportedCommands)
		{
			_commandName = string.IsNullOrWhiteSpace(commandName) ? "<Unknown>" : commandName;
			// fixed 15616.
			_supportedCommands = new SortedSet<string>();
			_supportedCommands.UnionWith(supportedCommands);
		}

		public override void Execute()
		{
			Log.Debug("Execute DefaultCommand Command");
			var result = "not implemented";
			if (_supportedCommands.Count > 0)
			{
				var sb = new StringBuilder(1024);
				sb.Append($"Unsupported command '{_commandName}'.\n");
				sb.Append("Expected elements are:\n[");
				sb.Append(string.Join(", ", _supportedCommands));
				sb.Append("]");
				result = sb.ToString();
			}
			try
			{
				var response = new Response { Description = result, ResultCode = ResultCode.OperationNotImplemented.Code };
				SendResponse(response);
			}
			catch (Exception e)
			{
				Log.Error(e);
				SendError(e.Message);
			}
		}
	}
}