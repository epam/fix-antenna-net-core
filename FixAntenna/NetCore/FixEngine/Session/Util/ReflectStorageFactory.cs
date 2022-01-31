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
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Storage;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.Util
{
	/// <summary>
	/// Helper class to create storage factory.
	/// </summary>
	internal sealed class ReflectStorageFactory
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(ReflectStorageFactory));

		private ReflectStorageFactory()
		{
		}

		/// <summary>
		/// Creates storage factory.
		/// <p/>
		/// If <see cref="Config.StorageFactory"/> is not configured the <see cref="FilesystemStorageFactory"/> will be used.
		/// </summary>
		/// <param name="configuration"> the configuration </param>
		/// <returns> StorageFactory  </returns>
		public static IStorageFactory CreateStorageFactory(Config configuration)
		{
			var className = configuration.GetProperty(Config.StorageFactory, typeof(FilesystemStorageFactory).FullName);
			try
			{
				var reflectUtil = new ReflectUtilEx(Type.GetType(className));
				return reflectUtil.GetInstance(new object[]{ configuration }) as IStorageFactory;
			}
			catch (Exception e)
			{
				if (Log.IsDebugEnabled)
				{
					Log.Warn("Can not load storage factory for given configuration object, try with default constructor: " + e.Message, e);
				}
				else
				{
					Log.Warn("Can not load storage factory for given configuration object, try with default constructor: " + e.Message);
				}

				try
				{
					var reflectUtil = new ReflectUtilEx(Type.GetType(className));
					return reflectUtil.GetInstance(Array.Empty<object>()) as IStorageFactory;
				}
				catch (Exception e1)
				{
					if (Log.IsDebugEnabled)
					{
						Log.Warn("Can not load storage factory: " + className + ". Cause: " + e1.Message + ". Loaded default FilesystemStorageFactory.", e1);
					}
					else
					{
						Log.Warn("Can not load storage factory: " + className + ". Cause: " + e1.Message + ". Loaded default FilesystemStorageFactory.");
					}

					return new FilesystemStorageFactory(configuration);
				}
			}
		}
	}
}