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

using System.Text;
using Epam.FixAntenna.NetCore.Validation.Entities;

namespace Epam.FixAntenna.TagsGen
{
	/// <summary>
	/// Class for storing information about tags collections.
	/// </summary>
	internal class TagSet : EntitySet 
	{
		/// <summary>
		/// Af far all tags in the same file for each dictionary the name is Tags.
		/// </summary>
		public TagSet()
		{
			Name = "Tags";
		}

		/// <summary>
		/// Tags collection does not need to produce any additional output, so null.
		/// </summary>
		/// <param name="tabs">Number of tabs to indent.</param>
		/// <returns>Returns null as far as Tags do not need to generate additional elements here.</returns>
		public override StringBuilder Generate(int tabs)
		{
			return null;
		}

		/// <summary>
		/// Extracts required data from field definition and stores in the collection of tags.
		/// </summary>
		/// <param name="fieldDef">Field definition from dictionary.</param>
		public void AddTag(Fielddef fieldDef)
		{
			var description = fieldDef.Descr == null ? fieldDef.Name : Utils.ExtractDescription(fieldDef.Descr.Content);
			var tag = new Entity { Name = fieldDef.Name, Value = fieldDef.Tag, Description = description };
			SubEntities.Add(tag);
		}
	}
}
