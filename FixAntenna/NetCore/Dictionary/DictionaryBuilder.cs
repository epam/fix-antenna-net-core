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
using System.Xml;
using System.Xml.Serialization;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Common.ResourceLoading;
using Epam.FixAntenna.NetCore.Common.Xml;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Validation.Entities;

namespace Epam.FixAntenna.NetCore.Dictionary
{
	/// <summary>
	/// Dictionary builder.
	/// </summary>
	internal class DictionaryBuilder : IDictionaryBuilder
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(DictionaryBuilder));

		internal static IDictionary<FixVersionContainer, IType>
			FixdicMap = new Dictionary<FixVersionContainer, IType>();

		/// <inheritdoc />
		public virtual IType BuildDictionary(FixVersionContainer version, bool replaceData)
		{
			var type = FixdicMap.GetValueOrDefault(version);

			if (type == null || replaceData)
			{
				// preload standard
				var dictionaryFile = version.DictionaryFile;
				type = Mapping(null, dictionaryFile, version.FixVersion, false);

				var extensionFile = version.ExtensionFile;
				if (!string.IsNullOrEmpty(extensionFile))
				{
					type = Mapping(type, extensionFile, version.FixVersion, false);
				}

				FixdicMap[version] = type;
			}
			else
			{
				Log.Trace("Use cached dictionary: " + version);
			}

			return type;
		}

		public virtual IType UpdateDictionary(FixVersionContainer version)
		{
			var type = FixdicMap[version];
			var dictionaryFile = version.DictionaryFile;
			if (!string.IsNullOrEmpty(dictionaryFile))
			{
				type = Mapping(type, dictionaryFile, version.FixVersion, false);
			}

			var extensionFile = version.ExtensionFile;
			if (!string.IsNullOrEmpty(extensionFile))
			{
				type = Mapping(type, extensionFile, version.FixVersion, false);
			}

			FixdicMap[version] = type;
			return type;
		}

		public virtual bool LoadDescriptions { get; set; }

		private IType Mapping(IType dictionary, string dictFile, FixVersion fixVersion, bool replaceData)
		{
			var fixdic = dictionary;

			var root = new XmlRootAttribute
			{
				ElementName = "fixdic",
				IsNullable = false
			};

			using (var stream = ResourceLoader.DictionaryLoader.LoadResource(dictFile))
			{
				if (fixdic == null)
				{
					if (Log.IsDebugEnabled)
					{
						Log.Debug("Load dictionary: " + dictFile);
					}

					var dicType = FixVersion.Fixt11 == fixVersion ? typeof(FixTDic) : typeof(Fixdic);
					var serializer = new XmlSerializer(dicType, root);

					using (var reader = new XmlTextReader(stream))
					{
						reader.Namespaces = false;
						fixdic = (IType)serializer.Deserialize(reader);
						if (!LoadDescriptions)
						{
							var dic = fixdic as Fixdic;
							var r = Object.ReferenceEquals(dic, fixdic);
							if (dic != null)
							{
								dic.Msgdic.Blockdef.ForEach(blockdef =>
								{
									blockdef.FieldOrDescrOrGroup.RemoveAll(fdg => fdg is Descr);
								});
							}
						}
					}
				}
				else
				{
					var readerSettings = new XmlReaderSettings
					{
						DtdProcessing = DtdProcessing.Parse,
						MaxCharactersFromEntities = 1024
					};

					using (var reader = XmlReader.Create(stream, readerSettings))
					{
						var serializer = new XmlSerializer(fixdic.GetType(), root);
						fixdic = (IType)serializer.Deserialize(reader);
					}
				}
			}

			return fixdic;
		}

		public virtual void CleanCache()
		{
			FixdicMap.Clear();
		}
	}
}