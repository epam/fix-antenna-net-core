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

namespace Epam.FixAntenna.Tester.Updater
{
	public class DefaultUpdater : IMessageUpdater
	{
		private char _separator;

		public virtual string UpdateMessage(string message)
		{
			return message.Replace(_separator, '\x0001');
		}

		public virtual byte[] UpdateMessage(byte[] message)
		{
			return new byte[0]; //To change body of implemented methods use File | Settings | File Templates.
		}

		public virtual void SetMessageSeparator(string value)
		{
			if (string.ReferenceEquals(value, null) || value.Length != 1)
			{
				_separator = '\x0001';
			}
			else
			{
				_separator = value[0];
			}
		}
	}

}