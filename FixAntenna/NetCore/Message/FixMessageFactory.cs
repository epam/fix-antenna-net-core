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

namespace Epam.FixAntenna.NetCore.Message
{
	internal class FixMessageFactory
	{
		public static FixMessage NewInstanceFromPool(bool isUserOwned = true)
		{
			return RawFixUtil.GetFixMessageFromPool(isUserOwned);
		}

		public static FixMessage NewInstance(bool fromPool, bool isUserOwned)
		{
			return fromPool ? NewInstanceFromPool(isUserOwned) : new FixMessage(isUserOwned);
		}

		public static int FieldObjectsCreated => RawFixUtil.FieldObjectsCreated;

		public static int FieldListObjectsCreated => RawFixUtil.FieldListObjectsCreated;

		public static FixMessage NewInstanceFromPoolForEngineParse()
		{
			//FixMessage msg = RawFixUtil.GetFixMessageFromPool(false);
			//TBD! it will be nice to get message from pool but need to fix releasing this message
			// in MessagePumper if user will send it
			return new FixMessage { ForceCloneOnSend = true };
		}
	}
}