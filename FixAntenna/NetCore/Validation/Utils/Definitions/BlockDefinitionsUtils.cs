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
using System.Linq;
using Epam.FixAntenna.NetCore.Dictionary;
using Epam.FixAntenna.NetCore.Validation.Entities;
using Epam.FixAntenna.NetCore.Validation.Error;
using Epam.FixAntenna.NetCore.Validation.Exceptions.Mapping;

namespace Epam.FixAntenna.NetCore.Validation.Utils.Definitions
{
	internal sealed class BlockDefinitionsUtils : IFixMessageDefinitions<Blockdef>
	{
		private IDictionary<string, Blockdef> _blockDefMap;

		/// <summary>
		/// Creates the <c>BlockDefinitionsUtils</c>.
		/// </summary>
		/// <param name="dictionaryTypesContainer"> the dictionary </param>
		public BlockDefinitionsUtils(DictionaryTypes dictionaryTypesContainer)
		{
			if (dictionaryTypesContainer == null)
			{
				throw new ArgumentNullException(nameof(dictionaryTypesContainer));
			}

			Put(GetBlockDefinitions(dictionaryTypesContainer));
		}

		private List<Blockdef> GetBlockDefinitions(DictionaryTypes dictionaryTypesContainer)
		{
			var fixdicElements = dictionaryTypesContainer.Dictionaries.OfType<Fixdic>();
			return fixdicElements.SelectMany(x => x.Msgdic.Blockdef).ToList();
		}

		/// <summary>
		/// Gets set of block
		/// </summary>
		public ISet<string> GetMessageTypes()
		{
			return new HashSet<string>(_blockDefMap.Keys);
		}

		/// <summary>
		/// Gets required tags for message.
		/// </summary>
		/// <param name="blockName"> the block name </param>
		public IList<int> GetRequiredTags(string blockName)
		{
			IList<int> requiredTags = new List<int>();
			_blockDefMap.TryGetValue(blockName, out var blockdef);
			CheckBlock(blockdef, blockName);
			foreach (var obj in blockdef.FieldOrDescrOrGroup)
			{
				if (obj is Field)
				{
					var field = (Field)obj;
					if ("Y".Equals(field.Req))
					{
						requiredTags.Add(field.Tag);
					}
				}
			}

			return requiredTags;
		}

		private void CheckBlock(Blockdef blockdef, string blockName)
		{
			if (blockdef == null)
			{
				throw new BlockDefinitionsException(
					FixErrorBuilder.CreateBuilder().BuildError(FixErrorCode.Other, blockName), null);
			}
		}

		/// <summary>
		/// Puts the blocks to the map.
		/// </summary>
		/// <param name="elements"> the block collection </param>
		public void Put(IList<Blockdef> elements)
		{
			if (_blockDefMap == null)
			{
				_blockDefMap = new Dictionary<string, Blockdef>();
			}

			foreach (var blockDef in elements)
			{
				_blockDefMap[blockDef.Id] = blockDef;
			}
		}

		/// <summary>
		/// Gets the collection of block.
		/// </summary>
		public ICollection<Blockdef> Get()
		{
			return _blockDefMap.Values;
		}

		/// <summary>
		/// Gets the block.
		/// </summary>
		/// <param name="blockName"> the block name </param>
		public Blockdef Get(string blockName)
		{
			_blockDefMap.TryGetValue(blockName, out var blockdef);
			CheckBlock(blockdef, blockName);
			return blockdef;
		}

		/// <summary>
		/// Checks if block with name blockName exists.
		/// </summary>
		/// <param name="blockName"> the  block name </param>
		public bool Contains(string blockName)
		{
			return GetMessageTypes().Contains(blockName);
		}
	}
}