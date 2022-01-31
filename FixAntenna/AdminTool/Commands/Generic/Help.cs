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
using System.Text;
using System.Xml;
using Epam.FixAntenna.Fixicc.Message;
using Epam.FixAntenna.NetCore.Common.ResourceLoading;

namespace Epam.FixAntenna.AdminTool.Commands.Generic
{
	/// <summary>
	/// The Help command.
	/// Send the message to specified session.
	/// </summary>
	internal class Help : Command
	{
		public override void Execute()
		{
			Log.Debug("Execute HelpCommand Command");
			try
			{
				var document = new XmlDocument();
				document.Load(ResourceLoader.DefaultLoader.LoadResource("Resources.HelpData.xml"));
				var commandsElement = document.DocumentElement;

				var helpData = new HelpData();
				helpData.FIXAdminProtocolVersion = "1.0";
				foreach (var request in ParseSupportedRequest(commandsElement))
				{
					helpData.SupportedRequest.Add(request);
				}
				var response = new Response();
				response.HelpData = helpData;
				SendResponseSuccess(response);
			}
			catch (Exception e)
			{
				Log.Error("unknown error", e);
				SendError(e.Message);
			}
		}

		private ICollection<string> ParseSupportedRequest(XmlElement commandsElement)
		{
			var supportedRequest = new HashSet<string>();
			var nodeList = commandsElement.ChildNodes;
			for (var i = 0; i < nodeList.Count; i++)
			{
				var request = nodeList.Item(i);
				if (request.NodeType == XmlNodeType.Text)
				{
					continue;
				}
				supportedRequest.Add(ParseRequest(request));
			}
			return supportedRequest;
		}

		private string ParseRequest(XmlNode request)
		{
			var sb = new StringBuilder(256);
			sb.Append("<").Append(request.Name).Append(">");
			if (request.HasChildNodes)
			{
				var nodeList = request.ChildNodes;
				for (var i = 0; i < nodeList.Count; i++)
				{
					var childNode = nodeList.Item(i);
					if (childNode.NodeType == XmlNodeType.Text)
					{
						var value = GetTextNodeValue(childNode);
						if (!string.IsNullOrWhiteSpace(value))
						{
							sb.Append(value);
						}
						continue;
					}
					ParseChildNodes(sb, childNode);
				}
			}
			return sb.Append("</").Append(request.Name).Append(">").ToString();
		}

		private string GetTextNodeValue(XmlNode childNode)
		{
			return childNode.Value?.Trim();
		}

		private void ParseChildNodes(StringBuilder sb, XmlNode childNode)
		{
			var nodeValues = ParseRequest(childNode);
			if (!string.IsNullOrWhiteSpace(nodeValues))
			{
				sb.Append(ParseRequest(childNode));
			}
		}
	}
}