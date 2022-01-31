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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Epam.FixAntenna.NetCore.FixEngine.Storage.File
{
	internal class SlicedFileManager
	{
		private readonly string _dir;
		private readonly string _fileExtension;
		private readonly string _fileNameParent;
		private readonly string _filePrefix;
		protected internal int ChunkId = 1;

		internal string NumerationPattern = "\\.([0-9]{1,10})\\.";

		public SlicedFileManager(string fileName)
		{
			_fileNameParent = fileName;
			var file = new FileInfo(fileName);
			_dir = file.DirectoryName;
			var name = file.Name;
			var startExtension = name.LastIndexOf('.');
			_fileExtension = startExtension > 0 ? name.Substring(startExtension + 1) : ""; // +1 for skip dot
			_filePrefix = startExtension > 0 ? name.Substring(0, startExtension) : name;
		}

		public virtual void Initialize()
		{
			var list = new DirectoryInfo(_dir).GetFileSystemInfos()
				.Where(x => ShouldSelect(x.Name, _filePrefix, _fileExtension)).ToList();
			ChunkId = list.Any() ? GetLastFile(list) : 1;
		}

		public virtual string GetNextFileName()
		{
			return GetFileName(++ChunkId);
		}

		public virtual int GetChunkId()
		{
			return ChunkId;
		}

		public virtual string GetFileNameParent()
		{
			return _fileNameParent;
		}

		public virtual string GetFileName(int chunkId)
		{
			if (_fileExtension.Length > 0)
			{
				return _fileNameParent.Substring(0, _fileNameParent.Length - _fileExtension.Length) +
						FormatChunkId(chunkId) + '.' + _fileExtension;
			}

			return _fileNameParent + '.' + FormatChunkId(chunkId);
		}

		private bool ShouldSelect(string fileName, string filePrefix, string fileExtension)
		{
			var pattern = Regex.Replace(filePrefix, "\\.", "\\\\.") + NumerationPattern + fileExtension + "$";
			return Regex.IsMatch(fileName, pattern);
		}

		public virtual int GetLastFile(List<FileSystemInfo> files)
		{
			return files.Select(GetChunkNo).Max();
		}

		public virtual int GetChunkNo(FileSystemInfo file)
		{
			var regex = new Regex(Regex.Replace(_filePrefix, "\\.", "\\\\.") + NumerationPattern + _fileExtension +
								"$");
			var res = regex.Match(file.Name);
			return int.Parse(res.Groups[1].Value);
		}

		public virtual string GetFileName()
		{
			return GetFileName(ChunkId);
		}

		private string FormatChunkId(int chunkId)
		{
			return $"{chunkId:D3}";
		}
	}
}