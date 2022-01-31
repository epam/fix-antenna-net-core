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

namespace Epam.FixAntenna.NetCore.Common.Logging
{
	/// <summary>
	/// The log factory implementation.
	/// Provides functionality for create and configure NLog or Common Logging instance,
	/// if no one is exists, the DefaultLog implementation is used instead.
	/// </summary>
	public static class LogFactory
	{
		private static readonly ILogFactory Instance;
		private const int HashLength = 7;
		private static readonly string ProductVersion = typeof(LogFactory).Assembly
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

		static LogFactory()
		{
			try
			{
				Instance = new NLogFactory();
				var log = Instance.GetLog(typeof(NLogFactory));
				
				PrintProductVersion(log);
				
				log.Trace("NLog logger wrapper initialized");
			}
			catch (Exception)
			{
				Instance = new DefaultLogFactory();
			}
		}

		/// <summary>
		/// Gets log instance.
		/// </summary>
		/// <param name="aClass"> the class </param>
		public static ILog GetLog(Type aClass)
		{
			return Instance.GetLog(aClass);
		}

		/// <summary>
		/// Gets log instance.
		/// </summary>
		/// <param name="name"> the logical name of the <see cref="ILog"/> instance </param>
		public static ILog GetLog(string name)
		{
			return Instance.GetLog(name);
		}

		private static void PrintProductVersion(ILog logger)
		{
			var version = ProductVersion;
			var year = DateTime.Now.Year;

			// truncate the commit hash
			var iHash = version.IndexOf('+') + 1;
			if (iHash > 0 && version.Length > iHash + HashLength)
			{
				version = version.Substring(0, iHash + HashLength);
			}

			logger.Info($"FIX Antenna .NET Core {version} Copyright 2020-{year} EPAM Systems. Licensed under the Apache 2.0");
		}
	}
}