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

using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Common.ResourceLoading;
using Epam.FixAntenna.NetCore.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.Util
{
	public class SessionParametersBuilder
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(SessionParametersBuilder));
		public const string SessionTypeProp = "sessionType";
		private const string AcceptorType = "acceptor";
		private const string InitiatorType = "initiator";

		private static IPropertiesFilter _acceptorFilter = new SessionTypeFilter(AcceptorType);
		private static IPropertiesFilter _initiatorFilter = new SessionTypeFilter(InitiatorType);

		// SessionParameters List build service

		/// <summary>
		/// Build list of SessionParameters using default configuration file.
		/// </summary>
		public static IDictionary<string, SessionParameters> BuildSessionParametersList()
		{
			var p = new Properties(Config.GlobalConfiguration.Properties);
			return ConstructListFromProperties(p);
		}

		/// <summary>
		/// Build list of SessionParameters using input file name.
		/// </summary>
		/// <param name="file"> configuration file </param>
		/// <returns> list of SessionParameters </returns>
		public static IDictionary<string, SessionParameters> BuildSessionParametersList(string file)
		{
			var properties = GetPropertiesFromFile(file);
			return ConstructListFromProperties(properties);
		}

		/// <summary>
		/// Build list of acceptor's SessionParameters using default configuration file.
		/// </summary>
		public static IDictionary<string, SessionParameters> BuildAcceptorSessionParametersList()
		{
			var p = new Properties(Config.GlobalConfiguration.Properties);
			return ConstructListFromProperties(p, _acceptorFilter);
		}

		/// <summary>
		/// Build list of initiator's SessionParameters using default configuration file.
		/// </summary>
		public static IDictionary<string, SessionParameters> BuildInitiatorSessionParametersList()
		{
			var p = new Properties(Config.GlobalConfiguration.Properties);
			return ConstructListFromProperties(p, _initiatorFilter);
		}


		/// <summary>
		/// Build list of acceptor's SessionParameters using input file name.
		/// </summary>
		/// <param name="file"> configuration file </param>
		/// <returns> list of SessionParameters </returns>
		public static IDictionary<string, SessionParameters> BuildAcceptorSessionParametersList(string file)
		{
			var properties = GetPropertiesFromFile(file);
			return ConstructListFromProperties(properties, _acceptorFilter);
		}

		/// <summary>
		/// Build list of acceptor's SessionParameters using input file name.
		/// </summary>
		/// <param name="file"> configuration file </param>
		/// <returns> list of SessionParameters </returns>
		public static IDictionary<string, SessionParameters> BuildInitiatorSessionParametersList(string file)
		{
			var properties = GetPropertiesFromFile(file);
			return ConstructListFromProperties(properties, _initiatorFilter);
		}

		private static IDictionary<string, SessionParameters> ConstructListFromProperties(Properties properties)
		{
			return ConstructListFromProperties(properties, new PropertiesFilterAnonymousInnerClass());
		}

		internal class PropertiesFilterAnonymousInnerClass : IPropertiesFilter
		{
			public bool FilterSession(string sessionId, Properties props)
			{
				return true;
			}
		}

		private static IDictionary<string, SessionParameters> ConstructListFromProperties(Properties properties, IPropertiesFilter filter)
		{
			var sessionIdsParam = properties.GetProperty("sessionIDs");
			var sessionParamsMap = new Dictionary<string, SessionParameters>();
			if (string.IsNullOrWhiteSpace(sessionIdsParam))
			{
				return sessionParamsMap;
			}
			var propertiesMap = new Dictionary<string, Properties>();
			var defaultProps = GetSessionProperties(properties, "default");
			foreach (var splittedId in sessionIdsParam.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
			{
				var sessionId = splittedId.Trim();
				var props = new Properties();
				props.PutAll(defaultProps);
				props.PutAll(GetSessionProperties(properties, sessionId));
				propertiesMap[sessionId] = props;
			}

			if (propertiesMap.Count == 0)
			{
				propertiesMap[string.Empty] = properties;
			}

			foreach (var mapEntry in propertiesMap)
			{
				var sessionId = mapEntry.Key;
				var sessionProps = mapEntry.Value;
				if (filter.FilterSession(sessionId, sessionProps))
				{
					sessionParamsMap[sessionId] = ConstructFromProperties(sessionProps);
				}
			}
			return sessionParamsMap;
		}


		private static SessionParameters ConstructFromProperties(Properties properties, string sessionId)
		{
			var defaultProps = GetSessionProperties(properties, "default");
			var props = new Properties();
			props.PutAll(defaultProps);
			props.PutAll(GetSessionProperties(properties, sessionId));
			return ConstructFromProperties(props);
		}

		private static Properties GetSessionProperties(Properties data, string sessionId)
		{
			var pr = new Properties();
			var sessionPrefix = $"sessions.{sessionId}.";
			var sessionPrefixLength = sessionPrefix.Length;
			
			foreach (KeyValuePair<string,string> entry in data)
			{
				if (entry.Key.StartsWith(sessionPrefix, StringComparison.OrdinalIgnoreCase))
				{
					pr.Put(entry.Key.Substring(sessionPrefixLength), entry.Value);
				}
			}

			// check out the corresponding environment variables
			var evs = Environment.GetEnvironmentVariables();
			var evPrefix = Properties.PrepareEvPrefix(sessionId);
			var evPrefixLength = evPrefix.Length;
			foreach (string evName in evs.Keys)
			{
				if (evName.StartsWith(evPrefix, StringComparison.OrdinalIgnoreCase))
				{
					var paramName = evName.Substring(evPrefixLength);
					pr.Put(paramName, evs[evName].ToString());
					ParamSources.Instance.Set(paramName, ParamSource.Environment, sessionId);
				}
			}

			pr.Put("sessionID", sessionId);
			return pr;
		}

		/// <summary>
		/// Build SessionParameters using default configuration file name and session id.
		/// </summary>
		/// <param name="sessionId"> specific session id </param>
		/// <returns> session parameters </returns>
		public static SessionParameters BuildSessionParameters(string sessionId)
		{
			var properties = new Properties(Config.GlobalConfiguration.Properties);
			return ConstructFromProperties(properties, sessionId);
		}

		/// <summary>
		/// Build SessionParameters using input file name and session id.
		/// </summary>
		/// <param name="file"> configuration file </param>
		/// <param name="sessionId">   specific session id </param>
		/// <returns> session parameters </returns>
		public static SessionParameters BuildSessionParameters(string file, string sessionId)
		{
			var properties = GetPropertiesFromFile(file);
			return ConstructFromProperties(properties, sessionId);
		}

		private static Properties GetPropertiesFromFile(string file)
		{
			if (!string.IsNullOrEmpty(file))
			{
				try
				{
					if (file.EndsWith(".xml", StringComparison.Ordinal))
					{
						var xmlConfig = new XmlDocument();
						//xmlConfig.setDelimiterParsingDisabled(true);
						xmlConfig.Load(file);
						return GetProperties(xmlConfig);
					}
				}
				catch (Exception e)
				{
					var s = "Unable to load config from xml: " + file;
					Log.Fatal(s, e);
					throw;
				}
				if (file.EndsWith(".properties", StringComparison.Ordinal))
				{
					using (var inStream = ResourceLoader.DefaultLoader.LoadResource(file))
					{
						try // load default properties
						{
							return new Properties(inStream);
						}
						catch (IOException ex)
						{
							var s = "Unable to load session properties: " + file;
							Log.Fatal(s, ex);
							throw;
						}
					}
				}
			}
			throw new ArgumentException("Wrong config file: " + file);
		}

		private static SessionParameters ConstructFromProperties(Properties properties)
		{
			Config config = null;
			try
			{
				config = (Config) Config.GlobalConfiguration.Clone();
				//overload properties
				config.AddAllProperties(properties.ToDictionary());
			}
			catch (Exception e)
			{
				if (Log.IsDebugEnabled)
				{
					Log.Warn("Can't clone global configuration", e);
				}
				else
				{
					Log.Warn("Can't clone global configuration. " + e.Message);
				}
				config = new Config(properties.ToDictionary());
			}
			var @params = new SessionParameters(config);
			@params.FromProperties(properties.ToDictionary());
			return @params;
		}

		private static Properties GetProperties(XmlDocument config)
		{
			var props = new Properties();
			foreach (XmlNode node in config.DocumentElement.SelectNodes("*"))
			{
				ProcessNode("", node, props);
			}
			return props;
		}

		private static void ProcessNode(string prefix, XmlNode node, Properties props)
		{
			if (node == null)
			{
				return;
			}
			var attr = node.Attributes?["id"];
			var key = attr?.Value ?? node.Name;

			if (node.SelectNodes("*").Count == 0)
			{
				var value = node.Value ?? node.InnerText;
				if (value != null)
				{
					props.Put(prefix + (prefix.Length == 0 ? "" : ".") + key, value);
				}
			}
			else
			{
				prefix = prefix + (prefix.Length == 0 ? "" : ".") + key;
				foreach (XmlNode childNode in node.SelectNodes("*"))
				{
					ProcessNode(prefix, childNode, props);
				}
			}
		}

		internal interface IPropertiesFilter
		{
			bool FilterSession(string sessionId, Properties props);
		}

		private sealed class SessionTypeFilter : IPropertiesFilter
		{
			private readonly string _sessionType;

			public SessionTypeFilter(string sessionType)
			{
				_sessionType = sessionType;
			}

			public bool FilterSession(string sessionId, Properties props)
			{
				var st = props.GetProperty(SessionTypeProp);
				return st == null || _sessionType.Equals(st, StringComparison.OrdinalIgnoreCase);
			}
		}
	}
}