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

namespace Epam.FixAntenna.NetCore.FixEngine.Storage.Queue
{
	internal class FileHelper
	{
		private readonly string _filename;

		public FileHelper(string filename)
		{
			_filename = filename;
		}

		public virtual FileHelper CopyToTemporaryFile()
		{
			var tmpFileName = _filename + ".corrupted~";
			if (System.IO.File.Exists(tmpFileName))
			{
				System.IO.File.Delete(tmpFileName);
			}

			System.IO.File.Move(_filename, tmpFileName);
			return this;
		}

		public virtual FileHelper CreateNewFile()
		{
			var emptyFile = new FileInfo(_filename);
			if (emptyFile.Exists)
			{
				throw new Exception("Could not create new file: " + _filename + "(file exist = " + emptyFile.Exists +
									")");
			}

			using (System.IO.File.Create(_filename))
			{
			}

			return this;
		}
	}
}