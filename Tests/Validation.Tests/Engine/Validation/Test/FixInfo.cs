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

using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;

namespace Epam.FixAntenna.Validation.Tests.Engine.Validation.Test
{
	/// <summary>
	/// FIXInfo  provides container for common use Fixdic and FixVersion in validation process.
	/// </summary>
	internal class FixInfo
	{
		private readonly FixVersionContainer _appVersion;
		private readonly FixVersionContainer _version;

		public FixInfo(FixVersion version)
		{
			_version = FixVersionContainer.GetFixVersionContainer(version);
		}

		public FixInfo(FixVersion version, FixVersion appVersion)
		{
			_version = FixVersionContainer.GetFixVersionContainer(version);
			_appVersion = FixVersionContainer.GetFixVersionContainer(appVersion);
		}

		public virtual FixVersionContainer GetVersion()
		{
			return _version;
		}

		public virtual FixVersionContainer GetAppVersion()
		{
			return _appVersion;
		}
	}
}