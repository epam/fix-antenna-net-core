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

namespace Epam.FixAntenna.NetCore.Message
{
	/// <summary>
	/// Thrown when an exceptional message condition has occurred. For
	/// example, No SOH symbol at the end of message throws an
	/// instance of this class.
	/// </summary>
	internal class GarbledMessageException : Exception
	{
		private readonly bool _critical;

		public GarbledMessageException()
		{
		}

		public GarbledMessageException(string message) : base(message)
		{
		}

		protected internal GarbledMessageException(string message, bool critical) : base(message)
		{
			_critical = critical;
		}

		public GarbledMessageException(string description, string garbledMessage) : base(
			description + ": " + garbledMessage)
		{
		}

		public GarbledMessageException(string description, string garbledMessage, bool critical) : base(
			description + ": " + garbledMessage)
		{
			_critical = critical;
		}

		public GarbledMessageException(string message, Exception cause) : base(message, cause)
		{
		}

		public virtual bool IsCritical()
		{
			return _critical;
		}
	}
}