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
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Acceptor
{
	/// <summary>
	/// Logon message parser.
	/// </summary>
	internal class LogonMessageParser
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(LogonMessageParser));
		private static readonly byte[] ResetSeqNumFlagValue = "Y".AsByteArray();

		/// <summary>
		/// Gets session parameters from login message.
		/// </summary>
		/// <param name="loginMessage"> the logon message </param>
		/// <param name="host">         the host </param>
		/// <param name="port">         the port </param>
		/// <returns> SessionParameters </returns>
		/// <exception cref="ArgumentException"> if target or sender are invalid </exception>
		public virtual ParseResult ParseLogon(FixMessage loginMessage, string host, int port)
		{
			var parseResult = new ParseResult();
			parseResult.SessionParameters.Host = host;
			parseResult.SessionParameters.Port = port;

			var targetCompId = loginMessage.GetTagValueAsString(Tags.TargetCompID);
			if (string.IsNullOrEmpty(targetCompId))
			{
				throw new ArgumentException("TargetCompID cannot be empty: " + loginMessage);
			}

			parseResult.SessionParameters.SenderCompId = targetCompId;
			var sendCompId = loginMessage.GetTagValueAsString(Tags.SenderCompID);
			if (string.IsNullOrEmpty(sendCompId))
			{
				throw new ArgumentException("SenderCompID cannot be empty: " + loginMessage);
			}

			parseResult.SessionParameters.TargetCompId = sendCompId;

			var logonMessage = DecryptLogon(parseResult, loginMessage, port);

			parseResult.SessionParameters.SessionQualifier = logonMessage.GetTagValueAsString(parseResult.SessionParameters.Configuration.GetPropertyAsInt(Config.LogonMessageSessionQualifierTag));

			parseResult.SessionParameters.SenderLocationId = logonMessage.GetTagValueAsString(Tags.TargetLocationID);
			parseResult.SessionParameters.TargetLocationId = logonMessage.GetTagValueAsString(Tags.SenderLocationID);
			parseResult.SessionParameters.SenderSubId = logonMessage.GetTagValueAsString(Tags.TargetSubID);
			parseResult.SessionParameters.TargetSubId = logonMessage.GetTagValueAsString(Tags.SenderSubID);

			if (logonMessage.IsTagExists(Tags.HeartBtInt))
			{
				try
				{
					var hbi = logonMessage.GetTagAsInt(Tags.HeartBtInt);
					parseResult.SessionParameters.HeartbeatInterval = hbi;
				}
				catch (ArgumentException parseIntExc)
				{
					var errorMessage = "Incorrect or undefined heartbeat interval. 108=" + logonMessage.GetTagValueAsString(Tags.HeartBtInt);
					Log.Error(errorMessage + ": " + loginMessage.ToPrintableString(), parseIntExc);
					parseResult.SessionParameters.LogonError = errorMessage;
				}
			}
			else
			{
				var errorMessage = "HeartBtInt(108) cannot be empty";
				Log.Error(errorMessage + ": " + loginMessage.ToPrintableString());
				parseResult.SessionParameters.LogonError = errorMessage;
			}

			parseResult.SessionParameters.FixVersion = FixVersion.GetInstanceByMessageVersion(logonMessage.GetTagValueAsString(Tags.BeginString));
			if (logonMessage.IsTagExists(Tags.DefaultApplVerID))
			{
				parseResult.SessionParameters.AppVersion = FixVersion.GetInstanceByFixtVersion(logonMessage.GetTagAsInt(Tags.DefaultApplVerID));
			}

			parseResult.SessionParameters.IncomingLoginMessage = logonMessage;

			return parseResult;
		}

		private FixMessage DecryptLogon(ParseResult parseResult, FixMessage loginMessage, int port)
		{
			var encMethod = loginMessage.GetTagValueAsString(Tags.EncryptMethod);
			if (encMethod == null)
			{
				Log.Warn("Invalid logon - there is no mandatory EncryptMethod(98) field. Will use None(0) encryption type");
			}

			if (encMethod != "0")
			{
				Log.Warn($"Invalid logon - unknown EncryptMethod(98) field: {encMethod}. Will use None(0) encryption type");
			}

			return loginMessage;
		}

		internal class ParseResult
		{
			internal ParsedSessionParameters SessionParameters { get; set; } = new ParsedSessionParameters();
		}
	}
}