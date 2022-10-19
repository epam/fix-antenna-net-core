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
using System.Globalization;
using System.IO;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.FixEngine.Storage.File;
using Epam.FixAntenna.NetCore.FixEngine.Storage.Queue;

namespace Epam.FixAntenna.NetCore.FixEngine.Storage
{
	/// <summary>
	/// Provides ability to store messages in the file.
	/// </summary>
	internal class FilesystemStorageFactory : IStorageFactory
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(FilesystemStorageFactory));

		/// <summary>
		/// Locator for backup files of incoming storage
		/// </summary>
		protected ILogFileLocator BackupIncomingLogFileLocator;

		/// <summary>
		/// Locator for backup files of outgoing storage
		/// </summary>
		protected ILogFileLocator BackupOutgoingLogFileLocator;

		protected readonly ConfigurationAdapter ConfigAdapter;

		/// <summary>
		/// Locator for incoming storage file
		/// </summary>
		protected ILogFileLocator IncomingLogFileLocator;

		/// <summary>
		/// Locator for outgoing storage file
		/// </summary>
		protected ILogFileLocator OutgoingLogFileLocator;

		/// <summary>
		/// Locator for session property file
		/// </summary>
		private ILogFileLocator _propertyFileLocator;

		/// <summary>
		/// Locator for outgoing queue
		/// </summary>
		protected ILogFileLocator QueueFileLocator;

		private static readonly object SyncObj = new object();

		/// <summary>
		/// Creates the <c>FilesystemStorageFactory</c> storage.
		/// </summary>
		public FilesystemStorageFactory(Config configuration)
		{
			ConfigAdapter = new ConfigurationAdapter(configuration);
			CheckDirectories();
			var directory = ConfigAdapter.StorageDirectory;
			var backupDirectory = ConfigAdapter.BackupStorageDirectory;
			CreateLocators(directory, backupDirectory);
		}

		/// <summary>
		/// Gets queue for session.
		/// If parameter <c>inMemoryQueue</c> configured, the queue will be <see cref="InMemoryQueue{T}"/>,
		/// otherwise <see cref="PersistentInMemoryQueue{T}"/>.
		/// </summary>
		/// <param name="sessionParameters"> the parameter for session </param>
		public virtual IQueue<FixMessageWithType> CreateQueue(SessionParameters sessionParameters)
		{
			CheckDirectories();
			if (ConfigAdapter.PreferredSendingMode == SendingMode.SyncNoqueue || ConfigAdapter.IsInMemoryQueueEnabled)
			{
				return new InMemoryQueue<FixMessageWithType>();
			}

			return new PersistentInMemoryQueue<FixMessageWithType>(QueueFileLocator.GetFileName(sessionParameters),
				new FixMessageWithTypeFactory());
		}

		/// <summary>
		/// Gets incoming message storage.
		/// If parameter <c>incomingStorageIndexed</c> configured, the message storage will be <see cref="IndexedMessageStorage"/>,
		/// otherwise <see cref="FlatFileMessageStorage"/>
		/// </summary>
		/// <param name="sessionParameters"> the session parameters </param>
		/// <seealso cref="IStorageFactory"> </seealso>
		/// <seealso cref="FlatFileMessageStorage"> </seealso>
		public virtual IMessageStorage CreateIncomingMessageStorage(SessionParameters sessionParameters)
		{
			CheckDirectories();
			AbstractFileMessageStorage storage = ConfigAdapter.IsIncomingStorageIndexed
				? new IndexedMessageStorage(sessionParameters.Configuration)
				: new FlatFileMessageStorage(sessionParameters.Configuration);

			storage.FileName = IncomingLogFileLocator.GetFileName(sessionParameters);
			storage.SetBackupFileLocator(BackupIncomingLogFileLocator);
			return storage;
		}

		/// <summary>
		/// Gets outgoing message storage.
		/// If parameter <c>outgoingStorageIndexed</c> configured, the message storage will be <see cref="IndexedMessageStorage"/>,
		/// otherwise <see cref="FlatFileMessageStorage"/>
		/// </summary>
		/// <param name="sessionParameters"> the session parameters </param>
		public virtual IMessageStorage CreateOutgoingMessageStorage(SessionParameters sessionParameters)
		{
			CheckDirectories();
			AbstractFileMessageStorage storage = ConfigAdapter.IsOutgoingStorageIndexed
				? new IndexedMessageStorage(sessionParameters.Configuration)
				: new FlatFileMessageStorage(sessionParameters.Configuration);

			storage.FileName = OutgoingLogFileLocator.GetFileName(sessionParameters);
			storage.SetBackupFileLocator(BackupOutgoingLogFileLocator);
			return storage;
		}

		/// <summary>
		/// Stores session parameters to file.
		/// </summary>
		/// <param name="sessionParameters"> the session parameters </param>
		/// <param name="state"> session runtime state </param>
		/// <exception cref="IOException"> if I/O error occurred </exception>
		public virtual void SaveSessionParameters(SessionParameters sessionParameters, FixSessionRuntimeState state)
		{
			var fileName = _propertyFileLocator.GetFileName(sessionParameters);
			var properties = sessionParameters.ToProperties();
			properties.PutAll(state.ToProperties());

			lock (SyncObj)
			{
				using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
				using (var writer = new StreamWriter(stream))
				{
					writer.WriteLine("#" + DateTime.Now.ToString("ddd MMM dd HH:mm:ss zz yyyy", CultureInfo.InvariantCulture));
					foreach (var prop in properties)
					{
						writer.WriteLine($"{prop.Key}={prop.Value}");
					}
				}
			}
		}

		/// <summary>
		/// Loads session parameters from file.
		/// </summary>
		/// <param name="sessionParameters"> the session parameters</param>
		/// <param name="state"> session runtime state </param>
		/// <returns>true if loaded</returns>
		public virtual bool LoadSessionParameters(SessionParameters sessionParameters, FixSessionRuntimeState state)
		{
			try
			{
				var fileName = _propertyFileLocator.GetFileName(sessionParameters);
				if (!System.IO.File.Exists(fileName))
				{
					return false;
				}

				IDictionary<string, string> properties;
				lock (SyncObj)
				{
					using (var stream = new BufferedStream(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
					{
						properties = new Properties(stream).ToDictionary();
					}
				}

				if (Log.IsDebugEnabled)
				{
					Log.Debug($"Load session parameters: {fileName}");
				}

				if (properties == null)
					return false;

				sessionParameters.FromProperties(properties);
				state.FromProperties(properties);
				return true;
			}
			catch (ArgumentException ex)
			{
				Log.Warn(ex.Message, ex);
			}
			catch (Exception e)
			{
				Log.Debug("Can't load session parameters. Cause: " + e.Message);
			}

			return false;
		}

		private void CreateLocators(string directory, string backupDirectory)
		{
			OutgoingLogFileLocator = new DefaultLogFileLocator(directory, ConfigAdapter.OutgoingStorageTemplate);
			BackupOutgoingLogFileLocator =
				new TimestampLogFileLocator(backupDirectory, ConfigAdapter.BackupOutgoingStorageTemplate);

			IncomingLogFileLocator = new DefaultLogFileLocator(directory, ConfigAdapter.IncomingStorageTemplate);
			BackupIncomingLogFileLocator =
				new TimestampLogFileLocator(backupDirectory, ConfigAdapter.BackupIncomingStorageTemplate);

			_propertyFileLocator = new DefaultLogFileLocator(directory, ConfigAdapter.PropertiesTemplate);
			QueueFileLocator = new DefaultLogFileLocator(directory, ConfigAdapter.OutgoingQueueTemplate);
		}

		private void CheckDirectories()
		{
			var directory = ConfigAdapter.StorageDirectory;
			Directory.CreateDirectory(directory);
			var backupDirectory = ConfigAdapter.BackupStorageDirectory;
			Directory.CreateDirectory(backupDirectory);
		}

		public virtual ILogFileLocator GetOutgoingLogFileLocator()
		{
			return OutgoingLogFileLocator;
		}

		public virtual void SetPropertyFileLocator(ILogFileLocator propertyFileLocator)
		{
			_propertyFileLocator = propertyFileLocator;
		}

		public virtual void SetQueueFileLocator(ILogFileLocator queueFileLocator)
		{
			QueueFileLocator = queueFileLocator;
		}

		public virtual void SetOutgoingLogFileLocator(ILogFileLocator outgoingLogFileLocator)
		{
			OutgoingLogFileLocator = outgoingLogFileLocator;
		}

		public virtual ILogFileLocator GetIncomingLogFileLocator()
		{
			return IncomingLogFileLocator;
		}

		public virtual void SetIncomingLogFileLocator(ILogFileLocator incomingLogFileLocator)
		{
			IncomingLogFileLocator = incomingLogFileLocator;
		}
	}
}