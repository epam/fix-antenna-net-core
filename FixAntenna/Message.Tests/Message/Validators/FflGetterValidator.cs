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
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.Message.Tests.Validators
{
	internal abstract class FflGetterValidator
	{
		public abstract int GetMaxOccurrence(int messageMaxOccurrence);

		public abstract string PrepareTagValueForRead(string ffl, int tagId, int occurrence);

		public abstract void CheckGetter(FixMessage ffl, int tagId);

		public abstract void CheckGetterWithOccurrence(FixMessage ffl, int tagId, int occurrence);

		public abstract void CheckGetterAtIndex(FixMessage ffl, int occurrence, int tagIndex);

		public virtual string ReplaceFieldValue(int tagId, int occurrence, string value, string msg)
		{
			var fieldPrefix = "" + '\u0001' + tagId + '=';
			var startIndex = 0;
			for (var i = 0; i < occurrence; i++)
			{
				startIndex = msg.IndexOf(fieldPrefix, startIndex, StringComparison.Ordinal);
				if (startIndex < 0)
				{
					return msg;
				}

				startIndex += fieldPrefix.Length;
			}

			var endIndex = msg.IndexOf('\u0001', startIndex);
			if (endIndex < 0)
			{
				endIndex = msg.Length;
			}

			return msg.Substring(0, startIndex) + value + msg.Substring(endIndex, msg.Length - endIndex);
		}

		public virtual string GetValidatorName()
		{
			return "[" + GetType().Name + "]";
		}
	}
}