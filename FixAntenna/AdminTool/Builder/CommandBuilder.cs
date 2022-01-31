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
using System.IO;
using System.Reflection;
using Epam.FixAntenna.AdminTool.Commands;

namespace Epam.FixAntenna.AdminTool.Builder
{
	/// <summary>
	/// Provides ability to build the command instance.
	/// </summary>
	internal class CommandBuilder
	{
		private static readonly CommandBuilder Instance = new CommandBuilder();
		protected internal readonly Dictionary<MapKey, Type> ClassCommandCash = new Dictionary<MapKey, Type>();

		private CommandBuilder()
		{
		}

		public static CommandBuilder GetInstance()
		{
			return Instance;
		}

		/// <summary>
		/// Creates the command instance.
		/// </summary>
		/// <param name="commandName"> the command name </param>
		/// <param name="externalPackage"> the package where command situated
		///  </param>
		public virtual Command BuildCommand(string commandName, string externalPackage)
		{
			var commandClass = FindCommandClass(commandName, externalPackage);
			try
			{
				return (Command) Activator.CreateInstance(commandClass);
			}
			catch (TypeLoadException e)
			{
				throw new AdminToolException(e);
			}
			catch (TargetInvocationException e)
			{
				throw new AdminToolException(e);
			}
		}

		/// <summary>
		/// Finds the command in command cash, if command does not exist
		/// find in externalPackage.
		/// </summary>
		/// <param name="name">            the  command name </param>
		/// <param name="externalPackage"> the package were command exist
		/// </param>
		/// <returns> Class </returns>
		private Type FindCommandClass(string name, string externalPackage)
		{
			Type commandClass = null;
			if (!string.IsNullOrWhiteSpace(externalPackage))
			{
				var mapKey = new MapKey { CommandName = name, PackageName = externalPackage };
				if (!ClassCommandCash.TryGetValue(mapKey, out commandClass))
				{
					commandClass = GetCommandClass(externalPackage, name);
				}
				if (commandClass == null)
				{
					throw new ArgumentException();
				}
				ClassCommandCash[mapKey] = commandClass;
			}

			// no class in cash
			if (commandClass == null)
			{
				// try get from default package
				var mapKey = new MapKey { CommandName = name, PackageName = AdminConstants.DefaultCommandPackage };
				if (!ClassCommandCash.TryGetValue(mapKey, out commandClass))
				{
					commandClass = GetCommandClass(AdminConstants.DefaultCommandPackage, name);
					if (commandClass == null)
					{
					   throw new ArgumentException();
					}
					mapKey.PackageName = AdminConstants.DefaultCommandPackage;
					ClassCommandCash[mapKey] = commandClass;
				}
			}
			return commandClass;
		}

		/// <summary>
		/// Returns class by short name.
		/// </summary>
		/// <param name="nameOfPackage"> the name of package. </param>
		/// <param name="className">     the short name of class. </param>
		/// <returns> Instance of Class or null if class  with input shortname not found.. </returns>
		public static Type GetCommandClass(string nameOfPackage, string className)
		{
			// nameOfPackage consist of namespace and assembly name: 'namespace,assembly'
			try
			{
				var parts = nameOfPackage.Split(',');
				var typeName = parts.Length > 1
					? $"{parts[0]}.{className},{parts[1]}"
					: $"{parts[0]}.{className}";

				return Type.GetType(typeName);
			}
			catch (IOException e)
			{
				throw new AdminToolException(e);
			}
			catch (TypeLoadException e)
			{
				throw new AdminToolException(e);
			}
		}

		/// <summary>
		/// Map key bean
		/// </summary>
		protected internal class MapKey
		{
			internal string PackageName;
			internal string CommandName;

			public override bool Equals(object o)
			{
				if (this == o)
				{
					return true;
				}
				if (o == null || GetType() != o.GetType())
				{
					return false;
				}

				var mapKey = (MapKey) o;
				if (!CommandName?.Equals(mapKey.CommandName, StringComparison.OrdinalIgnoreCase) ?? mapKey.CommandName != null)
				{
					return false;
				}
				if (!PackageName?.Equals(mapKey.PackageName, StringComparison.OrdinalIgnoreCase) ?? mapKey.PackageName != null)
				{
					return false;
				}

				return true;
			}

			public override int GetHashCode()
			{
				var result = PackageName != null ? PackageName.GetHashCode() : 0;
				result = 31 * result + CommandName != null ? CommandName.GetHashCode() : 0;
				return result;
			}
		}
	}
}