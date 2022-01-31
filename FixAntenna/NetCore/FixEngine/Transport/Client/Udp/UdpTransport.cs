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
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Common.Utils;

namespace Epam.FixAntenna.NetCore.FixEngine.Transport.Client.Udp
{
	/// <summary>
	/// UDP transport implementation.
	/// </summary>
	internal class UdpTransport : ITransport
	{
		private int _bufferSize = -1;

		private IPEndPoint _inetAddress;

		private readonly ILog _log = LogFactory.GetLog(typeof(UdpTransport));

		private string _multicastAddress;
		private string _networkInterface;
		private int _port = -1;

		private Socket _socket;
		private UdpClient _udpClient;
		internal NetworkInterface Intf;
		internal MulticastOption McastOption;

		/// <summary>
		/// Creates UDP transport.
		/// </summary>
		/// <param name="multicastAddress">the multicast address </param>
		/// <param name="port">the transport port </param>
		public UdpTransport(string multicastAddress, int port)
		{
			_multicastAddress = multicastAddress;
			_port = port;
		}

		/// <summary>
		/// Setter for network interface.
		/// </summary>
		public virtual string NetworkInterfaceName
		{
			set => _networkInterface = value;
		}

		/// <inheritdoc />
		public virtual bool IsBlockingSocket => true;

		/// <inheritdoc />
		public virtual bool IsSecured => false;

		/// <summary>
		/// Method opens the transport.
		/// The implementation create a multicast socket and bind it to a specific port,
		/// and then joins created socket to multicast group.
		/// </summary>
		/// <exception cref="IOException">if an I/O error occurs</exception>
		public virtual void Open()
		{
			try
			{
				_inetAddress = new IPEndPoint(IPAddress.Parse(_multicastAddress), _port);
				_socket = new Socket(SocketType.Dgram, ProtocolType.Udp);

				if (_bufferSize > 0)
				{
					if (_log.IsDebugEnabled)
					{
						_log.Debug("Change the default DatagramSocket buffer from " + _socket.ReceiveBufferSize + " to " +
									_bufferSize);
					}

					_socket.ReceiveBufferSize = _bufferSize;
				}

				_socket.Bind(new IPEndPoint(IPAddress.Any, _port));

				if (_networkInterface == null)
				{
					if (_log.IsDebugEnabled)
					{
						_log.Debug("Join group " + _inetAddress);
					}

					McastOption = new MulticastOption(_inetAddress.Address);
				}
				else
				{
					Intf = GetNetworkInterfaceByName(_networkInterface);
					var index = -1;
					if (Intf.SupportsMulticast)
					{
						if (Intf.Supports(NetworkInterfaceComponent.IPv4))
						{
							index = Intf.GetIPProperties().GetIPv4Properties().Index;
						}
					}

					if (index >= 0)
					{
						if (_log.IsDebugEnabled)
						{
							_log.Debug("Join group " + _inetAddress + " on interface " + Intf.ToString());
						}

						McastOption = new MulticastOption(_inetAddress.Address, index);
					}
					else
					{
						if (_log.IsDebugEnabled)
						{
							_log.Debug("Join group " + _inetAddress + ". Interface " + Intf.ToString() +
										"is specified but it is not multicast capable, ignore it.");
						}

						McastOption = new MulticastOption(_inetAddress.Address);
					}
				}

				_socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, McastOption);
				_udpClient = new UdpClient
				{
					Client = _socket
				};
			}
			catch (SocketException ex)
			{
				throw new IOException(ex.Message, ex);
			}
		}

		/// <summary>
		/// Closes the transport.
		/// <p/>
		/// Leave a multicast group and close socket.
		/// </summary>
		/// <exception cref="IOException">if an I/O error occurs</exception>
		public virtual void Close()
		{
			try
			{
				if (_socket != default)
				{
					_socket.ShutdownAndClose();

					if (McastOption != default)
					{
						_socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DropMembership, McastOption);
					}

					_socket = default;
					_udpClient = default;
				}
			}
			catch (SocketException ex)
			{
				throw new IOException(ex.Message, ex);
			}
		}

		/// <inheritdoc />
		public virtual bool IsOpen
		{
			get { return _socket != default; }
		}

		/// <inheritdoc />
		public virtual void Write(byte[] message)
		{
			try
			{
				_udpClient.Send(message, message.Length, _inetAddress);
			}
			catch (SocketException ex)
			{
				throw new IOException(ex.Message, ex);
			}
		}

		/// <inheritdoc />
		public virtual int Write(byte[] message, int offset, int length)
		{
			try
			{
				var msg = new byte[length];
				Array.Copy(message, offset, msg, 0, length);
				_udpClient.Send(msg, msg.Length, _inetAddress);
				return length;
			}
			catch (SocketException ex)
			{
				throw new IOException(ex.Message, ex);
			}
		}

		/// <inheritdoc />
		public virtual int Read(byte[] buffer)
		{
			return Read(buffer, 0, buffer.Length);
		}

		/// <inheritdoc />
		public virtual int Read(byte[] buffer, int offset, int length)
		{
			try
			{
				IPEndPoint source = default;
				var data = _udpClient.Receive(ref source);
				Array.Copy(data, 0, buffer, offset, length >= data.Length ? length : data.Length);

				return data.Length;
			}
			catch (SocketException ex)
			{
				throw new IOException(ex.Message, ex);
			}
		}

		/// <inheritdoc />
		public IPEndPoint RemoteEndPoint => _socket?.RemoteEndPoint as IPEndPoint;

		/// <inheritdoc />
		public IPEndPoint LocalEndPoint => _socket?.LocalEndPoint as IPEndPoint;

		/// <inheritdoc />
		public virtual int Write(ByteBuffer buffer)
		{
			throw new NotSupportedException();
		}

		/// <inheritdoc />
		public virtual int Write(ByteBuffer buffer, int offset, int length)
		{
			throw new NotSupportedException();
		}

		/// <inheritdoc />
		public virtual int Read(ByteBuffer buffer, int offset, int length)
		{
			throw new NotSupportedException();
		}

		/// <inheritdoc />
		public virtual int Read(ByteBuffer buffer)
		{
			throw new NotSupportedException();
		}

		/// <inheritdoc />
		public virtual void WaitUntilReadyToWrite()
		{
		}

		public virtual void SetBufferSize(int value)
		{
			_bufferSize = value;
		}

		private static NetworkInterface GetNetworkInterfaceByName(string name)
		{
			foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
			{
				if (ni.Name == name)
				{
					return ni;
				}
			}

			return null;
		}
	}
}