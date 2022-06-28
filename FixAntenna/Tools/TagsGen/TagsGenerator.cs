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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Xsl;

using Epam.FixAntenna.NetCore.Validation.Entities;

namespace Epam.FixAntenna.TagsGen
{
	/// <summary>
	/// Main class for Tags Generator.
	/// </summary>
	internal class TagsGenerator
	{
		private readonly string _fileName;
		private readonly string _namespace;
		private readonly string _outputDirectory;
		private readonly string _header;
		
		private string _dictionaryName;
		private readonly TagSet _tags = new TagSet();
		private readonly Dictionary<string, NamedValueSet> _enums = new Dictionary<string, NamedValueSet>();

		/// <summary>
		/// Just initialize some fields.
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="ns"></param>
		/// <param name="outDir"></param>
		public TagsGenerator(string fileName, string ns, string outDir, string header)
		{
			_fileName = fileName;
			_namespace = ns;
			_outputDirectory = outDir;
			_header = header;
		}

		/// <summary>
		/// Main processing routine for the dictionary.
		/// </summary>
		public void ProcessFile()
		{
			if (!File.Exists(_fileName))
				throw new FileNotFoundException("Dictionary not found", _fileName);

			Fixdic dictionary;
			try
			{
				dictionary = ParseDictionary();
			}
			catch (InvalidOperationException e)
			{
				throw new InvalidDictionaryException(_fileName, e);
			}

			// extract FIX version from the dictionary name
			_dictionaryName = dictionary.Id.Pascalize();

			// creating directory with name from previous step
			var filesDir = Path.Combine(_outputDirectory, _dictionaryName);
			Directory.CreateDirectory(filesDir);

			_tags.Description = "List of tags, defined in the dictionary " + Utils.GetDictionaryDescription(dictionary);

			// looking through the filed definitions to extract tags and named values
			foreach (var fieldDefinition in dictionary.Fielddic.Fielddef)
			{
				_tags.AddTag(fieldDefinition);
				
				if (!fieldDefinition.Item.Any())
					continue;

				// adding named values if defined in the field definition
				var items = new NamedValueSet
				{
					Name = fieldDefinition.Name, 
					Value = fieldDefinition.Tag, 
					Description = fieldDefinition.Descr == null ? fieldDefinition.Name : Utils.ExtractDescription(fieldDefinition.Descr.Content)
				};
				items.AddItems(fieldDefinition.Item);
				_enums.Add(fieldDefinition.Name, items);
			}

			// looking through valueBlockDefinitions and extract values
			foreach (var valblockdef in dictionary.Fielddic.Valblockdef)
			{
				if (_enums.TryGetValue(valblockdef.Id, out var en))
				{
					en.AddItems(valblockdef.ItemOrRangeOrDescr);
				}
				else
				{
					var items = new NamedValueSet { Name = valblockdef.Id, Description = valblockdef.Name };
					items.AddItems(valblockdef.ItemOrRangeOrDescr);
					_enums.Add(valblockdef.Id, items);
				}
			}

			// Exporting tags to the source file
			WriteFile(_tags, _header);
			foreach (var namedValueSet in _enums.Values)
			{
				// Exporting named values to the source file
				WriteFile(namedValueSet, _header);
			}
		}

		/// <summary>
		/// Deserializes the provided dictionary to the Fixdic class object.
		/// </summary>
		/// <returns>Returns deserialized dictionary.</returns>
		private Fixdic ParseDictionary()
		{
			var serializer = new XmlSerializer(typeof(Fixdic), new XmlRootAttribute
			{
				ElementName = "fixdic",
				Namespace = "http://www.b2bits.com/FIXProtocol"
			});

			using var fileStream = new FileStream(_fileName, FileMode.Open, FileAccess.Read);
			
			if (!IsQfix(fileStream))
			{
				return (Fixdic)serializer.Deserialize(fileStream);
			}

			var transformation = typeof(TagsGenerator).Assembly.GetManifestResourceStream("Epam.FixAntenna.TagsGen.qfix2fixdic.xsl");
			using var convertedStream = new MemoryStream();
			var arguments = new XsltArgumentList();
			arguments.AddParam("dict-id", string.Empty, DictId(_fileName));
			var trans = new XslCompiledTransform();
			using (var transformationReader = XmlReader.Create(transformation))
			using (var dictionaryReader = XmlReader.Create(fileStream))
			{
				trans.Load(transformationReader);
				trans.Transform(dictionaryReader, arguments, convertedStream);
			}

			convertedStream.Seek(0L, SeekOrigin.Begin);

			return (Fixdic)serializer.Deserialize(convertedStream);
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
			return Path.GetFileNameWithoutExtension(fileName).ToUpperInvariant();
		}

		private void WriteFile(EntitySet data, string prefix)
		{
			var tagsFile = new FileInfo(Path.Combine(_outputDirectory, _dictionaryName, data.Name + ".cs"));
			using (var file = tagsFile.CreateText())
			{
				if (!string.IsNullOrEmpty(prefix))
				{
					file.WriteLine(prefix);
				}

				file.WriteLine("using System.CodeDom.Compiler;");
				file.WriteLine("// ReSharper disable once CheckNamespace");
				file.WriteLine("namespace " +
											(string.IsNullOrEmpty(_namespace) ? _dictionaryName : _namespace + "." + _dictionaryName));
				file.WriteLine("{");
				file.WriteLine("\t/// <summary>");
				file.WriteLine("\t/// " + SecurityElement.Escape(data.Description));
				file.WriteLine("\t/// </summary>");
				file.WriteLine("\t[GeneratedCode(\"TagsGen\", \"" + Assembly.GetExecutingAssembly().GetName().Version + "\")]");
				file.WriteLine("\tpublic class " + data.Name);
				file.WriteLine("\t{");
				// generate TagId reference for named values
				file.Write(data.Generate(2));
				// create tags or named values here
				foreach (var record in data.SubEntities)
				{
					file.Write(record.Generate(2));
				}

				// end of create tags
				file.WriteLine("\t}");
				file.WriteLine("}");
				file.Flush();
			}
		}
	}
}
