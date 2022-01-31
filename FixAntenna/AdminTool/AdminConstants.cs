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

namespace Epam.FixAntenna.AdminTool
{
	/// <summary>
	/// The common constants.
	/// </summary>
	internal sealed class AdminConstants
	{
		/// <summary>
		/// Tag that contains component time zone. TimeZone format "+|- HH:MM, e.g. +03:00 or -03:00".
		/// </summary>
		public const int TimezoneTag = 10003;

		/// <summary>
		/// Tag that contains version of admin protocol. Example: 2.0.12
		/// </summary>
		public const int AdminProtocolVersionTag = 10004;

		/// <summary>
		/// The length of raw tag data.
		/// </summary>
		public const int XmlDataLenTag = 212;

		/// <summary>
		/// The raw tag for xml data.
		/// </summary>
		public const int XmlDataTag = 213;

		/// <summary>
		/// The  admin message type.
		/// </summary>
		public const string MessageType = "n";

		public const string SendMessageDelimiter = "&#01;";

		/// <summary>
		/// Package where situated command.
		/// </summary>
		public static string DefaultCommandPackage = "Epam.FixAntenna.AdminTool.Commands";
	}
}