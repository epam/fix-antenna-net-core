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

using System.Reflection;
using NUnit.Framework;

namespace Epam.FixAntenna.Common.Utils
{
	internal class EqualUtils
	{
		public static void RefletionEqual(object expected, object actual, string fieldPath = "Field ", 
			BindingFlags bindingFlags = 
				BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
		{
			if (expected == null && actual == null)
				return;

			Assert.IsNotNull(expected);
			Assert.IsNotNull(actual);

			var type = expected.GetType();
			Assert.IsInstanceOf(type, actual);

			foreach (var field in type.GetFields(bindingFlags))
			{
				var fieldType = field.FieldType;
				var expectedValue = field.GetValue(expected);
				var actualValue = field.GetValue(actual);

				if (fieldType.IsValueType)
				{
					var expectedStringValue = expectedValue.ToString();
					var actualStringlValue = actualValue.ToString();
					Assert.AreEqual(expectedStringValue, actualStringlValue, fieldPath + field.Name);
				}
				if (fieldType.IsClass)
					RefletionEqual(expectedValue, actualValue, fieldPath + field.Name + ".");
			}
		}
	}
}