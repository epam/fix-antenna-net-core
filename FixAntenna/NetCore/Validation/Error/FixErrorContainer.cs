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

namespace Epam.FixAntenna.NetCore.Validation.Error
{
	public sealed class FixErrorContainer
	{
		public FixErrorContainer()
		{
		}

		public FixErrorContainer(List<FixError> fixErrors)
		{
			Errors = new List<FixError>(fixErrors);
		}

		public List<FixError> Errors { get; } = new List<FixError>();

		public bool IsEmpty => Errors.Count == 0;

		public FixError IsPriorityError //TODO: naming?
		{
			get
			{
				var errorCode = -1;
				FixError priorityError = null;
				for (var i = 0; i < Errors.Count; i++)
				{
					var error = Errors[i];
					var code = error.FixErrorCode.Code;
					if (errorCode == -1)
					{
						errorCode = code;
						priorityError = error;
						continue;
					}

					if (code < errorCode)
					{
						errorCode = code;
						priorityError = error;
					}
				}

				return priorityError;
			}
		}

		public void Add(FixError error)
		{
			if (!Errors.Contains(error))
			{
				Errors.Add(error);
			}
		}

		public void Add(FixErrorContainer fixErrorContainer)
		{
			var errors = fixErrorContainer.Errors;
			for (var i = 0; i < errors.Count; i++)
			{
				Add(errors[i]);
			}
		}

		public void Clear()
		{
			Errors.Clear();
		}
	}
}