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

using System.Collections.Generic;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.FixEngine.Session.Impl;

namespace Epam.FixAntenna.NetCore.FixEngine
{
	/// <summary>
	/// Default fix session factory strategy implementation.
	/// This class provides access to FIX 4.0 - FIX 5.0 SP2 session factories.
	/// </summary>
	/// <seealso cref="IFixSessionFactoryStrategy"> </seealso>
	internal class DefaultFixSessionFactoryStrategy : IFixSessionFactoryStrategy
	{

		private static IFixSessionFactory _fix40 = new StandardSessionFactory(typeof(Fix40MessageFactory));
		private static IFixSessionFactory _fix41 = new StandardSessionFactory(typeof(Fix41MessageFactory));
		private static IFixSessionFactory _fix42 = new StandardSessionFactory(typeof(Fix42MessageFactory));
		private static IFixSessionFactory _fix43 = new StandardSessionFactory(typeof(Fix43MessageFactory));
		private static IFixSessionFactory _fix44 = new StandardSessionFactory(typeof(Fix44MessageFactory));
		private static IFixSessionFactory _fix50 = new StandardSessionFactory(typeof(Fixt11MessageFactory));
		private static IFixSessionFactory _fix50Sp1 = new StandardSessionFactory(typeof(Fixt11MessageFactory));
		private static IFixSessionFactory _fix50Sp2 = new StandardSessionFactory(typeof(Fixt11MessageFactory));

		private IDictionary<FixVersion, IFixSessionFactory> _fixVersions = new Dictionary<FixVersion, IFixSessionFactory>();
		private IDictionary<FixVersion, IFixSessionFactory> _appVersions = new Dictionary<FixVersion, IFixSessionFactory>();

		/// <summary>
		/// Creates the <c>DefaultFixSessionFactoryStrategy</c>.
		/// </summary>
		public DefaultFixSessionFactoryStrategy()
		{
			_fixVersions[FixVersion.Fix40] = _fix40;
			_fixVersions[FixVersion.Fix41] = _fix41;
			_fixVersions[FixVersion.Fix42] = _fix42;
			_fixVersions[FixVersion.Fix43] = _fix43;
			_fixVersions[FixVersion.Fix44] = _fix44;
			_fixVersions[FixVersion.Fixt11] = _fix50;

			_appVersions[FixVersion.Fix40] = _fix50;
			_appVersions[FixVersion.Fix41] = _fix50;
			_appVersions[FixVersion.Fix42] = _fix50;
			_appVersions[FixVersion.Fix43] = _fix50;
			_appVersions[FixVersion.Fix44] = _fix50;
			_appVersions[FixVersion.Fix50] = _fix50;
			_appVersions[FixVersion.Fix50Sp1] = _fix50Sp1;
			_appVersions[FixVersion.Fix50Sp2] = _fix50Sp2;
		}

		/// <summary>
		/// Sets a new session factory for fixVersion.
		/// </summary>
		/// <param name="fixVersion">        the fix version </param>
		/// <param name="fixSessionFactory"> the fix session factory </param>
		public virtual void SetFixVersionFactory(FixVersion fixVersion, IFixSessionFactory fixSessionFactory)
		{
			_fixVersions[fixVersion] = fixSessionFactory;
		}

		/// <summary>
		/// Sets a new session factory for appVersion.
		/// </summary>
		/// <param name="appVersion">        the  app fix version </param>
		/// <param name="fixSessionFactory"> the fix session factory </param>
		public virtual void SetAppVersionFactory(FixVersion appVersion, IFixSessionFactory fixSessionFactory)
		{
			_appVersions[appVersion] = fixSessionFactory;
		}

		/// <summary>
		/// Gets fix session factory.
		/// </summary>
		/// <param name="sessionParameters"> the session parameters </param>
		/// <returns> FixSessionFactory </returns>
		/// <exception cref="System.ArgumentException"> if session factory not exists. </exception>
		public virtual IFixSessionFactory GetFixSessionFactory(SessionParameters sessionParameters)
		{
			var appVersion = sessionParameters.AppVersion;
			if (appVersion == null)
			{
				var sessionFactory = _fixVersions[sessionParameters.FixVersion];
				if (sessionFactory == null)
				{
					throw new System.ArgumentException("No factory set for this fixVersion" + sessionParameters.FixVersion);
				}

				return sessionFactory;
			}

			if (sessionParameters.FixVersion.IsFixt)
			{
				var fixSessionFactory = _appVersions[appVersion];
				if (fixSessionFactory == null)
				{
					throw new System.ArgumentException("No factory set for this fixVersion" + appVersion);
				}

				return fixSessionFactory;
			}

			throw new System.ArgumentException("AppVersion is set, but FIXVersion is not FIXT");
		}
	}
}