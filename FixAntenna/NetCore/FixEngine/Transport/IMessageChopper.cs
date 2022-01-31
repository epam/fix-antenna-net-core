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

using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Transport
{
	internal interface IMessageChopper
	{
		bool IsMessageGarbled { get; }

		/// <summary>
		/// Returns error of last read message if message is garbled or null otherwise.
		/// </summary>
		/// <value> the instance of error enum. </value>
		GarbledMessageError Error { get; }

		/// <summary>
		/// Returns error position of last read message if message is garbled or -1 otherwise.
		/// </summary>
		/// <value> the error message string. </value>
		int ErrorPosition { get; }

		void ReadMessage(MsgBuf buf);

		/// <param name="parserListener"> </param>
		void SetUserParserListener(IFixParserListener parserListener);

		long MessageReadTimeInTicks { get; }

		void Reset();

		/// <summary>
		///
		/// </summary>
		RawFixUtil.IRawTags RawTags { get; set; }
	}

	internal static class MessageChopperFields
	{
		public const string EofReadError = "End of File read";
		public const string ReadError = "Read message error";
		public const string MessageIsTooLongError = "FIX message exceeded max size";
		public const string RawDataLengthIsTooBigError = "Raw data length value is too big";
	}
}