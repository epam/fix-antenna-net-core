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

using Epam.FixAntenna.Tester.Stage;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Epam.FixAntenna.NetCore.Common.Logging;

namespace Epam.FixAntenna.Tester
{
	internal class Case
	{
		protected internal static readonly ILog Log = LogFactory.GetLog(typeof(Case));

		protected internal ITransport Transport;
		protected internal string Name;
		protected internal ICaseLogger CaseLogger;
		protected internal int ReceivedMsgCount;
		protected internal int SentMsgCount;
		protected internal bool Success;

		public Case(string name, ICaseLogger caseLogger)
		{
			this.Success = true;
			this.Name = name;
			this.CaseLogger = caseLogger;
			Log.Info("===================================================================");
			Log.Info("Case '" + name + '\'');
		}

		public Case(string name, ITransport transport, ICaseLogger caseLogger)
		{
			this.Success = true;
			this.Transport = transport;
			this.Name = name;
			this.CaseLogger = caseLogger;
			Log.Info("===================================================================");
			Log.Info("Case '" + name + '\'');
		}

		public virtual void ReinitTransport(ITransport transport)
		{
			this.Transport = transport;
		}

		public virtual bool IsSuccess()
		{
			return Success;
		}

		public virtual void SetTransport(ITransport transport)
		{
			this.Transport = transport;
		}

		public virtual void RunSendStage(OutgoingMessage messages, IDictionary<string, string> @params)
		{
			Log.Info(">>>>> Send message #" + ++SentMsgCount + " for '" + Name + "'. Sending: " + messages.GetUpdatedMessage().Replace('\x0001', '#'));
			if (@params == null || @params.Count == 0)
			{
				Transport.SendMessage(messages.GetUpdatedMessage());
			}
			else
			{
				Transport.SendMessage(messages.GetUpdatedMessage(), @params);
			}
		}

		public virtual void RunSendStage(RawOutgoingMessage messages)
		{
			Log.Info(">>>>> Send message #" + ++SentMsgCount + " for '" + Name + "'. Sending: " + messages.GetUpdatedMessage().Replace('\x0001', '#'));
			Transport.SendMessage(Encoding.UTF8.GetString(messages.GetRawUpdatedMessage()));
		}

		public virtual void RunReceiveStage(ExpectedMessage expMessage, IDictionary<string, string> @params)
		{
			Log.Info("<<<<< Receive message #" + ++ReceivedMsgCount + " for '" + Name + ". Receiving: ");
			string message;
			try
			{
				if (@params == null || @params.Count == 0)
				{
					message = Transport.ReceiveMessage();
				}
				else
				{
					message = Transport.ReceiveMessage(@params);
				}

			}
			catch (IOException e)
			{
				Log.Warn("IOException " + e.Message + " while reading message. Assume we expect blank message 'connection close'");
				message = "";
			}
			if (expMessage.EqualsToEtalon(message))
			{
				Log.Info("Ok - equals");
				CaseLogger.LogOk(Name + '#' + ReceivedMsgCount);
			}
			else
			{
				Log.Info("ERROR - Not equals");
				Success = false;
				CaseLogger.LogError(Name + " #" + ReceivedMsgCount);
				CaseLogger.LogActual(message.Replace('\x0001', '#'));
				CaseLogger.LogExpected(expMessage.ToString());
			}
		}

		public virtual void Close()
		{
			Log.Info("Gracefully shutting down");
		}
	}
}