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
using Epam.FixAntenna.NetCore.FixEngine.Session.Common;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.Impl
{
	/// <summary>
	/// This class implements the abstract method <see cref="GetMessageFactory"/>
	/// for creation <see cref="IFixMessageFactory"/> instances.
	/// </summary>
	internal class StandardSessionFactory : AbstractFixSessionFactory
	{
		private Type _factory;

		/// <summary>
		/// Creates the <see cref="IFixMessageFactory"/>.
		/// </summary>
		public StandardSessionFactory(Type factoryClass)
		{
			_factory = factoryClass;
		}

		/// <summary>
		/// Gets the <see cref="IFixMessageFactory"/>.
		/// </summary>
		public override IFixMessageFactory MessageFactory
		{
			get
			{
				try
				{
					return (IFixMessageFactory)Activator.CreateInstance(_factory);
				}
				catch (Exception e)
				{
					throw new Exception("Invalid message factory class", e);
				}
			}
		}
	}
}