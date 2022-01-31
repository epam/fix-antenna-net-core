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
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;

namespace Epam.FixAntenna.NetCore.FixEngine.Transport
{
	internal class ConnectionAuthenticator
	{
		/// <summary>
		/// Indicates that a certificate can be used as a Secure Sockets Layer (SSL) client certificate.
		/// </summary>
		/// <remarks>https://oidref.com/1.3.6.1.5.5.7.3.2</remarks>
		private const string ClientOid = "1.3.6.1.5.5.7.3.2";
		/// <summary>
		/// Indicates that a certificate can be used as an SSL server certificate.
		/// </summary>
		/// <remarks>https://oidref.com/1.3.6.1.5.5.7.3.1</remarks>
		private const string ServerOid = "1.3.6.1.5.5.7.3.1";

		private readonly ConfigurationAdapter _configAdapter;
		private static ILog Log = LogFactory.GetLog(typeof(ConnectionAuthenticator));
		private static readonly ConcurrentDictionary<string, X509Certificate2> CertCache = new ConcurrentDictionary<string, X509Certificate2>();

		public SslProtocols Protocol { get; }
		public string CaCertificate { get; }
		public string Certificate { get; }
		public string Password { get; }
		public string ServerCN { get; }
		public bool CheckCertificateRevocation { get; }
		public bool ValidatePeerCertificate { get; }

		public ConnectionAuthenticator(ConfigurationAdapter adapter)
		{
			_configAdapter = adapter;

			Protocol = _configAdapter.SslProtocol;
			CaCertificate = _configAdapter.SslCaCertificate;
			Certificate = _configAdapter.SslCertificate;
			Password = _configAdapter.SslCertificatePassword;
			ServerCN = _configAdapter.SslServerName;
			ValidatePeerCertificate = _configAdapter.SslValidatePeerCertificate;
			CheckCertificateRevocation = _configAdapter.SslCheckCertificateRevocation && ValidatePeerCertificate;
		}

		public Stream AuthenticateAcceptor(Stream innerStream)
		{
#pragma warning disable CA2000 // Dispose objects before losing scope
			var stream = new SslStream(innerStream, false, ClientValidationCallback, SelectCertificateCallback);
#pragma warning restore CA2000 // Dispose objects before losing scope

			if (string.IsNullOrEmpty(Certificate))
				throw new IOException($"{Config.SslCertificate} not configured.");

			try
			{
				var certificate = LoadCertificateFromCache(Certificate, Password);
				stream.AuthenticateAsServer(certificate, ValidatePeerCertificate, Protocol, CheckCertificateRevocation);
				PrintTlsInfo(stream);
				return stream;
			}
			catch (Exception e)
			{
				if (e.InnerException != null)
				{
					Log.Error($"{ e.Message} Inner exception: {e.InnerException.Message}");
				}
				else
				{
					Log.Error(e.Message);
				}
				
				stream.Close();
				throw new IOException("Cannot establish secured connection with client.", e);
			}
		}

		public Stream AuthenticateInitiator(Stream innerStream)
		{
			var stream = new SslStream(innerStream, false, ServerValidationCallback, SelectCertificateCallback);

			try
			{
				var certs = GetClientCertificates(Certificate, Password);
				stream.AuthenticateAsClient(ServerCN, certs, Protocol, CheckCertificateRevocation);
				PrintTlsInfo(stream);
				return stream;
			}
			catch (Exception e)
			{
				if (e.InnerException != null)
				{
					Log.Error($"{ e.Message} Inner exception: {e.InnerException.Message}");
				}
				else
				{
					Log.Error(e.Message);
				}

				stream.Close();
				throw new IOException("Cannot establish secured connection to server.", e);
			}
		}

		private void PrintTlsInfo(SslStream stream)
		{
			var log = new StringBuilder();
			log.AppendLine("Secure connection information:");
			log.AppendLine("Protocol:               " + stream.SslProtocol);
			log.AppendLine("Authenticated:          " + stream.IsAuthenticated);
			log.AppendLine("Mutually authenticated: " + stream.IsMutuallyAuthenticated);
			log.AppendLine("Encrypted:              " + stream.IsEncrypted);
			log.AppendLine("Signed:                 " + stream.IsSigned);
			log.AppendLine("Auth. as server:        " + stream.IsServer);
			log.AppendLine("Revocation checked:     " + stream.CheckCertRevocationStatus);
			log.AppendLine("Cipher:                 " + stream.CipherAlgorithm);
			log.AppendLine("Hash:                   " + stream.HashAlgorithm);

			Log.Debug(log.ToString());
		}

		private static X509Certificate2 LoadCertificateFromCache(string cert, string certPassword)
		{
			return CertCache.GetOrAdd(cert, (key) => LoadCertificate(cert, certPassword));
		}

		private static X509Certificate2 LoadCertificate(string cert, string password)
		{
			X509Certificate2 certificate;

			// If no extension is found try to get from certificate store
			if (File.Exists(cert))
			{
#pragma warning disable CA2000 // Dispose objects before losing scope
				certificate = password != null ? new X509Certificate2(cert, password) : new X509Certificate2(cert);
#pragma warning restore CA2000 // Dispose objects before losing scope
			}
			else
			{
				certificate = LoadCertificateFromStore(cert, new X509Store(StoreLocation.LocalMachine))
											?? LoadCertificateFromStore(cert, new X509Store(StoreLocation.CurrentUser));
			}

			if (certificate == null)
				throw new IOException($"Cannot find certificate {cert}.");

			return certificate;
		}

		private static X509Certificate2 LoadCertificateFromStore(string cert, X509Store store)
		{
			try
			{
				store.Open(OpenFlags.ReadOnly);
				var certCollection = store.Certificates;
				var currentCerts = certCollection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);
				currentCerts = currentCerts.Find(cert.Contains("CN=") ? X509FindType.FindBySubjectDistinguishedName : X509FindType.FindBySubjectName, cert, false);

				return currentCerts.Count == 0 ? null : currentCerts[0];
			}
			finally
			{
				store.Close();
			}
		}

		private X509CertificateCollection GetClientCertificates(string cert, string password)
		{
			if (string.IsNullOrEmpty(cert))
			{
				return new X509Certificate2Collection();
			}

			var certificates = new X509Certificate2Collection();
			var clientCert = LoadCertificateFromCache(cert, password);
			certificates.Add(clientCert);
			return certificates;
		}

		private bool ServerValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			return VerifyRemoteCertificate(chain, certificate, sslPolicyErrors, ServerOid);
		}

		private bool VerifyRemoteCertificate(X509Chain chain, X509Certificate certificate, SslPolicyErrors sslPolicyErrors, string oid)
		{
			if (ValidatePeerCertificate == false)
				return true;

			if (certificate == null)
			{
				LogChainInformation(chain, sslPolicyErrors, certificate);
				return false;
			}

			if (!ContainsEnhancedKeyUsage(certificate, oid))
			{
				LogChainInformation(chain, sslPolicyErrors, certificate);
				Log.Warn($"Remote certificate is not intended for {(oid == ClientOid ? "client" : "server")} authentication.");
				return false;
			}

			if (string.IsNullOrEmpty(CaCertificate))
			{
				LogChainInformation(chain, sslPolicyErrors, certificate);
				return sslPolicyErrors == SslPolicyErrors.None;
			}

			using (var chain0 = new X509Chain())
			{
				chain0.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
				chain0.ChainPolicy.ExtraStore.Add(LoadCertificateFromCache(CaCertificate, null));
				chain0.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
				var isValid = chain0.Build((X509Certificate2)certificate);

				if (isValid)
					sslPolicyErrors &= ~SslPolicyErrors.RemoteCertificateChainErrors;
				else
					sslPolicyErrors |= SslPolicyErrors.RemoteCertificateChainErrors;
			}

			LogChainInformation(chain, sslPolicyErrors, certificate);
			return sslPolicyErrors == SslPolicyErrors.None;
		}

		private void LogChainInformation(X509Chain chain, SslPolicyErrors sslPolicyErrors, X509Certificate certificate)
		{
			if (certificate == null)
			{
				Log.Warn($"Remote party doesn't provide certificate.");
			}
			else
			{
				Log.Trace($"Certificate subject: '{certificate.Subject}' issued by '{certificate.Issuer}'");
			}
			
			if (sslPolicyErrors == SslPolicyErrors.None && !Log.IsTraceEnabled)
			{
				return;
			}

			Log.Trace($"Actual SslPolicyErrors: {sslPolicyErrors}");
			
			if (chain != null)
			{
				var chainStatus = CollectChainStatus(chain);
				Log.Trace($"Actual certificate validation chain status: {chainStatus}");
			}
		}

		private string CollectChainStatus(X509Chain chain)
		{
			if (chain == null)
				return string.Empty;

			var result = new StringBuilder();
			foreach (var chainElement in chain.ChainElements)
			{
				var certSubject = chainElement.Certificate?.Subject;
				var elementStatus = string.Join(",", chainElement.ChainElementStatus
					.Select((s) => s.StatusInformation));
				result.AppendFormat("\n - Chain element: '{0}', status: '{1}'", certSubject, elementStatus);
			}

			return result.ToString();
		}

		private X509Certificate SelectCertificateCallback(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
		{
			if (localCertificates == null || localCertificates.Count <= 0)
				return null;

			if (acceptableIssuers != null && acceptableIssuers.Length > 0)
			{
				foreach (var certificate in localCertificates)
				{
					var issuer = certificate.Issuer;
					if (Array.IndexOf(acceptableIssuers, issuer) != -1)
						return certificate;
				}
			}
			
			return localCertificates[0];
		}

		private bool ClientValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			return VerifyRemoteCertificate(chain, certificate, sslPolicyErrors, ClientOid);
		}

		private static bool ContainsEnhancedKeyUsage(X509Certificate certificate, string oid)
		{
			if (certificate is X509Certificate2 certificate2)
				return CheckExtensions(certificate2, oid);

			using (var cert2 = new X509Certificate2(certificate))
			{
				return CheckExtensions(cert2, oid);
			}
		}

		private static bool CheckExtensions(X509Certificate2 cert, string oid)
		{
			foreach (var extension in cert.Extensions)
			{
				if (!(extension is X509EnhancedKeyUsageExtension keyUsage))
				{
					continue;
				}

				foreach (var usage in keyUsage.EnhancedKeyUsages)
				{
					if (usage.Value == oid)
						return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Internal use only.
		/// </summary>
		internal static void ClearCertCache()
		{
			CertCache.Clear();
		}
	}
}
