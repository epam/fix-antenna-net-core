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
using System.Text;
using System.Text.RegularExpressions;

namespace Epam.FixAntenna.NetCore.Helpers
{
	public static class StringHelper
	{
		public static string[] Split(this string self, string delimeter, bool removeEmptyEntries)
		{
			var splitArray = self.Split(delimeter.ToCharArray(),
				removeEmptyEntries ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);

			return splitArray;
		}

		public static string NewString(byte[] bytes)
		{
			return NewString(bytes, 0, bytes.Length);
		}

		public static string NewString(byte[] bytes, int index, int count)
		{
			return Encoding.UTF8.GetString(bytes, index, count);
		}

		public static byte[] AsByteArray(this string self)
		{
			return AsByteArray(Encoding.UTF8, self);
		}

		public static byte[] AsByteArray(this string self, Encoding encoding)
		{
			return AsByteArray(encoding, self);
		}

		private static byte[] AsByteArray(Encoding encoding, string s)
		{
			var bytes = new byte[encoding.GetByteCount(s)];
			encoding.GetBytes(s, 0, s.Length, bytes, 0);
			return bytes;
		}

		public static string UrlEncode(this string self)
		{
			return self?.TrimEnd('=').Replace('+', '-').Replace('/', '_');
		}

		public static string UrlDecode(this string self)
		{
			var src = self.Replace('_', '/').Replace('-', '+');
			switch (self.Length % 4)
			{
				case 2: src += "=="; break;
				case 3: src += "="; break;
			}

			return src;
		}

		public static string ReplaceAll(this string self, string regex, string replacement)
		{
			return Regex.Replace(self, regex, replacement);
		}
	}

	internal class Utf8Writer : StringWriter
	{
		public override Encoding Encoding => Encoding.UTF8;
	}
}