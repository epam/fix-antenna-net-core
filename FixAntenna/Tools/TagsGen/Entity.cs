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

using System.Security;
using System.Text;

namespace Epam.FixAntenna.TagsGen
{
	/// <summary>
	/// Common class for storing data about tags or named values.
	/// </summary>
	internal class Entity
	{
		/// <summary>
		/// Name for named value or TagId for tags.
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// Value for named value or tag. Could be int or string.
		/// </summary>
		public object Value { get; set; }
		/// <summary>
		/// Description for the named value or tag.
		/// </summary>
		public string Description { get; set; }
		/// <summary>
		/// Generates string representation for the entity.
		/// </summary>
		/// <param name="tabs">Number of tabs to indent.</param>
		/// <returns>Returns StringBuilder object containing string representation for the entity.</returns>
		public virtual StringBuilder Generate(int tabs)
		{
			var output = new StringBuilder();
			var indent = new string('\t', tabs);
			var type = Value is int ? "int" : "string";

			output.AppendLine(indent + "/// <summary>");
			output.AppendLine(indent + "/// " + SecurityElement.Escape(Description));
			output.AppendLine(indent + "/// </summary>");
			output.AppendLine(indent + $"public const {type} {Name} = {Utils.Prepare(Value)};");
			return output;
		}
	}
}
