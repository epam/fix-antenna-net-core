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

namespace Epam.FixAntenna.NetCore.FixEngine
{
	public enum ChangesType
	{
		/// <summary>
		/// Use this type to update existed tags in header and trailer.
		/// </summary>
		UpdateSmhAndSmt,

		/// <summary>
		/// Use this type to add header's and trailer's tags to the message.
		///
		/// Note: make sure that message doesn't contains header's and trailer's tags.
		/// In other case tags could be duplicated.
		/// </summary>
		AddSmhAndSmt,

		/// <summary>
		/// Use this type to update existed tags in header and trailer, except SenderCompID(49).
		/// This type is similar to UPDATE_SMH_AND_SMT but doesn't update SenderCompID(49) tag.
		/// </summary>
		UpdateSmhAndSmtDonotUpdateSndr,

		/// <summary>
		/// Use this type to make sure that all tags in header and trailer
		/// have only one instance in message.
		/// </summary>
		DeleteAndAddSmhAndSmt,

		/// <summary>
		/// Update Sub and Location Ids only if CompId is absent. In other case lease them as they are in message.
		/// </summary>
		UpdateSmhAndSmtExceptCompids
	}
}