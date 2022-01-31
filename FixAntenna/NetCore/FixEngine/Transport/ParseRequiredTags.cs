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
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Transport
{
	/// <summary>
	/// Contain all required tagId to operate the engine.
	/// </summary>
	internal class ParseRequiredTags
	{
		private static int[] _headerTags = new int[]
		{
			Tags.ApplVerID,
			Tags.ApplVerID,
			Tags.BeginString,
			Tags.BodyLength,
			Tags.CstmApplVerID,
			Tags.DeliverToCompID,
			Tags.DeliverToLocationID,
			Tags.DeliverToSubID,
			Tags.HopCompID,
			Tags.HopRefID,
			Tags.HopSendingTime,
			Tags.LastMsgSeqNumProcessed,
			Tags.MessageEncoding,
			Tags.MsgSeqNum,
			Tags.MsgType,
			Tags.NoHops,
			Tags.OnBehalfOfCompID,
			Tags.OnBehalfOfLocationID,
			Tags.OnBehalfOfSubID,
			Tags.OrigSendingTime,
			Tags.PossDupFlag,
			Tags.PossResend,
			Tags.SecureData,
			Tags.SecureDataLen,
			Tags.SenderCompID,
			Tags.SenderLocationID,
			Tags.SenderSubID,
			Tags.SendingTime,
			Tags.TargetCompID,
			Tags.TargetLocationID,
			Tags.TargetSubID,
			Tags.XmlData,
			Tags.XmlDataLen
		};

		private static int[] _trailerTags = new int[]
		{
			Tags.CheckSum,
			Tags.Signature,
			Tags.SignatureLength
		};

		private static int[] _adminTags = new int[]
		{
			Tags.TestReqID,

			Tags.DefaultApplVerID,
			Tags.EncryptMethod,
			Tags.HeartBtInt,
			Tags.MaxMessageSize,
			Tags.MsgDirection,
			Tags.NextExpectedMsgSeqNum,
			Tags.NoMsgTypes,
			Tags.Password,
			Tags.RawData,
			Tags.RawDataLength,
			Tags.RefApplVerID,
			Tags.RefCstmApplVerID,
			Tags.RefMsgType,
			Tags.ResetSeqNumFlag,
			Tags.TestMessageIndicator,
			Tags.Username,

			Tags.EncodedText,
			Tags.EncodedTextLen,
			Tags.Text,

			Tags.EncodedText,
			Tags.EncodedTextLen,
			Tags.RefMsgType,
			Tags.RefSeqNum,
			Tags.RefTagID,
			Tags.SessionRejectReason,
			Tags.Text,

			Tags.BeginSeqNo,
			Tags.EndSeqNo,

			Tags.GapFillFlag,
			Tags.NewSeqNo,

			Tags.TestReqID
		};

		private static int[] _reqTags = AppendArray(_headerTags, _trailerTags, _adminTags);

	//    private static int[] sortReqTags = sortAndRemoveDuplicate(reqTags);
		private static bool[] _sparseArrayReqTags = ToSparseArray(_reqTags);
		private static bool[] _sparseArrayHeaderTags = ToSparseArray(_headerTags);
		private static bool[] _sparseArrayTrailerTags = ToSparseArray(_trailerTags);

		private static bool[] ToSparseArray(int[] original)
		{
			var max = 0;
			for (var i = 0; i < original.Length; i++)
			{
				max = Math.Max(max, original[i]);
			}
			var sparse = new bool[max + 1];
			for (var i = 0; i < original.Length; i++)
			{
				sparse[original[i]] = true;
			}
			return sparse;
		}

		private static int[] AppendArray(params int[][] arrays)
		{
			var size = 0;
			foreach (var arr in arrays)
			{
				size += arr.Length;
			}
			var result = new int[size];
			var count = 0;
			foreach (var arr in arrays)
			{
				Array.Copy(arr, 0, result, count, arr.Length);
				count += arr.Length;
			}
			return result;
		}

		private static int[] SortAndRemoveDuplicate(int[] original)
		{
			var duplicateNum = 0;
			Array.Sort(original);
			for (var i = 1;i < original.Length; i++)
			{
				if (original[i - 1] == original[i])
				{
					duplicateNum++;
				}
			}
			if (duplicateNum > 0)
			{
				var newSize = original.Length - duplicateNum;
				var arrayNoDup = new int[newSize];
				arrayNoDup[0] = original[0];
				var j = 1;
				for (var i = 1;i < original.Length; i++)
				{
					if (original[i - 1] != original[i])
					{
						arrayNoDup[j++] = original[i];
					}
				}
				return arrayNoDup;
			}
			return original;
		}

		/// <summary>
		/// Do checks: this tag is required to operate the engine. </summary>
		/// <param name="tag"> for check </param>
		/// <returns> true if is required </returns>
		internal static bool IsRequired(int tag)
		{
			return tag < _sparseArrayReqTags.Length && _sparseArrayReqTags[tag];
	//        return Arrays.binarySearch(sortReqTags, tag)>=0;
		}

		internal static bool IsHeader(int tag)
		{
			return tag < _sparseArrayHeaderTags.Length && _sparseArrayHeaderTags[tag];
		}

		internal static bool IsTrailer(int tag)
		{
			return tag < _sparseArrayTrailerTags.Length && _sparseArrayTrailerTags[tag];
		}

		internal static bool IsAdminMsg(TagValue type)
		{
			return type.Length == 1 && IsAdminMsg(type.Buffer[type.Offset]);
		}

		internal static bool IsAdminMsg(byte type)
		{
			return (type >= (byte)'0' && type <= (byte)'5') || type == (byte)'A';
		}
	}
}