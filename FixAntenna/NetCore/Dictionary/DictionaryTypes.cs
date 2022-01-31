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
using Epam.FixAntenna.NetCore.Common.Xml;

namespace Epam.FixAntenna.NetCore.Dictionary
{
	/// <summary>
	/// This class is storage for Dictionaries of FIX version.
	/// This class clones of IDictionary objects and returns to the clients.
	/// </summary>
	internal class DictionaryTypes
	{
		/// <summary>
		/// Creates <c>Dictionaries</c>.
		/// </summary>
		public DictionaryTypes(IList<IType> types)
		{
			Dictionaries = Clone(types);
		}

		/// <summary>
		/// Gets dictionaries.
		/// </summary>
		public virtual IList<IType> Dictionaries { get; }

		private IList<IType> Clone(IList<IType> types)
		{
			IList<IType> typeList = new List<IType>();
			foreach (var type in types)
			{
				typeList.Add((IType)type.Clone());
			}

			return typeList;
		}

		public override string ToString()
		{
			return "Dictionaries{" +
					"types=" + Dictionaries +
					'}';
		}
	}
}