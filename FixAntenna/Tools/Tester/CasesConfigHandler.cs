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

using Epam.FixAntenna.Tester.Comparator;
using Epam.FixAntenna.Tester.Logger;
using Epam.FixAntenna.Tester.Stage;
using Epam.FixAntenna.Tester.Updater;
using System;
using System.Collections.Generic;
using System.IO;
using Epam.FixAntenna.NetCore.Common.Logging;

namespace Epam.FixAntenna.Tester
{
    public sealed class CasesConfigHandler : IDefaultHandler, IDisposable
    {
        private static readonly ILog _log = LogFactory.GetLog(typeof(CasesConfigHandler));

        private const string TASK = "task";
        public const string LOGGER_ATTRIBUTE = "logger";
        private const string TESTER_PREFIX = "Epam.FixAntenna.Tester.";
        private const string TRANSPORT = "transport";
        private static readonly string _transportPrefix =   TESTER_PREFIX + "Transport.";
        private static readonly string _updaterPrefix =     TESTER_PREFIX + "Updater.";
        private static readonly string _comparatorPrefix =  TESTER_PREFIX + "Comparator.";
        private static readonly string _logPrefix =         TESTER_PREFIX + "Logger.";
        private static readonly string _taskPrefix =        TESTER_PREFIX + "Task.";
        private const string CASE = "case";
        private const string PARAM = "param";
        private const string CLASS_NAME_ATTRIBUTE = "className";
        private const string NAME_ATTRIBUTE = "name";
        private const string EXPECT = "expect";
        private const string SEND = "send";
        private const string SEPARATOR = "separator";

        private CustomConcurrentDictionary<string, object> _sessions = new CustomConcurrentDictionary<string, object>();

        private Stack<string> _currentTag = new Stack<string>();
        private ResultCounter _counter = new ResultCounter();

        private string _caseName;
        private Case _currentCase;

        private ITransport _transport;

        private IDictionary<string, string> _currentParams;
        private string _currentParamName;
        private string _currentMessage = "";
        private IDictionary<string, string> _currentMessageAttributes = new CustomDictionary<string, string>();

        private Type _currentValidatorClass = typeof(IMessageComparator);

        private IMessageUpdater _currentUpdater;
        private Stack<ITask> _currentTask = new Stack<ITask>();
        private ICaseLogger _logger;
        private string _currentParamValue;
        private string _separator;

        private string _fileName;

        public CasesConfigHandler(string fileName)
        {
            this._fileName = fileName;
            this._currentParams = new CustomDictionary<string, string>();
        }

        public ResultCounter GetCounter()
        {
            return _counter;
        }

        public void StartElement(string uri, string localName, string qName, Attributes attributes)
        {
            try
            {
                _currentTag.Push(qName);
                ProcessStartElement(qName, attributes);
            }
            catch (Exception e)
            {
                _log.Error(e, e);
                throw new Exception("Unrecoverable error during the test");
            }
        }

        public void EndElement(string uri, string localName, string qName)
        {
            try
            {
                ProcessEndElement(qName);
                _currentTag.Pop();
            }
            catch (Exception e)
            {
                _log.Error(e, e);
                throw;
            }
        }

        private void ProcessStartElement(string qName, Attributes attributes)
        {
            if (CASE.Equals(qName))
            {
                InitCaseStuff(attributes);
            }
            else if (TRANSPORT.Equals(qName))
            {
                StartTransport(attributes);
            }
            else if (PARAM.Equals(qName))
            {
                _currentParamName = attributes.GetValue(NAME_ATTRIBUTE);
                _currentParamValue = "";
            }
            else if (TASK.Equals(qName))
            {
                _log.Debug("Creating " + TASK);
                _currentParams = new CustomDictionary<string, string>();
                _currentTask.Push( (ITask)System.Activator.CreateInstance(Type.GetType(_taskPrefix + attributes.GetValue(CLASS_NAME_ATTRIBUTE))) );
            }
            if (SEND.Equals(qName) || EXPECT.Equals(qName))
            {
                _currentMessageAttributes = GetProperties(attributes);
            }
        }

        private void ProcessEndElement(string qName)
        {
            if (CASE.Equals(qName))
            {
                if (_currentCase.IsSuccess())
                {
                    _counter.IncSuccess();
                }
                else
                {
                    _counter.IncFailed();
                }
                _currentCase.Close();
                DisposeTasks();
            }
            else if (PARAM.Equals(qName))
            {
                _currentParams[_currentParamName] = _currentParamValue;
            }
            else if (TRANSPORT.Equals(qName))
            {
                _transport.Dispose();
                _transport = null;
            }
            else if (TASK.Equals(qName))
            {
                _log.Debug("Initializing " + TASK + " with: " + _currentParams);
                _currentTask.Peek().Init(_currentParams, _sessions);
                _log.Debug("Executing " + TASK);
                _currentTask.Peek().DoTask();
            }
            else if (EXPECT.Equals(qName))
            {
                IMessageComparator comparator = (IMessageComparator)System.Activator.CreateInstance(_currentValidatorClass);
                comparator.SetMessageSeparator(_separator);
                _currentCase.RunReceiveStage(new ExpectedMessage(_currentMessage.Trim(), comparator), _currentMessageAttributes);
                _currentMessage = "";
                _currentMessageAttributes = new CustomDictionary<string, string>();
            }
            else if (SEND.Equals(qName))
            {
                try
                {
                    _currentCase.RunSendStage(new OutgoingMessage(_currentMessage.Trim(), _currentUpdater), _currentMessageAttributes);
                    _currentMessage = "";
                    _currentMessageAttributes = new CustomDictionary<string, string>();
                }
                catch (IOException e)
                {
                    _log.Error(e, e);
                }
            }
        }

        public void Characters(char[] chars, int start, int length)
        {
            if (PARAM.Equals(_currentTag.Peek()))
            {
                _currentParamValue += new string(chars, start, length);
            }

            if (EXPECT.Equals(_currentTag.Peek()) || SEND.Equals(_currentTag.Peek()))
            {
                _currentMessage += new string(chars, start, length);
            }
        }

        private void StartTransport(Attributes attributes)
        {
            _log.Debug("Initing transport");
            _transport = (ITransport)System.Activator.CreateInstance(Type.GetType(_transportPrefix + attributes.GetValue(CLASS_NAME_ATTRIBUTE)));
            _transport.Init(GetProperties(attributes));
            _log.Debug("Staring transport");
            _transport.Open();
            _log.Debug("Succussed");
            if (_currentCase == null)
            {
                _currentCase = new Case("(" + _fileName + ") " + _caseName, _transport, _logger);
            }
            else
            {
                _currentCase.ReinitTransport(_transport);
            }
        }

        private IDictionary<string, string> GetProperties(Attributes attributes)
        {
            int length = attributes.GetLength();
            IDictionary<string, string> map = new CustomDictionary<string, string>();
            for (int i = 0; i < length; i++)
            {
                map[attributes.GetQName(i)] = attributes.GetValue(i);
            }
            return map;
        }

        private void InitCaseStuff(Attributes attributes)
        {
            _caseName = attributes.GetValue(NAME_ATTRIBUTE);
            _log.Info("Case '" + _caseName + "' initialization");
            _separator = attributes.GetValue(SEPARATOR);
            if (string.ReferenceEquals(_separator, null))
            {
                _separator = "#";
                _log.Debug("Using default " + SEPARATOR + ":" + _separator);
            }
            else
            {
                _log.Debug("Using " + SEPARATOR + ":" + _separator);
            }
            InitLogger(attributes);
            InitComparator(attributes);
            InitUpdater(attributes);
        }

        private void InitUpdater(Attributes attributes)
        {
            _log.Debug("Initing message updater");
            string updaterClass = attributes.GetValue("updater");
            if (string.ReferenceEquals(updaterClass, null))
            {
                _log.Debug("Not specified, so using default");
                updaterClass = typeof(LazyUpdater).FullName;
            }
            else
            {
                updaterClass = _updaterPrefix + updaterClass;
            }
            _currentUpdater = (IMessageUpdater)System.Activator.CreateInstance(Type.GetType(updaterClass));
            _currentUpdater.SetMessageSeparator(_separator);
            _log.Debug("Success");
        }

        private void InitComparator(Attributes attributes)
        {
            _log.Debug("Initing comparator");
            string compClass = attributes.GetValue("comparator");
            if (string.ReferenceEquals(compClass, null))
            {
                _log.Debug("Not specified, so using default value");
                compClass = typeof(RegExpFlexibleComparator).FullName;
            }
            else
            {
                compClass = _comparatorPrefix + compClass;
            }
            _currentValidatorClass = Type.GetType(compClass);
            _log.Debug("Success");
        }

        private void InitLogger(Attributes attributes)
        {
            _log.Debug("Initing logger");
            string logClass = attributes.GetValue(LOGGER_ATTRIBUTE);
            if (string.ReferenceEquals(logClass, null))
            {
                _log.Debug("Not specified, so using default");
                logClass = typeof(LoggingCaseLogger).FullName;
            }
            else
            {
                logClass = _logPrefix + logClass;
            }
            _log.Debug("class:" + logClass);
            _logger = (ICaseLogger)System.Activator.CreateInstance(Type.GetType(logClass));
            _log.Debug("Success");
        }

        private void DisposeTasks()
        {
            while (_currentTask.Count > 0)
            {
                _currentTask.Pop().Dispose();
            }
        }

        public void Dispose()
        {
            _transport?.Dispose();
            _transport = null;
            DisposeTasks();
        }
    }
}