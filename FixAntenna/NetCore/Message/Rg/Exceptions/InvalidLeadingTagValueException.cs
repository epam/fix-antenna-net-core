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
	internal class InvalidLeadingTagValueException : RepeatingGroupException
	{
		private const string MsgGreater = "Leading tag value greater than entries count. LeadingTag = {0:d}. ";
		private const string MsgLess = "Leading tag value less than entries count. LeadingTag = {0:d}. ";
		private readonly bool _greater;
		private readonly int _leadingTag;

		public InvalidLeadingTagValueException(int leadingTag, bool greater, FixVersion version, string messageType) :
			this(leadingTag, greater, FixVersionContainer.GetFixVersionContainer(version), messageType)
		{
		}

		public InvalidLeadingTagValueException(int leadingTag, bool greater, FixVersionContainer version,
			string messageType) : base(version, messageType)
		{
			_greater = greater;
			_leadingTag = leadingTag;
		}

		public override string Message
		{
			get
			{
				if (_greater)
				{
					return string.Format(MsgGreater, _leadingTag) + base.Message;
				}

				return string.Format(MsgLess, _leadingTag) + base.Message;
			}
		}
	}
}