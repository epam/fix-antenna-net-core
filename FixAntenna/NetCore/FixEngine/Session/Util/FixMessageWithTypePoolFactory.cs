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
using Epam.FixAntenna.NetCore.Common.Pool;
using Epam.FixAntenna.NetCore.Common.Pool.Provider;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.Util
{
	internal class FixMessageWithTypePoolFactory
	{
		private static readonly IPool<FixMessageWithType> Pool;
		private static readonly ILog Log = LogFactory.GetLog(typeof(FixMessageWithTypePoolFactory));

		static FixMessageWithTypePoolFactory()
		{
			Pool = PoolFactory.GetConcurrentBucketsPool(10, 200, 2000, new AbstractPoolableProvider());
		}

		public static FixMessageWithType GetFixMessageWithTypeFromPool()
		{
			try
			{
				return Pool.Object;
			}
			catch (Exception e)
			{
				Log.Debug("Can't get new instance from pool (will be allocated new object): " + e.Message, e);
			}

			return new FixMessageWithType();
		}

		public static FixMessageWithType GetFixMessageWithTypeFromPool(FixMessage list, string type)
		{
			var msgWithType = GetFixMessageWithTypeFromPool();
			msgWithType.Init(list, type);
			return msgWithType;
		}

		public static FixMessageWithType GetFixMessageWithTypeFromPool(FixMessage list, ChangesType? changesType)
		{
			var msgWithType = GetFixMessageWithTypeFromPool();
			msgWithType.Init(list, changesType);
			return msgWithType;
		}

		public static void ReturnObj(FixMessageWithType message)
		{
			if (message.IsOriginatingFromPool())
			{
				message.Clear();
				Pool.ReturnObject(message);
			}
		}

		public static int GetObjectsCreated()
		{
			return Pool.ObjectsCreated;
		}

		private class AbstractPoolableProvider : AbstractPoolableProvider<FixMessageWithType>
		{
			public override FixMessageWithType Create()
			{
				var list = new FixMessageWithType();
				list.SetOriginatingFromPool();
				return list;
			}

			public override void Activate(FixMessageWithType t)
			{
			}
		}
	}
}