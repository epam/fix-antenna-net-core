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

namespace Epam.FixAntenna.NetCore.Validation
{
	/// <summary>
	/// The message validator interface.
	/// </summary>
	public interface IMessageValidator
	{
		/// <summary>
		/// Validates the message.
		/// </summary>
		/// <param name="message"> the fix message
		/// </param>
		/// <returns> the result contains the errors collection if it exists.
		///  </returns>
		IValidationResult Validate(Message.FixMessage message);

		/// <summary>
		/// Validates the message.
		/// </summary>
		/// <param name="message"> the ValidationResult object to avoid GC </param>
		/// <param name="result"> the fix message
		/// </param>
		/// <returns> the result contains the errors collection if it exists.
		///  </returns>
		void Validate(Message.FixMessage message, IValidationResult result);

		/// <summary>
		/// Validates the content of message.
		/// </summary>
		/// <param name="msgType"> the message type </param>
		/// <param name="content"> the content of message
		///  </param>
		IValidationResult ValidateContent(string msgType, Message.FixMessage content);

		/// <summary>
		/// Sets the content validation flag.
		/// </summary>
		/// <value>
		///   the content validation flag
		/// </value>
		bool ContentValidation { get; set; }
	}
}