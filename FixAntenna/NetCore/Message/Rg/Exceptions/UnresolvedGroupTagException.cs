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
	internal class UnresolvedGroupTagException : RepeatingGroupException
	{
		private const string Msg = "Tag {0:d} is unresolved for group with leading tag {1:d}. ";
		private readonly int _invalidTag;
		private readonly int _leadingTag;

		public UnresolvedGroupTagException(int invalidTag, int leadingTag, FixVersion version, string msgType) : this(
			invalidTag, leadingTag, FixVersionContainer.GetFixVersionContainer(version), msgType)
		{
		}

		public UnresolvedGroupTagException(int invalidTag, int leadingTag, FixVersionContainer version, string msgType)
			: base(version, msgType)
		{
			_invalidTag = invalidTag;
			_leadingTag = leadingTag;
		}

		public override string Message => string.Format(Msg, _invalidTag, _leadingTag) + base.Message;
	}
}