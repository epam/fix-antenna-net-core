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

namespace Epam.FixAntenna.NetCore.FixEngine
{
	/// <summary>
	/// User must implement this interface for handling error and warning.
	/// </summary>
	/// <seealso cref="IFixSession.ErrorHandler"> </seealso>
	public interface IErrorHandler
	{
		/// <summary>
		/// This method is invoked every time when warning is occurred.
		/// </summary>
		/// <param name="description"> warn description </param>
		/// <param name="throwable"> error </param>
		void OnWarn(string description, Exception throwable);

		/// <summary>
		/// This method is invoked every time when error is occurred.
		/// </summary>
		/// <param name="description"> error description </param>
		/// <param name="throwable"> error </param>
		void OnError(string description, Exception throwable);

		/// <summary>
		/// This method is invoked every time when fatal error is occurred.
		/// </summary>
		/// <param name="description"> fatal error description </param>
		/// <param name="throwable"> error </param>
		void OnFatalError(string description, Exception throwable);
	}
}