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

using Epam.FixAntenna.NetCore.FixEngine.Storage.Queue;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.Util
{
	/// <summary>
	/// FixMessageWithType type factory.
	/// </summary>
	/// <seealso cref="IQueueableFactory{T}"></seealso>
	internal class FixMessageWithTypeFactory : IQueueableFactory<FixMessageWithType>
	{
		/// <summary>
		/// Create FixMessageWithType instance.
		/// </summary>
		/// <returns> FixMessageWithType
		///  </returns>
		public virtual FixMessageWithType CreateObject()
		{
			return FixMessageWithTypePoolFactory.GetFixMessageWithTypeFromPool();
		}
	}
}