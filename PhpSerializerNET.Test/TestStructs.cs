
/**
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
**/

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PhpSerializerNET.Test {
	public struct MyStruct {
		public string foo;
		public string bar;
	}

	public struct MyStructIgnoreBar {
		public string foo;
		[PhpIgnore]
		public string bar;
	}

	public struct MyStructRenamedBar {
		public string foo;
		[PhpProperty("foobar")]
		public string bar;
	}


	[TestClass]
	public class TestStructs {


		[TestMethod]
		public void SerializeStruct() {
			Assert.AreEqual(
				"a:2:{s:3:\"foo\";s:3:\"Foo\";s:3:\"bar\";s:3:\"Bar\";}",
				PhpSerialization.Serialize(
					new MyStruct() { foo = "Foo", bar = "Bar" }
				)
			);
		}

		[TestMethod]
		public void DeserializeArrayToStruct() {
			var value = PhpSerialization.Deserialize<MyStruct>(
				"a:2:{s:3:\"foo\";s:3:\"Foo\";s:3:\"bar\";s:3:\"Bar\";}"
			);

			Assert.AreEqual(
				"Foo",
				value.foo
			);
			Assert.AreEqual(
				"Bar",
				value.bar
			);
		}

		[TestMethod]
		public void DeserializeStructWithExcessKeys() {
			var value = PhpSerialization.Deserialize<MyStruct>(
				"a:3:{s:3:\"foo\";s:3:\"Foo\";s:3:\"bar\";s:3:\"Bar\";s:6:\"foobar\";s:6:\"FooBar\";}",
				new PhpDeserializationOptions() { AllowExcessKeys = true }
			);

			Assert.AreEqual(
				"Foo",
				value.foo
			);
			Assert.AreEqual(
				"Bar",
				value.bar
			);
		}

		[TestMethod]
		public void ThrowsOnStructWithExcessKeys() {
			var ex = Assert.ThrowsException<DeserializationException>(() => PhpSerialization.Deserialize<MyStruct>(
				"a:3:{s:3:\"foo\";s:3:\"Foo\";s:3:\"bar\";s:3:\"Bar\";s:6:\"foobar\";s:6:\"FooBar\";}",
				new PhpDeserializationOptions() { AllowExcessKeys = false }
			));
			Assert.AreEqual("Could not bind the key \"foobar\" to struct of type MyStruct: No such field.", ex.Message);
		}

		[TestMethod]
		public void DeserializeStructCaseInsensitive() {
			var value = PhpSerialization.Deserialize<MyStruct>(
				"a:2:{s:3:\"FOO\";s:3:\"Foo\";s:3:\"BAR\";s:3:\"Bar\";}",
				new PhpDeserializationOptions() { CaseSensitiveProperties = false }
			);

			Assert.AreEqual(
				"Foo",
				value.foo
			);
			Assert.AreEqual(
				"Bar",
				value.bar
			);
		}

		[TestMethod]
		public void DeserializeIgnoreField() {
			var value = PhpSerialization.Deserialize<MyStructIgnoreBar>(
				"a:2:{s:3:\"foo\";s:3:\"Foo\";s:3:\"bar\";s:3:\"Bar\";}"
			);
			Assert.AreEqual(
				"Foo",
				value.foo
			);
			Assert.AreEqual(
				null,
				value.bar
			);
		}

		[TestMethod]
		public void DeserializePropertyName() {
			var value = PhpSerialization.Deserialize<MyStructRenamedBar>(
				"a:2:{s:3:\"foo\";s:3:\"Foo\";s:6:\"foobar\";s:3:\"Bar\";}"
			);
			Assert.AreEqual(
				"Foo",
				value.foo
			);
			Assert.AreEqual(
				"Bar",
				value.bar
			);
		}

		[TestMethod]
		public void DeserializeBoolToStruct() {
			var ex = Assert.ThrowsException<DeserializationException>(
				() => PhpSerialization.Deserialize<MyStruct>(
					"b:1;"
				)
			);

			Assert.AreEqual(
				"Can not assign value \"1\" (at position 0) to target type of MyStruct.",
				ex.Message
			);
		}

		[TestMethod]
		public void DeserializeStringToStruct() {
			var ex = Assert.ThrowsException<DeserializationException>(
				() => PhpSerialization.Deserialize<MyStruct>(
					"s:3:\"foo\";"
				)
			);

			Assert.AreEqual(
				"Can not assign value \"foo\" (at position 0) to target type of MyStruct.",
				ex.Message
			);
		}

		[TestMethod]
		public void DeserializeNullToStruct() {
			Assert.AreEqual(
				default(MyStruct),
				PhpSerialization.Deserialize<MyStruct>(
					"N;"
				)
			);
		}
	}
}