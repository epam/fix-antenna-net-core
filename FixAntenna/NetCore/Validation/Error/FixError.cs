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

namespace Epam.FixAntenna.NetCore.Validation.Error
{
	public sealed class FixError : IComparable<FixError>
	{
		public FixError(FixErrorCode fixErrorCode, string description, TagValue tagValue)
		{
			FixErrorCode = fixErrorCode;
			Description = description;
			TagValue = tagValue?.Clone(); // clone original TagValue, as far as it refer same object in foreach
		}

		public int CompareTo(FixError o)
		{
			if (FixErrorCode.Equals(o.FixErrorCode))
			{
				return string.Compare(Description, o.Description, StringComparison.Ordinal);
			}

			return FixErrorCode.Code - o.FixErrorCode.Code;
		}

		public FixErrorCode FixErrorCode { get; }

		public string Description { get; set; }

		public TagValue TagValue { get; set; }

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}

			if (o == null || GetType() != o.GetType())
			{
				return false;
			}

			var fixError = (FixError)o;

			return !(!Description?.Equals(fixError.Description) ?? fixError.Description != null)
					&& FixErrorCode.Equals(fixError.FixErrorCode)
					&& !(!TagValue?.Equals(fixError.TagValue) ?? fixError.TagValue != null);
		}

		public override int GetHashCode()
		{
			var result = FixErrorCode != null ? FixErrorCode.GetHashCode() : 0;
			result = 31 * result + (Description?.GetHashCode() ?? 0);
			return result;
		}

		public override string ToString()
		{
			return "FIXError{" +
					"fixErrorCode=" + FixErrorCode +
					", description='" + Description + '\'' +
					", field = " + TagValue + '}';
		}

		public FixError Clone()
		{
			return new FixError(FixErrorCode, Description, TagValue);
		}
	}
}