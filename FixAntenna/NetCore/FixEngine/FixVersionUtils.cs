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
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Logging;

namespace Epam.FixAntenna.NetCore.FixEngine
{
	/// <summary>
	/// FIX Version utils class helper.
	/// </summary>
	internal class FixVersionUtils
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(FixVersionUtils));
		private readonly string _versionFixStr;

		private readonly string _versionFixtStr;

		public FixVersionUtils(string valueVersion)
		{
			if (valueVersion.Contains(":"))
			{
				_versionFixtStr = valueVersion.Substring(0, valueVersion.LastIndexOf(":", StringComparison.Ordinal))
					.Trim();
				_versionFixStr = valueVersion.Substring(valueVersion.LastIndexOf(":", StringComparison.Ordinal) + 1)
					.Trim();
			}
			else
			{
				_versionFixStr = valueVersion.Trim();
			}
		}

		public virtual FixVersion GetFixtVersion()
		{
			if (_versionFixtStr != null)
			{
				if (_versionFixtStr.Equals("FIXT.1.1", StringComparison.OrdinalIgnoreCase))
				{
					return FixVersion.Fixt11;
				}

				var msg = "Can't parse string \"" + _versionFixtStr + "\" to FIXTVersion";
				Log.Warn(msg);
					throw new ArgumentException(msg);
				}

				return null;
			}

		public virtual FixVersion GetFixVersion()
		{
			FixVersion fixVersion;
			try
			{
				fixVersion = FixVersion.GetInstanceByMessageVersion(_versionFixStr);
			}
			catch (ArgumentException ex)
			{
				if (Log.IsWarnEnabled)
				{
					Log.Warn("Can't parse string \"" + _versionFixStr + "\" to FIXVersion: " + ex);
				}

				throw;
			}

			return fixVersion;
		}
	}
}