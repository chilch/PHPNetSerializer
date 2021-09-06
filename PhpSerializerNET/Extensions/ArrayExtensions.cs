/**
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
**/


using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PhpSerializerNET
{
	internal static class ArrayExtensions
	{
		public static string Utf8Substring(this byte[] array, int start, int length)
		{
			return Encoding.UTF8.GetString(array.Skip(start).Take(length).ToArray());
		}
	}
}