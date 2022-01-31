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

using Epam.FixAntenna.Fixicc.Message;
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Epam.FixAntenna.AdminTool.Resources;
using Epam.FixAntenna.AdminTool.Commands;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Acceptor;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using Version = Epam.FixAntenna.NetCore.Common.Utils.Version;

namespace Epam.FixAntenna.AdminTool
{
	/// <summary>
	/// Main class for AdminTool Application.
	/// </summary>
	internal sealed class AdminTool : IFixAdminSessionListener
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(AdminTool));
		private string _commandPackageName = AdminConstants.DefaultCommandPackage;

		internal IFixSession AdminSession { get; set; }
		private readonly ICommandHandler _commandHandler;
		private IConfiguredSessionRegister _configuredSessionRegister;
		private readonly object _lock = new object();

		/// <summary>
		/// Creates the {@code AdminTool }.
		/// </summary>
		public AdminTool()
		{
			_commandHandler = new CommandHandler();
		}

		public void SetSessionRegister(IConfiguredSessionRegister configuredSessionRegister)
		{
			_configuredSessionRegister = configuredSessionRegister;
		}

		/// <summary>
		/// Process the request.
		/// </summary>
		/// <param name="message"> the fix message </param>
		internal void Process(FixMessageAdapter message)
		{
			long? commandRequestId = null;
			try
			{
				Monitor.Enter(_lock);

				// validate message
				if (!ValidateMessage(message))
				{
					return;
				}
				var xmlDataField = message.GetTag(AdminConstants.XmlDataTag);

				// get command
				var command = _commandHandler.GetCommand(xmlDataField.StringValue, _commandPackageName);
				commandRequestId = command.RequestId;

				// check request id
				if (!ValidateRequestId(commandRequestId))
				{
					return;
				}

				// set admin session
				command.AdminFixSession = AdminSession;

				// set session acceptor register
				command.ConfiguredSessionRegister = _configuredSessionRegister;

				// execute command
				command.Execute();

				// set last request id
				ResetRequestId(commandRequestId);
			}
			catch (AdminToolException e)
			{
				Log.Error($"Unexpected error:{e.Message}", e);
				SendResponse(e.Message, ResultCode.OperationUnknownError, commandRequestId);
			}
			catch (Exception e)
			{
				Log.Error($"Unexpected error:{e.Message}", e);
				SendResponse(e.Message, ResultCode.OperationUnknownError, commandRequestId);
			}
			finally
			{
				Monitor.Exit(_lock);
			}
		}

		/// <summary>
		/// Sets the package of commands.
		/// </summary>
		private void SetSessionExternalPackageName()
		{
			var externalPackage = AdminSession.Parameters.Configuration.GetProperty(Config.AutostartAcceptorCommandPackage, AdminConstants.DefaultCommandPackage);
			if (string.IsNullOrWhiteSpace(externalPackage))
			{
				externalPackage = AdminConstants.DefaultCommandPackage;
				Log.Warn($"Parameter {Config.AutostartAcceptorCommandPackage} not configured.");
			}
			_commandPackageName = externalPackage;
		}

		/// <summary>
		/// Resets request id.
		/// </summary>
		/// <param name="commandRequestId"> the new request id </param>
		private void ResetRequestId(long? commandRequestId)
		{
			if (commandRequestId != null)
			{
				// old command without request id skip it
				// requestIDs.Add(commandRequestID);
			}
		}

		/// <summary>
		/// Validates the message.
		/// </summary>
		/// <param name="message"> the fix message </param>
		private bool ValidateMessage(FixMessageAdapter message)
		{
			var messageType = StringHelper.NewString(message.MsgType);
			if (!AdminConstants.MessageType.Equals(messageType, StringComparison.Ordinal))
			{
				Log.Error($"AdminTool is unable to process non FIX XML message, msg type:{messageType}");
				SendResponse(Strings.UnableToProcessNotXmldata, ResultCode.OperationReject, null);
				return false;
			}
			var xmlDataLen = message.GetTag(AdminConstants.XmlDataLenTag);
			if (xmlDataLen == null || xmlDataLen.StringValue.Equals("0", StringComparison.OrdinalIgnoreCase))
			{
				Log.Error($"XmlData is empty, msg type:{messageType}");
				SendResponse(Strings.XmlDataIsEmpty, ResultCode.OperationReject, null);
				return false;
			}
			return true;
		}

		/// <summary>
		/// Validates the requestID.
		/// </summary>
		/// <param name="commandRequestId"> </param>
		private bool ValidateRequestId(long? commandRequestId)
		{
			if (commandRequestId == null)
			{
				// old command did not have request id
				return true;
			}
			//if (requestIDs.contains(commandRequestID))
			//{
			//	sendResponse(ResourceHelper.GetCodeMessage(ResourceHelper.REQUEST_ID_HAS_BEEN_USED), ResultCode.OPERATION_REJECT.GetCode(), commandRequestID);
			//	return false;
			//}
			//else
			//{
				return true;
			//}
		}

		/// <summary>
		/// Sends the response.
		/// </summary>
		/// <param name="problem">   the discription </param>
		/// <param name="code">      the result code </param>
		/// <param name="requestId"> the last request id </param>
		private void SendResponse(string problem, ResultCode code, long? requestId)
		{
			var response = new Response
			{
				RequestID = requestId,
				ResultCode = code.Code,
				Description = problem
			};

			try
			{
				response.RequestID = requestId;
				var xml = MessageUtils.ToXml(response);
				var list = new FixMessage();
				list.AddTag(AdminConstants.XmlDataLenTag, xml.Length);
				list.AddTag(AdminConstants.XmlDataTag, xml);
				AdminSession.SendMessage(AdminConstants.MessageType, list);
			}
			catch (Exception e)
			{
				Log.Error("error on send response", e);
			}
		}

		/// <inheritdoc/>
		public void NewFixSession(IFixSession session)
		{
			AdminSession = session;
			AdminSession.SetFixSessionListener(new AdminToolListener(this));
			try
			{
				var outgoingLoginMsg = AdminSession.Parameters.OutgoingLoginMessage;
				outgoingLoginMsg.Set(AdminConstants.TimezoneTag, GetFormattedTimeZone());
				outgoingLoginMsg.Set(AdminConstants.AdminProtocolVersionTag, Version.GetProductVersion(typeof(IMessage)));
				AdminSession.Connect();
				SetSessionExternalPackageName();
			}
			catch (IOException e)
			{
				throw new AdminToolException("error start session", e);
			}
		}

		internal static string GetFormattedTimeZone()
		{
			return DateTimeOffset.Now.ToString("%K", CultureInfo.InvariantCulture);
		}

		private class AdminToolListener : IFixSessionListener
		{
			private readonly AdminTool _tool;

			public AdminToolListener(AdminTool tool)
			{
				_tool = tool;
			}

			public void OnSessionStateChange(SessionState sessionState)
			{
				if (SessionState.IsDisconnected(sessionState))
				{
					_tool.AdminSession.Dispose();
				}
			}

			public void OnNewMessage(FixMessage message)
			{
				_tool.Process(message);
			}
		}
	}
}