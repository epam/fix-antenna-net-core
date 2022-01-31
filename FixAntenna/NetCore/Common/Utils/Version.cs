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
using System.Reflection;
using Epam.FixAntenna.NetCore.Common.Logging;

namespace Epam.FixAntenna.NetCore.Common.Utils
{
	internal class Version
	{
		private const string UnknownVersion = "1.0";

		/// <summary>
		/// Prints the version of a product.
		/// </summary>
		/// <param name="aClass"> the class from product jar FILE </param>
		/// <param name="log">    the log </param>
		public static void PrintVersionOfProduct(Type aClass, ILog log)
		{
			log.Info("Product Version: " + GetProductVersion(aClass));
		}

		public static string GetProductVersion(Type aClass, int limit)
		{
			var productVersion = GetProductVersion(aClass);
			if (productVersion.Length > limit)
			{
				productVersion = productVersion.Substring(0, limit);
			}

			return productVersion;
		}

		public static string GetProductVersion(Type aClass)
		{
			var version = Assembly.GetAssembly(aClass).GetName().Version.ToString();
			if (string.IsNullOrEmpty(version))
			{
				version = UnknownVersion;
			}

			return version;
		}
	}
}