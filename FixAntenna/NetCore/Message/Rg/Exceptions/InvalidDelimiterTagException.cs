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

using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;

namespace Epam.FixAntenna.NetCore.Message.Rg.Exceptions
{
	internal class InvalidDelimiterTagException : RepeatingGroupException
	{
		private const string Msg = "Expected delimiter tag {0:d}, but got tag {1:d}. ";
		private readonly int _delimTag;
		private readonly int _invalidTag;

		public InvalidDelimiterTagException(int delimTag, int invalidTag, FixVersion version, string messageType) :
			this(delimTag, invalidTag, FixVersionContainer.GetFixVersionContainer(version), messageType)
		{
		}

		public InvalidDelimiterTagException(int delimTag, int invalidTag, FixVersionContainer version,
			string messageType) : base(version, messageType)
		{
			_delimTag = delimTag;
			_invalidTag = invalidTag;
		}

		public override string Message => string.Format(Msg, _delimTag, _invalidTag) + base.Message;
	}
}