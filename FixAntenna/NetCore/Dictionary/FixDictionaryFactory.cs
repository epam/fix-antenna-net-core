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
using Epam.FixAntenna.NetCore.Configuration;

namespace Epam.FixAntenna.NetCore.Dictionary
{
	/// <summary>
	/// Simple dictionary factory that returns one instance of dictionary by input version of FIX protocol
	/// </summary>
	internal class FixDictionaryFactory : IDictionaryFactory
	{
		private static FixDictionaryFactory _fixDictionaryFactory;
		private static bool _loadDescriptions;
		private readonly DictionaryBuilder _dictionaryBuilder;

		private FixDictionaryFactory()
		{
			_dictionaryBuilder = new DictionaryBuilder();
		}

		/// <inheritdoc />
		public virtual DictionaryTypes GetDictionaries(FixVersionContainer fixVersion, FixVersionContainer appVersion)
		{
			IList<IType> types = new List<IType>();
			if (appVersion != null)
			{
				types.Add(_dictionaryBuilder.BuildDictionary(appVersion, false));
			}

			types.Add(_dictionaryBuilder.BuildDictionary(fixVersion, false));

			return new DictionaryTypes(types);
		}

		/// <inheritdoc />
		public virtual void LoadDictionary(FixVersionContainer fixVersion, FixVersionContainer appVersion)
		{
			if (appVersion != null)
			{
				_dictionaryBuilder.BuildDictionary(appVersion, true);
			}

			_dictionaryBuilder.BuildDictionary(fixVersion, true);
		}

		public static bool LoadDescriptions
		{
			get => _loadDescriptions;
			set
			{
				_loadDescriptions = value;
				Instance._dictionaryBuilder.LoadDescriptions = value;
			}
		}

		public static FixDictionaryFactory Instance
		{
			get
			{
				if (_fixDictionaryFactory == null)
				{
					_fixDictionaryFactory = new FixDictionaryFactory();
				}

				return _fixDictionaryFactory;
			}
		}

		public virtual void CleanDictionaryCache()
		{
			_dictionaryBuilder.CleanCache();
		}
	}
}