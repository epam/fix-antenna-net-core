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
using Epam.FixAntenna.NetCore.FixEngine.Storage.File;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Storage.File
{
	internal class SlicedFileManagerTest
	{
		private FileInfo _file;
		private SlicedFileManager _fileManager;
		private DirectoryInfo _tempDirectory;

		[SetUp]
		public virtual void SetUp()
		{
			_tempDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
			_tempDirectory.Create();

			_file = new FileInfo(Path.Combine(_tempDirectory.FullName, "slicedtest.log"));
			_fileManager = new SlicedFileManager(_file.FullName);
			_fileManager.Initialize();
		}

		[TearDown]
		public virtual void TearDown()
		{
			_tempDirectory.Delete(true);
		}

		[Test]
		public virtual void RecoveryLastFle()
		{
			var fileName = _fileManager.GetFileName();
			var data = "data";
			WriteToFile(fileName, data);
			for (var i = 0; i < 3; i++)
			{
				fileName = _fileManager.GetNextFileName();
				WriteToFile(fileName, data + i);
			}

			_fileManager = new SlicedFileManager(_file.FullName);
			_fileManager.Initialize();
			Assert.AreEqual(4, _fileManager.GetChunkId());
		}

		private void WriteToFile(string file, string data)
		{
			using (var stream = new FileStream(file, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
			using (var writer = new StreamWriter(stream))
			{
				writer.Write(data);
			}
		}
	}
}