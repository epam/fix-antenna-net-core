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

using Epam.FixAntenna.AdminTool.Commands;
using Epam.FixAntenna.Fixicc.Message;
using NUnit.Framework;

namespace Epam.FixAntenna.AdminTool.Tests.Commands
{
	internal class CommandHandlerTest
	{
		private CommandHandler _commandHandler = new CommandHandler();

		[Test]
		public void GetCanonicalCommandClassName()
		{
			IMessage sessionsList = new SessionsList();
			var className = _commandHandler.GetCanonicalCommandClassName(sessionsList);
			Assert.AreEqual(typeof(SessionsList).Name, className);
		}

		[Test]
		public void ReplaceInvalidCharacters()
		{
			var result = _commandHandler.ReplaceInvalidCharacters("4\u0001Hello&");
			Assert.AreEqual("4&amp;#01;Hello&amp;", result);
		}
	}
}