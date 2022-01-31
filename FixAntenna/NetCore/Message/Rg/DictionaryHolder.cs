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
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Dictionary;
using Epam.FixAntenna.NetCore.Validation.Entities;

namespace Epam.FixAntenna.NetCore.Message.Rg
{
	internal class DictionaryHolder
	{
		private static readonly Dictionary<string, GroupDict> Dictionaries = new Dictionary<string, GroupDict>();
		private static readonly FixDictionaryFactory DictionaryFactory = FixDictionaryFactory.Instance;

		public static GroupDict GetDictionary(FixVersion version)
		{
			return GetDictionary(FixVersionContainer.GetFixVersionContainer(version));
		}

		public static GroupDict GetDictionary(FixVersionContainer version)
		{
			if (!Dictionaries.TryGetValue(version.DictionaryId, out var groupDict))
			{
				var dict = DictionaryFactory.GetDictionaries(version, null);
				var fixdic = (Fixdic)dict.Dictionaries[0];
				groupDict = new GroupDict(fixdic);
				Dictionaries[version.DictionaryId] = groupDict;
				return groupDict;
			}

			return groupDict;
		}
	}
}