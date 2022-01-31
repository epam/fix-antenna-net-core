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

using System.Resources;

namespace Epam.FixAntenna.NetCore.Validation.Error.Resource
{
	internal sealed class ResourceHelper
	{
		private static readonly ResourceManager ResourceManager = new ResourceManager(typeof(ValidationMessages));

		private ResourceHelper()
		{
		}

		public static string GetStringMessage(string key, long sequenceNumber, string messageType, int? tag)
		{
			var tagValue = "N/A";
			if (tag != null)
			{
				tagValue = tag.ToString();
			}

			return string.Format(ResourceManager.GetString(key), sequenceNumber, messageType, tagValue);
		}

		public static string GetStringMessage(string key)
		{
			return ResourceManager.GetString(key);
		}
	}
}