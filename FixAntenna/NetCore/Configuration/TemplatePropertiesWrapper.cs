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
using System.Collections.Immutable;
using System.Linq;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Helpers;

namespace Epam.FixAntenna.NetCore.Configuration
{
	/// <summary>
	/// Wraps <see cref="Properties"/> for handle templates like key=${value}.
	/// </summary>
	internal sealed class TemplatePropertiesWrapper : ICloneable
	{
		private const string TemplStart = "${";
		private const string TemplEnd = "}";
		private const string TemplNegation = "!";
		private static readonly int TemplNegationLen = TemplNegation.Length;
		private readonly Dictionary<string, (string, string)> _properties = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase);

		public TemplatePropertiesWrapper()
		{
		}

		public TemplatePropertiesWrapper(Properties properties) : this(properties?.ToDictionary())
		{
		}

		public TemplatePropertiesWrapper(IDictionary<string, string> properties)
		{
			if (properties == null)
			{
				throw new ArgumentNullException(nameof(properties));
			}

			foreach (var prop in properties)
			{
				_properties[prop.Key] = (prop.Key, prop.Value);
			}
		}

		private TemplatePropertiesWrapper(IDictionary<string, (string, string)> properties)
		{
			if (properties == null)
			{
				throw new ArgumentNullException(nameof(properties));
			}

			foreach (var prop in properties)
			{
				_properties[prop.Key] = prop.Value;
			}
		}

		public object Clone()
		{
			var cloned = new TemplatePropertiesWrapper(_properties);
			return cloned;
		}

		public void Put(string key, string value)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			_properties[key.ToLowerInvariant()] = (key, value);
		}

		/// <summary>
		/// Gets value by key.
		/// If value is template methods replace it by template value.
		/// If template value doesn't exist it will be replace by defaultValue .
		/// </summary>
		/// <returns> String </returns>
		public string GetProperty(string key, string defaultValue = null)
		{
			var evValue = Properties.ReadEnvironmentVariable(key);
			if (!string.IsNullOrWhiteSpace(evValue))
			{
				ParamSources.Instance.Set(key, ParamSource.Environment);
				return evValue;
			}
			else if (_properties.TryGetValue(key.ToLowerInvariant(), out var value))
			{
				ParamSources.Instance.Set(key, ParamSource.Config);
				return GetTemplateProperty(value.Item2);
			}
			else
			{
				ParamSources.Instance.Set(key, ParamSource.Default);
				return defaultValue;
			}
		}

		public void Clear()
		{
			_properties.Clear();
		}

		public IDictionary<string, string> GetAllProperties()
		{
			return _properties.ToImmutableDictionary(k => k.Value.Item1, v => v.Value.Item2);
		}

		public ISet<string> GetKeys()
		{
			return _properties.Values.Select(i => i.Item1).ToImmutableHashSet();
		}

		private string GetTemplateProperty(string value)
		{
			var start = 0;
			while (true)
			{
				// try to find TEMPL_START - TEMPL_END pair
				start = value.IndexOf(TemplStart, start, StringComparison.Ordinal);
				if (start != -1)
				{
					var end = value.IndexOf(TemplEnd, start + TemplStart.Length, StringComparison.Ordinal);
					if (end != -1)
					{
						// try to find TEMPL_NEGATION prefix
						if (start >= TemplNegationLen && TemplNegation.Equals(
								value.Substring(start - TemplNegationLen, start - (start - TemplNegationLen))))
						{
							// template negation found
							// remove negation label only
							value = value.Substring(0, start - TemplNegation.Length) + value.Substring(start);
							start = end + TemplEnd.Length - TemplNegation.Length;
						}
						else
						{
							// template subst
							var name = value.Substring(start + TemplStart.Length, end - start - TemplStart.Length);
							var templVal = Environment.GetEnvironmentVariable(name);
							if (string.IsNullOrEmpty(templVal))
							{
								templVal = _properties.TryGetValue(name.ToLowerInvariant(), out (string, string) v) ? v.Item2 : string.Empty;
							}

							value = value.Substring(0, start) + templVal + value.Substring(end + TemplEnd.Length);
							start += templVal.Length;
						}
					}
					else
					{
						break;
					}
				}
				else
				{
					break;
				}
			}

			return value;
		}

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

			var that = (TemplatePropertiesWrapper)o;

			if (_properties == that._properties)
			{
				return true;
			}

			return _properties.Count == that._properties.Count && !_properties.Except(that._properties).Any();
		}

		public override int GetHashCode()
		{
			return _properties != null ? _properties.GetHashCode() : 0;
		}

		public bool Exists(string propertyName)
		{
			return _properties.ContainsKey(propertyName);
		}
	}
}