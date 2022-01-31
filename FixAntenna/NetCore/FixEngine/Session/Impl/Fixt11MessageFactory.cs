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

using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.Impl
{
	/// <summary>
	/// The FIXT 1.1 message factory implementation.
	/// </summary>
	internal class Fixt11MessageFactory : Fix44MessageFactory
	{
		/// <inheritdoc />
		public override byte[] GetLoginHeader()
		{
			var part1
				= new TagValue(Tags.DefaultApplVerID, SessionParameters.AppVersion.FixtVersion)
				.ToString()
				.AsByteArray();

			var part2 = base.GetLoginHeader();
			var buffer = new ByteBuffer(part1.Length + SeparatorLength + part2.Length);

			buffer.Add(part1);
			buffer.Add(Separator);
			buffer.Add(part2);
			return buffer.GetByteArray();
		}

		/// <inheritdoc />
		public override void CompleteLogin(FixMessage content)
		{
			content.AddTag(Tags.DefaultApplVerID, SessionParameters.AppVersion.FixtVersion);
			base.CompleteLogin(content);
		}
	}
}