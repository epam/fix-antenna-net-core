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

namespace Epam.FixAntenna.Tester
{
	/// <summary>
	/// XML Element Attributes
	/// </summary>
	public interface IAttributes
	{
		/// <summary>
		/// Returns number of attributes
		/// </summary>
		/// <returns></returns>
		int GetLength();

		/// <summary>
		/// Returns qualified attribute name by index or null if the index is out of range
		/// </summary>
		/// <returns></returns>
		string GetQName(int index);

		/// <summary>
		/// Returns attribute value by index or null if the index is out of range
		/// </summary>
		/// <returns></returns>
		string GetValue(int index);

		/// <summary>
		/// Returns attribute value by qualified attribute name or null if the attribute is not found
		/// </summary>
		/// <returns></returns>
		string GetValue(string name);
	}
}