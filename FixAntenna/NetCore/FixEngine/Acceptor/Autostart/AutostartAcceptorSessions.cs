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
using System.Linq;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.FixEngine.Storage;
using Epam.FixAntenna.NetCore.FixEngine.Transport;

namespace Epam.FixAntenna.NetCore.FixEngine.Acceptor.Autostart
{
	/// <summary>
	/// This is helper class, provides functionality to
	/// checks if session is autostart.
	/// User can't configure the startup sessions in
	/// engine.properties or default.properties files.
	/// </summary>
	internal class AutostartAcceptorSessions
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(AutostartAcceptorSessions));

		protected internal readonly IDictionary<string, AutostartSessionDetails> Map = new Dictionary<string, AutostartSessionDetails>();
		protected internal IConfiguredSessionRegister ConfiguredSessionRegister;
		protected internal const string Prefix = "autostart.acceptor";

		/// <summary>
		/// Creates the <c>AutostartAcceptorSessions</c>.
		/// </summary>
		public AutostartAcceptorSessions(Config config, IConfiguredSessionRegister configuredSessionRegister)
		{
			ConfiguredSessionRegister = configuredSessionRegister;
			var targetIds = config.GetProperty(Prefix + ".targetIds");
			if (!string.IsNullOrEmpty(targetIds))
			{
				var targets = targetIds.Split(new[] { ',', ' ', ';' });
				foreach (var targetId in targets)
				{
					Map[targetId] = new AutostartSessionDetails(config, targetId);
				}
			}
		}

		/// <summary>
		/// Checks if session is startup.
		/// </summary>
		/// <param name="sessionParameters"> the session parameters </param>
		/// <returns> true if it is </returns>
		public virtual bool IsAutostartSession(SessionParameters sessionParameters)
		{
			var asdetails = Map.GetValueOrDefault(sessionParameters.SenderCompId);
			return asdetails != null
					&& asdetails.AllowedIp(sessionParameters.Host)
					&& asdetails.AllowedUser(sessionParameters.IncomingUserName, sessionParameters.IncomingPassword);
		}

		public virtual bool IsAdminSession(SessionParameters sessionParameters)
		{
			var asdetails = Map.GetValueOrDefault(sessionParameters.SenderCompId);
			return asdetails != null;
		}

		/// <summary>
		/// Gets the fix session listener.
		/// </summary>
		/// <param name="sessionParameters"> the session parameters </param>
		/// <exception cref="InvalidOperationException"> if session listener can't be created. </exception>
		public virtual IFixServerListener GetFixServerListener(SessionParameters sessionParameters)
		{
			var asdetails = Map.GetValueOrDefault(sessionParameters.SenderCompId);
			try
			{
				var listener = (IFixServerListener) Activator.CreateInstance(asdetails.Clazz);
				if (listener is IFixAdminSessionListener)
				{
					((IFixAdminSessionListener) listener).SetSessionRegister(ConfiguredSessionRegister);
				}
				return listener;
			}
			catch (Exception e)
			{
				throw new InvalidOperationException("Cannot instantiate FIXServerListener class" + asdetails.Clazz.FullName, e);
			}
		}

		public virtual AutostartSessionDetails GetSessionDetails(SessionParameters sessionParameters)
		{
			var asdetails = Map.GetValueOrDefault(sessionParameters.SenderCompId);
			if (asdetails != null && asdetails.AllowedIp(sessionParameters.Host) && asdetails.AllowedUser(sessionParameters.IncomingUserName, sessionParameters.IncomingPassword))
			{
				return asdetails;
			}
			return null;
		}

		public virtual bool IsAcceptedAdminSession(SessionParameters sessionParameters)
		{

			if (!IsAdminSession(sessionParameters))
			{
				throw new ArgumentException();
			}

			var asdetails = Map.GetValueOrDefault(sessionParameters.SenderCompId);
			if (asdetails != null)
			{
				if (Log.IsDebugEnabled)
				{
					Log.Debug("Detected connection of autostart session: " + GetSessionId(sessionParameters));
				}

				var sessionUsername = sessionParameters.IncomingUserName;
				var sesionPassword = sessionParameters.IncomingPassword;
				if (!asdetails.AllowedUser(sessionUsername, sesionPassword))
				{
					Log.Warn("Username/password for session " + GetSessionId(sessionParameters) + " is different from expected.");
					return false;
				}

				if (!asdetails.AllowedIp(sessionParameters.Host))
				{
					Log.Warn("Connection from " + sessionParameters.Host + " not allowed for session " + GetSessionId(sessionParameters) + ".");
					return false;
				}

				return true;
			}
			else
			{
				return false;
			}
		}

		private string GetSessionId(SessionParameters sessionParameters)
		{
			return sessionParameters.SenderCompId + "-" + sessionParameters.TargetCompId;
		}

		/// <summary>
		/// Autostart session bean.
		/// </summary>
		internal class AutostartSessionDetails
		{
			internal string TargetCompId;
			internal string Password;
			internal string Login;
			internal string Ip;
			internal SubnetUtils[] IpMasks;
			internal Type Clazz;
			internal StorageType StorageType = StorageType.Transient;

			public AutostartSessionDetails(Config configuration, string targetCompId)
			{
				Login = configuration.GetProperty(Prefix + "." + targetCompId + ".login", "admin");
				Password = configuration.GetProperty(Prefix + "." + targetCompId + ".password", "admin");
				Ip = configuration.GetProperty(Prefix + "." + targetCompId + ".ip", "*");
				try
				{
					IpMasks = SubnetUtils.ParseIpMasks(Ip);
				}
				catch (Exception e)
				{
					throw new ArgumentException(Prefix + "." + targetCompId + ".ip property is invalid", e);
				}
				try
				{
					Clazz = Type.GetType(configuration.GetProperty(Prefix + "." + targetCompId + ".fixServerListener"));
				}
				catch (TypeLoadException e)
				{
					throw new ArgumentException(Prefix + "." + targetCompId + ".fixServerListener property is invalid", e);
				}

				TargetCompId = targetCompId;
				var propertyName = Prefix + "." + targetCompId + ".storageType";
				var storageType = configuration.GetProperty(propertyName, "Transient");
				if (Enum.TryParse(storageType, out StorageType type) && Enum.IsDefined(typeof(StorageType), type))
				{
					StorageType = type;
				}
				else
				{
					StorageType = StorageType.Transient;
					if (Log.IsWarnEnabled)
					{
						Log.Warn("Invalid value for '" + propertyName + "' property, value:" + storageType + ".");
					}
				}
			}
			
			public bool AllowedIp(string host)
			{
				return "*".Equals(Ip) || IpMasks.Any(ipMask => ipMask.IsInRange(host));
			}

			public virtual bool AllowedUser(string incomingUserName, string incomingPassword)
			{
				return Login.Equals(incomingUserName) && Password.Equals(incomingPassword);
			}
		}

		internal enum StorageType
		{
			Transient,
			Persistent
		}

		internal class AutoStartAcceptorFactory
		{
			public static IFixSession CreateSession(AutostartSessionDetails sessionDetails, SessionParameters sessionParameters, IFixTransport fixTransport)
			{
				var storageFactory = typeof(InMemoryStorageFactory).FullName;
				switch (sessionDetails.StorageType)
				{
					case StorageType.Transient:
						storageFactory = typeof(InMemoryStorageFactory).FullName;
						break;
					case StorageType.Persistent:
						storageFactory = typeof(FilesystemStorageFactory).FullName;
						break;
				}

				sessionParameters.Configuration.SetProperty(Config.StorageFactory, storageFactory);

				return StandardFixSessionFactory.GetFactory(sessionParameters).CreateAcceptorSession(sessionParameters, fixTransport);
			}
		}
	}
}