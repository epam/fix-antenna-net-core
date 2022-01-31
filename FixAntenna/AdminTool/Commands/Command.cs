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
using Epam.FixAntenna.AdminTool.Resources;
using Epam.FixAntenna.Fixicc.Message;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.AdminTool.Commands
{
	/// <summary>
	/// Common interface for all command of Administrative tool.
	/// </summary>
	internal abstract class Command
	{
		protected internal ILog Log;
		protected internal string XmlContent { get; set; }

		protected internal IConfiguredSessionRegister ConfiguredSessionRegister { get; set; }
		protected internal Request Request { get; set; }
		protected internal IFixSession AdminFixSession { get; set; }

		public Command()
		{
			Log = LogFactory.GetLog(GetType());
		}

		/// <summary>
		/// Gets request id from request.
		/// <p/>
		/// if request not set returns null
		/// </summary>
		public virtual long? RequestId => Request?.RequestID;

		/// <summary>
		/// Execute command.
		/// </summary>
		public abstract void Execute();

		/// <summary>
		/// Send response.
		/// </summary>
		/// <param name="response"> </param>
		public virtual void SendResponse(Response response)
		{
			try
			{
				if (response.ResultCode == null)
				{
					response.ResultCode = ResultCode.OperationSuccess.Code;
				}
				response.RequestID = RequestId;
				var xml = MessageUtils.ToXml(response);
				var list = new FixMessage();
				list.AddTag(AdminConstants.XmlDataLenTag, xml.Length);
				list.AddTag(AdminConstants.XmlDataTag, xml);
				AdminFixSession.SendMessage(AdminConstants.MessageType, list);
			}
			catch (Exception e)
			{
				Log.Error("send response", e);
			}
		}

		/// <summary>
		/// Send response.
		/// </summary>
		/// <param name="response"> </param>
		public virtual void SendResponseSuccess(Response response)
		{
			try
			{
				if (response.Description == null)
				{
					response.Description = string.Format(Strings.OperationSuccess, GetType().Name);
				}
				response.ResultCode = ResultCode.OperationSuccess.Code;
				response.RequestID = RequestId;
				var xml = MessageUtils.ToXml(response);
				var list = new FixMessage();
				list.AddTag(AdminConstants.XmlDataLenTag, xml.Length);
				list.AddTag(AdminConstants.XmlDataTag, xml);
				AdminFixSession.SendMessage(AdminConstants.MessageType, list);
			}
			catch (Exception e)
			{
				Log.Error("send response", e);
			}
		}

		/// <summary>
		/// Send response.
		/// </summary>
		/// <param name="problem"> </param>
		/// <param name="errorCode"> </param>
		public virtual void SendResponse(string problem, ResultCode errorCode)
		{
			var response = new Response();
			response.RequestID = RequestId;
			response.ResultCode = errorCode.Code;
			response.Description = string.Format(Strings.OperationError, GetType().Name, problem);
			SendResponse(response);
		}

		/// <summary>
		/// Send reject.
		/// </summary>
		/// <param name="problem"> </param>
		public virtual void SendReject(string problem)
		{
			SendResponse(problem, ResultCode.OperationReject);
		}

		/// <summary>
		/// Send error.
		/// </summary>
		/// <param name="problem"> </param>
		public virtual void SendError(string problem)
		{
			SendResponse(problem, ResultCode.OperationUnknownError);
		}

		/// <summary>
		/// Send invalid argument.
		/// </summary>
		/// <param name="problem"> </param>
		public virtual void SendInvalidArgument(string problem)
		{
			SendResponse(problem, ResultCode.OperationInvalidArgument);
		}

		/// <summary>
		/// Send error with unknown session.
		/// </summary>
		public virtual void SendUnknownSession(SessionId id)
		{
			var response = new Response();
			response.RequestID = RequestId;
			response.ResultCode = ResultCode.ResultUnknownSession.Code;
			response.Description = string.Format(Strings.OperationUnknownSessionId, GetType().Name, id.ToString());
			SendResponse(response);
		}

		/// <summary>
		/// Gets the fix session.
		/// </summary>
		/// <param name="sender"> - sender </param>
		/// <param name="target"> - target </param>
		/// <param name="qualifier"> - qualifier </param>
		public virtual IExtendedFixSession GetFixSession(string sender, string target, string qualifier)
		{
			return GetFixSession(new SessionId(sender, target, qualifier));
		}

		/// <summary>
		/// Gets the fix session.
		/// </summary>
		/// <param name="sessionId"> - sessionID </param>
		public virtual IExtendedFixSession GetFixSession(SessionId sessionId)
		{
			var readCopy = FixSessionManager.Instance.SessionListCopy;
			for (var i = 0; i < readCopy.Count; i++)
			{
				var session = readCopy[i];
				var parameters = session.Parameters;
				if (parameters.SessionId.Equals(sessionId))
				{
					return session;
				}
				else if (AreKeySessionParamsEqual(parameters, sessionId))
				{
					return session;
				}
			}
			return null;
		}

		/// <summary>
		/// Gets the configured fix session.
		/// </summary>
		/// <param name="senderCompId"> - sender </param>
		/// <param name="targetCompId"> - target </param>
		/// <param name="qualifier"> - qualifier </param>
		public virtual SessionParameters GetConfiguredSession(string senderCompId, string targetCompId, string qualifier)
		{
			return GetConfiguredSession(new SessionId(senderCompId, targetCompId, qualifier));
		}

		/// <summary>
		/// Gets the fix session.
		/// </summary>
		/// <param name="sessionId"> - sessionID </param>
		public virtual SessionParameters GetConfiguredSession(SessionId sessionId)
		{
			foreach (var parameters in GetConfiguredSessionParameters())
			{
				if (parameters.SessionId.Equals(sessionId))
				{
					return parameters;
				}
				else
				{
					if (AreKeySessionParamsEqual(parameters, sessionId))
					{
						return parameters;
					}
				}
			}
			return null;
		}

		public virtual bool AreKeySessionParamsEqual(SessionParameters parameters, SessionId sessionId)
		{
			return sessionId.Sender.Equals(parameters.SenderCompId, StringComparison.Ordinal)
						&& sessionId.Target.Equals(parameters.TargetCompId, StringComparison.Ordinal)
						&& (sessionId.Qualifier?.Equals(parameters.SessionQualifier, StringComparison.Ordinal)
								?? parameters.SessionQualifier == null);
		}

		/// <summary>
		/// Gets fix session list.
		/// </summary>
		public virtual IList<IExtendedFixSession> GetFixSessions()
		{
			return FixSessionManager.Instance.SessionListCopy;
		}

		/// <summary>
		/// Gets the configured fix session list.
		/// </summary>
		public virtual IList<SessionParameters> GetConfiguredSessionParameters()
		{
			return ConfiguredSessionRegister.RegisteredSessions;
		}
	}
}