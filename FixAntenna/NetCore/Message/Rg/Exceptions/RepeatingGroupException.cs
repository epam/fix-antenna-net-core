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
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;

namespace Epam.FixAntenna.NetCore.Message.Rg.Exceptions
{
	internal class RepeatingGroupException : Exception
	{
		private const string Msg = "FIX version = {0}, message type = {1}";
		private readonly string _messageType;
		private readonly FixVersionContainer _version;

		public RepeatingGroupException(FixVersion version, string messageType) : this(
			FixVersionContainer.GetFixVersionContainer(version), messageType)
		{
		}

		public RepeatingGroupException(FixVersionContainer version, string messageType)
		{
			_version = version;
			_messageType = messageType;
		}

		public override string Message => string.Format(Msg, _version.DictionaryId, _messageType);
	}
}