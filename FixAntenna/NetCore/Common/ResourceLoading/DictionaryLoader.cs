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
// limitations under the License.using System;

using Epam.FixAntenna.NetCore.Configuration;
using System;
using System.IO;
using System.Xml;
using System.Xml.Xsl;

namespace Epam.FixAntenna.NetCore.Common.ResourceLoading
{
	internal class DictionaryLoader : CurrentDirResourceLoader
	{
		public DictionaryLoader(string innerPath, ResourceLoader nextLoader) : base(innerPath, nextLoader)
		{
		}

		/// <inheritdoc />
		public override Stream LoadResource(string resourceName)
		{
			var stream = base.LoadResource(resourceName);

			if (!IsQfix(stream))
			{
				return stream;
			}

			try
			{
				var transformation = base.LoadResource("qfix2fixdic.xsl");

				var convertedStream = new MemoryStream();
				var arguments = new XsltArgumentList();
				arguments.AddParam("dict-id", string.Empty, DictId(resourceName));
				var trans = new XslCompiledTransform();
				using (var transformationReader = XmlReader.Create(transformation))
				using (var dictionaryReader = XmlReader.Create(stream))
				{
					trans.Load(transformationReader);
					trans.Transform(dictionaryReader, arguments, convertedStream);
				}

				convertedStream.Seek(0L, SeekOrigin.Begin);

				return convertedStream;
			}
			catch (Exception e)
			{
				Log.Trace("Dictionary file loader failed to load: " + resourceName, e);
				return NextLoader.LoadResource(resourceName);
			}
		}

		private bool IsQfix(Stream stream)
		{
			try
			{
				using (var reader = XmlReader.Create(stream))
				{
					if (!reader.ReadToFollowing("fix"))
					{
						return false;
					}

					var fixVersionType = reader.GetAttribute("type");
					var major = reader.GetAttribute("major");
					var minor = reader.GetAttribute("minor");

					int val;

					if (!string.IsNullOrEmpty(fixVersionType) && !(fixVersionType == "FIX" || fixVersionType == "FIXT"))
					{
						return false;
					}

					if (!int.TryParse(major, out val) || !int.TryParse(minor, out val))
					{
						return false;
					}
				}
			}
			catch
			{
				return false;
			}
			finally
			{
				stream.Seek(0L, SeekOrigin.Begin);
			}

			return true;
		}

		private string DictId(string resourceName)
		{
			var fileName = Path.GetFileName(resourceName);
			return FixVersionContainerFactory.StandardIds.TryGetValue(fileName, out string id) ?
				id :
				Path.GetFileNameWithoutExtension(fileName).ToUpperInvariant();
		}
	}
}
