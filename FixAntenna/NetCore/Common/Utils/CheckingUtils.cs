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
using System.Diagnostics;
using System.Threading;

namespace Epam.FixAntenna.NetCore.Common.Utils
{
	internal class CheckingUtils
	{
		/// <summary>
		/// Continuously check expression inside lambda till condition or timeout is met.
		/// No exception is thrown if condition is not met within the given timeout.
		/// Condition is checked every 5 ms.
		/// </summary>
		/// <param name="supplier">Delegate to be checked</param>
		/// <param name="timeout">Timeout</param>
		/// <returns>Check result</returns>
		public static bool? TryCheckWithinTimeout(SupplierWithException<bool?> supplier, TimeSpan timeout)
		{
			var sw = new Stopwatch();
			sw.Start();
			bool? result;
			do
			{
				result = supplier();
				Thread.Sleep(5);
			} while ((result == null || !result.Value) && sw.Elapsed < timeout);
			sw.Stop();
			return result;
		}

		/// <summary>
		/// Continuously check expression inside lambda till condition or timeout is met.
		/// <see cref="TimeoutException"/> is thrown if condition is not met within the given timeout.
		/// Condition is checked every 5 ms.
		/// </summary>
		/// <param name="supplier">Delegate to be checked</param>
		/// <param name="timeout">Timeout</param>
		/// <exception cref="TimeoutException"></exception>
		/// <returns>Check result</returns>
		public static bool? CheckWithinTimeout(SupplierWithException<bool?> supplier, TimeSpan timeout)
		{
			var result = TryCheckWithinTimeout(supplier, timeout);

			if (!result.HasValue || !result.Value)
			{
				var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff");
				throw new TimeoutException($"[{timestamp}] Condition was not fulfilled within {timeout}");
			}

			return true;
		}
	}
}