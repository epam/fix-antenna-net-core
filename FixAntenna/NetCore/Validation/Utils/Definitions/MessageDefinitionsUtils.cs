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
	internal sealed class MessageDefinitionsUtils : IFixMessageDefinitions<Msgdef>
	{
		/// <summary>
		/// Field messageTypeList
		/// </summary>
		private IDictionary<string, Msgdef> _messageTypeList;

		/// <summary>
		/// Constructor MessageTypes creates a new MessageTypes instance.
		/// </summary>
		/// <param name="dictionaryTypesContainer"> </param>
		public MessageDefinitionsUtils(DictionaryTypes dictionaryTypesContainer)
		{
			if (dictionaryTypesContainer == null)
			{
				throw new ArgumentNullException(nameof(dictionaryTypesContainer));
			}

			Put(GetMessageDefinitions(dictionaryTypesContainer));
		}

		private List<Msgdef> GetMessageDefinitions(DictionaryTypes dictionaryTypesContainer)
		{
			var fixdicElements = dictionaryTypesContainer.Dictionaries.OfType<Fixdic>();
			return fixdicElements.SelectMany(x => x.Msgdic.Msgdef).ToList();
		}

		/// <summary>
		/// Method GetMessageTypes
		/// </summary>
		/// <seealso cref="IFixMessageDefinitions{T}.GetMessageTypes()"> </seealso>
		public ISet<string> GetMessageTypes()
		{
			return new HashSet<string>(_messageTypeList.Keys);
		}

		/// <summary>
		/// Method getRequiredTag returns the requiredTag of this FixDefMap object.
		/// </summary>
		/// <param name="fieldList">             List of field,groups and blocks </param>
		/// <param name="blockDefinitionsUtils"> Block definition util </param>
		/// <returns> the requiredTag of this FixDefMap object. </returns>
		public IList<int> GetRequiredTags(IList<object> fieldList, BlockDefinitionsUtils blockDefinitionsUtils)
		{
			var requiredTags = new List<int>();
			var listLength = fieldList.Count;
			for (var objectCount = 0; objectCount < listLength; objectCount++)
			{
				var obj = fieldList[objectCount];
				if (obj is Field)
				{
					var field = (Field)obj;
					if ("Y".Equals(field.Req))
					{
						requiredTags.Add(field.Tag);
					}
				}
				else if (obj is Group)
				{
					var group = (Group)obj;
					if ("Y".Equals(@group.Req))
					{
						requiredTags.AddRange(GetRequiredTags(@group.Content,
							blockDefinitionsUtils));
					}
				}
				else if (obj is Block)
				{
					var block = (Block)obj;
					if ("Y".Equals(block.Req))
					{
						var blockIdent = block.Idref;
						IList<object> listFromBlock = blockDefinitionsUtils.Get(blockIdent).FieldOrDescrOrGroup;
						requiredTags.AddRange(GetRequiredTags(listFromBlock, blockDefinitionsUtils));
					}
				}
			}

			return requiredTags;
		}

		/// <summary>
		/// Method getRequiredTag returns the requiredTag of this FixDefMap object.
		/// </summary>
		/// <param name="messageType">           Type of message </param>
		/// <param name="blockDefinitionsUtils"> Block definition util </param>
		/// <returns> the requiredTag of this FixDefMap object. </returns>
		/// <exception cref="MessageDefinitionsException"> if message does not exist </exception>
		public IList<int> GetRequiredTags(string messageType, BlockDefinitionsUtils blockDefinitionsUtils)
		{
			IList<int> requiredTags;
			var msgdef = _messageTypeList[messageType];
			CheckMessageDefinition(msgdef);
			IList<object> fieldList = msgdef.FieldOrDescrOrAlias;
			requiredTags = GetRequiredTags(fieldList, blockDefinitionsUtils);
			return requiredTags;
		}

		/// <inheritdoc />
		public Msgdef Get(string messageType)
		{
			_messageTypeList.TryGetValue(messageType, out var value);
			return value;
		}

		public bool IsMessageTypeExist(string messageType)
		{
			return _messageTypeList.ContainsKey(messageType);
		}

		private void CheckMessageDefinition(Msgdef msgdef)
		{
			if (msgdef == null)
			{
				throw new MessageDefinitionsException(
					FixErrorBuilder.CreateBuilder().BuildError(FixErrorCode.InvalidMsgtype, "Invalid message type"),
					null);
			}
		}

		/// <inheritdoc />
		public void Put(IList<Msgdef> elements)
		{
			if (_messageTypeList == null)
			{
				_messageTypeList = new Dictionary<string, Msgdef>();
			}

			foreach (var msgDef in elements)
			{
				_messageTypeList[msgDef.Msgtype] = msgDef;
			}
		}

		/// <inheritdoc />
		public ICollection<Msgdef> Get()
		{
			return _messageTypeList.Values;
		}

		/// <inheritdoc />
		public bool Contains(string messageType)
		{
			return _messageTypeList.ContainsKey(messageType);
		}

		/// <summary>
		/// Method returns messageType by name of message
		/// </summary>
		/// <param name="msgType"> Name of message </param>
		/// <returns> Type of Message </returns>
		/// <exception cref="MessageDefinitionsException"> if message does not exist </exception>
		public string GetMessageTypeByName(string msgType)
		{
			var mCollection = Get();
			foreach (var msgdef in mCollection)
			{
				if (msgdef.Name.Equals(msgType))
				{
					return msgdef.Msgtype;
				}
			}

			CheckMessageDefinition(null);
			return null;
		}
	}
}