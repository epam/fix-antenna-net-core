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
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Helpers;

namespace Epam.FixAntenna.Fixicc.Message
{
	public class MessageUtils
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(MessageUtils));
		private static readonly Dictionary<string, Type> TypeHelperDict = typeof(MessageUtils).Assembly
			.ExportedTypes
			.Where(t => t.GetInterfaces().Contains(typeof(IMessage)))
			.ToDictionary(t => t.Name, t => t);

		public static string ToXml(IMessage request)
		{
			var serializer = new XmlSerializer(request.GetType());
			using (var sww = new Utf8Writer())
			{
				var settings = new XmlWriterSettings
				{
					Encoding = Encoding.UTF8,
					Indent = Log.IsDebugEnabled,
					OmitXmlDeclaration = !Log.IsDebugEnabled
				};
				using (var writer = XmlWriter.Create(sww, settings))
				{
					writer.WriteStartDocument(true);
					var ns = new XmlSerializerNamespaces();
					ns.Add("", "");
					serializer.Serialize(writer, request, ns);
					return sww.ToString();
				}
			}
		}

		public static IMessage FromXml(string responseXML)
		{
			using (var stream = new StringReader(responseXML))
			{
				using (var xmlReader = new XmlTextReader(stream))
				{
					xmlReader.MoveToContent();
					var className = $"{xmlReader.Name.Trim()}";
					var type = TypeHelperDict[className];
					var serializer = new XmlSerializer(type);
					return (IMessage)serializer.Deserialize(xmlReader);
				}
			}
		}

		public static string GetName(IMessage request)
		{
			return request.GetType().Name;
		}
	}
}