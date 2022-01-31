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

namespace Epam.FixAntenna.AdminTool
{
	/// <summary>
	/// Runtime exception for Admin Tool module
	/// </summary>
	internal class AdminToolException : Exception
	{
		private readonly string _description;

		public AdminToolException()
		{
		}

		public AdminToolException(string message, Exception innerException) : base(message, innerException)
		{
			_description = message;
		}

		/// <summary>
		/// Construct a <code>AdminToolException </code> with the specified detail message
		/// and nested exception.
		/// </summary>
		/// <param name="description"> string that describe the problem </param>
		public AdminToolException(string description) : base(description)
		{
			_description = description;
		}

		public AdminToolException(System.Exception cause) : base(cause?.Message, cause)
		{
			_description = cause?.Message;
		}

		/// <summary>
		/// Retrieve the innermost cause of this exception, if any.
		/// </summary>
		/// <returns> the innermost exception, or <code>null</code> if none </returns>
		public Exception GetRootCause()
		{
			Exception rootCause = null;
			var cause = InnerException;
			while (cause != null && cause != rootCause)
			{
				rootCause = cause;
				cause = cause.InnerException;
			}
			return rootCause;
		}

		/// <summary>
		/// Method getFastError returns the validationError of this FastExceptionobject.
		/// </summary>
		/// <returns> the fastError (type String) of this FastExceptionobject. </returns>
		public virtual string GetDescription()
		{
			return _description;
		}

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

			var that = (AdminToolException) o;

			return !(!string.IsNullOrEmpty(_description)
				? !_description.Equals(that._description, StringComparison.Ordinal)
				: !string.IsNullOrEmpty(that._description));
		}

		public override int GetHashCode()
		{
			return _description != null ? _description.GetHashCode() : 0;
		}

		public override string ToString()
		{
			return $"AdminToolException{{description='{_description}'}}";
		}
	}
}