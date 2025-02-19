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
using System.IO;
using System.Xml;
using NUnit.Framework;

namespace Epam.FixAntenna.Tester
{
	public static class TesterRunner
	{
		/// <summary>
		/// Runs test cases.
		/// </summary>
		/// <param name="path">Path to test cases.</param>
		public static void RunCase(string path)
		{
			XmlReaderSettings settings = new XmlReaderSettings()
			{
				XmlResolver = new XmlUrlResolver(),
				ValidationType = ValidationType.DTD,
				DtdProcessing = DtdProcessing.Parse,
				IgnoreComments = true,
			};

			using (var reader = XmlReader.Create(path, settings))
			using (var handler = new CasesConfigHandler(Path.GetFileNameWithoutExtension(path)))
			{
				ProcessDocument(reader, handler);
				Assert.IsFalse(handler.GetCounter().HasFaults);
			}
		}

		private static void ProcessDocument(XmlReader reader, IDefaultHandler handler)
		{
			while (reader.Read())
			{
				switch (reader.NodeType)
				{
					case XmlNodeType.Element:
						{
							string ns = reader.NamespaceURI;
							string name = reader.Name;
							var attributes = ReadAttributes(reader);
							handler?.StartElement(ns, name, name, attributes);
							break;
						}

					case XmlNodeType.Text:
						{
							string text = reader.Value;
							char[] characters = text.ToCharArray();
							handler?.Characters(characters, 0, characters.Length);
							break;
						}

					case XmlNodeType.EndElement:
						{
							string ns = reader.NamespaceURI;
							string name = reader.Name;
							handler?.EndElement(ns, name, name);
							break;
						}
				}
			}
		}

		private static Attributes ReadAttributes(XmlReader reader)
		{
			if (reader.NodeType != XmlNodeType.Element) throw new InvalidOperationException($"Invalid node type: {reader.NodeType}");

			Attributes attributes = new Attributes();

			if (reader.HasAttributes)
			{
				while (reader.MoveToNextAttribute())
				{
					attributes.Add(reader.Name, reader.Value);
				}
				// Move the reader back to the element node.
				reader.MoveToElement();
			}

			return attributes;
		}
	}
}