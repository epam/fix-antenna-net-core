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
using System.Buffers;
using Epam.FixAntenna.NetCore.Helpers;

namespace Epam.FixAntenna.NetCore.Message.SpecialTags
{
	internal static class SpecialFixUtil
	{
		/// <summary>
		/// Parses provided byte array and returns string representation masking configured tags.
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		/// <param name="rawTags"></param>
		/// <param name="maskedTags"></param>
		/// <returns></returns>
		public static string GetMaskedString(byte[] buffer, int offset, int length, RawFixUtil.IRawTags rawTags, IMaskedTags maskedTags)
		{
			var raw = rawTags ?? RawFixUtil.DefaultRawTags;
			var masked = maskedTags ?? DefaultMaskedTags.Instance;

			var tmpBuffer = ArrayPool<byte>.Shared.Rent(length);
			try
			{
				buffer.AsSpan(offset, length).CopyTo(tmpBuffer);
				MaskFields(tmpBuffer.AsSpan(0, length), raw, masked);
				return StringHelper.NewString(tmpBuffer, 0, length);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(tmpBuffer);
			}
		}

		/// <summary>
		/// This method masks fields defined in configuration file. By default fields 554, 925 are masked.
		/// Values for such fields will be filled by asterisks in provided byte array.
		/// </summary>
		/// <param name="buffer"><see cref="Span{T}"/> - message to mask fields.</param>
		/// <param name="rawTags"><see cref="RawFixUtil.IRawTags"/> defined raw tags.</param>
		/// <param name="maskedTags"><see cref="IMaskedTags"/> defined tags to mask.</param>
		public static void MaskFields(Span<byte> buffer, RawFixUtil.IRawTags rawTags, IMaskedTags maskedTags)
		{
			var previousValueIndex = 0;
			var previousValueLength = 0;

			var valueStartIndex = 0;
			var tag = 0;
			var isTagParsing = true;

			for (var index = 0; index < buffer.Length; index++)
			{
				var b = buffer[index];
				if (isTagParsing)
				{
					if (b >= (byte)'0' && b <= (byte)'9')
					{
						tag = tag * 10 + (b - '0');
					}
					else if (b == (byte)'=')
					{
						valueStartIndex = index + 1;
						if (rawTags.IsWithinRawTags(tag))
						{
							if (FixTypes.TryParseLong(buffer.ToArray(), previousValueIndex, previousValueLength, out var rawTagLength))
							{
								if (maskedTags.IsTagListed(tag))
								{
									buffer.Slice(valueStartIndex, (int)rawTagLength).Fill((byte)'*');
								}
								index += (int)rawTagLength;
							}
						}
						isTagParsing = false;
					}
					else
					{
						break;
					}
				}
				else
				{
					if (b != 0x01)
					{
						continue;
					}

					if (maskedTags.IsTagListed(tag))
					{
						buffer.Slice(valueStartIndex, index - valueStartIndex).Fill((byte)'*');
					}

					tag = 0;
					isTagParsing = true;
					previousValueIndex = valueStartIndex;
					previousValueLength = index - valueStartIndex;
				}
			}
		}
	}
}
