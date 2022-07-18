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

using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Session.Common;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Message.Format;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Epam.FixAntenna.NetCore.FixEngine.Scheduler;
using static Epam.FixAntenna.NetCore.Configuration.Config;

namespace Epam.FixAntenna.NetCore.FixEngine
{
	/// <summary>
	/// This bean contains all session level configuration for current session.
	/// <p/>
	/// It is possible to confiniently define a list of custom FIX fields  that will be added to each message.
	/// For more complex message customization take a look at <see cref="AbstractFixSessionFactory"/>
	/// </summary>
	/// <seealso cref="AbstractFixSessionFactory"> for more precise customization </seealso>
	public class SessionParameters : ICloneable
	{
		public const int DefaultSequenceNum = 0;
		internal const int MaxSessionIdLength = 200;
		private static readonly ILog Log = LogFactory.GetLog(typeof(SessionParameters));

		private int? _port;

		private Config _configuration;

		public SessionId SessionId { get; private set; } = new SessionId("Sender", "Target");

		public SessionParameters(Config config)
		{
			_configuration = config;
			FixVersionContainer = FixVersionContainer.GetFixVersionContainer(_configuration, FixVersion.Fix42);
		}

		public SessionParameters()
		{
			try
			{
				_configuration = (Config)Config.GlobalConfiguration.Clone();
			}
			catch (Exception e)
			{
				throw new Exception(e.Message, e);
			}

			FixVersionContainer = FixVersionContainer.GetFixVersionContainer(_configuration, FixVersion.Fix42);
		}

		/// <summary>
		/// Gets or sets Configuration.
		/// </summary>
		public Config Configuration
		{
			get => _configuration;
			set => _configuration = (Config)value.Clone();
		}

		/// <summary>
		/// Gets FIX version.
		/// </summary>
		public FixVersion FixVersion
		{
			get => FixVersionContainer.FixVersion;
			set => FixVersionContainer = FixVersionContainer.GetFixVersionContainer(_configuration, value);
		}

		/// <summary>
		/// Sets FIX version
		/// </summary>
		/// <param name="fixVersion"></param>
		public void FixVersionFromString(string fixVersion)
		{
			if (string.IsNullOrEmpty(fixVersion))
			{
				throw new ArgumentException("Fix version is empty");
			}

			try
			{
				var version = FixVersion.GetInstanceByMessageVersion(fixVersion);
				FixVersionContainer = FixVersionContainer.GetFixVersionContainer(_configuration, version);
			}
			catch (ArgumentException)
			{
				FixVersionContainer = FixVersionContainer.GetFixVersionContainer(fixVersion, _configuration);
			}
		}

		/// <summary>
		/// Gets or sets the App version.
		/// </summary>
		public FixVersion AppVersion
		{
			get => AppVersionContainer?.FixVersion;
			set
			{
				if (value == null)
				{
					AppVersionContainer = null;
				}
				else
				{
					AppVersionContainer = FixVersionContainer.GetFixVersionContainer(_configuration, value);
				}
			}
		}

		/// <summary>
		/// Set FixVersionContainer by provided appVersion string.
		/// </summary>
		/// <param name="appVersion"></param>
		public void AppVersionFromString(string appVersion)
		{
			if (string.IsNullOrEmpty(appVersion))
			{
				throw new ArgumentException("App version is empty");
			}

			if (int.TryParse(appVersion, out int intVersion))
			{
				var version = FixVersion.GetInstanceByFixtVersion(intVersion);
				AppVersionContainer = FixVersionContainer.GetFixVersionContainer(_configuration, version);
			}
			else
			{
				try
				{
					var version = FixVersion.GetInstanceByMessageVersion(appVersion);
					AppVersionContainer = FixVersionContainer.GetFixVersionContainer(_configuration, version);
				}
				catch (ArgumentException)
				{
					AppVersionContainer = FixVersionContainer.GetFixVersionContainer(appVersion, _configuration);
				}
			}
		}

		public FixVersionContainer FixVersionContainer { get; set; }

		public FixVersionContainer AppVersionContainer { get; set; }

		/// <summary>
		/// Gets or sets user defined fields.
		/// <p/>
		/// If this list is not empty, Engine add it to each outgoing message.
		/// </summary>
		public FixMessage UserDefinedFields { get; set; } = new FixMessage();

		/// <summary>
		/// Add user defined field.
		/// </summary>
		/// <param name="tag"></param>
		/// <param name="value"></param>
		public void AddHeaderField(int tag, byte[] value)
		{
			UserDefinedFields.AddTag(tag, value);
		}

		/// <summary>
		/// Gets or sets sender comp id.
		/// </summary>
		public string SenderCompId
		{
			get => SessionId.Sender;
			set => SessionId.Sender = value;
		}

		/// <summary>
		/// Gets or sets target comp id.
		/// </summary>
		public string TargetCompId
		{
			get => SessionId.Target;
			set => SessionId.Target = value;
		}

		/// <summary>
		/// Gets or sets sender sub id.
		/// </summary>
		public string SenderSubId { get; set; }

		/// <summary>
		/// Gets or sets target sub id.
		/// </summary>
		public string TargetSubId { get; set; }

		/// <summary>
		/// Gets or sets sender location id.
		/// </summary>
		public string SenderLocationId { get; set; }

		/// <summary>
		/// Gets or sets target location id.
		/// </summary>
		public string TargetLocationId { get; set; }

		///<summary>
		///Change session identifier
		///</summary>
		///<param name="sessionId"> unique string value. maximum 200 characters length. Allowed characters: a-z, A-Z, 0-9, '.',
		/// '-', '_', ' '(space) and '!' </param>
		public void SetSessionId(string sessionId)
		{
			CheckSessionId(sessionId);
			SessionId.CustomSessionId = sessionId;
		}

		private void CheckSessionId(string sessionId)
		{
			if (sessionId == null)
			{
				throw new ArgumentException("sessionId is null");
			}

			if (!Regex.IsMatch(sessionId, @"^[_a-zA-Z0-9\-\. !]+$"))
			{
				throw new ArgumentException("Invalid sessionId: '" + sessionId + "'. Please use only letters, digits, '_', '-' and ' '");
			}

			if (sessionId.Length > MaxSessionIdLength)
			{
				throw new ArgumentException("sessionId is too long: '" + sessionId + "'. Please use smaller (<200 characters)");
			}
		}

		public string SessionQualifier
		{
			get => SessionId.Qualifier;
			set
			{
				SessionId.Qualifier = value;
				if (_configuration.GetPropertyAsBoolean(Config.SuppressSessionQualifierTagInLogonMessage))
				{
					OutgoingLoginMessage.RemoveTag(_configuration.GetPropertyAsInt(Config.LogonMessageSessionQualifierTag));
				}
				else if (!string.IsNullOrEmpty(value))
				{
					OutgoingLoginMessage.UpdateValue(_configuration.GetPropertyAsInt(Config.LogonMessageSessionQualifierTag),
						value, IndexedStorage.MissingTagHandling.AddIfNotExists);
				}
				else
				{
					OutgoingLoginMessage.RemoveTag(_configuration.GetPropertyAsInt(Config.LogonMessageSessionQualifierTag));
				}
			}
		}

		public bool IsCustomSessionId => SessionId.IsCustomSessionId;

		/// <summary>
		/// Gets or sets heartbeat interval.
		/// </summary>
		public int HeartbeatInterval { get; set; } = 30;

		/// <summary>
		/// Gets or sets host.
		/// </summary>
		public string Host { get; set; }

		public string BindIP { get; set; }

		/// <summary>
		/// Returns true if the Port is set.
		/// </summary>
		public bool HasPort => _port != null;

		/// <summary>
		/// Gets or sets port.
		/// </summary>
		public int? Port
		{
			get => _port ?? 0;
			set => _port = value;
		}

		/// <summary>
		/// Gets or sets force sequence reset option.
		/// </summary>
		public ForceSeqNumReset ForceSeqNumReset
		{
			get
			{
				try
				{
					var propertyValue = Configuration.GetProperty(Config.ForceSeqNumReset);

					if (string.IsNullOrEmpty(propertyValue))
					{
						ParamSources.Instance.Set(Config.ForceSeqNumReset, ParamSource.Default, SessionId.ToString());
						propertyValue = ForceSeqNumReset.Never.ToString();
					}

					if (!(Enum.TryParse(propertyValue, true, out ForceSeqNumReset forceSeqNumReset) 
						  && Enum.IsDefined(typeof(ForceSeqNumReset), forceSeqNumReset)))
					{
						Log.Warn("Invalid forceSeqNumReset parameter.");
						ParamSources.Instance.Set(Config.ForceSeqNumReset, ParamSource.Default, SessionId.ToString());
						return ForceSeqNumReset.Never;
					}

					return forceSeqNumReset;
				}
				catch (ArgumentException)
				{
					Log.Warn("Invalid forceSeqNumReset parameter.");
					return ForceSeqNumReset.Never;
				}
			}
			set => Configuration.SetProperty(Config.ForceSeqNumReset, value.ToString());
		}

		public string UserName
		{
			set => OutgoingLoginMessage.UpdateValue(Tags.Username, value, IndexedStorage.MissingTagHandling.AddIfNotExists);
			get => OutgoingLoginMessage.GetTagValueAsString(Tags.Username);
		}

		public string Password
		{
			get => OutgoingLoginMessage.GetTagValueAsString(Tags.Password);
			set => OutgoingLoginMessage.UpdateValue(Tags.Password, value, IndexedStorage.MissingTagHandling.AddIfNotExists);
		}

		public string IncomingUserName => IncomingLoginMessage.GetTagValueAsString(Tags.Username);

		public string IncomingPassword => IncomingLoginMessage.GetTagValueAsString(Tags.Password);

		/// <summary>
		/// Gets or sets incoming login fields.
		/// </summary>
		/// <value> FixMessage list of field </value>
		/// <remarks>Engine use IncomingLoginMessage only for acceptor session.</remarks>
		public FixMessage IncomingLoginMessage { get; set; } = new FixMessage();

		/// <summary>
		/// Gets or sets outgoing login fields.
		/// <p/>
		/// This parameter used only for initiator session,
		/// Engine added outgoingLoginFixMessage to login message.
		/// </summary>
		/// <value> list of fields </value>
		public FixMessage OutgoingLoginMessage { get; set; } = new FixMessage();

		/// <summary>
		/// Add field to outgoing login fields list.
		/// </summary>
		/// <param name="tag"> </param>
		/// <param name="value"> </param>
		public void AddOutgoingLoginField(int tag, string value)
		{
			OutgoingLoginMessage.AddTag(tag, value);
		}

		/// <summary>
		/// Add field to outgoing login fields list.
		/// </summary>
		/// <param name="tag"> </param>
		/// <param name="value"> </param>
		public void AddOutgoingLoginField(int tag, byte[] value)
		{
			OutgoingLoginMessage.AddTag(tag, value);
		}

		/// <summary>
		/// Add field to outgoing login fields list.
		/// </summary>
		/// <param name="tag"> </param>
		/// <param name="value"> </param>
		public void AddOutgoingLoginField(int tag, byte[] value, int offset, int length)
		{
			OutgoingLoginMessage.AddTag(tag, value, offset, length);
		}

		/// <summary>
		/// Add field to outgoing login fields list.
		/// </summary>
		/// <param name="tag"> </param>
		/// <param name="value"> </param>
		public void AddOutgoingLoginField(int tag, long value)
		{
			OutgoingLoginMessage.AddTag(tag, value);
		}

		/// <summary>
		/// Add field to outgoing login fields list.
		/// </summary>
		/// <param name="tag"> </param>
		/// <param name="value"> </param>
		public void AddOutgoingLoginField(int tag, double value, int precision)
		{
			OutgoingLoginMessage.AddTag(tag, value, precision);
		}

		/// <summary>
		/// Add field to outgoing login fields list.
		/// </summary>
		/// <param name="tag"> </param>
		/// <param name="value"> </param>
		public void AddOutgoingLoginField(int tag, DateTimeOffset value, FixDateFormatterFactory.FixDateType type)
		{
			OutgoingLoginMessage.AddCalendarTag(tag, value, type);
		}

		/// <summary>
		/// Creates and returns a copy of this object.
		/// </summary>
		/// <returns> SessionParameters </returns>
		public object Clone()
		{
			var @params = (SessionParameters)base.MemberwiseClone();
			@params.SessionId = (SessionId)SessionId.Clone();
			@params.UserDefinedFields = UserDefinedFields.DeepClone(false, UserDefinedFields.IsUserOwned);
			@params.IncomingLoginMessage = IncomingLoginMessage.DeepClone(false, IncomingLoginMessage.IsUserOwned);
			@params.OutgoingLoginMessage = OutgoingLoginMessage.DeepClone(false, OutgoingLoginMessage.IsUserOwned);
			@params.ForceSeqNumReset = ForceSeqNumReset;
			@params.FixVersionContainer = FixVersionContainer;
			@params.AppVersionContainer = AppVersionContainer;
			@params.IncomingSequenceNumber = IncomingSequenceNumber;
			@params.OutgoingSequenceNumber = OutgoingSequenceNumber;
			@params.Configuration = Configuration;
			@params.CustomLoader = CustomLoader;
			@params.Destinations = new List<DnsEndPoint>(Destinations);
			return @params;
		}

		/// <summary>
		/// Creates and returns a copy of this object.
		/// The engine calls this method when the specific parameters should be serialized to properties.
		/// </summary>
		public Dictionary<string, string> ToProperties()
		{
			var properties = new Dictionary<string, string>();
			if (SenderCompId != null)
			{
				properties.Add("senderCompID", SenderCompId);
			}
			if (TargetCompId != null)
			{
				properties.Add("targetCompID", TargetCompId);
			}
			if (!string.IsNullOrEmpty(SessionQualifier))
			{
				properties.Add("sessionQualifier", SessionQualifier);
			}
			if (IsCustomSessionId)
			{
				properties.Add("sessionID", SessionId.ToString());
			}

			if (Host != null)
			{
				properties.Add("host", Host);
			}

			properties.Add("port", Convert.ToString(Port));
			properties.Add("FixVersion", FixVersion.MessageVersion);
			properties.Add("lastSeqNumResetTimestamp", LastSeqNumResetTimestamp.ToString());
			properties.Add("inSeqNumsForNextConnect", IncomingSequenceNumber.ToString());
			properties.Add("outSeqNumsForNextConnect", OutgoingSequenceNumber.ToString());
			return properties;
		}

		/// <summary>
		/// Creates the SessionParameters from properties.
		/// The engine calls this method when the stored parameters should be de-serialized from properties.
		/// </summary>
		/// <param name="properties"> the properties </param>
		public void FromProperties(IDictionary<string, string> properties)
		{
			if (properties.ContainsKey("sessionID"))
			{
				SetSessionId(properties.GetValueOrDefault("sessionID"));
			}

			SenderCompId = properties.GetValueOrDefault("senderCompID");
			TargetCompId = properties.GetValueOrDefault("targetCompID");

			if (properties.ContainsKey("sessionQualifier"))
			{
				SessionQualifier = properties.GetValueOrDefault("sessionQualifier");
			}

			if (properties.ContainsKey("senderSubID"))
			{
				SenderSubId = properties.GetValueOrDefault("senderSubID");
			}

			if (properties.ContainsKey("senderLocationID"))
			{
				SenderLocationId = properties.GetValueOrDefault("senderLocationID");
			}

			if (properties.ContainsKey("targetSubID"))
			{
				TargetSubId = properties.GetValueOrDefault("targetSubID");
			}

			if (properties.ContainsKey("targetLocationID"))
			{
				TargetLocationId = properties.GetValueOrDefault("targetLocationID");
			}

			if (properties.ContainsKey("host"))
			{
				Host = properties.GetValueOrDefault("host");
			}

			if (properties.ContainsKey("port"))
			{
				Port = Convert.ToInt32(properties.GetValueOrDefault("port", "3000"));
			}

			if (properties.ContainsKey("bindIP"))
			{
				BindIP = properties.GetValueOrDefault("bindIP");
			}

			SetDestinationsIfPresent(properties);

			if (properties.ContainsKey("appVersion"))
			{
				AppVersionFromString(properties.GetValueOrDefault("appVersion"));
			}

			if (properties.ContainsKey("fixVersion"))
			{
				FixVersionFromString(properties.GetValueOrDefault("fixVersion"));
			}

			if (properties.ContainsKey("heartbeatInterval"))
			{
				HeartbeatInterval = Convert.ToInt32(properties.GetValueOrDefault("heartbeatInterval"));
			}

			if (properties.ContainsKey("lastSeqNumResetTimestamp"))
			{
				LastSeqNumResetTimestamp = Convert.ToInt64(properties.GetValueOrDefault("lastSeqNumResetTimestamp"));
			}

			if (properties.ContainsKey("FixMessage"))
			{
				UserDefinedFields = RawFixUtil.GetFixMessage(properties.GetValueOrDefault("FixMessage").AsByteArray());
			}

			if (properties.ContainsKey("incomingLoginFixMessage"))
			{
				IncomingLoginMessage = RawFixUtil.GetFixMessage(properties.GetValueOrDefault("incomingLoginFixMessage", string.Empty).AsByteArray());
			}

			if (properties.ContainsKey("outgoingLoginFixMessage"))
			{
				OutgoingLoginMessage = RawFixUtil.GetFixMessage(properties.GetValueOrDefault("outgoingLoginFixMessage", string.Empty).AsByteArray());
			}

			if (properties.ContainsKey("username"))
			{
				UserName = properties.GetValueOrDefault("username");
			}

			if (properties.ContainsKey("password"))
			{
				Password = properties.GetValueOrDefault("password");
			}

			if (properties.ContainsKey("inSeqNumsForNextConnect"))
			{
				IncomingSequenceNumber = Convert.ToInt64(properties.GetValueOrDefault("inSeqNumsForNextConnect", "-1"));
			}
			else if (properties.ContainsKey("incomingSequenceNumber"))
			{
				IncomingSequenceNumber = Convert.ToInt64(properties.GetValueOrDefault("incomingSequenceNumber"));
			}
			else
			{
				IncomingSequenceNumber = DefaultSequenceNum;
				ParamSources.Instance.Set("incomingSequenceNumber", ParamSource.Default, SessionId.ToString());
			}

			if (properties.ContainsKey("outSeqNumsForNextConnect"))
			{
				OutgoingSequenceNumber = Convert.ToInt64(properties.GetValueOrDefault("outSeqNumsForNextConnect", "-1"));
			}
			else if (properties.ContainsKey("outgoingSequenceNumber"))
			{
				OutgoingSequenceNumber = Convert.ToInt64(properties.GetValueOrDefault("outgoingSequenceNumber"));
			}
			else
			{
				OutgoingSequenceNumber = DefaultSequenceNum;
				ParamSources.Instance.Set("outgoingSequenceNumber", ParamSource.Default, SessionId.ToString());
			}
		}

		public void SetDestinationsIfPresent(IDictionary<string, string> properties)
		{
			var index = 0;
			while (properties.ContainsKey($"socketConnectAddress_{index}"))
			{
				var key = $"socketConnectAddress_{index}";
				var value = properties.GetValueOrDefault(key);
				var address = value;
				var delimiterPos = address.IndexOf(':');
				var host = address.Substring(0, delimiterPos);
				var port = Convert.ToInt32(address.Substring(delimiterPos + 1));
				AddDestination(host, port);
				index++;
			}
		}

		/// <summary>
		/// Gets or sets last seq num reset timestamp.
		/// </summary>
		public long LastSeqNumResetTimestamp { get; set; }

		public bool IsSetSeqNumsOnNextConnect => IsSetInSeqNumsOnNextConnect && SetOutSeqNumsOnNextConnect;

		public bool IsSetInSeqNumsOnNextConnect => IncomingSequenceNumber > 0; //TODO: naming

		public bool SetOutSeqNumsOnNextConnect => OutgoingSequenceNumber > 0;

		public long IncomingSequenceNumber { set; get; } = DefaultSequenceNum;

		public long OutgoingSequenceNumber { set; get; } = DefaultSequenceNum;

		public void DisableInSeqNumsOnNextConnect() //TODO: naming
		{
			IncomingSequenceNumber = DefaultSequenceNum;
		}

		public void DisableOutSeqNumsOnNextConnect() //TODO: naming
		{
			OutgoingSequenceNumber = DefaultSequenceNum;
		}


		public override string ToString()
		{
			return string.Join(", ", ToProperties());
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

			var that = (SessionParameters)o;

			return
				SessionId.Equals(that.SessionId)
				&& Destinations.SequenceEqual(that.Destinations)
				&& HeartbeatInterval == that.HeartbeatInterval
				&& HeartbeatInterval == that.HeartbeatInterval
				&& Port == that.Port
				&& Host == that.Host
				&& FixVersionContainer.Equals(that.FixVersionContainer)
				&& AppVersionContainer == null
					? that.AppVersionContainer == null
					: AppVersionContainer.Equals(that.AppVersionContainer)
				&& Configuration == that.Configuration
				&& UserDefinedFields == that.UserDefinedFields
				&& IncomingLoginMessage == that.IncomingLoginMessage
				&& OutgoingLoginMessage == that.OutgoingLoginMessage
				&& SenderCompId == that.SenderCompId
				&& SenderLocationId == that.SenderLocationId
				&& SenderSubId == that.SenderSubId
				&& TargetCompId == that.TargetCompId
				&& TargetLocationId == that.TargetLocationId
				&& TargetSubId == that.TargetSubId
				&& CustomLoader == that.CustomLoader;
		}

		public override int GetHashCode()
		{
			return new HashCodeBuilder(17, 37)
				.Append(SessionId)
				.Append(SenderCompId)
				.Append(SenderLocationId)
				.Append(SenderSubId)
				.Append(TargetCompId)
				.Append(TargetLocationId)
				.Append(TargetSubId)
				.Append(HeartbeatInterval)
				.Append(FixVersionContainer)
				.Append(AppVersionContainer)
				.Append(Host)
				.Append(Port)
				.Append(UserDefinedFields)
				.Append(IncomingLoginMessage)
				.Append(OutgoingLoginMessage)
				.Append(Configuration)
				.Append(Destinations)
				.Append(CustomLoader)
				.GetHashCode();
		}

		private void ReplaceDestination(string oldHost, int oldPort, string newHost, int newPort)
		{
			if (!string.IsNullOrEmpty(oldHost) && oldPort > 0)
			{
				Destinations.Remove(new DnsEndPoint(oldHost, oldPort));
			}
			if (!string.IsNullOrEmpty(newHost) && newPort > 0)
			{
				Destinations.Insert(0, new DnsEndPoint(newHost, newPort));
			}
		}

		/// <summary>
		/// Gets alternative(backup) destinations for initiator. </summary>
		/// <value> list of alternative destinations. </value>
		public IList<DnsEndPoint> Destinations { get; private set; } = new List<DnsEndPoint>();

		/// <summary>
		/// Add alternative(backup) destination for initiator. </summary>
		/// <param name="host"> backup host </param>
		/// <param name="port"> backup port </param>
		public void AddDestination(string host, int port)
		{
			Destinations.Add(new DnsEndPoint(host, port));
		}

		/// <summary>
		/// Add alternative(backup) destination for initiator. </summary>
		/// <param name="destination"> backup address </param>
		public void AddDestination(DnsEndPoint destination)
		{
			Destinations.Add(destination);
		}

		/// <summary>
		/// Add alternative(backup) destinations for initiator. </summary>
		/// <param name="destinations"> backup addresses </param>
		public void AddAllDestinations(ICollection<DnsEndPoint> destinations)
		{
			((List<DnsEndPoint>)Destinations).AddRange(destinations);
		}

		/// <summary>
		/// Remove alternative(backup) destination from connections list. </summary>
		/// <param name="host"> backup host </param>
		/// <param name="port"> backup port </param>
		public void RemoveDestination(string host, int port)
		{
			Destinations.Remove(new DnsEndPoint(host, port));
		}

		/// <summary>
		/// Remove alternative(backup) destination from connections list. </summary>
		/// <param name="destination"> backup address </param>
		public void RemoveDestination(DnsEndPoint destination)
		{
			Destinations.Remove(destination);
		}

		/// <summary>
		/// Remove alternative(backup) destinations from connections list. </summary>
		/// <param name="destinations"> backup addresses </param>
		public void RemoveAllDestinations(ICollection<DnsEndPoint> destinations)
		{
			Destinations = Destinations.Except(destinations) as IList<DnsEndPoint>; //TODO: check this
		}

		/// <summary>
		/// Creates initiator fix session.
		/// <p/>
		/// User can use
		/// <c>StandardFixSessionFactory.GetFactory(SessionParameters).CreateInitiatorSession(SessionParameters)</c>
		/// instead this method.
		/// </summary>
		/// @deprecated use <seealso cref="SessionParameters.CreateAcceptorSession()"/> or <seealso cref="CreateInitiatorSession"/> instead
		public IFixSession CreateNewFixSession()
		{
			return StandardFixSessionFactory.GetFactory(this).CreateInitiatorSession(this);
		}

		/// <summary>
		/// Creates acceptor session in disconnected state
		/// </summary>
		/// <returns> FIX session </returns>
		public IFixSession CreateAcceptorSession()
		{
			return StandardFixSessionFactory.GetFactory(this).CreateAcceptorSession(this);
		}

		/// <summary>
		/// Creates initiator session
		/// </summary>
		/// <returns> FIX session </returns>
		public IFixSession CreateInitiatorSession()
		{
			return StandardFixSessionFactory.GetFactory(this).CreateInitiatorSession(this);
		}

        /// <summary>
        /// Creates scheduled initiator session
        /// </summary>
        /// <returns> FIX session </returns>
        public IScheduledFixSession CreateScheduledInitiatorSession()
        {
            return (IScheduledFixSession)StandardFixSessionFactory.GetFactory(this).CreateInitiatorSession(this);
        }

		public void PrintConfiguration()
		{
			if (Log.IsDebugEnabled)
			{
				Log.Debug((new ParameterLogHelper(this)).PrintConfiguration(this));
			}
		}

		public bool IsNeedToIncludeLastProcessed()
		{
			return Configuration.GetPropertyAsBoolean(Config.IncludeLastProcessed, false) && FixVersion.CompareTo(FixVersion.Fix42) >= 0; // include only if configuration says yes and current session FixVersion is 4.2 or above
		}

		/// <summary>
		/// Gets or sets the loader for loading configurable classes.
		/// </summary>
		/// <value> </value>
		public Func<string, object> CustomLoader { set; get; }

		private class ParameterLogHelper
		{
			private readonly SessionParameters _outerInstance;

			internal StringBuilder StringWriter;
			internal const int MaxParamLength = 80;

			public ParameterLogHelper(SessionParameters outerInstance)
			{
				_outerInstance = outerInstance;
				StringWriter = new StringBuilder(1024);
			}

			public string PrintConfiguration(SessionParameters sessionParameters)
			{
				var configuration = sessionParameters.Configuration;
				var sessionId = _outerInstance.SessionId.ToString();

				WriteNewLine();
				PrintTitle("Start configuration of session: " + _outerInstance.SessionId + "(" + sessionId + ")");
				// session parameters
				PrintParameter("Host", sessionParameters.Host, ParamSources.Instance.Get("host", sessionId));
				
				// special processing for SSL port
				PrintParameter("Port", sessionParameters.Port,
					sessionParameters.Port == configuration.GetPropertyAsInt(Config.SslPort)
						? ParamSources.Instance.Get(SslPort, sessionId)
						: ParamSources.Instance.Get("port", sessionId));

				PrintParameter("HBi", sessionParameters.HeartbeatInterval, ParamSources.Instance.Get("heartbeatInterval", sessionId));
				PrintParameter("FixVersion", sessionParameters.FixVersionContainer, ParamSources.Instance.Get("fixVersion", sessionId));
				PrintParameter("appVersion", sessionParameters.AppVersionContainer, ParamSources.Instance.Get("appVersion", sessionId));
				PrintParameter("incomingSequenceNumber", sessionParameters.IncomingSequenceNumber, ParamSources.Instance.Get("incomingSequenceNumber", sessionId));
				PrintParameter("outgoingSequenceNumber", sessionParameters.OutgoingSequenceNumber, ParamSources.Instance.Get("outgoingSequenceNumber", sessionId));
				PrintParameter("outgoingLoginFixMessage", sessionParameters.OutgoingLoginMessage.ToPrintableString(), ParamSources.Instance.Get("outgoingLoginFixMessage", sessionId));
				PrintParameter("resetInSeqNumsOnNextConnect", sessionParameters.IsSetInSeqNumsOnNextConnect, ParamSources.Instance.Get("incomingSequenceNumber", sessionId));
				PrintParameter("resetOutSeqNumsOnNextConnect", sessionParameters.SetOutSeqNumsOnNextConnect, ParamSources.Instance.Get("outgoingSequenceNumber", sessionId));
				PrintParameter("customHandlersLoaderClass", sessionParameters.CustomLoader, ParamSources.Default);
				for (var i = 0; i < _outerInstance.Destinations.Count; i++)
				{
					var key = $"socketConnectAddress_{i}";
					var inetAddress = _outerInstance.Destinations[i];
					PrintParameter(key, $"{inetAddress.Host}:{inetAddress.Port}", ParamSources.Instance.Get(key, sessionId));
				}

				PrintParameter(MaxMessageSize);
				PrintParameter(MaxMessagesToSendInBatch);
				PrintParameter(AutoreconnectAttempts);
				PrintParameter(AutoreconnectDelayInMs);
				PrintParameter(ConnectAddress);

				PrintParameter(InMemoryQueue);
				PrintParameter(QueueThresholdSize);
				PrintParameter(OutgoingStorageIndexed);

				PrintParameter(Config.ForceSeqNumReset, sessionParameters.ForceSeqNumReset, ParamSources.Instance.Get(Config.ForceSeqNumReset, sessionId));
				PrintParameter(ResendRequestNumberOfMessagesLimit);
				PrintParameter(MaxRequestResendInBlock);

				PrintParameter(CyclicSwitchBackupConnection);
				PrintParameter(EnableAutoSwitchToBackupConnection);

				PrintParameter(ResetOnSwitchToBackup);
				PrintParameter(ResetOnSwitchToPrimary);

				PrintParameter(SendCpuAffinity);
				PrintParameter(RecvCpuAffinity);

				PrintParameter(Accuracy);
				PrintParameter(TimestampsPrecisionInTags);
				PrintParameter(EnableMessageRejecting);
				PrintParameter(EnableNagle);
				PrintParameter(ForcedLogoffTimeout);
				PrintParameter(IncludeLastProcessed);

				PrintParameter(LoginWaitTimeout);

				PrintParameter(TimestampsInLogs);
				PrintParameter(TimestampsPrecisionInLogs);
				PrintParameter(LogFilesTimeZone);

				// begin SSL
				PrintParameter(RequireSsl, configuration.GetPropertyAsBoolean(RequireSsl, defaultValue: false, warnToLog: true), ParamSources.Instance.Get(RequireSsl, sessionId));
				var sslPort = configuration.GetProperty(SslPort, new ValidatorIntegerList(1, 65535), nullable: true, warnInLog: true);
				PrintParameter(SslPort, sslPort, ParamSources.Instance.Get(SslPort, sessionId));
				PrintParameter(SslCertificate);
				PrintParameter(SslCaCertificate);
				PrintParameter(SslCheckCertificateRevocation, configuration.GetPropertyAsBoolean(SslCheckCertificateRevocation, defaultValue: false, warnToLog: true), ParamSources.Instance.Get(SslCheckCertificateRevocation, sessionId));
				PrintParameter(SslProtocol);
				PrintParameter(SslServerName);
				PrintParameter(SslValidatePeerCertificate, configuration.GetPropertyAsBoolean(SslValidatePeerCertificate, defaultValue: false, warnToLog: true), ParamSources.Instance.Get(SslValidatePeerCertificate, sessionId));
				// end SSL

				PrintParameter(TradePeriodBegin);
				PrintParameter(TradePeriodEnd);
				PrintParameter(TradePeriodTimeZone);

				PrintTitle("End configuration of session: " + _outerInstance.SessionId);

				return StringWriter.ToString();
			}

			public override string ToString()
			{
				return StringWriter.ToString();
			}

			public ParameterLogHelper PrintTitle(string title)
			{
				var paramLength = title.Length;
				var diff = (MaxParamLength - paramLength) / 2;
				while (diff-- > 0)
				{
					StringWriter.Append("-");
				}
				StringWriter.Append(" ").Append(title).Append(" ");
				diff = (MaxParamLength - paramLength) / 2;
				while (diff-- > 0)
				{
					StringWriter.Append("-");
				}
				StringWriter.AppendLine();
				return this;
			}

			public ParameterLogHelper WriteNewLine()
			{
				StringWriter.AppendLine();
				return this;
			}

			public ParameterLogHelper PrintParameter(string paramName)
			{
				var value = _outerInstance.Configuration.GetProperty(paramName);
				var source = ParamSources.Instance.Get(paramName, _outerInstance.SessionId.ToString());
				PrintParameter(paramName, value, source);
				return this;
			}

			public ParameterLogHelper PrintParameter(string paramName, object paramValue, string paramSource)
			{
				if (paramValue == null)
				{
					paramValue = string.Empty;
				}

				var paramLength = paramName.Length;
				StringWriter.Append(paramName);
				var diff = MaxParamLength - paramLength;
				while (diff-- > 0)
				{
					StringWriter.Append(".");
				}
				StringWriter.Append(paramValue);
				StringWriter.Append(" (");
				StringWriter.Append(paramSource);
				StringWriter.Append(")");
				StringWriter.AppendLine();
				return this;
			}
		}

		/// <summary>
		/// Return true if parameter object describe the same FIX session.
		/// </summary>
		public bool IsSimilar(SessionParameters other)
		{
			return IsSimilar(other, null);
		}

		/// <summary>
		/// Return true if parameter object describe the same FIX session.
		/// </summary>
		public bool IsSimilar(SessionParameters other, List<string> errors)
		{
			var result = true;
			if (!SenderCompId.Equals(other.SenderCompId))
			{
				AddErrorDescription(errors, $"SenderCompId not similar: '{SenderCompId}' vs '{other.SenderCompId}'");
				result = false;
			}

			if (!SenderLocationId?.Equals(other.SenderLocationId) ?? !string.IsNullOrEmpty(other.SenderLocationId))
			{
				AddErrorDescription(errors, $"SenderLocationId not similar: '{SenderLocationId}' vs '{other.SenderLocationId}'");
				result = false;
			}

			if (!SenderSubId?.Equals(other.SenderSubId) ?? !string.IsNullOrEmpty(other.SenderSubId))
			{
				AddErrorDescription(errors, $"SenderSubId not similar: '{SenderSubId}' vs '{other.SenderSubId}'");
				result = false;
			}

			if (!TargetCompId.Equals(other.TargetCompId))
			{
				AddErrorDescription(errors, $"TargetCompId not similar: '{TargetCompId}' vs '{other.TargetCompId}'");
				result = false;
			}

			if (!TargetSubId?.Equals(other.TargetSubId) ?? !string.IsNullOrEmpty(other.TargetSubId))
			{
				AddErrorDescription(errors, $"TargetSubId not similar: '{TargetSubId}' vs '{other.TargetSubId}'");
				result = false;
			}

			if (!TargetLocationId?.Equals(other.TargetLocationId) ?? !string.IsNullOrEmpty(other.TargetLocationId))
			{
				AddErrorDescription(errors, $"TargetLocationId not similar: '{TargetLocationId}' vs '{other.TargetLocationId}'");
				result = false;
			}

			if (!SessionQualifier?.Equals(other.SessionQualifier) ?? !string.IsNullOrEmpty(other.SessionQualifier))
			{
				AddErrorDescription(errors, $"SessionQualifier not similar: '{SessionQualifier}' vs '{other.SessionQualifier}'");
				result = false;
			}

			if (!FixVersionContainer.Similar(other.FixVersionContainer))
			{
				AddErrorDescription(errors, $"FIXVersionContainer not similar: '{FixVersionContainer}' vs '{other.FixVersionContainer}'");
				result = false;
			}
			if (!AppVersionContainer?.Similar(other.AppVersionContainer) ?? other.AppVersionContainer != null)
			{
				AddErrorDescription(errors, $"AppVersionContainer not similar: '{AppVersionContainer}' vs '{other.AppVersionContainer}'");
				result = false;
			}

			var thisAddresses = Destinations;
			var otherAddresses = other.Destinations;
			if (thisAddresses.Count != otherAddresses.Count)
			{
				AddErrorDescription(errors, $"SocketConnectAddresses are different: '{string.Join(",", thisAddresses)}' vs '{string.Join(",", otherAddresses)}'");
				result = false;
			}
			else
			{
				for (var i = 0; i < thisAddresses.Count; i++)
				{
					if (!thisAddresses[i].Equals(otherAddresses[i]))
					{
						AddErrorDescription(errors, $"SocketConnectAddresses are different: '{thisAddresses[i]}' vs '{otherAddresses[i]}'");
						result = false;
					}
				}
			}
			return result;
		}

		private static void AddErrorDescription(List<string> errors, string msg)
		{
			if (Log.IsDebugEnabled)
			{
				Log.Debug(msg);
			}

			errors?.Add(msg);
		}
	}
}