namespace JsonParser.Tests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Collections.Generic;
    using JsonParserLib;

    [TestClass]
    public class JsonParserTests
    {
        #region Tests for ParseJson Method

        [TestMethod]
        public void ParseJson_ValidJsonWithStrings_ReturnsDictionary()
        {
            // Arrange
            string json = "{\"key1\":\"value1\", \"key2\":\"value2\"}";
            var expected = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };

            // Act
            var result = JsonParser.ParseJson(json);

            // Assert
            CollectionAssert.AreEquivalent(expected, result);
        }

        [TestMethod]
        public void ParseJson_ValidJsonWithIntegers_ReturnsDictionaryWithStrings()
        {
            // Arrange
            string json = "{\"key1\":\"123\", \"key2\":\"456\"}";
            var expected = new Dictionary<string, string>
            {
                { "key1", "123" },
                { "key2", "456" }
            };

            // Act
            var result = JsonParser.ParseJson(json);

            // Assert
            CollectionAssert.AreEquivalent(expected, result);
        }

        [TestMethod]
        public void ParseJson_ValidJsonWithEscapedCharacters_ReturnsDictionaryWithParsedValues()
        {
            // Arrange
            string json = "{\"key1\":\"Line1\\nLine2\", \"key2\":\"Tab\\tSeparated\"}";
            var expected = new Dictionary<string, string>
            {
                { "key1", "Line1\nLine2" },
                { "key2", "Tab\tSeparated" }
            };

            // Act
            var result = JsonParser.ParseJson(json);

            // Assert
            CollectionAssert.AreEquivalent(expected, result);
        }

        [TestMethod]
        public void ParseJson_JsonWithWhitespaceAroundFields_ReturnsCorrectDictionary()
        {
            // Arrange
            string json = "{   \"key1\" : \"value1\" , \"key2\" :   \"value2\"  }";
            var expected = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };

            // Act
            var result = JsonParser.ParseJson(json);

            // Assert
            CollectionAssert.AreEquivalent(expected, result);
        }

        [TestMethod]
        public void ParseJson_JsonWithEscapedQuotes_ReturnsCorrectDictionary()
        {
            // Arrange
            string json = "{\"key1\":\"He said \\\"Hello\\\"\", \"key2\":\"Another \\\"Test\\\"\"}";
            var expected = new Dictionary<string, string>
            {
                { "key1", "He said \"Hello\"" },
                { "key2", "Another \"Test\"" }
            };

            // Act
            var result = JsonParser.ParseJson(json);

            // Assert
            CollectionAssert.AreEquivalent(expected, result);
        }

        [TestMethod]
        public void ParseJson_JsonWithUnicodeEscapeSequences_ReturnsParsedUnicodeCharacters()
        {
            // Arrange
            string json = "{\"key1\":\"Hello \\u0048ello\"}";  // \u0048 is Unicode for 'H'
            var expected = new Dictionary<string, string>
            {
                { "key1", "Hello Hello" }
            };

            // Act
            var result = JsonParser.ParseJson(json);

            // Assert
            CollectionAssert.AreEquivalent(expected, result);
        }

        [TestMethod]
        public void ParseJson_InvalidJson_ThrowsFormatException()
        {
            // Arrange
            string invalidJson = "{\"key1\":\"value1\", key2:\"value2\"}"; // Missing quotes around key2

            // Act & Assert
            Assert.ThrowsException<FormatException>(() => JsonParser.ParseJson(invalidJson));
        }

        [TestMethod]
        public void ParseJson_MissingColonBetweenKeyAndValue_ThrowsFormatException()
        {
            // Arrange
            string invalidJson = "{\"key1\" \"value1\", \"key2\":\"value2\"}"; // Missing colon after key1

            // Act & Assert
            Assert.ThrowsException<FormatException>(() => JsonParser.ParseJson(invalidJson));
        }

        [TestMethod]
        public void ParseJson_ValidJsonWithNumbersAsValues_ReturnsDictionaryWithNumbersAsStrings()
        {
            // Arrange
            string json = "{\"key1\":\"42\", \"key2\":\"100\"}";
            var expected = new Dictionary<string, string>
            {
                { "key1", "42" },
                { "key2", "100" }
            };

            // Act
            var result = JsonParser.ParseJson(json);

            // Assert
            CollectionAssert.AreEquivalent(expected, result);
        }

        [TestMethod]
        public void ParseJson_JsonWithOnlyWhitespace_ThrowsFormatException()
        {
            // Arrange
            string invalidJson = "    "; // Only whitespace, no valid JSON

            // Act & Assert
            Assert.ThrowsException<FormatException>(() => JsonParser.ParseJson(invalidJson));
        }

        [TestMethod]
        public void ParseJson_JsonWithNestedQuotes_ThrowsFormatException()
        {
            // Arrange
            string invalidJson = "{\"key\":\"value with \"nested\" quotes\"}"; // Invalid due to nested quotes without escaping

            // Act & Assert
            Assert.ThrowsException<FormatException>(() => JsonParser.ParseJson(invalidJson));
        }

        #endregion
    }
}
