/**
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
**/

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PhpSerializerNET.Test {
	[TestClass]
	public class SerializePrimitives {
		[TestMethod]
		public void SerializesNull() {
			Assert.AreEqual(
				"N;",
				PhpSerialization.Serialize(null)
			);
		}

		[TestMethod]
		public void ConvertsProperly() {
			var result = PhpSerialization.Deserialize<long>("b:0;");

			Assert.AreEqual(0, result);

			result = PhpSerialization.Deserialize<long>("b:1;");

			Assert.AreEqual(1, result);

			double number = PhpSerialization.Deserialize<double>("i:10;");

			Assert.AreEqual(10.00, number);
		}
	}
}
