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
using System.Xml;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.ResourceLoading;

namespace Epam.FixAntenna.NetCore.Configuration
{
	public sealed class FixVersionContainer
	{
		public string DictionaryId { get; private set; }

		public FixVersion FixVersion { get; private set; }

		public string DictionaryFile { get; private set; }

		public string ExtensionFile { get; private set; }

		private FixVersionContainer()
		{
		}

		public FixVersionContainer(string dictionaryId, FixVersion fixVersion, string dictionaryFile, string extensionFile = null)
		{
			DictionaryId = dictionaryId ?? throw new ArgumentException("dictionaryID should be unique and not null");
			FixVersion = fixVersion;
			DictionaryFile = dictionaryFile;
			ExtensionFile = extensionFile;
		}

		public static FixVersionContainer GetFixVersionContainer(FixVersion fixVersion)
		{
			return FixVersionContainerFactory.GetFixVersionContainer(fixVersion);
		}

		public static FixVersionContainer GetFixVersionContainer(string dictionaryID, Config configuration)
		{
			return FixVersionContainerFactory.GetFixVersionContainer(dictionaryID, configuration);
		}

		public static FixVersionContainer GetFixVersionContainer(Config configuration, FixVersion fixVersion)
		{
			return FixVersionContainerFactory.GetFixVersionContainer(configuration, fixVersion);
		}

		public static FixVersionContainer GetFixVersionContainer(string dictionaryId, FixVersion fixVersion)
		{
			return FixVersionContainerFactory.GetFixVersionContainer(dictionaryId, fixVersion);
		}

		public static FixVersionContainer GetFixVersionContainer(string dictionaryId, Config configuration, FixVersion fixVersion)
		{
			return FixVersionContainerFactory.GetFixVersionContainer(dictionaryId, configuration, fixVersion);
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}

			if (!(o is FixVersionContainer that))
			{
				return false;
			}

			return DictionaryId.Equals(that.DictionaryId);
		}

		public override int GetHashCode()
		{
			var result = DictionaryId.GetHashCode();
			//        result = 31 * result + fixVersion.hashCode();
			//        result = 31 * result + dictionaryFile.hashCode();
			//        result = 31 * result + (extensionFile != null ? extensionFile.hashCode() : 0);
			return result;
		}

		public override string ToString()
		{
			return "FIXVersionContainer{" +
					"dictionaryID='" + DictionaryId + '\'' +
					", fixVersion=" + FixVersion +
					", dictionaryFile='" + DictionaryFile + '\'' +
					", extensionFile='" + ExtensionFile + '\'' +
					'}';
		}

		/// <summary>
		/// Make sure that <c>FIXVersionContainers</c> at least based on same fix version
		/// </summary>
		public bool Similar(FixVersionContainer that)
		{
			if (this.Equals(that))
			{
				return true;
			}

			return that != null && FixVersion.Equals(that.FixVersion);
		}

		public static Builder NewBuilder() //TODO: used only in tests. Should it be here?
		{
			return new Builder(new FixVersionContainer());
		}

		public class Builder
		{
			private const string DictionaryHeaderName = "fixdic";
			private const string DictionaryFixVersionParamName = "fixversion";
			private const string DictionaryFixVersionFixt11Value = "T1.1";
			private const string FixVersionPrefix = "FIX.";
			private readonly FixVersionContainer _container;

			public Builder(FixVersionContainer container)
			{
				_container = container;
			}

			public Builder SetDictionaryFile(string dictionaryFile) //TODO: used only in tests. Should it be here?
			{
				_container.DictionaryFile = dictionaryFile;
				return this;
			}

			public FixVersionContainer Build()
			{
				if (_container.DictionaryFile == null)
				{
					throw new InvalidOperationException("'dictionaryFile' is null, it is mandatory parameter");
				}

				if (_container.FixVersion == null)
				{
					_container.FixVersion = ExtractFixVersion(_container.DictionaryFile);
				}

				if (_container.DictionaryId == null)
				{
					_container.DictionaryId = _container.FixVersion.MessageVersion.Replace(".", "");
				}

				return _container;
			}

			private FixVersion ExtractFixVersion(string dictFile)
			{
				try
				{
					using (var @is = ResourceLoader.DictionaryLoader.LoadResource(dictFile))
					{
						using (var reader = XmlReader.Create(@is))
						{
							if (reader.ReadToDescendant(DictionaryHeaderName) &&
								reader.MoveToAttribute(DictionaryFixVersionParamName))
							{
								var fixVersion = reader.ReadContentAsString();
								return DictionaryFixVersionFixt11Value.Equals(fixVersion) ? FixVersion.Fixt11 : FixVersion.GetInstanceByMessageVersion(FixVersionPrefix + fixVersion);
							}
						}
					}
				}
				catch (Exception e)
				{
					throw new Exception("Unable to process dictionary file '" + dictFile, e);
				}

				throw new ArgumentException("File '" + dictFile + "' does not have fix version");
			}
		}
	}
}