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

namespace Epam.FixAntenna.Tester
{
    public class Attributes : IAttributes
    {
        private List<Tuple<string, string>> _attributes = new List<Tuple<string, string>>();

        #region IAttributes interface

        /// <inheritdoc/>
        public int GetLength()
        {
            return _attributes.Count;
        }

        /// <inheritdoc/>
        public string GetQName(int index)
        {
            return GetAttrByIndex(index)?.Item1;
        }

        /// <inheritdoc/>
        public string GetValue(int index)
        {
            return GetAttrByIndex(index)?.Item2;
        }

        /// <inheritdoc/>
        public string GetValue(string name)
        {
            return GetAttrByName(name)?.Item2;
        }
        #endregion

        public void Add(string name, string value)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (GetAttrByName(name) != null) throw new ArgumentOutOfRangeException($"Attribute {name} already exists");

            _attributes.Add(new Tuple<string, string>(name, value));
        }

        private Tuple<string, string> GetAttrByIndex(int index)
        {
            if (0 <= index && index < _attributes.Count)
                return _attributes[index];
            else
                return null;
        }
        
        private Tuple<string, string> GetAttrByName(string name)
        {
            foreach (var atr in _attributes)
                if (atr.Item1 == name)
                    return atr;
            return null;
        }
    }
}