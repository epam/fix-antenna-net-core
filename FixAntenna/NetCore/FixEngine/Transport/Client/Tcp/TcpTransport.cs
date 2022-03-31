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
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;

namespace Epam.FixAntenna.NetCore.FixEngine.Transport.Client.Tcp
{
	/// <summary>
	/// TCP socket transport implementation.
	/// </summary>
	internal class TcpTransport : SocketTransport
	{
		private readonly ConfigurationAdapter _adapter;

		protected bool EnableTcpNoDelay;
		protected bool EnableTls;

		public Func<Stream, Stream> AuthenticateStream = (stream) => stream;

		/// <summary>
		/// Creates transport.
		/// </summary>
		internal TcpTransport()
		{
		}

		/// <summary>
		/// Creates transport.
		/// </summary>
		/// <param name="remoteHost"> the transport host </param>
		/// <param name="remotePort"> the transport port </param>
		internal TcpTransport(string remoteHost, int remotePort) : this()
		{
			RemoteHost = remoteHost;
			RemotePort = remotePort;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="remoteHost"></param>
		/// <param name="remotePort"></param>
		/// <param name="parameters"></param>
		internal TcpTransport(string remoteHost, int remotePort, SessionParameters parameters) : this (remoteHost, remotePort)
		{
			var configuration = parameters.Configuration;
			_adapter = new ConfigurationAdapter(configuration);

			EnableTcpNoDelay = !_adapter.IsNagleEnabled;
			EnableTls = _adapter.IsSslEnabled;
			SendBufferSize = _adapter.TcpSendBufferSize;
			ReceiveBufferSize = _adapter.TcpReceiveBufferSize;
			BindIP = parameters.BindIP;
		}

		/// <summary>
		/// Creates transport.
		/// </summary>
		/// <param name="remoteHost">the transport host </param>
		/// <param name="remotePort">the transpot port </param>
		/// <param name="enableTcpNoDelay"> enable/disable Nagle sockets algorithm </param>
		public TcpTransport(string remoteHost, int remotePort, bool enableTcpNoDelay) : this(remoteHost, remotePort)
		{
			EnableTcpNoDelay = enableTcpNoDelay;
		}

		/// <summary>
		/// Target host name or IP address.
		/// </summary>
		public string RemoteHost { get; }

		/// <summary>
		/// Target port.
		/// </summary>
		public int RemotePort { get; }

		private string BindIP { get; }

		/// <inheritdoc />
		public override void Open()
		{
			try
			{
				CreateSocket();
				if (!string.IsNullOrEmpty(BindIP))
				{
					Socket.Bind(BindIP.ToEndPoint(0));
				}
				Socket.Connect(RemoteHost.ToEndPoint(RemotePort));
				CreateStream();
			}
			catch (SocketException ex)
			{
				throw new IOException(ex.Message, ex);
			}
		}

		public async Task OpenAsync()
		{
			try
			{
				CreateSocket();
				await Socket.ConnectAsync(RemoteHost.ToEndPoint(RemotePort)).ConfigureAwait(false);
				CreateStream();
			}
			catch (SocketException ex)
			{
				throw new IOException(ex.Message, ex);
			}
		}

		private void CreateSocket()
		{
			if (Log.IsDebugEnabled)
			{
				Log.Debug(EnableTls ? "Secure TCP transport" : "Regular TCP transport");
			}

			if (string.IsNullOrWhiteSpace(RemoteHost) || RemotePort == 0)
			{
				throw new InvalidOperationException("Cannot connect without address/port");
			}

			if (Log.IsTraceEnabled)
			{
				Log.Trace($"Passed TCP_NO_DELAY for {RemoteEndPoint.AsString()}: {EnableTcpNoDelay}");
			}

			Socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
			Socket.NoDelay = EnableTcpNoDelay;
			if (SendBufferSize > 0)
			{
				Socket.SendBufferSize = SendBufferSize;
			}

			if (ReceiveBufferSize > 0)
			{
				Socket.ReceiveBufferSize = ReceiveBufferSize;
			}
		}

		private void CreateStream()
		{
			if (Log.IsInfoEnabled)
			{
				Log.Info(
					$"Connection created from local address: {Socket.LocalEndPoint.AsString()} to remote address: {Socket.RemoteEndPoint.AsString()}");
			}
#pragma warning disable CA2000
			Stream netStream = new NetworkStream(Socket);
#pragma warning restore CA2000

			Os = Is = EnableTls ? AuthenticateStream(netStream) : netStream;
		}
	}
}
