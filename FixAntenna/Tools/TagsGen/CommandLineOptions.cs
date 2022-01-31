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
using CommandLine;
using CommandLine.Text;

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Epam.FixAntenna.TagsGen
{
	// ReSharper disable once ClassNeverInstantiated.Global
	internal class CommandLineOptions
	{
		[Option('i', "in", Default = true, Required = true, HelpText = "Input files to be processed.")]
		public IEnumerable<string> InputFiles { get; set; }

		[Option('o', "out", Default = false, Required = true, HelpText = "Path to output directory.")]
		public string OutDir { get; set; }

		[Option('n', "ns", Required = false, HelpText = "Namespace for generated classes.")]
		public string Namespace { get; set; }

		[Option('p', "proj", Required = false, HelpText = "Name for the generated project.")]
		public string ProjectName { get; set; }

		[Option('b', "build", Required = false, HelpText = "Build the generated project, optionally configuration (Release|Debug).")]
		public string Build { get; set; }

		[Option('y', Required = false, Default = false, Hidden = true)]
		public bool Overwrite { get; set; }

		[Option("add-copyright", Required = false, Default = false, Hidden = true)]
		public bool AddCopyright { get; set; }

		[Usage(ApplicationAlias = "TagsGen")]
		public static IEnumerable<Example> Examples
		{
			get
			{
				return new List<Example>() 
				{
					new Example("Generate constants from fixdic44.xml file to 'Generated' folder, with default project name ('Fixdic44')", 
						new CommandLineOptions { InputFiles = new[]{"fixdic44.xml"}, OutDir = "Generated" }),
					new Example("Generate constants from 'fixdic44.xml' and 'fixdic50.xml' files to 'Generated' folder, with provided project name ('Constants')",
						new CommandLineOptions { InputFiles = new[]{"fixdic44.xml", "fixdic50.xml"}, OutDir = "Generated", ProjectName = "Constants"}),
					new Example("Generate constants from 'fixdic44.xml' and 'fixdic50.xml' files to 'Generated' folder, with provided project name ('Constants') and namespace 'TagsGen.Generated' and build with Release configuration",
						new CommandLineOptions { InputFiles = new[]{"fixdic44.xml", "fixdic50.xml"}, OutDir = "Generated", ProjectName = "Constants", Namespace = "TagsGen.Generated", Build = "Release"})
				};
			}
		}
	}
}
