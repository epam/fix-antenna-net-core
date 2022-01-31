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

namespace Epam.FixAntenna.NetCore.Dictionary
{
	/// <summary>
	/// Build of instance of Dictionaries using input parameter. By default this is simply procedure
	/// that returns instance of Dictionaries with one Dictionary for example FIXDictionaryFactory,
	/// but there is factory that returns complex instance for example FIXTDictionaryFactory
	/// </summary>
	internal interface IDictionaryFactory
	{
		/// <summary>
		/// Returns of instance of <see cref="DictionaryTypes"/> by FIX version.
		/// </summary>
		/// <param name="fixVersion"> Instance of <see cref="FixVersion"/> </param>
		/// <returns> Instance of <see cref="DictionaryTypes"/> with dictionaries </returns>
		DictionaryTypes GetDictionaries(FixVersionContainer fixVersion, FixVersionContainer appVersion);

		/// <summary>
		/// Loads dictionary by version of FIX protocol.
		/// </summary>
		/// <param name="fixVersion"> Version of FIX protocol </param>
		void LoadDictionary(FixVersionContainer fixVersion, FixVersionContainer appVersion);
	}
}