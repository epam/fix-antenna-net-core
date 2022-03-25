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

using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.FixEngine.Storage;

namespace Epam.FixAntenna.NetCore.FixEngine.ResetLogon.Util
{
	internal class SessionParameterPersistenceHelper
	{
		private readonly SessionParameters _sessionParameters;

		public SessionParameterPersistenceHelper(SessionParameters sessionParameters)
		{
				_sessionParameters = sessionParameters;
		}

		public virtual void Store()
		{
			var storageFactory = ReflectStorageFactory.CreateStorageFactory(_sessionParameters.Configuration);
			if (storageFactory is IInitializable)
			{
				((IInitializable)storageFactory).Init(_sessionParameters);
			}
			storageFactory.SaveSessionParameters(_sessionParameters, new FixSessionRuntimeState());
			if (storageFactory is IClosable)
			{
				((IClosable)storageFactory).Close();
			}
		}
	}
}