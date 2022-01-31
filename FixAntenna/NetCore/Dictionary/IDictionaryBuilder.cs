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

using Epam.FixAntenna.NetCore.Common.Xml;
using Epam.FixAntenna.NetCore.Configuration;

namespace Epam.FixAntenna.NetCore.Dictionary
{
	/// <summary>
	/// Build of instance of Dictionary.
	/// This builder can be used for build custom fix dictionary and registered fix dictionary.
	/// </summary>
	internal interface IDictionaryBuilder
	{
		/// <summary>
		/// Build dictionary by input version of FIX protocol
		/// </summary>
		/// <param name="version">            Version of FIX protocol </param>
		/// <param name="replaceData">        if flag is set to false, the data will be add to
		///                           standard dictionary, otherwise the data will be replaced. </param>
		/// <returns> instance of dictionary </returns>
		IType BuildDictionary(FixVersionContainer version, bool replaceData);

		IType UpdateDictionary(FixVersionContainer version);
	}
}