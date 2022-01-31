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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Xml;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Dictionary;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation.Entities;
using Epam.FixAntenna.NetCore.Validation.Error;
using Epam.FixAntenna.NetCore.Validation.Exceptions;
using Epam.FixAntenna.NetCore.Validation.Exceptions.Mapping;
using Epam.FixAntenna.NetCore.Validation.Utils.Cache;
using Epam.FixAntenna.NetCore.Validation.Utils.Containers;
using Epam.FixAntenna.NetCore.Validation.Utils.Definitions;
using Epam.FixAntenna.NetCore.Validation.Validators.Condition;
using Epam.FixAntenna.NetCore.Validation.Validators.Condition.Container;

namespace Epam.FixAntenna.NetCore.Validation.Utils
{
	/// <summary>
	/// Utility to work with FIX messages, such as validate,
	/// returns custom data of message and etc.
	/// </summary>
	internal sealed class FixUtil
	{
		private static readonly IDictionary<int, Field> EmptyFieldMap = new Dictionary<int, Field>();
		private static FixErrorBuilder _fixErrorBuilder = FixErrorBuilder.CreateBuilder();
		private readonly BlockDefinitionsUtils _blockDefinitionsUtils;

		private readonly Dictionary<string, IConditionalMessage> _conditionsCache =
			new Dictionary<string, IConditionalMessage>();

		private readonly DictionaryTypes _dictionaryTypes;
		private readonly IDictionary<string, IList<int>> _fieldCache = new Dictionary<string, IList<int>>();

		private readonly IDictionary<string, IList<Fielddef>> _fieldDefMessageTypeCache =
			new Dictionary<string, IList<Fielddef>>();

		private readonly IDictionary<string, Fielddef> _fieldDefNameCache = new Dictionary<string, Fielddef>();
		private readonly IDictionary<int, Fielddef> _fieldDefTagIdCache = new Dictionary<int, Fielddef>();
		private readonly IDictionary<string, ISet<int>> _fieldRequiredCache = new Dictionary<string, ISet<int>>();
		private readonly MessagesCache _groupsCacheWithInternalGroups = new MessagesCache();
		private readonly MessageDefinitionsUtils _msgDefinitionsUtils;
		private readonly Blockdef _smhDef;
		private readonly ISet<int> _smhTags = new HashSet<int>();
		private readonly Blockdef _smtDef;
		private readonly ISet<int> _smtTags = new HashSet<int>();
		private readonly FixVersionContainer _versionContainer;
		private int[] _allTags;
		private Fielddef[] _fieldsdef;
		private IList<Valblockdef> _valblockdefs;

		/// <summary>
		/// Constructor FixUtils creates a new FixUtils instance.
		/// </summary>
		/// <param name="version"> the Version of FIX protocol </param>
		/// <exception cref="ArgumentException"> </exception>
		public FixUtil(FixVersionContainer version) : this(version, null)
		{
		}

		/// <summary>
		/// Constructor FixUtils creates a new FixUtils instance.
		/// </summary>
		/// <param name="version">       the Version of FIX protocol </param>
		/// <param name="appFixVersion"> the App version of FIX protocol </param>
		/// <exception cref="ArgumentException"> </exception>
		public FixUtil(FixVersionContainer version, FixVersionContainer appFixVersion)
		{
			if (version == null)
			{
				throw new ArgumentException();
			}

			if (appFixVersion != null)
			{
				if (version.FixVersion == FixVersion.Fixt11)
				{
					_versionContainer = appFixVersion;
				}
				else
				{
					_versionContainer = version;
				}
			}
			else
			{
				_versionContainer = version;
			}

			_dictionaryTypes = FixDictionaryFactory.Instance.GetDictionaries(version, appFixVersion);
			_msgDefinitionsUtils = new MessageDefinitionsUtils(_dictionaryTypes);
			_blockDefinitionsUtils = new BlockDefinitionsUtils(_dictionaryTypes);
			_smhDef = _blockDefinitionsUtils.Get(Constants.Smh);
			_smhTags.AddRange(GetFieldsTags(_smhDef.FieldOrDescrOrGroup));
			_smtDef = _blockDefinitionsUtils.Get(Constants.Smt);
			_smtTags.AddRange(GetFieldsTags(_smtDef.FieldOrDescrOrGroup));
			PrepareFieldsDefinitions(_dictionaryTypes);
			// cached fields after initialization of all utils
			PutFields();
			PutRequiredField();

			// cached condition after initialization of all utils
			PutConditions();
			// cached fieldDefs for messageTypes
			PrepareFieldDefCache();
			// cached fieldDefs for messageTypes
			PrepareValblockdefs();
			// cached groups data
			PrepareGroupsCache();
		}

		/// <summary>
		/// Gets val blocks.
		/// </summary>
		public IList<Valblockdef> GetValblockdefs()
		{
			return _valblockdefs;
		}

		/// <summary>
		/// Gets tags.
		/// </summary>
		public int[] GetAllTags()
		{
			return _allTags;
		}

		public bool IsHeader(int tag)
		{
			return _smhTags.Contains(tag);
		}

		public bool IsTrailer(int tag)
		{
			return _smtTags.Contains(tag);
		}

		/// <summary>
		/// Gets fields.
		/// </summary>
		public Fielddef[] GetFieldDef()
		{
			return _fieldsdef;
		}

		/// <summary>
		/// Gets conditional cache.
		/// </summary>
		public Dictionary<string, IConditionalMessage> GetConditionalCache()
		{
			return _conditionsCache;
		}

		/// <summary>
		/// Returns map of condition operators from input list
		/// </summary>
		/// <param name="list">            the input list. </param>
		/// <param name="rootTag">         the root tag of group. </param>
		/// <param name="conditionalType"> the type of conditional FIX message. </param>
		/// <returns> Map of condition operators </returns>
		private IConditionalMessage GetConditions<T1>(IList<T1> list, int rootTag, ConditionalType conditionalType)
		{
			var listLength = list.Count;
			var conditions = new Dictionary<int, ICondition>();
			var conditionalMessage = BuildConditionalMessage(conditionalType, rootTag, conditions);
			for (var objectCount = 0; objectCount < listLength; objectCount++)
			{
				var obj = list[objectCount];
				switch (obj)
				{
					case Field field:
					{
						var condition = field.Condreq;
						BuildCondition(conditions, field, condition);
						break;
					}
					case Group group:
					{
						var groupRootTag = @group.Nofield;
						var condGroup = GetConditions(@group.Content, groupRootTag, ConditionalType.Group);
						condGroup.SetRequired(@group.Req != null && "Y".Equals(@group.Req));
						if (conditions.TryGetValue(groupRootTag, out var condition))
						{
							// added into group conditional list.
							condGroup.GetConditionMap()[groupRootTag] = condition;
							// remove from the root message conditional list.
							conditions.Remove(groupRootTag);
						}

						conditionalMessage.AddConditionalGroup((ConditionalGroup)condGroup);
						break;
					}
					case Block block:
					{
						var blockIdent = block.Idref;
						var listFromBlock = _blockDefinitionsUtils.Get(blockIdent).FieldOrDescrOrGroup;
						var conditionalBlock = GetConditions(listFromBlock, block.Tag,
							ConditionalType.Block);
						if (block.Condreq != null)
						{
							var conditionValidateParser = new ConditionValidateParser(block.Condreq);
							((ConditionalBlock)conditionalBlock).SetCondition(conditionValidateParser.GetCondition());
						}

						conditionalBlock.SetRequired(block.Req != null && "Y".Equals(block.Req));
						conditionalMessage.AddConditionalBlock((ConditionalBlock)conditionalBlock);
						break;
					}
				}
			}

			return conditionalMessage;
		}

		//TODO: need to create implementation of LinkedHashMap in .net
		private void BuildCondition(Dictionary<int, ICondition> conditions, Field field, string condition)
		{
			if (condition != null)
			{
				var conditionValidateParser = new ConditionValidateParser(condition);
				conditions.Add(field.Tag, conditionValidateParser.GetCondition());
			}
		}

		private IConditionalMessage BuildConditionalMessage(ConditionalType conditionalType, int rootTag,
			IDictionary<int, ICondition> conditions)
		{
			IConditionalMessage conditionalMessage;
			switch (conditionalType)
			{
				case ConditionalType.Message:
					conditionalMessage = new ConditionalMessage(rootTag, conditions);
					break;
				case ConditionalType.Group:
					conditionalMessage = new ConditionalGroup(rootTag, conditions);
					break;
				case ConditionalType.Block:
					conditionalMessage = new ConditionalBlock(rootTag, conditions);
					break;
				default:
					conditionalMessage = new ConditionalMessage(rootTag, conditions);
					break;
			}

			return conditionalMessage;
		}

		/// <summary>
		/// Puts fields into cache
		/// </summary>
		private void PutFields()
		{
			var msgdefCollection = _msgDefinitionsUtils.Get();
			foreach (var msgdef in msgdefCollection)
			{
				IList<object> list = msgdef.FieldOrDescrOrAlias;
				_fieldCache[msgdef.Msgtype] = GetFieldsTags(list);
			}

			// puts header fields
			var blockdef = _blockDefinitionsUtils.Get(Constants.Smh);
			_fieldCache[Constants.Smh] = GetFieldsTags(blockdef.FieldOrDescrOrGroup);

			// puts trailer fields
			blockdef = _blockDefinitionsUtils.Get(Constants.Smt);
			_fieldCache[Constants.Smt] = GetFieldsTags(blockdef.FieldOrDescrOrGroup);
		}

		private void PutRequiredField()
		{
			foreach (var type in _msgDefinitionsUtils.GetMessageTypes())
			{
				_fieldRequiredCache[type] = BuildRequiredTagsForMessage(type);
			}
		}

		/// <summary>
		/// Gets tags for message.
		/// </summary>
		/// <param name="messageType"> the message type of message </param>
		public IList<int> GetTagsByMsgType(string messageType)
		{
			return _fieldCache[messageType];
		}

		/// <summary>
		/// Gets fields for message.
		/// </summary>
		/// <param name="messageType"> the message type of message </param>
		public IList<Fielddef> GetFieldsByMessageType(string messageType)
		{
			var fielddefs = new List<Fielddef>(_fieldDefMessageTypeCache[Constants.Smh]);
			fielddefs.AddRange(_fieldDefMessageTypeCache[messageType]);
			fielddefs.AddRange(_fieldDefMessageTypeCache[Constants.Smt]);
			return fielddefs;
		}

		/// <summary>
		/// Gets fix version.
		/// </summary>
		public FixVersion GetVersion()
		{
			return _versionContainer.FixVersion;
		}

		public FixVersionContainer GetVersionContainer()
		{
			return _versionContainer;
		}

		/// <summary>
		/// Gets field.
		/// </summary>
		/// <param name="tag"> the tag </param>
		public Fielddef GetFieldDefByTag(int tag)
		{
			_fieldDefTagIdCache.TryGetValue(tag, out var value);
			return value;
		}

		/// <summary>
		/// Gets field.
		/// </summary>
		/// <param name="name"> the field name </param>
		public Fielddef GetFieldDefByName(string name)
		{
			var nameBasedKey = name.ToLower();
			if (_fieldDefNameCache.ContainsKey(nameBasedKey))
			{
				return _fieldDefNameCache[name.ToLower()];
			}

			var excText = new StringBuilder();
			excText.Append("Unknown field name ");
			excText.Append("[");
			excText.Append(name);
			excText.Append("]");
			excText.Append(" in FIX version ");
			excText.Append("[");
			excText.Append(_versionContainer.FixVersion);
			excText.Append("]");
			throw new DictionaryRuntimeException(excText.ToString());
		}

		/// <summary>
		/// Gets field tag.
		/// </summary>
		/// <param name="name"> the field name </param>
		public int GetFieldTagByName(string name)
		{
			return GetFieldDefByName(name).Tag;
		}

		/// <summary>
		/// Gets field type by field name.
		/// </summary>
		/// <param name="name"> the field name </param>
		public string GetFieldTypeByFieldName(string name)
		{
			return GetFieldDefByName(name).Type;
		}

		/// <summary>
		/// Gets field type by field tag.
		/// </summary>
		/// <param name="tag"> the filed tag. </param>
		public string GetFieldTypeByFieldTag(int tag)
		{
			return GetFieldDefByTag(tag).Type;
		}

		/// <summary>
		/// Find group by start tag.
		/// </summary>
		/// <param name="msgType">    the message type. </param>
		/// <param name="startTagId"> the start tag if repeating group. </param>
		public Group FindGroup(string msgType, int startTagId)
		{
			var msgContent = GetMessageDefUtils().Get(msgType).FieldOrDescrOrAlias;
			if (msgContent == null)
			{
				var excText = new StringBuilder();
				excText.Append("Unknown message type ").Append("[").Append(msgType).Append("]")
					.Append(" in FIX version ").Append("[").Append(_versionContainer.FixVersion).Append("]");
				throw new DictionaryRuntimeException(excText.ToString());
			}

			return SearchGroupInContent(msgContent, startTagId);
		}

		private Group SearchGroupInContent(IList<object> content, int startTagId)
		{
			foreach (var o in content)
			{
				switch (o)
				{
					case Group grp when grp.Startfield == startTagId:
						return grp;
					case Group grp:
					{
						grp = SearchGroupInContent(grp.Content, startTagId);
						if (grp != null)
						{
							return grp;
						}
						break;
					}
					case Block block:
					{
						var blockIdent = block.Idref;
						var blockContent = _blockDefinitionsUtils.Get(blockIdent).FieldOrDescrOrGroup;
						var grp = SearchGroupInContent(blockContent, startTagId);
						if (grp != null)
						{
							return grp;
						}
						break;
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Get repeating group content.
		/// </summary>
		/// <param name="msgType">    the message type. </param>
		/// <param name="startTagId"> the start tag if repeating group. </param>
		public IList<object> GetGroupContent(string msgType, int startTagId)
		{
			var group = FindGroup(msgType, startTagId);
			if (group != null)
			{
				return @group.Content;
			}

			var excText = new StringBuilder();
			excText.Append("Unknown repeating group with start tag ");
			excText.Append("[");
			excText.Append(startTagId);
			excText.Append("]");
			excText.Append(" in message of type ");
			excText.Append("[");
			excText.Append(msgType);
			excText.Append("]");
			excText.Append(" in FIX version ");
			excText.Append("[");
			excText.Append(_versionContainer.FixVersion);
			excText.Append("]");
			throw new DictionaryRuntimeException(excText.ToString());
		}

		/// <summary>
		/// Get all field defs in repeating group.
		/// </summary>
		/// <param name="msgType">    the message type. </param>
		/// <param name="startTagId"> the start tag if repeating group. </param>
		public IList<Fielddef> GetGroupFieldDefs(string msgType, int startTagId)
		{
			return GetFieldDefs(GetGroupContent(msgType, startTagId), true);
		}

		/// <summary>
		/// Get message type field def hierarchy.
		/// </summary>
		/// <param name="msgType"> the message type. </param>
		public IList<object> GetMessageFieldDefHier(string msgType)
		{
			var smhContent = GetSmhContentHier();
			var bodyContent = GetFieldDefsHier(_msgDefinitionsUtils.Get(msgType).FieldOrDescrOrAlias);
			var smtContent = GetSmtContentHier();

			var fielddefs = new List<object>(smhContent.Count + bodyContent.Count + smtContent.Count);
			fielddefs.AddRange(smhContent);
			fielddefs.AddRange(bodyContent);
			fielddefs.AddRange(smtContent);
			return fielddefs;
		}

		private IList<object> GetSmhContentHier()
		{
			var blockdef = _blockDefinitionsUtils.Get(Constants.Smh);
			return GetFieldDefsHier(blockdef.FieldOrDescrOrGroup);
		}

		private IList<object> GetSmtContentHier()
		{
			var blockdef = _blockDefinitionsUtils.Get(Constants.Smt);
			return GetFieldDefsHier(blockdef.FieldOrDescrOrGroup);
		}

		/// <summary>
		/// Method isTagDefinedForMessage, verifies whether the tag is present in a
		/// message by type of message.
		/// </summary>
		/// <param name="msgType"> the message type </param>
		/// <param name="tagNum">  the num of tag </param>
		/// <returns> true if presents, otherwise false </returns>
		public bool IsTagDefinedForMessage(string msgType, int tagNum)
		{
			return IsContainsTagInFieldsList(msgType, tagNum) || IsContainsTagInFieldsList(Constants.Smh, tagNum) ||
					IsContainsTagInFieldsList(Constants.Smt, tagNum) ||
					msgType.StartsWith("U", StringComparison.Ordinal);
		}

		/// <summary>
		/// Method getRequiredTagsForMessage returns array of required tags, or empty
		/// array if message does not have required tags.
		/// </summary>
		/// <param name="msgType"> the type of message </param>
		/// <returns> int[] Array of required tags </returns>
		/// <exception cref="MessageDefinitionsException">if message does not exist </exception>
		public ISet<int> GetRequiredTagsForMessage(string msgType)
		{
			var reqTags = _fieldRequiredCache[msgType];
			if (reqTags == null)
			{
				throw new MessageDefinitionsException(
					new FixError(FixErrorCode.InvalidMsgtype, "Message type '" + msgType + "' does not exist", null),
					new Exception().InnerException);
			}

			return reqTags;
		}

		private ISet<int> BuildRequiredTagsForMessage(string msgType)
		{
			ISet<int> requiredTags = new HashSet<int>();
			requiredTags.AddRange(_msgDefinitionsUtils.GetRequiredTags(msgType, _blockDefinitionsUtils));
			requiredTags.AddRange(_blockDefinitionsUtils.GetRequiredTags(Constants.Smh));
			requiredTags.AddRange(_blockDefinitionsUtils.GetRequiredTags(Constants.Smt));
			return requiredTags;
		}

		/// <summary>
		/// Verifies whether the tag is defines for message by type of message
		/// </summary>
		/// <param name="shortName"> the type of message </param>
		/// <param name="tagNum">    the tag </param>
		/// <returns> <c>true</c> if defines, otherwise <c>false</c> </returns>
		public bool IsTagDefinedForMessageOrBlock(string shortName, int tagNum)
		{
			var msgDefinitions = GetDefinition(shortName);
			if (msgDefinitions == null)
			{
				return false;
			}

			switch (msgDefinitions)
			{
				case Msgdef msgdef:
					return IsContainsTagInFieldsList(msgdef.FieldOrDescrOrAlias, tagNum);
				case Blockdef blockdef:
					return IsContainsTagInFieldsList(blockdef.FieldOrDescrOrGroup, tagNum);
				default:
					return false;
			}
		}

		/// <summary>
		/// Checks if fields list of message Contains tags from block
		/// </summary>
		/// <param name="list">      the list of field of block </param>
		/// <param name="fixMessage"> the field list of message </param>
		/// <returns> true if Contains in otherwise false </returns>
		public bool HasRequiredTagInMessage<T1>(IList<T1> list, Message.FixMessage fixMessage)
		{
			return HasRequiredTagInMessage(list, fixMessage, false);
		}

		public bool HasRequiredTagInMessage<T1>(IList<T1> list, Message.FixMessage fixMessage, bool withInternalGroup)
		{
			var listLength = list.Count;
			for (var objectIndex = 0; objectIndex < listLength; objectIndex++)
			{
				var obj = list[objectIndex];
				switch (obj)
				{
					case Field field:
					{
						if (fixMessage.IsTagExists(field.Tag))
						{
							return true;
						}

						break;
					}
					case Block block:
					{
						var blockIdent = block.Idref;
						IList<object> listFromBlock = _blockDefinitionsUtils.Get(blockIdent).FieldOrDescrOrGroup;
						if (HasRequiredTagInMessage(listFromBlock, fixMessage, withInternalGroup))
						{
							return true;
						}

						break;
					}
					default:
					{
						if (withInternalGroup && obj is Group grp)
						{
							int? nofield = grp.Nofield;
							IList<object> groupContent = grp.Content;
							if (HasRequiredTagInMessage(groupContent, fixMessage, withInternalGroup))
							{
								return true;
							}
						}

						break;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Gets group tags, the returned result includes the inner groups.
		/// </summary>
		/// <param name="msgType">   the message type </param>
		/// <param name="groupTag">  the group tag </param>
		/// <param name="fixMessage"> the list of fields </param>
		public IDictionary<int, Field> GetGroupTagsWithInternalGroups(string msgType, int groupTag,
			Message.FixMessage fixMessage)
		{
			return GetGroupsTags(msgType, groupTag, fixMessage, true);
		}

		/// <summary>
		/// Gets group tags, the returned result does not include the inner groups.
		/// </summary>
		/// <param name="msgType">   the message type </param>
		/// <param name="tag">       the tag </param>
		/// <param name="fixMessage"> the list of fields </param>
		public IDictionary<int, Field> GetGroupTagsWithOutInternalGroups(string msgType, int tag,
			Message.FixMessage fixMessage)
		{
			return GetGroupsTags(msgType, tag, fixMessage, false);
		}

		/// <summary>
		/// Gets group tags.
		/// </summary>
		/// <param name="msgType">           the message type </param>
		/// <param name="groupTag">          the group tag </param>
		/// <param name="fixMessage">         the list of fields </param>
		/// <param name="withInternalGroup"> if flag is true the inner groups will be included </param>
		public IDictionary<int, Field> GetGroupsTags(string msgType, int groupTag, Message.FixMessage fixMessage,
			bool withInternalGroup)
		{
			if (groupTag == -1)
			{
				return EmptyFieldMap;
			}

			var msgdef = _msgDefinitionsUtils.Get(msgType);
			if (msgdef == null)
			{
				return EmptyFieldMap;
			}

			var objectList = msgdef.FieldOrDescrOrAlias;
			return GetGroupFields(groupTag, objectList, fixMessage, withInternalGroup);
		}

		private bool CheckNeededBlock<T1>(Message.FixMessage fixMessage, Block block, IList<T1> listFromBlock)
		{
			return HasRequiredTagInMessage(listFromBlock, fixMessage) ||
					IsConditionalRequired(block.Condreq, fixMessage);
		}

		/// <summary>
		/// Checks if tag exist.
		/// </summary>
		/// <param name="tag"> the tag </param>
		public bool IsKnownTag(int tag)
		{
			foreach (var allTag in _allTags)
			{
				if (allTag == tag)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Checks if group <c>groupTag</c> defined for message type <c>msgType</c>
		/// </summary>
		/// <param name="groupTag"> the group tag </param>
		/// <param name="msgType">  the message type </param>
		public bool IsGroupTag(string msgType, int groupTag)
		{
			var gc = _groupsCacheWithInternalGroups.Get(msgType);
			if (gc == null)
			{
				return false;
			}

			return gc.GetGroupCache(groupTag) != null;
		}

		/// <summary>
		/// Returns start tag for group by goup tag.
		/// </summary>
		/// <param name="msgType"> Type of message. </param>
		/// <param name="tag">     Tag of Group length. </param>
		/// <returns> Start field tag of group, if msgType does not exist return -1 </returns>
		public int GetStartTagForGroup(string msgType, int tag)
		{
			var msgdef = _msgDefinitionsUtils.Get(msgType);
			if (msgdef == null)
			{
				return -1;
			}

			// this implementations does not search in inner blocks and SMH, SMT
			IList<object> objectList = msgdef.FieldOrDescrOrAlias;
			return GetStartTagForGroup(tag, objectList);
		}

		/// <summary>
		/// Method getDefUtils returns the defUtils of this FixUtils object.
		/// </summary>
		/// <returns> the defUtils of this FixUtils object. </returns>
		public MessageDefinitionsUtils GetMessageDefUtils()
		{
			return _msgDefinitionsUtils;
		}

		/// <summary>
		/// Method getBlockDefUtils returns the blockDefUtils of this FixUtils
		/// object.
		/// </summary>
		/// <returns> the blockDefUtils of this FixUtils object. </returns>
		public BlockDefinitionsUtils GetBlockDefUtils()
		{
			return _blockDefinitionsUtils;
		}

		/// <summary>
		/// Method getFixdic returns the fixdic of this FixUtils object.
		/// </summary>
		/// <returns> the fixdic of this FixUtils object. </returns>
		public DictionaryTypes GetFixdic()
		{
			return _dictionaryTypes;
		}

		/// <summary>
		/// Method getSmhDef returns the smhDef of this FixUtils object.
		/// </summary>
		/// <returns> the smhDef of this FixUtils object. </returns>
		public Blockdef GetSmhDef()
		{
			return _smhDef;
		}

		/// <summary>
		/// Method getSmtDef returns the smtDef of this FixUtils object.
		/// </summary>
		/// <returns> the smtDef of this FixUtils object. </returns>
		public Blockdef GetSmtDef()
		{
			return _smtDef;
		}

		private void PrepareFieldDefCache()
		{
			var msgCollection = _msgDefinitionsUtils.Get();
			// puts header fields
			var blockdef = _blockDefinitionsUtils.Get(Constants.Smh);
			var fielddefs = GetFieldDefs(blockdef.FieldOrDescrOrGroup, true);
			_fieldDefMessageTypeCache[Constants.Smh] = fielddefs;
			PrepareFieldDefTagMap(fielddefs);

			// puts messages fields
			foreach (var msgdef in msgCollection)
			{
				IList<object> list = msgdef.FieldOrDescrOrAlias;
				fielddefs = GetFieldDefs(list, true);
				_fieldDefMessageTypeCache[msgdef.Msgtype] = fielddefs;
				PrepareFieldDefTagMap(fielddefs);
			}

			// puts trailer fields
			blockdef = _blockDefinitionsUtils.Get(Constants.Smt);
			fielddefs = GetFieldDefs(blockdef.FieldOrDescrOrGroup, true);
			_fieldDefMessageTypeCache[Constants.Smt] = fielddefs;
			PrepareFieldDefTagMap(fielddefs);
		}

		private void PrepareFieldDefTagMap(IList<Fielddef> fielddefs)
		{
			var size = fielddefs.Count;
			for (var i = 0; i < size; i++)
			{
				var fielddef = fielddefs[i];
				_fieldDefTagIdCache[fielddef.Tag] = fielddef;
				_fieldDefNameCache[fielddef.Name.ToLower()] = fielddef;
			}
		}

		/// <summary>
		/// Gets the collection of field defs.
		/// </summary>
		/// <param name="fieldsWithGroupsAndBlocks"> the collection with fields, groups and blocks </param>
		public IList<object> GetFieldDefsHier<T1>(IList<T1> fieldsWithGroupsAndBlocks)
		{
			var listLength = fieldsWithGroupsAndBlocks.Count;
			IList<object> fielddefs = new List<object>();
			for (var objectCount = 0; objectCount < listLength; objectCount++)
			{
				object obj = fieldsWithGroupsAndBlocks[objectCount];
				if (obj is Field)
				{
					var field = (Field)obj;
					foreach (var fielddef in _fieldsdef)
					{
						if (fielddef.Tag == field.Tag)
						{
							fielddefs.Add(fielddef);
						}
					}
				}
				else if (obj is Group)
				{
					fielddefs.Add(GetFieldDefsHier(((Group)obj).Content));
				}
				else if (obj is Block)
				{
					var blockIdent = ((Block)obj).Idref;
					IList<object> listFromBlock = _blockDefinitionsUtils.Get(blockIdent).FieldOrDescrOrGroup;
					((List<object>)fielddefs).AddRange(GetFieldDefsHier(listFromBlock));
				}
			}

			return fielddefs;
		}

		/// <summary>
		/// Gets the collection of field defs.
		/// </summary>
		/// <param name="fieldsWithGroupsAndBlocks"> the collection with fields, groups and blocks </param>
		/// <param name="useGroupTags">              the flag provides to include  the fields of group to result collection </param>
		public IList<Fielddef> GetFieldDefs<T1>(IList<T1> fieldsWithGroupsAndBlocks, bool useGroupTags)
		{
			var listLength = fieldsWithGroupsAndBlocks.Count;
			IList<Fielddef> fielddefs = new List<Fielddef>();
			for (var objectCount = 0; objectCount < listLength; objectCount++)
			{
				object obj = fieldsWithGroupsAndBlocks[objectCount];
				if (obj is Field)
				{
					var field = (Field)obj;
					foreach (var fielddef in _fieldsdef)
					{
						if (fielddef.Tag == field.Tag)
						{
							fielddefs.Add(fielddef);
						}
					}
				}
				else if (obj is Group && useGroupTags)
				{
					((List<Fielddef>)fielddefs).AddRange(GetFieldDefs(((Group)obj).Content, true));
				}
				else if (obj is Block)
				{
					var blockIdent = ((Block)obj).Idref;
					IList<object> listFromBlock = _blockDefinitionsUtils.Get(blockIdent).FieldOrDescrOrGroup;
					((List<Fielddef>)fielddefs).AddRange(GetFieldDefs(listFromBlock, true));
				}
			}

			return fielddefs;
		}

		/// <summary>
		/// Returns list of tags of fields from input list.
		/// </summary>
		/// <param name="list"> Input list </param>
		/// <returns> List of tags of fields </returns>
		public IList<int> GetFieldsTags<T1>(IList<T1> list)
		{
			var listLength = list.Count;
			var tags = new List<int>();
			for (var objectCount = 0; objectCount < listLength; objectCount++)
			{
				var obj = list[objectCount];
				switch (obj)
				{
					case Field field:
						tags.Add(field.Tag);
						break;
					case Group grp:
						tags.AddRange(GetFieldsTags(grp.Content));
						break;
					case Block block:
					{
						var blockIdent = block.Idref;
						var listFromBlock = _blockDefinitionsUtils.Get(blockIdent).FieldOrDescrOrGroup;
						tags.AddRange(GetFieldsTags(listFromBlock));
						break;
					}
				}
			}

			return tags;
		}

		/// <summary>
		/// Returns list of tags of fields from input list.
		/// </summary>
		/// <param name="list"> Input list </param>
		/// <returns> List of tags of fields </returns>
		public IList<Field> GetFields<T1>(IList<T1> list)
		{
			var listLength = list.Count;
			IList<Field> tags = new List<Field>();
			for (var objectCount = 0; objectCount < listLength; objectCount++)
			{
				object obj = list[objectCount];
				if (obj is Field)
				{
					var field = (Field)obj;
					tags.Add(field);
				}
				else if (obj is Group)
				{
					((List<Field>)tags).AddRange(GetFields(((Group)obj).Content));
				}
				else if (obj is Block)
				{
					var blockIdent = ((Block)obj).Idref;
					IList<object> listFromBlock = _blockDefinitionsUtils.Get(blockIdent).FieldOrDescrOrGroup;
					((List<Field>)tags).AddRange(GetFields(listFromBlock));
				}
			}

			return tags;
		}

		/// <summary>
		/// Returns list of tags of fields from input list.
		/// </summary>
		/// <param name="messageType"> the type of FIX Message. </param>
		/// <returns> List of tags of fields </returns>
		public IList<Field> GetFields(string messageType)
		{
			return GetFields(_msgDefinitionsUtils.Get(messageType).FieldOrDescrOrAlias);
		}

		public GroupTagInfo GetTagInfoAboutGroup(IList<object> fieldOrDescrOrAlias, int tag)
		{
			var groupTagInfo = new GroupTagInfo(false, GroupTagInfo.DefaultRootGroupTag);
			if (fieldOrDescrOrAlias == null)
			{
				return new GroupTagInfo(false, GroupTagInfo.DefaultRootGroupTag);
			}

			var countOfElemtntsInList = fieldOrDescrOrAlias.Count;
			for (var indexOfElement = 0; indexOfElement < countOfElemtntsInList; indexOfElement++)
			{
				var o = fieldOrDescrOrAlias[indexOfElement];
				if (o is Group grp)
				{
					if (HasTagInContent(grp.Content, tag))
					{
						return new GroupTagInfo(true, grp.Nofield);
					}

					groupTagInfo = GetTagInfoAboutGroup(grp.Content, tag);
					if (CheckChangeOfIngo(groupTagInfo))
					{
						break;
					}
				}
				else if (o is Block block)
				{
					var blockIdent = block.Idref;
					IList<object> listFromBlock = _blockDefinitionsUtils.Get(blockIdent).FieldOrDescrOrGroup;
					groupTagInfo = GetTagInfoAboutGroup(listFromBlock, tag);
					if (CheckChangeOfIngo(groupTagInfo))
					{
						break;
					}
				}
			}

			return groupTagInfo;
		}

		/// <summary>
		/// Counts the group length.
		/// </summary>
		/// <param name="message">           the message </param>
		/// <param name="indexOfGroupTags">  the index of group tag </param>
		/// <param name="startGroupTag">     the start group tag </param>
		/// <param name="lengthOfGroupTag">  the length of group tag </param>
		/// <param name="stackOfGroupsTag">  the group tags </param>
		/// <param name="messageType">       the message type </param>
		/// <param name="rootGroupStartTag"> the root group start tag </param>
		public int CountLengthForGroupUnit(TagValue[] message, int indexOfGroupTags, int startGroupTag,
			TagValue lengthOfGroupTag, ISet<int> stackOfGroupsTag, string messageType, int rootGroupStartTag)
		{
			IList<int> theFirstTags = new List<int>();
			var lengthOfGroup = 0;
			if (indexOfGroupTags == -1)
			{
				var contOfGroups = 0;
				theFirstTags.Add(lengthOfGroupTag.TagId);
				theFirstTags.Add(rootGroupStartTag);
				for (var indexOfFiled = 0; indexOfFiled < message.Length; indexOfFiled++)
				{
					var fixField = message[indexOfFiled];
					if (contOfGroups == 1)
					{
						var tag = fixField.TagId;
						if (theFirstTags.Contains(tag))
						{
							return lengthOfGroup;
						}

						if (stackOfGroupsTag.Contains(tag))
						{
							lengthOfGroup = indexOfFiled + 1;
						}
						else
						{
							var fieldList = FixMessageFactory.NewInstanceFromPool();
							fieldList.AddAll(message.ToList());
							var grFieldMap = GetGroupTagsWithInternalGroups(messageType, fixField.TagId, fieldList);
							if (grFieldMap != null && grFieldMap.Count > 0)
							{
								return lengthOfGroup;
							}
						}
					}
					else
					{
						if (fixField.TagId == startGroupTag)
						{
							--contOfGroups;
						}

						// if count of groups higer than size of message.
						if (indexOfFiled == message.Length - 1)
						{
							return CountLengthForOneGroupUnit(message, indexOfGroupTags, startGroupTag,
								lengthOfGroupTag, stackOfGroupsTag, messageType, rootGroupStartTag);
						}
					}
				}
			}
			else
			{
				var lengthOfPart = message.Length - (indexOfGroupTags + 1);
				if (lengthOfPart > 0)
				{
					var partOfMessageWithGroup = new TagValue[lengthOfPart];
					Array.Copy(message, indexOfGroupTags + 1, partOfMessageWithGroup, 0, lengthOfPart);
					lengthOfGroup = CountLengthForGroupUnit(partOfMessageWithGroup, -1, startGroupTag, lengthOfGroupTag,
						stackOfGroupsTag, messageType, rootGroupStartTag);
				}
			}

			return lengthOfGroup;
		}

		/// <summary>
		/// Counts the group length.
		/// </summary>
		/// <param name="message">           the message </param>
		/// <param name="indexOfGroupTags">  the index of group tag </param>
		/// <param name="startGroupTag">     the start group tag </param>
		/// <param name="lengthOfGroupTag">  the length of group tag </param>
		/// <param name="stackOfGroupsTag">  the group tags </param>
		/// <param name="messageType">       the message type </param>
		/// <param name="rootGroupStartTag"> the root group start tag </param>
		public int CountLengthForGroupUnit(Message.FixMessage message, int indexOfGroupTags, int startGroupTag,
			TagValue lengthOfGroupTag, ISet<int> stackOfGroupsTag, string messageType, int rootGroupStartTag)
		{
			IList<int> theFirstTags = new List<int>();
			var lengthOfGroup = 0;
			var msgSize = message.Length;
			if (indexOfGroupTags == -1)
			{
				theFirstTags.Add(lengthOfGroupTag.TagId);
				theFirstTags.Add(rootGroupStartTag);

				for (var indexOfFiled = 0; indexOfFiled < msgSize; indexOfFiled++)
				{
					var fixField = message[indexOfFiled];
					var tag = fixField.TagId;
					if (theFirstTags.Contains(tag))
					{
						// checks if the parent root tag.
						if (rootGroupStartTag == tag)
						{
							// checks if index less than size of message
							if (indexOfFiled + 1 < msgSize)
							{
								// checks if next tag from the group
								if (stackOfGroupsTag.Contains(message[indexOfFiled + 1].TagId))
								{
									lengthOfGroup = indexOfFiled + 1;
									continue;
								}

								// checks the last but one tag from the group.
								if (indexOfFiled + 2 == msgSize)
								{
									lengthOfGroup = indexOfFiled + 1;
									continue;
								}
							}
						}

						return lengthOfGroup;
					}

					if (stackOfGroupsTag.Contains(tag))
					{
						lengthOfGroup = indexOfFiled + 1;
						// if count of groups higer than size of message.
						if (indexOfFiled == msgSize - 1)
						{
							return CountLengthForOneGroupUnit(message, indexOfGroupTags, startGroupTag,
								lengthOfGroupTag, stackOfGroupsTag, messageType, rootGroupStartTag);
						}
					}
					else
					{
						var grFieldMap = GetGroupTagsWithInternalGroups(messageType, fixField.TagId, message);
						if (grFieldMap != null && grFieldMap.Count > 0)
						{
							return lengthOfGroup;
						}
					}
				}
			}
			else
			{
				var lengthOfPart = msgSize - (indexOfGroupTags + 1);
				if (lengthOfPart > 0)
				{
					var partOfMessageWithGroup = new Message.FixMessage();
					CopyFixElements(message, indexOfGroupTags + 1, partOfMessageWithGroup, lengthOfPart);
					lengthOfGroup = CountLengthForGroupUnit(partOfMessageWithGroup, -1, startGroupTag, lengthOfGroupTag,
						stackOfGroupsTag, messageType, rootGroupStartTag);
				}
			}

			return lengthOfGroup;
		}

		private void CopyFixElements(Message.FixMessage src, int srcPos, Message.FixMessage dest, int length)
		{
			var srcSize = src.Length;
			for (var srcIndex = 0; srcIndex < srcSize; srcIndex++)
			{
				if (srcIndex < srcPos)
				{
					continue;
				}

				if (srcIndex == length + srcPos)
				{
					break;
				}

				dest.Add(src[srcIndex]);
			}
		}

		/// <summary>
		/// Counts the group length.
		/// </summary>
		/// <param name="message">           the message </param>
		/// <param name="indexOfGroupTags">  the index of group tag </param>
		/// <param name="startGroupTag">     the start group tag </param>
		/// <param name="lengthOfGroupTag">  the length of group tag </param>
		/// <param name="stackOfGroupsTag">  the group tags </param>
		/// <param name="messageType">       the message type </param>
		/// <param name="rootGroupStartTag"> the root group start tag </param>
		public int CountLengthForOneGroupUnit(TagValue[] message, int indexOfGroupTags, int startGroupTag,
			TagValue lengthOfGroupTag, ISet<int> stackOfGroupsTag, string messageType, int rootGroupStartTag)
		{
			IList<int> theFirstTags = new List<int>();
			var lengthOfGroup = 0;
			if (indexOfGroupTags == -1)
			{
				// int contOfGroups =
				// Integer.parseInt(lengthOfGroupTag.GetStringValue());
				theFirstTags.Add(lengthOfGroupTag.TagId);
				theFirstTags.Add(rootGroupStartTag);
				for (var indexOfFiled = 0; indexOfFiled < message.Length; indexOfFiled++)
				{
					var fixField = message[indexOfFiled];
					var tag = fixField.TagId;
					// check required first tag for the group;
					if (indexOfFiled != 0 && tag == startGroupTag)
					{
						return lengthOfGroup;
					}

					if (theFirstTags.Contains(tag))
					{
						return lengthOfGroup;
					}

					if (stackOfGroupsTag.Contains(tag))
					{
						lengthOfGroup = indexOfFiled + 1;
					}
					else
					{
						var fieldList = FixMessageFactory.NewInstanceFromPool();
						fieldList.AddAll(message);
						var grFieldMap = GetGroupTagsWithInternalGroups(messageType, fixField.TagId, fieldList);
						if (grFieldMap != null && grFieldMap.Count > 0)
						{
							return lengthOfGroup;
						}
					}
				}
			}
			else
			{
				var lengthOfPart = message.Length - (indexOfGroupTags + 1);
				if (lengthOfPart > 0)
				{
					var partOfMessageWithGroup = new TagValue[lengthOfPart];
					Array.Copy(message, indexOfGroupTags + 1, partOfMessageWithGroup, 0, lengthOfPart);
					lengthOfGroup = CountLengthForOneGroupUnit(partOfMessageWithGroup, -1, startGroupTag,
						lengthOfGroupTag, stackOfGroupsTag, messageType, rootGroupStartTag);
				}
			}

			return lengthOfGroup;
		}

		/// <summary>
		/// Counts the group length.
		/// </summary>
		/// <param name="message">           the message </param>
		/// <param name="indexOfGroupTags">  the index of group tag </param>
		/// <param name="startGroupTag">     the start group tag </param>
		/// <param name="lengthOfGroupTag">  the length of group tag </param>
		/// <param name="stackOfGroupsTag">  the group tags </param>
		/// <param name="messageType">       the message type </param>
		/// <param name="rootGroupStartTag"> the root group start tag </param>
		public int CountLengthForOneGroupUnit(Message.FixMessage message, int indexOfGroupTags, int startGroupTag,
			TagValue lengthOfGroupTag, ISet<int> stackOfGroupsTag, string messageType, int rootGroupStartTag)
		{
			IList<int> theFirstTags = new List<int>();
			var lengthOfGroup = 0;
			var msgSize = message.Length;
			if (indexOfGroupTags == -1)
			{
				// int contOfGroups =
				// Integer.parseInt(lengthOfGroupTag.GetStringValue());
				theFirstTags.Add(lengthOfGroupTag.TagId);
				theFirstTags.Add(rootGroupStartTag);
				for (var indexOfFiled = 0; indexOfFiled < msgSize; indexOfFiled++)
				{
					var fixField = message[indexOfFiled];
					var tag = fixField.TagId;
					// check required first tag for the group;
					if (indexOfFiled != 0 && tag == startGroupTag)
					{
						return lengthOfGroup;
					}

					if (theFirstTags.Contains(tag))
					{
						return lengthOfGroup;
					}

					if (stackOfGroupsTag.Contains(tag))
					{
						lengthOfGroup = indexOfFiled + 1;
					}
					else
					{
						var grFieldMap = GetGroupTagsWithInternalGroups(messageType, fixField.TagId, message);
						if (grFieldMap != null && grFieldMap.Count > 0)
						{
							return lengthOfGroup;
						}
					}
				}
			}
			else
			{
				var lengthOfPart = msgSize - (indexOfGroupTags + 1);
				if (lengthOfPart > 0)
				{
					var partOfMessageWithGroup = new Message.FixMessage();
					CopyFixElements(message, indexOfGroupTags + 1, partOfMessageWithGroup, lengthOfPart);
					lengthOfGroup = CountLengthForOneGroupUnit(partOfMessageWithGroup, -1, startGroupTag,
						lengthOfGroupTag, stackOfGroupsTag, messageType, rootGroupStartTag);
				}
			}

			return lengthOfGroup;
		}

		/// <summary>
		/// Gets the field.
		/// </summary>
		/// <param name="msgType"> the message type </param>
		/// <param name="tag">     the tag </param>
		/// <returns> field if tag occurred otherwise nl </returns>
		public Field GetField(string msgType, int? tag)
		{
			var fields = GetFields(msgType);
			foreach (var field in fields)
			{
				if (field.Tag == tag)
				{
					return field;
				}
			}

			return null;
		}

		/// <summary>
		/// Checks if tag is required for message type.
		/// </summary>
		/// <param name="msgType"> the message type </param>
		/// <param name="tag">     the tag </param>
		public bool IsRequiredTag(string msgType, int tag)
		{
			return GetRequiredTagsForMessage(msgType).Contains(tag);
		}

		private bool CheckChangeOfIngo(GroupTagInfo groupTagInfo)
		{
			return groupTagInfo.IsGroupTag() && GroupTagInfo.DefaultRootGroupTag != groupTagInfo.GetRootGroupTag();
		}

		private bool HasTagInContent(IList<object> content, int tag)
		{
			var hasTag = false;
			if (content == null)
			{
				return hasTag;
			}

			var countOfElemtntsInList = content.Count;
			for (var indexOfElement = 0; indexOfElement < countOfElemtntsInList; indexOfElement++)
			{
				var o = content[indexOfElement];
				if (o is Field field)
				{
					if (field.Tag == tag)
					{
						hasTag = true;
						break;
					}
				}
				else if (o is Block block)
				{
					var blockIdent = block.Idref;
					//TODO: get rid of object - replace with IFindable
					IList<object> listFromBlock = _blockDefinitionsUtils.Get(blockIdent).FieldOrDescrOrGroup;
					if (HasTagInContent(listFromBlock, tag))
					{
						hasTag = true;
						break;
					}
				}
			}

			return hasTag;
		}

		private void PrepareValblockdefs()
		{
			var valblockDefs = new List<Valblockdef>();
			var types = _dictionaryTypes.Dictionaries;
			if (types != null)
			{
				foreach (var type in types)
				{
					if (type is Fixdic fixdic)
					{
						valblockDefs.AddRange(fixdic.Fielddic.Valblockdef);
					}
				}
			}

			_valblockdefs = new ReadOnlyCollection<Valblockdef>(valblockDefs);
		}

		/// <summary>
		/// Prepare fields definitions for input dictionary of FIX protocol.
		/// </summary>
		/// <param name="dictionaryTypes"> Input dictionary of FIX protocol </param>
		public void PrepareFieldsDefinitions(DictionaryTypes dictionaryTypes)
		{
			var fixDictionaries = dictionaryTypes.Dictionaries;
			if (fixDictionaries == null)
			{
				return;
			}

			// fixed bug, allTags and fieldsdef builds only for last dictionary
			// bug 14795: Validation of tag values does not function for FIXT.1.1 protocol
			var fielddefs = new List<Fielddef>();
			AddFixField(fielddefs, fixDictionaries);
			AddFixtFields(fielddefs, fixDictionaries);

			_fieldsdef = fielddefs.ToArray();
			_allTags = new int[_fieldsdef.Length];
			for (var i = 0; i < _fieldsdef.Length; i++)
			{
				_allTags[i] = _fieldsdef[i].Tag;
			}
		}

		private void AddFixField(List<Fielddef> fielddefs, IList<IType> typeList)
		{
			foreach (var type in typeList)
			{
				if (type is Fixdic fixdic)
				{
					if (!fixdic.IsFixtDictionary)
					{
						var fielddefList = fixdic.Fielddic.Fielddef;
						if (fielddefList != null)
						{
							fielddefs.AddRange(fielddefList);
						}
					}
				}
			}
		}

		private void AddFixtFields(List<Fielddef> fielddefs, IList<IType> dictionaries)
		{
			foreach (var type in dictionaries)
			{
				if (type is Fixdic fixdic)
				{
					if (fixdic.IsFixtDictionary)
					{
						// fixed bug: 15039 FIXT1.1. sessions 4.0, 4.1, 4.2 aren't created.
						// and bug 15064 Unexpected behavior after first Heartbeat for FIXT.1.1. FIX.4.0 and FIX.4.2 sessions
						var fixtFieldList = fixdic.Fielddic.Fielddef.ToList();
						for (var j = fixtFieldList.Count - 1; j > 0; j--)
						{
							var fixtField = fixtFieldList[j];
							for (var i = 0; i < fielddefs.Count; i++)
							{
								var fieldDef = fielddefs[i];
								var tag = fieldDef.Tag;
								if (tag == fixtField.Tag)
								{
									fielddefs[i] = fixtField; // replace field from fixt dictionary
									fixtFieldList.RemoveAt(j);
									break;
								}
							}
						}

						// add filtered fixt fields
						fielddefs.AddRange(fixtFieldList);
					}
				}
			}
		}

		/// <summary>
		/// Method isContainsTagInFieldsList, verifies whether the tag is present in
		/// a list.
		/// </summary>
		/// <param name="list"> the input list </param>
		/// <param name="tag">  the tag </param>
		/// <returns> <c>true</c> if presents, otherwise <c>false</c> </returns>
		private bool IsContainsTagInFieldsList(IList<object> list, int tag)
		{
			var listLength = list.Count;
			for (var objectCount = 0; objectCount < listLength; objectCount++)
			{
				var obj = list[objectCount];
				if (obj is Field field && field.Tag == tag)
				{
					return true;
				}

				if (obj is Group group && IsContainsTagInFieldsList(@group.Content, tag))
				{
					return true;
				}

				if (obj is Block block)
				{
					var blockIdent = block.Idref;
					IList<object> listFromBlock = _blockDefinitionsUtils.Get(blockIdent).FieldOrDescrOrGroup;
					if (IsContainsTagInFieldsList(listFromBlock, tag))
					{
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Verifies whether the tag is present in a message by type of message.
		/// </summary>
		/// <param name="messageType"> the type of message </param>
		/// <param name="tag">         the tag </param>
		/// <returns> <c>true</c> if presents, otherwise <c>false</c> </returns>
		private bool IsContainsTagInFieldsList(string messageType, int tag)
		{
			var tags = _fieldCache[messageType];
			return tags != null && tags.Contains(tag);
		}

		/// <summary>
		/// Puts conditions into cache.
		/// </summary>
		private void PutConditions()
		{
			var msgdefCollection = _msgDefinitionsUtils.Get();
			foreach (var msgdef in msgdefCollection)
			{
				var list = msgdef.FieldOrDescrOrAlias;
				var key = msgdef.Msgtype;
				if (_conditionsCache.TryGetValue(msgdef.Msgtype, out var value))
				{
					_conditionsCache.Remove(key);
				}

				_conditionsCache.Add(key, GetConditions(list, -1, ConditionalType.Message));
			}
		}

		/// <summary>
		/// Method getDef returns map of message definition by message type.
		/// </summary>
		/// <param name="messageType"> the type of message </param>
		/// <returns> IFixMessageDefinitions definitions of message </returns>
		private IFindable GetDefinition(string messageType)
		{
			return (IFindable)_msgDefinitionsUtils.Get(messageType) ?? _blockDefinitionsUtils.Get(messageType);
		}

		private IDictionary<int, Field> GetGroupFields<T1>(int groupTag, IList<T1> messageFieldsOrGroups,
			Message.FixMessage message, bool withInternalGroup)
		{
			var fields = new Dictionary<int, Field>(32);
			foreach (object obj in messageFieldsOrGroups)
			{
				if (obj is Group)
				{
					var group = (Group)obj;
					if (@group.Nofield == groupTag)
					{
						fields.PutAll(GetGroupFields(@group.Content, message, withInternalGroup));
					}
					else
					{
						fields.PutAll(GetGroupFields(groupTag, @group.Content, message, withInternalGroup));
					}
				}
				else if (obj is Block)
				{
					var block = (Block)obj;
					var condition = block.Condreq;
					IList<object> listFromBlock = _blockDefinitionsUtils.Get(block.Idref).FieldOrDescrOrGroup;
					var flatListFromBlock = GetFields(listFromBlock);
					if (HasRequiredTagInMessage(flatListFromBlock, message, withInternalGroup) ||
						IsConditionalRequired(condition, message))
					{
						fields.PutAll(GetGroupFields(groupTag, listFromBlock, message, withInternalGroup));
					}
				}
			}

			return fields;
		}

		private bool HasGroup<T1>(IList<T1> listFromBlock)
		{
			var objectCount = listFromBlock.Count;
			for (var objectIndex = 0; objectIndex < objectCount; objectIndex++)
			{
				var obj = listFromBlock[objectIndex];
				switch (obj)
				{
					case Group _:
						return true;
					case Block block:
					{
						var blockId = block.Idref ?? block.Name;
						if (HasGroup(_blockDefinitionsUtils.Get(blockId).FieldOrDescrOrGroup))
						{
							return true;
						}
						break;
					}
				}
			}
			return false;
		}

		private int GetStartTagForGroup<T1>(int tag, IList<T1> objectList)
		{
			var objectCount = objectList.Count;
			var startTag = -1;
			for (var objectIndex = 0; objectIndex < objectCount; objectIndex++)
			{
				if (startTag != -1)
				{
					break;
				}

				var obj = objectList[objectIndex];
				if (obj is Group grp)
				{
					if (grp.Nofield == tag)
					{
						startTag = grp.Startfield;
						break;
					}

					startTag = GetStartTagForGroup(tag, grp.Content);
				}
				else if (obj is Block block)
				{
					var blockIdent = block.Idref;
					var listFromBlock = _blockDefinitionsUtils.Get(blockIdent).FieldOrDescrOrGroup;
					if (HasGroup(listFromBlock))
					{
						startTag = GetStartTagForGroup(tag, listFromBlock);
					}
				}
			}

			return startTag;
		}

		private IDictionary<int, Field> GetGroupFields<T1>(IList<T1> objectList, Message.FixMessage fixMessage,
			bool withInternalGroup)
		{
			var fields = new Dictionary<int, Field>(32);
			var objectCount = objectList.Count;
			for (var objectIndex = 0; objectIndex < objectCount; objectIndex++)
			{
				var obj = objectList[objectIndex];
				switch (obj)
				{
					case Field field:
						fields[field.Tag] = field;
						break;
					case Group group when withInternalGroup:
					{
						if (fixMessage.GetTag(@group.Nofield) != null)
						{
							fields.PutAll(GetGroupFields(@group.Content, fixMessage, withInternalGroup));
						}
						break;
					}
					case Group group:
						fields.Remove(@group.Nofield);
						break;
					case Block block:
					{
						var blockIdent = block.Idref;
						var listFromBlock = _blockDefinitionsUtils.Get(blockIdent).FieldOrDescrOrGroup;
						if (HasRequiredTagInMessage(listFromBlock, fixMessage, withInternalGroup) ||
								IsConditionalRequired(block.Condreq, fixMessage) || IsRequired(block.Req))
						{
							fields.PutAll(GetGroupFields(listFromBlock, fixMessage, withInternalGroup));
						}
						break;
					}
				}
			}

			return fields;
		}

		public GroupsCache GetGroupsCache(string msgType)
		{
			var groupsCache = _groupsCacheWithInternalGroups.Get(msgType);
			return groupsCache ?? new GroupsCache();
		}

		private void PrepareGroupsCache()
		{
			var msgTypes = _msgDefinitionsUtils.GetMessageTypes();
			foreach (var msgType in msgTypes)
			{
				var msgdef = _msgDefinitionsUtils.Get(msgType);
				PrepareGroupsCache(msgType, msgdef.FieldOrDescrOrAlias);
			}
		}

		private void PrepareGroupsCache<T1>(string msgType, IList<T1> objectList)
		{
			var objectCount = objectList.Count;
			for (var objectIndex = 0; objectIndex < objectCount; objectIndex++)
			{
				var obj = objectList[objectIndex];
				switch (obj)
				{
					case Group grp:
					{
						var groupId = grp.Nofield;
						var startFieldId = grp.Startfield;
						var groupCache = new GroupCache(groupId, startFieldId);
						groupCache.PutAllCache(GetGroupFields(msgType, groupId, grp.Content, true));
						var groupsCache = _groupsCacheWithInternalGroups.Get(msgType);
						PutValueIntoGroupsCache(msgType, groupId, groupId, groupCache, groupsCache);
						break;
					}
					case Block block:
					{
						var blockIdent = block.Idref;
						var listFromBlock = _blockDefinitionsUtils.Get(blockIdent).FieldOrDescrOrGroup;
						PrepareGroupsCache(msgType, listFromBlock);
						break;
					}
				}
			}
		}

		private IDictionary<int, Field> GetGroupFields<T1>(string msgType, int groupId, IList<T1> objectList,
			bool withInternalGroup)
		{
			var fields = new Dictionary<int, Field>(32);
			var objectCount = objectList.Count;
			for (var objectIndex = 0; objectIndex < objectCount; objectIndex++)
			{
				var obj = objectList[objectIndex];
				switch (obj)
				{
					case Field field:
						fields[field.Tag] = field;
						break;
					case Group group when withInternalGroup:
					{
						var internalGroupId = @group.Nofield;
						var internalGroupStartFieldId = @group.Startfield;
						var groupCache = new GroupCache(internalGroupId, internalGroupStartFieldId);
						groupCache.PutAllCache(GetGroupFields(msgType, internalGroupId, @group.Content,
							withInternalGroup));
						var groupsCache = _groupsCacheWithInternalGroups.Get(msgType);
						PutValueIntoGroupsCache(msgType, groupId, internalGroupId, groupCache, groupsCache);
						break;
					}
					case Group group:
						fields.Remove(@group.Nofield);
						break;
					case Block block:
					{
						var blockIdent = block.Idref;
						var listFromBlock = _blockDefinitionsUtils.Get(blockIdent).FieldOrDescrOrGroup;
						var blockCache = new BlockCache(block, groupId);
						blockCache.PutAllCache(GetGroupFields(msgType, groupId, listFromBlock, withInternalGroup));
						var groupsCache = _groupsCacheWithInternalGroups.Get(msgType);
						PutValueIntoGroupsCache(msgType, groupId, groupId, blockCache, groupsCache);
						break;
					}
				}
			}

			return fields;
		}

		private void PutValueIntoGroupsCache<T1, T2>(string msgType, int parentGroupId, int groupId,
			ICache<T1, T2> iCache, GroupsCache groupsCache)
		{
			if (groupsCache == null)
			{
				groupsCache = new GroupsCache();
			}

			switch (iCache)
			{
				case BlockCache blockCache:
					groupsCache.PutBlockCache(parentGroupId, blockCache);
					break;
				case GroupCache groupCache:
					groupsCache.PutGroupCache(parentGroupId, groupId, groupCache);
					break;
			}

			_groupsCacheWithInternalGroups.Put(msgType, groupsCache);
		}

		private bool IsRequired(string text)
		{
			return text != null && !text.Equals("N");
		}

		private bool IsConditionalRequired(string condition, Message.FixMessage fixMessage)
		{
			if (condition is null)
			{
				return false;
			}

			var conditionValidateParser = new ConditionValidateParser(condition);
			var iCondition = conditionValidateParser.GetCondition();
			return iCondition.IsRequired(fixMessage);
		}

		/// <summary>
		/// Checks if message body contain tag.
		/// <i>Note: tags from group ignored.</i>
		/// </summary>
		/// <param name="messageType"> the message type </param>
		/// <param name="tag">         the tag </param>
		public bool IsMessageContainField(string messageType, int tag)
		{
			var msgDef = GetMessageDefUtils().Get(messageType);
			return IsMessageContainField(msgDef.FieldOrDescrOrAlias, tag);
		}

		private bool IsMessageContainField<T1>(IList<T1> fields, int tag)
		{
			foreach (var obj in fields)
			{
				switch (obj)
				{
					case Field field when field.Tag == tag:
						return true;
					case Block block:
					{
						var blockIdent = block.Idref;
						var listFromBlock = _blockDefinitionsUtils.Get(blockIdent).FieldOrDescrOrGroup;
						if (IsMessageContainField(listFromBlock, tag))
						{
							return true;
						}
						break;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Gets valblockdef by idRef.
		/// </summary>
		/// <param name="idRef"> the id ref name </param>
		/// <returns> Valblockdef </returns>
		public Valblockdef GetValblockdef(string idRef)
		{
			foreach (var valBlock in _valblockdefs)
			{
				if (idRef.Equals(valBlock.Id))
				{
					return valBlock;
				}
			}

			return null;
		}

		public static string DescrToHtmlStr(Descr descr)
		{
			if (descr == null)
			{
				return null;
			}

			var xmlSerializer = new XmlSerializer(typeof(Descr));
			using (var sww = new Utf8Writer())
			{
				var settings = new XmlWriterSettings
				{
					Encoding = Encoding.UTF8,
					Indent = true
				};
				using (var writer = XmlWriter.Create(sww, settings))
				{
					writer.WriteStartDocument(true);
					//hack to avoid namespace writing to the descr element
					var ns = new XmlSerializerNamespaces();
					ns.Add("", "");
					xmlSerializer.Serialize(writer, descr, ns);
					return sww.ToString();
				}
			}
		}

		public static string CommentToHtmlStr(Comment comment)
		{
			if (comment == null)
			{
				return null;
			}

			var xmlSerializer = new XmlSerializer(typeof(Comment));
			using (var sww = new Utf8Writer())
			{
				var settings = new XmlWriterSettings
				{
					Encoding = Encoding.UTF8,
					Indent = true
				};
				using (var writer = XmlWriter.Create(sww, settings))
				{
					writer.WriteStartDocument(true);
					//hack to avoid namespace writing to the comment element
					var ns = new XmlSerializerNamespaces();
					ns.Add("", "");
					xmlSerializer.Serialize(writer, comment, ns);
					return sww.ToString();
				}
			}
		}
	}
}