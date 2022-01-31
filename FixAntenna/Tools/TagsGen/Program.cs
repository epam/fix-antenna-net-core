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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;

namespace Epam.FixAntenna.TagsGen
{
	internal static class Program
	{
		/// <summary>
		/// Start here.
		/// </summary>
		/// <param name="args">Provided command line arguments.</param>
		/// <returns>Exit code.</returns>
		public static int Main(string[] args)
		{
			return Parser.Default.ParseArguments<CommandLineOptions>(args).MapResult(Generate, HandleParseError);
		}

		/// <summary>
		/// Generates source files and compile it.
		/// </summary>
		/// <param name="options">Parsed command line arguments.</param>
		/// <returns>Exit code.</returns>
		private static int Generate(CommandLineOptions options)
		{
			var csprojFile = PrepareProject(options);
			if (csprojFile == null)	
				return -1; // cannot prepare project

			var header = string.Empty;
			if (options.AddCopyright)
			{
				header =
					@"// Copyright (c) 2021 EPAM Systems
//
// Licensed under the Apache License, Version 2.0 (the ""License"");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an ""AS IS"" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License." + Environment.NewLine;
			}

			var tasks = new List<Task>();
			foreach (var inputFile in Utils.GetInputFiles(options))
			{
				var gen = new TagsGenerator(inputFile, options.Namespace, options.OutDir, header);
				Console.WriteLine($"Processing '{inputFile}'...");
				var task = Task.Run(gen.ProcessFile);
				tasks.Add(task);
			}

			try
			{
				Task.WaitAll(tasks.ToArray());
			}
			catch (AggregateException e)
			{
				Console.WriteLine("\nThe following errors occurred when generating the source files:");
				foreach (var ie in e.InnerExceptions)
				{
					switch (ie)
					{
						case FileNotFoundException fnf:
							Console.WriteLine($"Dictionary file not found: {fnf.FileName}, skipping.");
							break;
						case InvalidDictionaryException ide:
							Console.WriteLine($"Cannot parse file: {ide.FileName}, skipping.");
							Console.WriteLine($"\tError: {ide.Message}");
							break;
						default:
							Console.WriteLine("\n---\n{0}", ie);
							break;
					}
				}
			}

			try
			{
				if (tasks.Any(t => t.IsCompletedSuccessfully))
				{
					BuildLibrary(options, csprojFile);
					Console.WriteLine("TagsGen completed successfully.");
					return 0;
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
			return -1;
		}

		/// <summary>
		/// Displays Help information, Version information, or shows argument parsing errors.
		/// </summary>
		/// <param name="errors">Parsing errors.</param>
		/// <returns>Returns 0 if user requested Help or Version information, or -1 otherwise.</returns>
		[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
		[SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "<Pending>")]
		private static int HandleParseError(IEnumerable<Error> errors)
		{
			return errors.IsVersion() || errors.IsHelp() ? 0 : -1;
		}

		/// <summary>
		/// Creates project from template with provided project name and source namespace.
		/// </summary>
		/// <param name="options"></param>
		/// <returns>Return full path to created project, if project prepared successfully, or null otherwise.</returns>
		private static string PrepareProject(CommandLineOptions options)
		{
			return !DirectoryReady(options) ? null : CreateProjectTemplate(options);
		}

		/// <summary>
		/// Checks if the directory ready for files: not exists or empty.
		/// </summary>
		/// <param name="options">Command line options.</param>
		/// <returns>Returns true, if the directory is empty or not exists.</returns>
		private static bool DirectoryReady(CommandLineOptions options)
		{
			if (Directory.Exists(options.OutDir) && Directory.EnumerateFiles(options.OutDir).Any())
			{
				// check if we have 'overwrite' argument
				if (options.Overwrite)
				{
					Directory.Delete(options.OutDir, true);
				}
				else
				{
					var confirmed = false;
					// ask user to confirm overwriting
					Console.WriteLine(
						$"Directory '{options.OutDir}' already exists and not empty. All existing files will be deleted. Proceed? (Y/n)");
					while (!confirmed)
					{
						var key = Console.ReadKey(true).Key;
						switch (key)
						{
							case ConsoleKey.Y:
							case ConsoleKey.Enter:
								Directory.Delete(options.OutDir, true);
								confirmed = true;
								break;
							case ConsoleKey.Escape:
							case ConsoleKey.N:
								Console.WriteLine("Target directory not empty. File processing cancelled.");
								return false;
						}
					}
				}
			}

			Directory.CreateDirectory(options.OutDir);
			Console.WriteLine("Output directory: " + new DirectoryInfo(options.OutDir).FullName);
			return true;
		}

		/// <summary>
		/// Returns project name form provided command line argument or from first input file name.
		/// </summary>
		/// <param name="options">Parsed command line arguments.</param>
		/// <returns>Project name to be generated.</returns>
		private static string GetTargetProjectName(CommandLineOptions options)
		{
			if (!string.IsNullOrEmpty(options.ProjectName))
			{
				return options.ProjectName;
			}

			var first = Utils.GetInputFiles(options).First();
			var fi = new FileInfo(first);
			return Path.GetFileNameWithoutExtension(fi.Name);
		}

		/// <summary>
		/// Creates csproj file with provided parameters.
		/// </summary>
		/// <param name="options">Parsed command line arguments.</param>
		/// <returns>Full path to created project file.</returns>
		private static string CreateProjectTemplate(CommandLineOptions options)
		{
			var projectName = GetTargetProjectName(options);
			var projFile = Path.Combine(options.OutDir, projectName + ".csproj");
			using (var proj = new StreamWriter(projFile))
			{
				proj.WriteLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
				proj.WriteLine("\t<PropertyGroup>");
				proj.WriteLine("\t\t<TargetFramework>netstandard2.0</TargetFramework>");
				if (!string.IsNullOrEmpty(options.Namespace))
				{
					proj.WriteLine($"\t\t<RootNamespace>{options.Namespace}.{projectName}</RootNamespace>");
				}
				proj.WriteLine("\t</PropertyGroup>");
				proj.WriteLine("</Project>");

				Console.WriteLine("Project name: " + projectName);
				if (!string.IsNullOrEmpty(options.Namespace))
					Console.WriteLine("Namespace: " + options.Namespace);

				return projFile;
			}
		}

		/// <summary>
		/// Builds the generated sources if user requested this.
		/// </summary>
		/// <param name="options">Parsed command line options.</param>
		/// <param name="projFile">Full path to project file.</param>
		private static void BuildLibrary(CommandLineOptions options, string projFile)
		{
			if (string.IsNullOrEmpty(options.Build))
				return;

			Console.WriteLine(Environment.NewLine + "Building generated sources...");

			var psi = new ProcessStartInfo
			{
				FileName = "dotnet",
				Arguments = " build " + projFile + " -c " + options.Build,
				UseShellExecute = false,
				CreateNoWindow = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};

			using (var pr = new Process { StartInfo = psi })
			{
				pr.OutputDataReceived += (s, ev) =>
				{
					if (!string.IsNullOrWhiteSpace(ev.Data))
					{
						Console.WriteLine(ev.Data);
					}
				};

				pr.ErrorDataReceived += (s, err) =>
				{
					if (!string.IsNullOrWhiteSpace(err.Data))
					{
						Console.WriteLine("Error: " + err.Data);
					}
				};

				pr.EnableRaisingEvents = true;
				pr.Start();
				pr.BeginOutputReadLine();
				pr.BeginErrorReadLine();

				pr.WaitForExit();
			}
		}
	}
}
