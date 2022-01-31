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

using Epam.FixAntenna.NetCore.Validation;
using Epam.FixAntenna.NetCore.Validation.Error;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.Validation
{
	/// <summary>
	/// Wraps the <see cref="FixErrorContainer"/>.
	/// </summary>
	internal class ValidationResultWrapper : IValidationResult
	{
		private readonly FixErrorContainer _errors;

		/// <summary>
		/// Creates the <c>ValidationResultWrapper</c>.
		/// </summary>
		/// <param name="errorContainer"> the error container </param>
		public ValidationResultWrapper(FixErrorContainer errorContainer)
		{
			_errors = errorContainer;
		}

		/// <inheritdoc />
		public virtual bool IsMessageValid => _errors.IsEmpty;

		/// <inheritdoc />
		public FixErrorContainer Errors
		{
			get { return _errors; }
		}

		/// <inheritdoc />
		public virtual void Reset()
		{
			_errors.Clear();
		}
	}
}