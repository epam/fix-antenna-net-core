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

using System.Collections.Generic;

namespace Epam.FixAntenna.NetCore.Validation.Utils.Definitions
{
	internal interface IFixMessageDefinitions<T>
	{
		/// <summary>
		/// Puts the element into map.
		/// </summary>
		/// <param name="elements"> the list of elements </param>
		void Put(IList<T> elements);

		/// <summary>
		/// Returns the collection of values of this Map.
		/// </summary>
		/// <returns> the messageDefs of this MessageTypes object. </returns>
		ICollection<T> Get();

		/// <summary>
		/// Gets element from map by key
		/// </summary>
		/// <param name="messageType"> of type String </param>
		/// <returns> instance of T </returns>
		T Get(string messageType);

		/// <summary>
		/// Checks if Contains input message type in dictionary of FIX protocol
		/// </summary>
		/// <param name="messageType"> Message Type </param>
		/// <returns> boolean true if Contains </returns>
		bool Contains(string messageType);

		/// <summary>
		/// Returns the messageTypes of this FixDefMap object.
		/// </summary>
		/// <returns> the messageTypes of this FixDefMap object. </returns>
		ISet<string> GetMessageTypes();
	}
}