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
using Epam.FixAntenna.NetCore.Helpers;

namespace Epam.FixAntenna.Tester.Updater
{
	public class LazyUpdater : IMessageUpdater
	{
		protected const int HEADER_LENGTH = 12;
		protected const int TRAILER_LENGTH = 7;
		protected internal char Separator;

		public virtual string UpdateMessage(string message)
		{
			if (message.Length < HEADER_LENGTH + TRAILER_LENGTH)
			{
				throw new System.ArgumentException("Invalid FIX incomingMessage");
			}
			return UpdateChecksum(UpdateLength(UpdateSendingTime(ReplaceSeparator(message))));
		}

		public virtual string UpdateOriginalSendingTime(string message)
		{
			int indexOf = message.IndexOf("\x0001122=", StringComparison.Ordinal);
			int till = message.IndexOf('\x0001', indexOf + 1);
			if (indexOf != -1 && till != -1)
			{
				return message.Substring(0, indexOf) + "\x0001122=" + GetDate() + message.Substring(till);
			}
			else
			{
				return message;
			}
		}

		public virtual byte[] UpdateMessage(byte[] message)
		{
			return new byte[0]; //To change body of implemented methods use File | Settings | File Templates.
		}

		public virtual string UpdateChecksum(string message)
		{
			string body = message.Substring(0, message.Length - TRAILER_LENGTH);
			byte[] bodyArray = body.AsByteArray();
			long sum = 0L;
			foreach (byte aBodyArray in bodyArray)
			{
				sum += aBodyArray;
			}

			return body + "10=" + (sum % 256).ToString("000") + '\x0001';
		}

		public virtual string UpdateSendingTime(string message)
		{
			int indexOf = message.IndexOf("\x000152=", StringComparison.Ordinal);
			int till = message.IndexOf('\x0001', indexOf + 1);
			if (indexOf != -1 && till != -1)
			{
				return message.Substring(0, indexOf) + "\x000152=" + GetDate() + message.Substring(till);
			}
			else
			{
				return message;
			}
		}

		public virtual string GetDate()
		{

			return DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss");
		}

		public virtual string UpdateLength(string message)
		{
			int indexOf = message.IndexOf('\x0001', HEADER_LENGTH);
			return message.Substring(0, HEADER_LENGTH) + (message.Length - indexOf - TRAILER_LENGTH - 1) + message.Substring(indexOf);
		}

		public virtual string ReplaceSeparator(string message)
		{
			return message.Replace(Separator, '\x0001');
		}

		public virtual void SetMessageSeparator(string value)
		{
			if (value is null || value.Length != 1)
			{
				Separator = '\x0001';
			}
			else
			{
				Separator = value[0];
			}
		}
	}

}