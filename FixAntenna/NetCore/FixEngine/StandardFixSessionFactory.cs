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

using Epam.FixAntenna.NetCore.FixEngine.Transport;

namespace Epam.FixAntenna.NetCore.FixEngine
{
	/// <summary>
	/// Standard session factory.
	/// Engine used this factory for creation initiator and acceptor sessions.
	/// User can replace current behaviour by using <see cref="SetFixSessionFactoryStrategy"/> method.
	/// </summary>
	internal class StandardFixSessionFactory : IFixSessionFactory
	{
		private static IFixSessionFactoryStrategy _strategy = new DefaultFixSessionFactoryStrategy();

		/// <summary>
		/// Replace default strategy with own implementation.
		/// </summary>
		/// <param name="astrategy"> strategy </param>
		/// <seealso cref="IFixSessionFactoryStrategy"> </seealso>
		public static void SetFixSessionFactoryStrategy(IFixSessionFactoryStrategy astrategy)
		{
			_strategy = astrategy;
		}

		/// <inheritdoc />
		public virtual IFixSession CreateInitiatorSession(SessionParameters details)
		{
			return GetFactory(details).CreateInitiatorSession(details);
		}

		/// <inheritdoc />
		public virtual IFixSession CreateAcceptorSession(SessionParameters sessionParameters)
		{
			return GetFactory(sessionParameters).CreateAcceptorSession(sessionParameters);
		}

		/// <inheritdoc />
		public virtual IFixSession CreateAcceptorSession(SessionParameters details, IFixTransport transport)
		{
			return GetFactory(details).CreateAcceptorSession(details, transport);
		}

		public static IFixSessionFactory GetFactory(SessionParameters details)
		{
			return _strategy.GetFixSessionFactory(details);
		}
	}
}