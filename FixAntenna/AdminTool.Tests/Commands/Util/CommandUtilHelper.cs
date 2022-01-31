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

using Epam.FixAntenna.Fixicc.Message;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.AdminTool.Tests.Commands.Util
{
	internal static class CommandUtilHelper
	{
		public static Response GetXmlData(FixMessage fixMessage)
		{
			var xmlDataLen = fixMessage.GetTag(212);
			if (xmlDataLen == null || xmlDataLen.StringValue.Equals("0"))
			{
				return null;
			}
			var xmlData = fixMessage.GetTag(213).StringValue;
			var xmlString = xmlData.Replace("\u0001", AdminConstants.SendMessageDelimiter);

			return (Response) MessageUtils.FromXml(xmlString);
		}

		/// <summary>
		/// method building FIX field with request
		/// </summary>
		/// <param name="request"> </param>
		/// <returns> FixMessage </returns>
		public static FixMessage BuildFixMessage(Request request)
		{
			var xml = MessageUtils.ToXml(request);
			var list = new FixMessage();
			list.AddTag(35, "n");
			list.AddTag(212, xml.Length);
			list.AddTag(213, xml);
			return list;
		}
	}
}