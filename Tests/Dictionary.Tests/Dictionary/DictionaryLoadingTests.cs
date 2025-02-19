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

using System.Runtime.CompilerServices;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.ResourceLoading;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Dictionary;
using Epam.FixAntenna.NetCore.Validation.Entities;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Dictionary.Tests.Dictionary
{
	[TestFixture]
	public class DictionaryLoadingTests
	{
		private readonly DictionaryBuilder _builder = new DictionaryBuilder();

		[Test]
		// Loading embedded resources relies on stack trace information to find all assemblies that
		// participate in the call chain.
		// But the stack trace can be optimized for execution in the Release mode and the calling assembly is not necessary there.
		// Unfortunately this functionality looks like not very good designed and it's better avoid relying on it.
		[MethodImpl(MethodImplOptions.NoOptimization)]
		public void EmbeddedDictionaryLoaded()
		{
			ClassicAssert.DoesNotThrow(() =>
			{
				var fix40 = new FixVersionContainer("myfix40_embedded_resource", FixVersion.Fix40,
					"Loading/EmbeddedResources/base40.xml");
				_builder.BuildDictionary(fix40, false);
			});
		}

		[Test]
		public void OutputDictionaryLoaded()
		{
			ClassicAssert.DoesNotThrow(() =>
			{
				var fix40 = new FixVersionContainer("myfix40_output_resource", FixVersion.Fix40,
					"Loading/OutputResources/base40.xml");
				_builder.BuildDictionary(fix40, false);
			});
		}

		[Test]
		public void OutputDictionaryTakenInsteadOfEmbedded()
		{
			// Loading/LoadingOrder/base40.xml is embedded resource and is not copied to the output dir
			// Loading/LoadingOrder/base40_output.xml is embedded resource and copied to the output dir with the name of base40.xml
			// These files have the same path but contain different titles; test checks that output dictionary is taken
			var fix40 = new FixVersionContainer("myfix40_loading_order_resource", FixVersion.Fix40,
				"Loading/LoadingOrder/base40.xml");
			var dictionary = (Fixdic)_builder.BuildDictionary(fix40, false);

			ClassicAssert.AreEqual("FIX 4.0 Output Resource", dictionary.Title);
		}

		[Test]
		public void DictionaryNotFound()
		{
			ClassicAssert.Throws<ResourceNotFoundException>(() =>
			{
				var fix40 = new FixVersionContainer("myfix40", FixVersion.Fix40,
					"Loading/does_not_exist.xml");
				_builder.BuildDictionary(fix40, false);
			});
		}
	}
}
