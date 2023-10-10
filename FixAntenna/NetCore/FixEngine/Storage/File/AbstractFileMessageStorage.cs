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
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.FixEngine.Storage.Timestamp;

namespace Epam.FixAntenna.NetCore.FixEngine.Storage.File
{
	/// <summary>
	/// Abstract file storage implementation.
	/// The base functionality of this class.
	/// </summary>
	internal abstract class AbstractFileMessageStorage : IMessageStorage
	{
		private readonly object _lock = new object();
		private ILogFileLocator _backupFileLocator;

		private volatile bool _isClosed = true;

		private string _file;

		private readonly ILog _log;
		private byte[] _timeFormatBuffer = Array.Empty<byte>();

		protected int FormatLength;
		protected volatile bool Disposed;

		protected internal FileStream AccessFile;
		protected internal Config Configuration;

		protected internal IStorageTimestamp StorageTimestamp;

		public AbstractFileMessageStorage(Config configuration)
		{
			_log = LogFactory.GetLog(GetType());

			Configuration = configuration;
			StorageTimestamp = StorageTimestampFactory.GetStorageTimestamp(configuration);
			ResetPrefixTimeZone();
		}

		/// <inheritdoc />
		public virtual void RetrieveMessages(long from, long to, IMessageStorageListener listener, bool blocking)
		{
			VerifyNotDisposed();
			RetrieveMessagesImplementation(from, to, listener, blocking);
		}

		protected abstract void RetrieveMessagesImplementation(long from, long to, IMessageStorageListener listener,
			bool blocking);

		/// <inheritdoc />
		public virtual byte[] RetrieveMessage(long num)
		{
			VerifyNotDisposed();

			var message = new byte[][] { null };
			RetrieveMessages(num, num, new MessageStorageListener(message), true);
			return message[0];
		}

		/// <summary>
		/// Initialize the storage. <br/>
		/// <b>This method should be called before file storage is used.
		/// Storage file and some variable (like <see cref="FormatLength"/>) are initialized in it.</b>
		/// </summary>
		/// <returns> the last sequence number </returns>
		/// <exception cref="IOException"> if I/O error occurred </exception>
		public virtual long Initialize()
		{
			VerifyNotDisposed();

			FormatLength = CalculateFormatLength();

			if (Initialized())
			{
				Close();
			}

			OpenStorageFile();
			var nextSequenceNumber = GetNextSequenceNumber();
			if (_log.IsDebugEnabled)
			{
				_log.Debug($"Initialise message storage with sequenceNumber: {nextSequenceNumber}");
			}

			return nextSequenceNumber;
		}

		/// <inheritdoc />
		public virtual void AppendMessage(byte[] timestampFormatted, byte[] message)
		{
			VerifyNotDisposed();
			AppendMessage(message); // todo: or better to throw exception here?
		}

		/// <inheritdoc />
		public virtual void AppendMessage(byte[] message)
		{
			VerifyNotDisposed();
			lock (_lock)
			{
				AppendMessage(message, 0, message.Length);
			}
		}

		/// <inheritdoc />
		public virtual void AppendMessage(byte[] timestampFormatted, byte[] message, int offset, int length)
		{
			VerifyNotDisposed();
			AppendMessage(message, offset, length); // todo: or better to throw exception here?
		}

		/// <inheritdoc />
		public virtual void AppendMessage(byte[] message, int offset, int length)
		{
			VerifyNotDisposed();
			lock (_lock)
			{
				var ticks = DateTimeHelper.CurrentTicks;
				AppendMessageInternal(ticks, message, offset, length);
			}
		}

		/// <summary>
		/// Appends message to storage
		/// </summary>
		/// <param name="ticks">the timestamp parameter </param>
		/// <param name="message"> the array of bytes </param>
		/// <param name="offset"> </param>
		/// <param name="length"> </param>
		/// <exception cref="IOException"> if I/O error occurred </exception>
		protected internal abstract long AppendMessageInternal(long ticks, byte[] message, int offset, int length);

		/// <inheritdoc />
		public virtual void BackupStorage(SessionParameters sessionParameters)
		{
			VerifyNotDisposed();
			lock (_lock)
			{
				var wasClosed = CloseStorage();
				BackupOrDeleteStorage(sessionParameters);
				if (wasClosed)
				{
					ReopenStorage();
				}
			}
		}

		/// <inheritdoc />
		public virtual void Close()
		{
			VerifyNotDisposed();
			_isClosed = true;
			if (AccessFile != null)
			{
				if (_log.IsTraceEnabled)
				{
					_log.Trace($"Closing storage file {FileName}");
				}

				AccessFile.Dispose();
			}

			_log.Debug("File storage closed");
		}

		/// <summary>
		/// This method is used to calculate <see cref="FormatLength"/>.
		/// Override it to change how <see cref="FormatLength"/> is calculated.
		/// </summary>
		/// <returns></returns>
		protected internal virtual int CalculateFormatLength()
		{
			return StorageTimestamp.GetFormatLength();
		}

		private void ResetPrefixTimeZone()
		{
			var timeZoneId = Configuration.GetProperty(Config.LogFilesTimeZone);

			if (string.IsNullOrEmpty(timeZoneId))
				return;

			if (DateTimeHelper.TryParseTimeZoneOffset(timeZoneId, out var offset))
			{
				StorageTimestamp.ResetTimeZone(offset);
				return;
			}

			_log.Warn($"Invalid \"{Config.LogFilesTimeZone}\" parameter: {timeZoneId}");
		}

		public virtual byte[] GetPrefixFormat(long ticks)
		{
			VerifyNotDisposed();
			if (_timeFormatBuffer.Length != FormatLength)
			{
				_timeFormatBuffer = new byte[FormatLength];
			}

			StorageTimestamp.Format(ticks, _timeFormatBuffer);
			return _timeFormatBuffer;
		}

		/// <summary>
		/// Gets or sets the file.
		/// </summary>
		public virtual string FileName
		{
			get
			{
				VerifyNotDisposed();
				return _file;
			}
			set
			{
				VerifyNotDisposed();
				_file = value;
			}
		}

		public virtual void SetBackupFileLocator(ILogFileLocator fileLocator)
		{
			VerifyNotDisposed();
			_backupFileLocator = fileLocator;
		}

		protected virtual void OpenStorageFile()
		{
			VerifyNotDisposed();

			if (_log.IsTraceEnabled)
			{
				_log.Trace($"Opening storage file {FileName}");
			}

			AccessFile = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
			_isClosed = false;
		}

		protected virtual bool Initialized()
		{
			VerifyNotDisposed();
			return !IsClosed;
		}

		/// <summary>
		/// Gets next sequence number.
		/// </summary>
		/// <exception cref="IOException"> if I/O error occurred </exception>
		protected abstract long GetNextSequenceNumber();

		public virtual void OpenStorage()
		{
			VerifyNotDisposed();
			_isClosed = false;
		}

		protected void NotifyListener(IMessageStorageListener listener, bool blocking, byte[] message)
		{
			VerifyNotDisposed();

			void ActionToExecute()
			{
				listener.OnMessage(message);
			}

			if (blocking)
			{
				ActionToExecute();
			}
			else
			{
				Task.Run(ActionToExecute);
			}
		}

		private void ReopenStorage()
		{
			Initialize();
		}

		private void BackupOrDeleteStorage(SessionParameters sessionParameters)
		{
			var configurationAdapter = new ConfigurationAdapter(sessionParameters.Configuration);
			switch (configurationAdapter.StorageCleanupMode)
			{
				case StorageCleanupMode.Backup:
					BackupStorageFile(FileName, _backupFileLocator.GetFileName(sessionParameters));
					break;
				case StorageCleanupMode.Delete:
					DeleteStorageFile(FileName);
					break;
			}
		}

		private bool CloseStorage()
		{
			if (IsClosed)
			{
				return false;
			}

			try
			{
				Close();
			}
			catch (IOException e)
			{
				_log.Error($"Error on close storage. Cause: {e}", e);
			}

			return true;

		}

		/// <summary>
		/// Backups storage file.
		/// </summary>
		/// <param name="fullPathToStorageFile"> the path to file of in/out file </param>
		/// <param name="fullPathToDestinationBackupFile"> the destination place to backup file </param>
		public virtual void BackupStorageFile(string fullPathToStorageFile, string fullPathToDestinationBackupFile)
		{
			BackupFile(fullPathToStorageFile, fullPathToDestinationBackupFile);
		}

		public virtual void BackupFile(string fullPathToStorageFile, string fullPathToDestinationBackupFile)
		{
			var storageFile = new FileInfo(fullPathToStorageFile);
			if (storageFile.Length > 0)
			{
				if (_log.IsDebugEnabled)
				{
					_log.Debug($"Backup storage file: {fullPathToStorageFile} to: {fullPathToDestinationBackupFile}");
				}

				try
				{
					System.IO.File.Move(fullPathToStorageFile, fullPathToDestinationBackupFile);
				}
				catch (Exception ex)
				{
					throw new IOException(
						$"Backup operation failed. Can not rename file {fullPathToStorageFile} to {fullPathToDestinationBackupFile}", ex);
				}
			}
			else
			{
				if (_log.IsDebugEnabled)
				{
					_log.Debug($"Backup storage file: {fullPathToStorageFile} was empty. Not required");
				}
			}
		}

		/// <summary>
		/// Deletes storage file.
		/// </summary>
		/// <param name="fullPathToStorageFile"> the path to in/out file </param>
		public virtual void DeleteStorageFile(string fullPathToStorageFile)
		{
			DeleteFile(fullPathToStorageFile);
		}

		public virtual void DeleteFile(string fullPathToStorageFile)
		{
			if (_log.IsDebugEnabled)
			{
				_log.Debug($"Delete storage file: {fullPathToStorageFile}");
			}

			new FileInfo(fullPathToStorageFile).Delete();
		}

		/// <summary>
		/// Returns true if storage is closed.
		/// </summary>
		protected bool IsClosed => _isClosed;

		private class MessageStorageListener : IMessageStorageListener
		{
			private readonly byte[][] _message;

			public MessageStorageListener(byte[][] message)
			{
				_message = message;
			}

			public void OnMessage(byte[] result)
			{
				_message[0] = result;
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (Disposed)
			{
				return;
			}

			if (disposing)
			{
				Close();
			}

			Disposed = true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void VerifyNotDisposed()
		{
			if (Disposed)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}
		}
	}
}