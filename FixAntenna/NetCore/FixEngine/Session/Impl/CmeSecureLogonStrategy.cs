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
using System.Security.Cryptography;
using System.Text;
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.ResourceLoading;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using NLog;
using Version = Epam.FixAntenna.NetCore.Common.Utils.Version;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.Impl
{
	/// <summary>
	/// CME secure logon provides an ability to logon using SHA256 digital signature technique.
	/// This logon feature has been introduced by CME and provides highest security.
	/// </summary>
	internal class CmeSecureLogonStrategy : ILogonCustomizationStrategy
	{
		private static readonly ILogger Logger = LogManager.GetLogger(typeof(CmeSecureLogonStrategy).FullName);
		public const string SessionIdParamName = "Session ID";
		public const string AccessIdParamName = "Access ID";
		public const string SecretKeyParamName = "Secret Key";
		public const string CreationDateParamName = "Creation Date";
		public const string ExpirationDateParamName = "Expiration Date";
		public const string EnvironmentParamName = "Environment";
		public const string CanonicalStringDelimiter = "\n";
		public const int AppSystemNameTag = 1603;
		public const int TradingSystemVersionTag = 1604;
		public const int AppSystemVendorTag = 1605;
		public const int EncodedTextLenTag = 354;
		public const int EncodedTextTag = 355;
		public const int EncryptedPasswordMethodTag = 1400;
		public const string EncryptedPasswordMethodDefaultValue = "CME-1-SHA-256";
		public const int EncryptedPasswordLenTag = 1401;
		public const int EncryptedPasswordTag = 1402;
		public static readonly string DateFormat = "yyyy-MM-dd";
		public static readonly int[] RequiredTagsForCanonicalString = new int[]
		{
			Tags.MsgSeqNum,
			Tags.SenderCompID,
			Tags.SenderSubID,
			Tags.SendingTime,
			Tags.TargetSubID,
			Tags.HeartBtInt,
			Tags.SenderLocationID,
			Tags.LastMsgSeqNumProcessed,
			AppSystemNameTag,
			TradingSystemVersionTag,
			AppSystemVendorTag
		};
		private IDictionary<string, IDictionary<string, string>> _keysMap;

		public void SetSessionParameters(SessionParameters sessionParameters)
		{
			var configuration = sessionParameters.Configuration;
			var secKeyFile = configuration.GetProperty(Config.CmeSecureKeysFile);
			if (!string.IsNullOrEmpty(secKeyFile))
			{
				try
				{
					_keysMap = ParseKeys(secKeyFile);
				}
				catch (IOException e)
				{
					Logger.Error(e, "Error '{0}' while parsing '{1}'", e.Message, secKeyFile);
				}
			}
			else
			{
				Logger.Error("{0} is not defined, CME secure logon strategy will be disabled", Config.CmeSecureKeysFile);
			}
		}

		public void CompleteLogon(FixMessage logonMessage)
		{
			var senderCompId = logonMessage.GetTagValueAsString(Tags.SenderCompID);
			if (!string.IsNullOrEmpty(senderCompId) && senderCompId.Length >= 3)
			{
				var sessionId = senderCompId.Substring(0, 3);
				if (_keysMap.ContainsKey(sessionId))
				{
					var sessionParameters = _keysMap[sessionId];
					var expirationDateStr = sessionParameters[ExpirationDateParamName];
					if (IsDateExpired(expirationDateStr))
					{
						Logger.Error("Expiration date '{0}' is before now '{1}'. CME secure logon strategy will not be applied.", expirationDateStr, DateTime.Now.ToString(DateFormat));
						return;
					}
					try
					{
						var additionalTags = new FixMessage();
						if (!logonMessage.IsTagExists(AppSystemNameTag))
						{
							logonMessage.AddTag(AppSystemNameTag, "FIXAntenna .NET Core");
							logonMessage.AddTag(TradingSystemVersionTag, Version.GetProductVersion(typeof(FixVersion), 10));
							logonMessage.AddTag(AppSystemVendorTag, "B2BITS");
						}

						if (logonMessage.IsTagExists(Tags.EncryptMethod))
						{
							//cert tool does not accept logon with 98 tag
							logonMessage.RemoveTag(Tags.EncryptMethod);
						}
						if (logonMessage.IsTagExists(Tags.RawDataLength))
						{
							logonMessage.RemoveTag(Tags.RawDataLength);
						}
						if (logonMessage.IsTagExists(Tags.RawData))
						{
							logonMessage.RemoveTag(Tags.RawData);
						}

						var canonicalRequest = CreateCanonicalRequest(logonMessage);
						var secretKey = sessionParameters[SecretKeyParamName];
						var hmac = CalculateHmac(canonicalRequest, secretKey);

						var accessIdKey = sessionParameters[AccessIdParamName];
						additionalTags.AddTag(EncodedTextLenTag, accessIdKey.Length);
						additionalTags.AddTag(EncodedTextTag, accessIdKey);
						additionalTags.AddTag(EncryptedPasswordMethodTag, EncryptedPasswordMethodDefaultValue);
						additionalTags.AddTag(EncryptedPasswordLenTag, hmac.Length);
						additionalTags.AddTag(EncryptedPasswordTag, hmac);
						logonMessage.AddAll(additionalTags);
					}
					catch (Exception e)
					{
						Logger.Error(e, "Error '{0}' has occurred. CME secure logon strategy will not be applied", e.Message);
					}
				}
				else
				{
					Logger.Error("There is no key for session id '{0}' in secKey file.", sessionId);
				}
			}
			else
			{
				Logger.Error("SenderCompID '{0}' is less than 3 symbols. CME secure logon strategy will not be applied.", senderCompId);
			}
		}

		public virtual string CreateCanonicalRequest(FixMessage logonMessage)
		{
			var canonicalRequest = new StringBuilder();
			for (var i = 0; i < RequiredTagsForCanonicalString.Length; i++)
			{
				if (i > 0)
				{
					canonicalRequest.Append(CanonicalStringDelimiter);
				}
				canonicalRequest.Append(logonMessage.GetTagValueAsString(RequiredTagsForCanonicalString[i]));
			}
			return canonicalRequest.ToString();
		}

		public virtual bool IsDateExpired(string expirationDateStr)
		{
			var expirationDate = DateTime.ParseExact(expirationDateStr, DateFormat, null);
			return DateTime.Now > expirationDate;
		}

		public virtual string CalculateHmac(string canonicalRequest, string userKey)
		{
			string hash = null;
			try
			{
				using (var hmac = new HMACSHA256())
				{
					// Initialize HMAC instance with the key
					// Decode the key first, since it is base64url encoded
					var key = Convert.FromBase64String(userKey.UrlDecode());
					hmac.Key = key;
					hmac.Initialize();

					var txt = canonicalRequest.AsByteArray();
					var h = hmac.ComputeHash(txt);

					// Calculate HMAC, base64url encode the result and strip padding
					hash = Convert.ToBase64String(h).UrlEncode();
				}
			}
			catch (Exception e)
			{
				Logger.Error(e, e.Message);
			}

			return hash;
		}

		internal static IDictionary<string, IDictionary<string, string>> ParseKeys(string fileName)
		{
			IDictionary<string, IDictionary<string, string>> result = new Dictionary<string, IDictionary<string, string>>();
			using (var br = new StreamReader(ResourceLoader.DefaultLoader.LoadResource(fileName)))
			{
				if (br.Peek() >= 0)
				{
					var headerLine = br.ReadLine();
					var sessionIdIdx = headerLine.IndexOf(SessionIdParamName, StringComparison.Ordinal);
					var accessIdIdx = headerLine.IndexOf(AccessIdParamName, StringComparison.Ordinal);
					var secretKeyIdx = headerLine.IndexOf(SecretKeyParamName, StringComparison.Ordinal);
					var creationDateIdx = headerLine.IndexOf(CreationDateParamName, StringComparison.Ordinal);
					var expirationDateIdx = headerLine.IndexOf(ExpirationDateParamName, StringComparison.Ordinal);
					var environmentIdx = headerLine.IndexOf(EnvironmentParamName, StringComparison.Ordinal);
					while (br.Peek() >= 0)
					{
						var nextLine = br.ReadLine();
						if (!string.IsNullOrEmpty(nextLine))
						{
							var sessionId = GetValueAtIndex(nextLine, sessionIdIdx);
							IDictionary<string, string> parametersMap = new Dictionary<string, string>();
							parametersMap[AccessIdParamName] = GetValueAtIndex(nextLine, accessIdIdx);
							parametersMap[SecretKeyParamName] = GetValueAtIndex(nextLine, secretKeyIdx);
							parametersMap[CreationDateParamName] = GetValueAtIndex(nextLine, creationDateIdx);
							parametersMap[ExpirationDateParamName] = GetValueAtIndex(nextLine, expirationDateIdx);
							if (environmentIdx > 0)
							{
								parametersMap[EnvironmentParamName] = GetValueAtIndex(nextLine, environmentIdx);
							}
							if (result.ContainsKey(sessionId))
							{
								var existentKey = result[sessionId];
								var existentKeyExpirationDate = DateTime.ParseExact(existentKey[ExpirationDateParamName], DateFormat, null);
								var newKeyExpirationDate = DateTime.ParseExact(parametersMap[ExpirationDateParamName], DateFormat, null);
								if (newKeyExpirationDate > existentKeyExpirationDate)
								{
									result[sessionId] = parametersMap;
								}
							}
							else
							{
								result[sessionId] = parametersMap;
							}
						}
					}
				}
				else
				{
					throw new IOException("File '" + fileName + "' is empty");
				}
				return result;
			}
		}

		private static string GetValueAtIndex(string line, int startIndex)
		{
			var endIndex = line.Substring(startIndex).IndexOf(' ');
			if (endIndex == -1)
			{ //if there is no space at the end of the line
				endIndex = line.Substring(startIndex).Length;
			}
			return line.Substring(startIndex, endIndex);
		}
	}
}