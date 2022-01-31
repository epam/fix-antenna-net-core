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
using Epam.FixAntenna.AdminTool.Commands.Util;
using Epam.FixAntenna.Fixicc.Message;

namespace Epam.FixAntenna.AdminTool.Commands.Generic
{
	internal class GetFixProtocolsList : Command
	{
		public override void Execute()
		{
			var data = new FIXProtocolsListData();
			Log.Debug("Execute GetFIXProtocolsList Command");
			try
			{
				if (RequestId == null)
				{
					SendInvalidArgument("Parameter RequestID is required");
					return;
				}
				var list = CommandUtil.GetSupportedVersions();
				foreach (var versionStr in list)
				{
					var protocol = new FIXProtocolsListDataSupportedProtocol();
					protocol.Version = versionStr;
					data.SupportedProtocol.Add(protocol);
				}
				var response = new Response();
				response.FIXProtocolsListData = data;
				SendResponseSuccess(response);

			}
			catch (Exception e)
			{
				Log.Error(e);
				SendError(e.Message);
			}
		}
	}
}