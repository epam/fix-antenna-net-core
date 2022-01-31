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

using Epam.FixAntenna.NetCore.Validation.Error;

namespace Epam.FixAntenna.NetCore.Validation
{
	public interface IFixMessageValidator
	{
		/// <summary>
		/// Validates the fix message.
		/// </summary>
		/// <param name="message"> the fix message
		/// </param>
		/// <returns> returns the validation result
		///  </returns>
		FixErrorContainer ValidateFixMessage(Message.FixMessage message);

		/// <summary>
		/// Validates the fix message.
		/// </summary>
		/// <param name="msgType"> the type of message </param>
		/// <param name="message"> the list of fix fields
		/// </param>
		/// <returns> returns the validation result
		///  </returns>
		FixErrorContainer ValidateFixMessage(string msgType, Message.FixMessage message);

		/// <summary>
		/// Gets or sets the content validation flag.
		/// This flag enable/disable the content validation of message.
		/// The content validation works only for header and trailer blocks.
		/// </summary>
		/// <value> the flag </value>
		bool ContentValidation { get; set; }
	}
}