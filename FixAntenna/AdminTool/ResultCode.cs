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
	internal sealed class ResultCode
	{
		public static readonly ResultCode OperationSuccess =
			new ResultCode("OPERATION_SUCCESS", Fixicc.Message.ResultCode.Item0);

		public static readonly ResultCode OperationNotImplemented =
			new ResultCode("OPERATION_NOT_IMPLEMENTED", Fixicc.Message.ResultCode.Item1);

		public static readonly ResultCode ResultUnknownSession =
			new ResultCode("RESULT_UNKNOWN_SESSION", Fixicc.Message.ResultCode.Item3);

		public static readonly ResultCode OperationUnknownError =
			new ResultCode("OPERATION_UNKNOWN_ERROR", Fixicc.Message.ResultCode.Item6);

		public static readonly ResultCode OperationReject =
			new ResultCode("OPERATION_REJECT", Fixicc.Message.ResultCode.Item7);

		public static readonly ResultCode OperationInvalidArgument =
			new ResultCode("OPERATION_INVALID_ARGUMENT", Fixicc.Message.ResultCode.Item9);

		private ResultCode(string name, Fixicc.Message.ResultCode code)
		{
			Name = name;
			Code = code;
		}

		public Fixicc.Message.ResultCode Code { get; }

		public string Name { get; }

		public override string ToString()
		{
			return Name;
		}
	}
}