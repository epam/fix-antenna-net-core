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

using System.Collections.Concurrent;

namespace Epam.FixAntenna.NetCore.Common
{
	/// <summary>
	/// Source of the value of a parameter
	/// </summary>
	public enum ParamSource
	{
		Environment,
		Config,
		Default
	}

	/// <summary>
	/// Storage for sources of parameters.
	/// Singleton.
	/// </summary>
	internal sealed class ParamSources
	{
		public const string EnvironmentVariable = "Environment variable";
		public const string FromConfigFile = "From config file";
		public const string Default = "Default";

		private readonly ConcurrentDictionary<string, ParamSource> _paramSources = new ConcurrentDictionary<string, ParamSource>();

		private static readonly ParamSources _instance = new ParamSources();

		internal static ParamSources Instance => _instance;

		private ParamSources()
		{
		}

		/// <summary>
		/// Returns the source of a parameter
		/// </summary>
		/// <param name="paramName"></param>
		/// <param name="sessionId"></param>
		/// <returns></returns>
		public string Get(string paramName, string sessionId = null)
		{
			// 1. lock for session specific
			//
			if (!string.IsNullOrEmpty(sessionId) && _paramSources.TryGetValue($"sessions.{sessionId}.{paramName}", out var sessionParamSource))
			{
				return ParamSourceToString(sessionParamSource);
			}
			// 2. lock for session defaults
			//
			else if (_paramSources.TryGetValue($"sessions.default.{paramName}", out var sessionDefaultParamSource))
			{
				return ParamSourceToString(sessionDefaultParamSource);
			}
			// 3. lock for global
			//
			else if (_paramSources.TryGetValue(paramName, out var globalParamSource))
			{
				return ParamSourceToString(globalParamSource);
			}
			else
			{
				return Default;
			}
		}

		/// <summary>
		/// Sets the parameter source
		/// </summary>
		/// <param name="paramName"></param>
		/// <param name="paramSource"></param>
		/// <param name="sessionId"></param>
		public void Set(string paramName, ParamSource paramSource, string sessionId = null)
		{
			var key = string.IsNullOrEmpty(sessionId) ? paramName : $"sessions.{sessionId}.{paramName}";
			_paramSources[key] = paramSource;
		}

		public static string ParamSourceToString(ParamSource paramSource)
		{
			if (paramSource == ParamSource.Environment)
			{
				return EnvironmentVariable;
			}
			else if (paramSource == ParamSource.Config)
			{
				return FromConfigFile;
			}
			else if (paramSource == ParamSource.Default)
			{
				return Default;
			}
			else
			{
				return "Unknown";
			}
		}
	}
}
