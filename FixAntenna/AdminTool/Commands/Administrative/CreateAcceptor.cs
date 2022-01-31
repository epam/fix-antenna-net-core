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
using Epam.FixAntenna.Fixicc.Message;
using Epam.FixAntenna.NetCore.FixEngine;

namespace Epam.FixAntenna.AdminTool.Commands.Administrative
{
	/// <summary>
	/// The CreateAcceptor command.
	/// Creates a new acceptor session.
	/// </summary>
	internal class CreateAcceptor : CreateSession
	{
		public override void Execute()
		{
			Log.Debug("Execute CreateAcceptor Command");
			try
			{
				if (RequestId == null)
				{
					SendInvalidArgument("Parameter RequestID is required");
					return;
				}

				var createAcceptorRequest = (Fixicc.Message.CreateAcceptor) Request;

				var details = new SessionParameters();
				if (!FillSenderTargetIds(createAcceptorRequest, details))
				{
					return;
				}

				if (!FillFixVersion(createAcceptorRequest, details))
				{
					return;
				}

				var extraSessionParams = createAcceptorRequest.ExtraSessionParams;
				if (extraSessionParams != null)
				{ // fixed bug 15170 Session using the RAI was not created
					if (!FillExtraSessionParams(details, extraSessionParams))
					{
						return;
					}
				}

				ConfiguredSessionRegister.RegisterSession(details);
				SendResponseSuccess(new Response());
			}
			catch (Exception e)
			{
				Log.Error(e.Message, e);
				SendError(e.Message);
			}
		}
	}
}