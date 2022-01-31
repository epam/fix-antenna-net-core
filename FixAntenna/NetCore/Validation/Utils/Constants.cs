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

namespace Epam.FixAntenna.NetCore.Validation.Utils
{
	internal class Constants
	{
		/// <summary>
		/// Field BEGIN_CUSTOM_TAG
		/// </summary>
		public const int BeginCustomTag = 5000;

		/// <summary>
		/// Field Header
		/// </summary>
		public const string Smh = "SMH";

		/// <summary>
		/// Field Trailer
		/// </summary>
		public const string Smt = "SMT";

		// Conditional validation constants

		/// <summary>
		/// Field Exist Tag
		/// </summary>
		public const string ExisttagWord = "existtags";

		/// <summary>
		/// Field False
		/// </summary>
		public const string FalseWord = "false";

		/// <summary>
		/// Field Or
		/// </summary>
		public const string OrWord = "or";

		/// <summary>
		/// Field And
		/// </summary>
		public const string AndWord = "and";

		/// <summary>
		/// Field Not
		/// </summary>
		public const string NotWord = "not";

		/// <summary>
		/// Field GROUP_IDENT
		/// </summary>
		public const string GroupIdent = "G$";

		/// <summary>
		/// Field TAG_IDENT
		/// </summary>
		public const string TagIdent = "T$";

		/// <summary>
		/// CRITICAL_TAGS_ORDER_HEADER
		/// </summary>
		public static readonly int[] CriticalTagsOrderHeader = { 8, 9, 35 };

		/// <summary>
		/// Field CRITICAL_TAGS_ORDER_TRAILER
		/// </summary>
		public static readonly int[] CriticalTagsOrderTrailer = { 10 };
	}
}