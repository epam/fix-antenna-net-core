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
using System.Collections.Generic;
using Epam.FixAntenna.NetCore.Common;

namespace Epam.FixAntenna.NetCore.Configuration
{
	public class FixVersionContainerFactory
	{
		private static readonly string DictionaryFilenamePattern = "validation.{0}.additionalDictionaryFileName";
		private static readonly string DictionaryUpdatePattern = "validation.{0}.additionalDictionaryUpdate";
		private static readonly IDictionary<FixVersion, string> DictFiles = new Dictionary<FixVersion, string>();
		public static readonly IDictionary<string, string> StandardIds = new Dictionary<string, string>();

		static FixVersionContainerFactory()
		{
			DictFiles[FixVersion.Fix40] = "fixdic40.xml";
			DictFiles[FixVersion.Fix41] = "fixdic41.xml";
			DictFiles[FixVersion.Fix42] = "fixdic42.xml";
			DictFiles[FixVersion.Fix43] = "fixdic43.xml";
			DictFiles[FixVersion.Fix44] = "fixdic44.xml";
			DictFiles[FixVersion.Fix50] = "fixdic50.xml";
			DictFiles[FixVersion.Fix50Sp1] = "fixdic50sp1.xml";
			DictFiles[FixVersion.Fix50Sp2] = "fixdic50sp2.xml";
			DictFiles[FixVersion.Fixt11] = "fixdict11.xml";

			foreach (var file in DictFiles)
			{
				StandardIds[file.Value] = file.Key.Id;
			}
		}

		public static FixVersionContainer GetFixVersionContainer(FixVersion fixVersion)
		{
			return GetFixVersionContainer(Config.GlobalConfiguration, fixVersion);
		}

		public static FixVersionContainer GetFixVersionContainer(string dictionaryId, FixVersion fixVersion)
		{
			return GetFixVersionContainer(dictionaryId, Config.GlobalConfiguration, fixVersion);
		}

		public static FixVersionContainer GetFixVersionContainer(Config configuration, FixVersion fixVersion)
		{
			var dictionaryId = GetDefaultDicId(fixVersion);
			return GetFixVersionContainer(dictionaryId, configuration, fixVersion);
		}

		public static FixVersionContainer GetFixVersionContainer(string dictionaryId, Config configuration)
		{
			var fixVersion = FixVersion.ValueOf(dictionaryId);
			if (fixVersion != null)
			{
				return GetFixVersionContainer(dictionaryId, configuration, fixVersion);
			}

			var customFixVersionConfig = configuration.GetCustomFixVersionConfig(dictionaryId);
			if (customFixVersionConfig == null)
			{
				throw new ArgumentException("No FIX version found with id: " + dictionaryId);
			}

			var version = FixVersion.GetInstanceByMessageVersion(customFixVersionConfig.FixVersion);
			return new FixVersionContainer(dictionaryId, version, customFixVersionConfig.FileName);
		}

		public static FixVersionContainer GetFixVersionContainer(string dictionaryId, Config configuration,
			FixVersion fixVersion)
		{
			var customDictFile = GetCustomDictionaryFile(dictionaryId, configuration);
			if (!string.IsNullOrEmpty(customDictFile))
			{
				var isExtensionFile = IsCustomDictionaryUpdate(dictionaryId, configuration);
				if (isExtensionFile)
				{
					return new FixVersionContainer(dictionaryId, fixVersion, DictFiles[fixVersion], customDictFile);
				}

				return new FixVersionContainer(dictionaryId, fixVersion, customDictFile);
			}

			return new FixVersionContainer(dictionaryId, fixVersion, DictFiles[fixVersion]);
		}

		private static string GetCustomDictionaryFile(string dictionaryId, Config configuration)
		{
			return configuration.GetProperty(string.Format(DictionaryFilenamePattern, dictionaryId));
		}

		private static bool IsCustomDictionaryUpdate(string dictionaryId, Config configuration)
		{
			return configuration.GetPropertyAsBoolean(string.Format(DictionaryUpdatePattern, dictionaryId), true);
		}
		private static string GetDefaultDicId(FixVersion fixVersion)
		{
			return fixVersion.MessageVersion.Replace(".", "");
		}
	}
}