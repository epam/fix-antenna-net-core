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

using System.Threading.Tasks;
using Epam.FixAntenna.Fixicc.Message;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework;

namespace Epam.FixAntenna.AdminTool.Tests.Smoke.Util
{
	internal static class SmokeUtil
	{
		public static void SendRequest(Request request, IFixSession session)
		{
			var xml = MessageUtils.ToXml(request);
			var list = new FixMessage();
			list.AddTag(212, xml.Length);
			list.AddTag(213, xml);
			session.SendMessage("n", list);
		}

		public static Response GetXmlData(FixMessage fixMessage)
		{
			var xmlDataLen = fixMessage.GetTag(212);
			if (xmlDataLen == null || xmlDataLen.StringValue.Equals("0"))
			{
				return null;
			}
			var xmlData = fixMessage.GetTag(213);
			var xmlString = xmlData.StringValue.Replace('\u0001', '#');

			return (Response) MessageUtils.FromXml(xmlString);
		}

		public static Task CreateTask(System.Action payload)
		{
			return Task.Factory.StartNew(payload, TaskCreationOptions.LongRunning)
				.ContinueWith(t => Assert.Fail(t.Exception?.InnerException?.Message), TaskContinuationOptions.OnlyOnFaulted);
		}
	}
}