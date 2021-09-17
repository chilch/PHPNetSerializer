﻿/**
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
**/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PhpSerializerNET {
	public static class PhpSerialization {

		/// <summary>
		/// Deserialize the given string into an object.
		/// </summary>
		/// <param name="input">
		/// Data in the PHP de/serialization format.
		/// </param>
		/// <param name="options">
		/// Options for deserialization. See the  <see cref="PhpDeserializationOptions"/> class for more details.
		/// </param>
		/// <returns>
		/// The deserialized data. Either null or one of these types:
		/// <br/>
		/// <see cref="bool" />, <br/>
		/// <see cref="long" />, <br/>
		/// <see cref="double" />, <br/>
		/// <see cref="string" />, <br/>
		/// <see cref="List{object}"/> for arrays with integer keys <br/>
		/// <see cref="Dictionary{object,object}"/> for arrays with mixed keys or objects <br/>
		/// <see cref="System.Dynamic.ExpandoObject"/> for objects (see options).
		///
		/// </returns>
		public static object Deserialize(string input, PhpDeserializationOptions options = null) {
			return new PhpDeserializer(input, options).Deserialize();
		}

		/// <summary>
		/// The serialized data to deserialize.
		/// </summary>
		/// <param name="input">
		/// Data in the PHP de/serialization format.
		/// </param>
		/// <param name="options">
		/// Options for deserialization. See the  <see cref="PhpDeserializationOptions"/> class for more details.
		/// </param>
		/// <typeparam name="T">
		/// The desired output type.
		/// This should be one of the primitives or a class with a public parameterless constructor.
		/// </typeparam>
		/// <returns>
		/// The deserialized object.
		/// </returns>
		public static T Deserialize<T>(
			string input,
			PhpDeserializationOptions options = null
		) {
			return new PhpDeserializer(input, options).Deserialize<T>();
		}

		/// <summary>
		/// Serialize an object into the PHP format.
		/// </summary>
		/// <param name="input">
		/// Object to serialize.
		/// </param>
		/// <returns>
		/// String representation of the input object.
		/// Note that circular references are terminated with "N;"
		/// Arrays, lists and dictionaries are serialized into arrays.
		/// Objects may also be serialized into arrays, if their respective struct or class does not have the <see cref="PhpClass"/> attribute.
		/// </returns>
		public static string Serialize(object input) {
			var seenObjects = new List<object>();
			return Serialize(input, seenObjects);
		}

		internal static object GetValue(this MemberInfo member, object input) {
			if (member is PropertyInfo property) {
				return property.GetValue(input);
			}
			if (member is FieldInfo field) {
				return field.GetValue(input);
			}
			return null;
		}

		private static string SerializeMember(MemberInfo member, object input, List<object> seenObjects) {
			var propertyName = member.GetCustomAttribute<PhpPropertyAttribute>() != null
				? member.GetCustomAttribute<PhpPropertyAttribute>().Name
				: member.Name;
			return $"{Serialize(propertyName)}{Serialize(member.GetValue(input), seenObjects)}";
		}

		private static string SerializeToObject(object input, List<object> seenObjects) {
			var className = input.GetType().GetCustomAttribute<PhpClass>().Name;
			if (string.IsNullOrEmpty(className)) {
				className = "stdClass";
			}
			StringBuilder output = new StringBuilder();
			var properties = input.GetType().GetProperties().Where(y => y.CanRead && y.GetCustomAttribute<PhpIgnoreAttribute>() == null);

			output.Append("O:");
			output.Append(className.Length);
			output.Append(":\"");
			output.Append(className);
			output.Append("\":");
			output.Append(properties.Count());
			output.Append(":{");
			foreach (PropertyInfo property in properties) {
				output.Append(SerializeMember(property, input, seenObjects));
			}
			output.Append("}");
			return output.ToString();
		}

		private static string Serialize(object input, List<object> seenObjects) {
			StringBuilder output = new StringBuilder();
			switch (input) {
				case long longValue: {
						return $"i:{longValue.ToString()};";
					}
				case int integerValue: {
						return $"i:{integerValue.ToString()};";
					}
				case double floatValue: {
						if (double.IsPositiveInfinity(floatValue)) {
							return $"d:INF;";
						}
						if (double.IsNegativeInfinity(floatValue)) {
							return $"d:-INF;";
						}
						if (double.IsNaN(floatValue)) {
							return $"d:NAN;";
						}
						return $"d:{floatValue.ToString(CultureInfo.InvariantCulture)};";
					}
				case string stringValue: {
						// Use the UTF8 byte count, because that's what the PHP implementation does:
						return $"s:{ASCIIEncoding.UTF8.GetByteCount(stringValue)}:\"{stringValue}\";";
					}
				case bool boolValue: {
						return boolValue ? "b:1;" : "b:0;";
					}
				case null: {
						return "N;";
					}
				case IDictionary dictionary: {
						if (seenObjects.Contains(input)) {
							// Terminate circular reference:
							// It might be better to make this optional. The PHP implementation seems to
							// throw an exception, from what I can tell
							return "N;";
						}
						if (dictionary.GetType().GenericTypeArguments.Count() > 0) {
							var keyType = dictionary.GetType().GenericTypeArguments[0];
							if (!keyType.IsIConvertible() && keyType != typeof(object)) {
								throw new Exception($"Can not serialize associative array with key type {keyType.FullName}");
							}
						}
						seenObjects.Add(input);
						output.Append($"a:{dictionary.Count}:");
						output.Append("{");


						foreach (DictionaryEntry entry in dictionary) {

							output.Append($"{Serialize(entry.Key)}{Serialize(entry.Value, seenObjects)}");
						}
						output.Append("}");
						return output.ToString();
					}
				case IList collection: {
						if (seenObjects.Contains(input)) {
							return "N;"; // See above.
						}
						seenObjects.Add(input);
						output.Append($"a:{collection.Count}:");
						output.Append("{");
						for (int i = 0; i < collection.Count; i++) {
							output.Append(Serialize(i, seenObjects));
							output.Append(Serialize(collection[i], seenObjects));
						}
						output.Append("}");
						return output.ToString();
					}
				default: {
						if (seenObjects.Contains(input)) {
							return "N;"; // See above.
						}
						seenObjects.Add(input);
						var inputType = input.GetType();
						
						if (inputType.GetCustomAttribute<PhpClass>() != null) // TODO: add option.
						{
							return SerializeToObject(input, seenObjects);
						}

						IEnumerable<MemberInfo> members = inputType.IsValueType
							? inputType.GetFields().Where(y => y.IsPublic && y.GetCustomAttribute<PhpIgnoreAttribute>() == null)
							: inputType.GetProperties().Where(y => y.CanRead && y.GetCustomAttribute<PhpIgnoreAttribute>() == null);

						output.Append($"a:{members.Count()}:");
						output.Append("{");
						foreach (var member in members) {
							output.Append(SerializeMember(member, input, seenObjects));
						}
						output.Append("}");
						return output.ToString();
					}
			}
		}
	}
}
