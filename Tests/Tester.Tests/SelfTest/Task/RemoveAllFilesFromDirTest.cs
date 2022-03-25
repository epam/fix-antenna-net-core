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
using Epam.FixAntenna.Tester.Task;
using NUnit.Framework;

namespace Epam.FixAntenna.Tester.SelfTest
{
	[TestFixture]
	public class RemoveAllFilesFromDirTest
	{
		internal RemoveAllFilesFromDir RemoveAllFilesFromDir;

		[Test]
		public virtual void TestDoTaskOnNonExistentDir()
		{
			try
			{
				RemoveAllFilesFromDir = new RemoveAllFilesFromDir();
				IDictionary<string, string> map = new Dictionary<string, string>();
				map["logs"] = "123412352315215";
				RemoveAllFilesFromDir.Init(map, new CustomConcurrentDictionary<string, object>());
				RemoveAllFilesFromDir.DoTask();
			}
			catch (Exception)
			{
				Assert.Fail();
			}
		}
	}
}