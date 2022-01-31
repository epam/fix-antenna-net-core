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
using Epam.FixAntenna.Fixicc.Message;
using Epam.FixAntenna.AdminTool.Builder;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Helpers;

namespace Epam.FixAntenna.AdminTool.Commands
{
	internal class CommandHandler : ICommandHandler
	{
		protected internal static ILog Log = LogFactory.GetLog(typeof(CommandHandler));
		private readonly Dictionary<string, Type> _supportedCommands = new Dictionary<string, Type>();

		/// <summary>
		/// Creates the <c>CommandHandler</c>.
		/// </summary>
		public CommandHandler()
		{
			// Register commands
			// Administrative
			_supportedCommands[typeof(ChangeSeqNum).Name] = typeof(Administrative.ChangeSeqNum);
			_supportedCommands[typeof(CreateInitiator).Name] = typeof(Administrative.CreateInitiator);
			_supportedCommands[typeof(CreateAcceptor).Name] = typeof(Administrative.CreateAcceptor);
			_supportedCommands[typeof(Delete).Name] = typeof(Administrative.Delete);
			_supportedCommands[typeof(StopSession).Name] = typeof(Administrative.StopSession);
			_supportedCommands[typeof(DeleteAll).Name] = typeof(Administrative.DeleteAll);
			_supportedCommands[typeof(ResetSeqNum).Name] = typeof(Administrative.ResetSeqNum);
			_supportedCommands[typeof(ToBackup).Name] = typeof(Administrative.ToBackup);
			_supportedCommands[typeof(ToBackup).Name] = typeof(Administrative.ToBackup);
			_supportedCommands[typeof(ToPrimary).Name] = typeof(Administrative.ToPrimary);
			// Generic
			// fixed 15515
			_supportedCommands[typeof(SendMessage).Name] = typeof(Generic.SendMessage);
			_supportedCommands[typeof(Heartbeat).Name] = typeof(Generic.Heartbeat);
			_supportedCommands[typeof(Help).Name] = typeof(Generic.Help);
			_supportedCommands[typeof(TestRequest).Name] = typeof(Generic.TestRequest);
			_supportedCommands[typeof(GetFIXProtocolsList).Name] = typeof(Generic.GetFixProtocolsList);
			// Monitoring
			_supportedCommands[typeof(SessionParams).Name] = typeof(Monitoring.SessionParams);
			_supportedCommands[typeof(SessionsList).Name] = typeof(Monitoring.SessionsList);
			_supportedCommands[typeof(SessionsSnapshot).Name] = typeof(Monitoring.SessionsSnapshot);
			_supportedCommands[typeof(SessionStatus).Name] = typeof(Monitoring.SessionStatus);
			// Statistic
			_supportedCommands[typeof(GeneralSessionsStat).Name] = typeof(Statistic.GeneralSessionsStat);
			_supportedCommands[typeof(ProceedStat).Name] = typeof(Statistic.ProceedStat);
			_supportedCommands[typeof(ReceivedStat).Name] = typeof(Statistic.ReceivedStat);
			_supportedCommands[typeof(SentStat).Name] = typeof(Statistic.SentStat);
			_supportedCommands[typeof(SessionStat).Name] = typeof(Statistic.SessionStat);
		}

		/// <inheritdoc/>
		public virtual Command GetCommand(string xmlContent, string externalPackage)
		{
			xmlContent = ReplaceInvalidCharacters(xmlContent);
			Command command = null;
			var className = string.Empty;
			IMessage message = null;
			try
			{
				message = MessageUtils.FromXml(xmlContent);
				if (message == null)
				{
					throw new Exception($"Error on parse xml content [{xmlContent}]");
				}
				className = GetCanonicalCommandClassName(message);
				if (_supportedCommands.ContainsKey(className))
				{
					command = (Command) Activator.CreateInstance(_supportedCommands[className]);
				}
				else
				{
					command = CommandBuilder.GetInstance().BuildCommand(className, externalPackage);
				}
			}
			catch (Exception ex)
			{
				Log.Debug("Exception in GetCommand", ex);
			}
			if (command == null)
			{
				command = new Generic.DefaultCommand(className, _supportedCommands.Keys);
			}
			command.Request = (Request) message;
			command.XmlContent = xmlContent;
			return command;
		}

		/// <summary>
		/// Replaces invalid characters,
		/// <c>SOH</c> replaced with <c>&amp;#01;</c> and <c>&amp;</c> with <c>&amp;amp;</c>
		/// </summary>
		/// <param name="xmlContent"> </param>
		/// <returns> string </returns>
		public virtual string ReplaceInvalidCharacters(string xmlContent)
		{
			// [-] Fixed 15166: Special symbol "#" RAI mechanism considered as incorrect.
			xmlContent = xmlContent.ReplaceAll("\u0001", AdminConstants.SendMessageDelimiter);
			// replace all ampersand
			xmlContent = xmlContent.ReplaceAll("&", "&amp;");
			return xmlContent;
		}

		/// <summary>
		/// Gets command class name
		/// <p/>
		/// if com.epam.Command will return Command
		/// </summary>
		/// <returns> Class Name </returns>
		public virtual string GetCanonicalCommandClassName(IMessage message)
		{
			return message?.GetType().Name;
		}
	}
}