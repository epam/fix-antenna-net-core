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
using System.Text;
using Epam.FixAntenna.NetCore.Message.Format;
using Epam.FixAntenna.NetCore.Message.Rg;

namespace Epam.FixAntenna.NetCore.Message
{
	public interface ITagList
	{
		int AddTag(TagValue tagValue);

		int AddTag(int tag, byte[] value);

		int AddTag(int tag, byte[] value, int offset, int length);

		int AddTag(int tag, long value);

		int AddTag(int tag, double value, int precision);

		int AddTag(int tag, string value);

		int AddCalendarTag(int tag, DateTimeOffset value, FixDateFormatterFactory.FixDateType type);

		int AddTag(int tagId, bool value);

		int UpdateValue(TagValue tagValue, IndexedStorage.MissingTagHandling missingTagHandling);

		int UpdateValue(int tag, byte[] value, int offset, int length,
			IndexedStorage.MissingTagHandling addIfNotExists);

		int UpdateValue(int tag, byte[] value, IndexedStorage.MissingTagHandling addIfNotExists);

		int UpdateValue(int tag, long value, IndexedStorage.MissingTagHandling addIfNotExists);

		int UpdateValue(int tag, double value, int precision, IndexedStorage.MissingTagHandling addIfNotExists);

		int UpdateValue(int tag, string strBuffer, IndexedStorage.MissingTagHandling addIfNotExists);

		int UpdateValue(int tag, bool value, IndexedStorage.MissingTagHandling addIfNotExists);

		int UpdateCalendarValue(int tag, DateTimeOffset value, FixDateFormatterFactory.FixDateType type,
			IndexedStorage.MissingTagHandling addIfNotExists);

		int GetTagIdAtIndex(int index);

		bool IsTagExists(int tag);

		bool IsRepeatingGroupExists(int leadingTag);

		RepeatingGroup GetOrAddRepeatingGroup(int leadingTag);

		void GetOrAddRepeatingGroup(int leadingTag, RepeatingGroup group);

		bool RemoveTag(int tag);

		bool RemoveTagAtIndex(int index);

		int GetTagIndex(int tag);

		void LoadTagValue(int tag, TagValue dest);

		void LoadTagValueByIndex(int index, TagValue dest);

		string GetTagValueAsString(int tag);

		string GetTagValueAsStringAtIndex(int index);

		byte[] GetTagValueAsBytes(int tag);

		byte[] GetTagValueAsBytesAtIndex(int index);

		int GetTagValueAsBytes(int tag, byte[] dest, int offset);

		int GetTagValueAsBytesAtIndex(int index, byte[] dest, int offset);

		byte GetTagValueAsByte(int tag);

		byte GetTagValueAsByteAtIndex(int index);

		byte GetTagValueAsByte(int tag, int offset);

		byte GetTagValueAsByteAtIndex(int index, int offset);

		bool GetTagValueAsBool(int tag);

		bool GetTagValueAsBoolAtIndex(int index);

		double GetTagValueAsDouble(int tag);

		double GetTagValueAsDoubleAtIndex(int index);

		long GetTagValueAsLong(int tag);

		long GetTagValueAsLongAtIndex(int index);

		void GetTagValueAsStringBuff(int tag, StringBuilder str);

		void GetTagValueAsStringBuffAtIndex(int index, StringBuilder str);

		RepeatingGroup GetRepeatingGroup(int leadingTag);

		void GetRepeatingGroup(int leadingTag, RepeatingGroup group);

		RepeatingGroup GetRepeatingGroupAtIndex(int index);

		void GetRepeatingGroupAtIndex(int index, RepeatingGroup group);

		RepeatingGroup AddRepeatingGroup(int leadingTag);

		void AddRepeatingGroup(int leadingTag, RepeatingGroup group);

		RepeatingGroup AddRepeatingGroup(int leadingTag, bool validation);

		void AddRepeatingGroup(int leadingTag, bool validation, RepeatingGroup group);

		RepeatingGroup CopyRepeatingGroup(RepeatingGroup source);

		void CopyRepeatingGroup(RepeatingGroup source, RepeatingGroup dest);

		bool RemoveRepeatingGroup(int leadingTag);

		bool RemoveRepeatingGroupAtIndex(int index);

		byte[] AsByteArray();

		string ToPrintableString();

		//int GetSize();
		int Count { get; }

		void Clear();

		bool IsEmpty { get; }

		void ReleaseInstance();

		ITagList Clone();
	}
}