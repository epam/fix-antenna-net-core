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
	internal class DuplicateTagException : RepeatingGroupException
	{
		private const string Msg = "Repeating Group with leading tag {0:d} already contains tag {1:d}. ";
		private readonly int _duplicateTag;
		private readonly int _leadingTag;

		public DuplicateTagException(int leadingTag, int duplicateTag, FixVersion version, string messageType) : this(
			leadingTag, duplicateTag, FixVersionContainer.GetFixVersionContainer(version), messageType)
		{
		}

		public DuplicateTagException(int leadingTag, int duplicateTag, FixVersionContainer version, string messageType)
			: base(version, messageType)
		{
			_leadingTag = leadingTag;
			_duplicateTag = duplicateTag;
		}

		public override string Message => string.Format(Msg, _leadingTag, _duplicateTag) + base.Message;
	}
}