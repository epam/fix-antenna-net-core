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
using System.Text;
using Epam.FixAntenna.NetCore.Validation.Entities;

namespace Epam.FixAntenna.TagsGen
{
	/// <summary>
	/// Class for storing information about named values collections.
	/// </summary>
	internal class NamedValueSet : EntitySet
	{
		/// <summary>
		/// Overrides base method to add information about referencing TgId.
		/// </summary>
		/// <param name="tabs">Number of tabs to indent.</param>
		/// <returns>Returns StringBuilder object containing string representation of the referenced TagId.</returns>
		public override StringBuilder Generate(int tabs)
		{
			if (Value == null)
				return null;

			var output = new StringBuilder();
			var indent = new string('\t', tabs);
			output.AppendLine(indent + "/// <summary>");
			output.AppendLine(indent + "/// TagId related to this field.");
			output.AppendLine(indent + "/// </summary>");
			output.AppendLine($"{indent}public const int TagId = {Value};");
			return output;
		}

		/// <summary>
		/// Extracts required data from the items collection in the field definition.
		/// </summary>
		/// <param name="items">Items collection for the filed.</param>
		public void AddItems(List<Item> items)
		{
			foreach (var item in items)
			{
				AddNamedValue(item);
			}
		}

		/// <summary>
		/// Extracts required data from the items collection in the field definition.
		/// </summary>
		/// <param name="items">Items collection for the filed.</param>
		public void AddItems(List<object> items)
		{
			foreach (var obj in items)
			{
				if (obj is Item item)
					AddNamedValue(item);
			}
		}

		private void AddNamedValue(Item item)
		{
			var name = Utils.ExtractName(item).Pascalize().SafeDigits();
			var desc = Utils.ExtractDescription(item.Content);

			if (string.IsNullOrEmpty(desc))
				desc = name;
			
			// fix for error CS0542: member names cannot be the same as their enclosing type
			if (name == Name)
			{
				name += "Value";
			}
			
			var namedValue = new Entity { Name = name, Value = item.Val, Description = desc };
			SubEntities.Add(namedValue);
		}
	}
}
