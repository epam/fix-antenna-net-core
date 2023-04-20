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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Common.ResourceLoading;

namespace Epam.FixAntenna.NetCore.Common
{
	internal sealed class Properties : IEnumerable
	{
		internal const string SessionIdKey = "sessionID";
		internal const string DefaultSessionId = "default";

		private static readonly ILog Log = LogFactory.GetLog(typeof(Properties));
		private ConcurrentDictionary<string, string> _props = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		private readonly Properties _defaults;

		public Properties() : this ((Properties)null) { }

		public Properties(Stream stream) : this(ReadStream(stream)) { }

		public Properties(Properties defaults)
		{
			_defaults = defaults;
		}

		public Properties(Dictionary<string, string> init)
		{
			_props = new ConcurrentDictionary<string, string>(init, StringComparer.OrdinalIgnoreCase);
		}

		public string GetProperty(string key)
		{
			var val = _props.GetValueOrDefault(key);
			return val == null && _defaults != null ? _defaults.GetProperty(key) : val;
		}

		public string GetProperty(string key, string defaultValue)
		{
			var val = GetProperty(key);
			return val ?? defaultValue;
		}

		public void SetProperty(string key, string value)
		{
			Put(key, value);
		}

		public void Put(string key, string value)
		{
			_props[key] = value;
		}

		public void PutAll(Properties props)
		{
			foreach (KeyValuePair<string, string> property in props)
			{
				_props[property.Key] = property.Value;
			}
		}

		public IDictionary<string, string> ToDictionary()
		{
			return _props.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);
		}

		#region Load
		public static Properties FromFile(string path)
		{
			using (var inStream = ResourceLoader.DefaultLoader.LoadResource(path))
			{
				return new Properties(inStream);
			}
		}

		public void Load(Stream inStream)
		{
			var loaded = ReadStream(inStream);
			_props = new ConcurrentDictionary<string, string>(loaded, StringComparer.OrdinalIgnoreCase);
		}

		public static string PrepareEvPrefix(string sessionId = null)
		{
			var sessionPrefix = sessionId != null ? $"sessions__{sessionId}__" : string.Empty;
			return $"FANET_{sessionPrefix}";
		}

		public static string ReadEnvironmentVariable(string key, string sessionId = null)
		{
			var ev = $"{PrepareEvPrefix(sessionId)}{key.Replace(".", "__")}";
			return Environment.GetEnvironmentVariable(ev);
	}

		private static Dictionary<string, string> ReadStream(Stream stream)
		{
			var loaded = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			using (var reader = new StreamReader(stream))
			{
				while (reader.Peek() > 0)
				{
					var line = reader.ReadLine();
					if (IsMeaningLine(line))
					{
						var eqInd = line.IndexOf('=');
						var key = line.Substring(0, eqInd).Trim(new []{' ', '"', '\''});
						var value = line.Substring(eqInd + 1).Trim(new[] { ' ', '"', '\'' });
						var ev = ReadEnvironmentVariable(key);
						if (!string.IsNullOrWhiteSpace(ev))
						{
							loaded[key] = ev;
							ParamSources.Instance.Set(key, ParamSource.Environment);
						}
						else
						{
							loaded[key] = value;
							ParamSources.Instance.Set(key, ParamSource.Config);
						}
					}
				}
			}

			return loaded;
		}

		private static bool IsMeaningLine(string line)
		{
			if (string.IsNullOrWhiteSpace(line)) return false;
			switch (line[0])
			{
				case ';':
				case '#':
				case '\'':
					return false;
				default:
					return line.Contains("=");
			}
		}
		#endregion

		#region Utils
		public string GetNotEmptyProperty(string key)
		{
			var propValue = _props.GetValueOrDefault(key);
			if (string.IsNullOrEmpty(propValue))
			{
				throw new ArgumentException("Can't extract properties " + key);
			}
			return propValue;
		}

		public int? GetNotEmptyIntegerProperty(string key)
		{
			string propValue = GetNotEmptyProperty(key);
			int? intValue = null;
			try
			{
				intValue = int.Parse(propValue);
			}
			catch (FormatException)
			{
				Log.Error("Can't parse to integer properties " + key);
				throw;
			}
			return intValue;
		}
		#endregion

		#region IEnumerable
		public IEnumerator GetEnumerator()
		{
			return _props.GetEnumerator();
		}
		#endregion
	}
}