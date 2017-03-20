using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace ArffTools
{
    /// <summary>
    /// Provides methods for reading ARFF (attribute-relation file format) data from a stream or file.
    /// </summary>
    public class ArffReader : IDisposable
    {
        StreamReader streamReader;

        ArffHeader arffHeader;

        bool disposed = false;

        /// <summary>
        /// Initializes a new <see cref="ArffReader"/> instance that reads from the specified stream using UTF-8 encoding.
        /// </summary>
        /// <param name="stream">The underlying stream that the <see cref="ArffReader"/> should read from.</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException"/>
        public ArffReader(Stream stream)
        {
            streamReader = new StreamReader(stream);
        }

        /// <summary>
        /// Initializes a new <see cref="ArffReader"/> instance that reads from the specified stream using the specified encoding.
        /// </summary>
        /// <param name="stream">The underlying stream that the <see cref="ArffReader"/> should read from.</param>
        /// <param name="encoding">The character encoding that should be used.</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException"/>
        public ArffReader(Stream stream, Encoding encoding)
        {
            streamReader = new StreamReader(stream, encoding);
        }

        /// <summary>
        /// Initializes a new <see cref="ArffReader"/> instance that reads from the specified file path using UTF-8 encoding.
        /// </summary>
        /// <param name="path">The file path that the <see cref="ArffReader"/> should read from.</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="FileNotFoundException"/>
        /// <exception cref="DirectoryNotFoundException"/>
        /// <exception cref="IOException"/>
        public ArffReader(string path)
        {
            streamReader = new StreamReader(path);
        }

        /// <summary>
        /// Initializes a new <see cref="ArffReader"/> instance that reads from the specified file path using the specified encoding.
        /// </summary>
        /// <param name="path">The file path that the <see cref="ArffReader"/> should read from.</param>
        /// <param name="encoding">The character encoding that should be used.</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="FileNotFoundException"/>
        /// <exception cref="DirectoryNotFoundException"/>
        /// <exception cref="IOException"/>
        /// <exception cref="NotSupportedException"/>
        public ArffReader(string path, Encoding encoding)
        {
            streamReader = new StreamReader(path, encoding);
        }

        private char UnescapeChar(char c)
        {
            switch (c)
            {
                case '"':
                case '\'':
                case '%':
                case '\\':
                default:
                    return c;
                case 'r':
                    return '\r';
                case 'n':
                    return '\n';
                case 't':
                    return '\t';
                case 'u': // the only universal character name supported is \u001E
                    return '\u001e';
            }
        }

        private string ReadToken(bool skipEndOfLine, out bool quoted, TextReader textReader)
        {
            char c = default(char);

            quoted = false;

            start:            

            // skip whitespace
            while (!textReader.EndOfStream()) 
            {
                c = (char)textReader.Peek();

                if (((c != '\r' && c != '\n') || skipEndOfLine) && char.IsWhiteSpace(c))
                    textReader.Read();
                else
                    break;
            } 

            if (c == '\r')
            {
                textReader.Read();
                if (textReader.Peek() == '\n')
                    textReader.Read();
                return null;
            }
            else if (c == '\n')
            {
                textReader.Read();
                return null;
            }
            else if (c == '%') // skip comment and return end-of-line
            {
                do
                {
                    c = (char)textReader.Read();
                    if (c == '\r' || c == '\n')
                        break;
                } while (!textReader.EndOfStream());
                if (c == '\r' && textReader.Peek() == '\n')
                    textReader.Read();
                if (skipEndOfLine)
                    goto start;
                return null;
            }

            if (textReader.EndOfStream())
                return null;

            StringBuilder token = new StringBuilder();

            char? quoteChar = null;

            if (textReader.Peek() == '\'' || textReader.Peek() == '\"')
                quoteChar = (char)textReader.Read();

            while (!textReader.EndOfStream())
            {
                c = (char)textReader.Peek();

                if (c == '\r' || c == '\n')
                {
                    if (quoteChar == null)
                        break;
                    else
                        throw new InvalidDataException("Unexpected end-of-line. Expected closing quotation mark.");
                }
                else if (token.Length != 0 && quoteChar == null && (char.IsWhiteSpace(c) || c == '%' || c == '{' || c == '}' || c == ','))
                    break;

                c = (char)textReader.Read();

                if (quoteChar == null)
                {
                    token.Append(c);

                    if (c == '%' || c == '{' || c == '}' || c == ',')
                        break;
                }
                else if (c == quoteChar)
                    break;
                else if (c == '\\')
                {
                    if (textReader.EndOfStream())
                        throw new InvalidDataException($"Unexpected end-of-line.");

                    c = (char)textReader.Read();
                    token.Append(UnescapeChar(c));

                    // the only universal character name supported is \u001E
                    if (c == 'u')
                        if ((char)textReader.Read() != '0' ||
                            (char)textReader.Read() != '0' ||
                            (char)textReader.Read() != '1' ||
                            (char)textReader.Read() != 'E')
                            throw new InvalidDataException($"Unsupported universal character name.");
                }
                else
                    token.Append(c);
            }

            quoted = quoteChar != null;

            return token.ToString();
        }

        private string ReadToken(out bool quoting, string expectedToken = null, bool ignoreCase = false, bool skipEndOfLine = false, bool? endOfLine = null, TextReader textReader = null)
        {
            if (textReader == null)
                textReader = streamReader;

            string token = ReadToken(skipEndOfLine, out quoting, textReader);

            if (endOfLine != null)
                if (endOfLine == true && token != null)
                    throw new InvalidDataException($"Unexpected token \"{token}\". Expected end-of-line.");
                else if (endOfLine == false && token == null)
                    if (expectedToken == null)
                        throw new InvalidDataException($"Unexpected end-of-line. Expected value.");
                    else
                        throw new InvalidDataException($"Unexpected end-of-line. Expected token \"{expectedToken}\".");

            if (expectedToken != null)
                if (string.Compare(token, expectedToken, ignoreCase, CultureInfo.InvariantCulture) != 0)
                    throw new InvalidDataException($"Unexpected token \"{token}\". Expected \"{expectedToken}\".");

            return token;
        }

        private string ReadToken(string expectedToken = null, bool ignoreCase = false, bool skipEndOfLine = false, bool? endOfLine = null, bool? quoting = null, TextReader textReader = null)
        {
            if (textReader == null)
                textReader = streamReader;

            string token = ReadToken(skipEndOfLine, out bool quoted, textReader);

            if (endOfLine != null)
                if (endOfLine == true && token != null)
                    throw new InvalidDataException($"Unexpected token \"{token}\". Expected end-of-line.");
                else if (endOfLine == false && token == null)
                    if (expectedToken == null)
                        throw new InvalidDataException($"Unexpected end-of-line. Expected value.");
                    else
                        throw new InvalidDataException($"Unexpected end-of-line. Expected token \"{expectedToken}\".");

            if (expectedToken != null)
                if (token == null)
                    throw new InvalidDataException($"Unexpected end-of-line. Expected token \"{expectedToken}\".");
                else if (string.Compare(token, expectedToken, ignoreCase, CultureInfo.InvariantCulture) != 0)
                    throw new InvalidDataException($"Unexpected token \"{token}\". Expected \"{expectedToken}\".");

            if (quoting != null)
                if (quoting.Value != quoted)
                    if (token != null)
                        throw new InvalidDataException($"Incorrect quoting for token \"{token}\".");
                    else
                        throw new InvalidDataException($"Unexpected end-of-line. Expected value.");

            return token;
        }

        private ArffAttribute ReadAttribute()
        {
            string attributeName = ReadToken(endOfLine: false);
            string typeString = ReadToken(endOfLine: false, quoting: false);

            ArffAttributeType attributeType;

            if (string.Equals(typeString, "numeric", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(typeString, "integer", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(typeString, "real", StringComparison.OrdinalIgnoreCase))
            {
                attributeType = ArffAttributeType.Numeric;
                ReadToken(endOfLine: true);
            }
            else if (string.Equals(typeString, "string", StringComparison.OrdinalIgnoreCase))
            {
                attributeType = ArffAttributeType.String;
                ReadToken(endOfLine: true);
            }
            else if (string.Equals(typeString, "date", StringComparison.OrdinalIgnoreCase))
            {
                string dateFormat = ReadToken();

                if (dateFormat == null)
                    attributeType = ArffAttributeType.Date();
                else
                {
                    attributeType = ArffAttributeType.Date(dateFormat);
                    ReadToken(endOfLine: true);
                }
            }
            else if (typeString == "{")
            {
                List<string> nominalValues = new List<string>();

                while (true)
                {
                    string value = ReadToken(out bool quoted, endOfLine: false);

                    if (!quoted && value == "}") 
                        break;
                    else if (!quoted && value == ",")
                        continue;
                    else
                        nominalValues.Add(value);
                }

                attributeType = ArffAttributeType.Nominal(nominalValues);
                ReadToken(endOfLine: true);
            }
            else if (string.Equals(typeString, "relational", StringComparison.OrdinalIgnoreCase))
            {
                ReadToken(endOfLine: true);

                List<ArffAttribute> childAttributes = new List<ArffAttribute>();

                while (true)
                {
                    string token = ReadToken(skipEndOfLine: true, endOfLine: false, quoting: false);

                    if (string.Equals(token, "@attribute", StringComparison.OrdinalIgnoreCase))
                    {
                        ArffAttribute attribute = ReadAttribute();

                        childAttributes.Add(attribute);
                    }
                    else if (string.Equals(token, "@end", StringComparison.OrdinalIgnoreCase))
                    {
                        ReadToken(expectedToken: attributeName, endOfLine: false);
                        ReadToken(endOfLine: true);
                        break;
                    }
                    else
                        throw new InvalidDataException($"Unexpected token \"{token}\". Expected \"@attribute\" or \"@end\".");
                }

                attributeType = ArffAttributeType.Relational(childAttributes);
            }
            else
                throw new InvalidDataException($"Unexpected token \"{typeString}\". Expected attribute type.");

            return new ArffAttribute(attributeName, attributeType);
        }

        /// <summary>
        /// Reads relation name and attribute declarations as an <see cref="ArffHeader"/> instance.
        /// </summary>
        /// <returns><see cref="ArffHeader"/> instance with read data.</returns>
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="InvalidOperationException"/>
        /// <exception cref="InvalidDataException"/>
        public ArffHeader ReadHeader()
        {
            if (disposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (arffHeader != null)
                throw new InvalidOperationException("The header has already been read by a previous call of ReadHeader.");

            List<ArffAttribute> attributes = new List<ArffAttribute>();

            ReadToken(expectedToken: "@relation", ignoreCase: true, skipEndOfLine: true, endOfLine: false, quoting: false);

            string relationName = ReadToken(endOfLine: false);

            ReadToken(endOfLine: true);

            while (true)
            {
                string token = ReadToken(skipEndOfLine: true, endOfLine: false, quoting: false);

                if (string.Equals(token, "@attribute", StringComparison.OrdinalIgnoreCase))
                {
                    ArffAttribute attribute = ReadAttribute();

                    attributes.Add(attribute);
                }
                else if (string.Equals(token, "@data", StringComparison.OrdinalIgnoreCase))
                {
                    ReadToken(endOfLine: true);
                    break;
                }
                else
                    throw new InvalidDataException($"Unexpected token \"{token}\". Expected \"@attribute\" or \"@data\".");
            }

            if (attributes.Count == 0)
                throw new InvalidDataException("Expected at least one \"@attribute\".");

            arffHeader = new ArffHeader(relationName, attributes);

            return arffHeader;
        }

        /// <summary>
        /// Reads data of a single instance. <c>null</c> is returned if the end-of-file is reached.
        /// </summary>
        /// <returns>The instance data as <see cref="object"/>[], or <c>null</c> if the end-of-file was reached.
        /// <para>The element types in the returned array depend on the type of their corresponding attribute:
        /// <see cref="double"/> (numeric attribute),
        /// <see cref="string"/> (string attribute),
        /// <see cref="int"/> (nominal attribute, index into nominal values array),
        /// <see cref="DateTime"/> (date attribute),
        /// <see cref="object"/>[][] (relational attribute).
        /// Missing values are represented as <c>null</c>.</para>
        /// </returns>
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="InvalidOperationException"/>
        /// <exception cref="InvalidDataException"/>
        public object[] ReadInstance()
        {
            return ReadInstance(out double? instanceWeight);
        }

        /// <summary>
        /// Reads data of a single instance. <c>null</c> is returned if the end-of-file is reached.
        /// </summary>
        /// <param name="instanceWeight">Variable that will be set to the instance weight or to <c>null</c>, if no weight is associated with the instance.</param>
        /// <returns>The instance data or <c>null</c> if the end-of-file was reached.
        /// <para>The element types in the returned array depend on the type of their corresponding attribute:
        /// <see cref="double"/> (numeric attribute),
        /// <see cref="string"/> (string attribute),
        /// <see cref="int"/> (nominal attribute, index into nominal values array),
        /// <see cref="DateTime"/> (date attribute),
        /// <see cref="object"/>[][] (relational attribute).
        /// Missing values are represented as <c>null</c>.</para>
        /// </returns>
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="InvalidOperationException"/>
        /// <exception cref="InvalidDataException"/>
        public object[] ReadInstance(out double? instanceWeight)
        {
            if (disposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (arffHeader == null)
                throw new InvalidOperationException("Before any instances can be read, the header needs to be read by a call to ReadHeader.");

            return ReadInstance(out instanceWeight, arffHeader.Attributes, streamReader);
        }

        private object[] ReadInstance(out double? instanceWeight, IReadOnlyList<ArffAttribute> attributes, TextReader textReader)
        {
            instanceWeight = null;

            char c = default(char);

            // skip whitespace, comments and end-of-line
            while (!textReader.EndOfStream())
            {
                c = (char)textReader.Peek();

                if (char.IsWhiteSpace(c))
                    textReader.Read();
                else if (c == '%')
                {
                    do
                    {
                        c = (char)textReader.Read();
                        if (c == '\r' || c == '\n')
                            break;
                    } while (!textReader.EndOfStream());
                    if (c == '\r' && textReader.Peek() == '\n')
                        textReader.Read();
                }
                else
                    break;
            }

            if (textReader.EndOfStream())
                return null;

            object[] instance;

            if (c == '{')
                instance = ReadSparseInstance(attributes, textReader);
            else
            {
                instance = new object[attributes.Count];

                for (int i = 0; i < instance.Length; i++)
                {
                    string value = ReadToken(out bool quoted, endOfLine: false, textReader: textReader);

                    instance[i] = ParseValue(value, quoted, attributes[i].Type);

                    if (i != instance.Length - 1)
                        ReadToken(expectedToken: ",", endOfLine: false, quoting: false, textReader: textReader);
                }
            }

            string token = ReadToken(quoting: false, textReader: textReader);

            if (token != null)
                if (token == ",")
                {
                    ReadToken(expectedToken: "{", endOfLine: false, quoting: false, textReader: textReader);
                    string weightToken = ReadToken(endOfLine: false, textReader: textReader);

                    if (!double.TryParse(weightToken, NumberStyles.Float, CultureInfo.InvariantCulture, out double weight))
                        throw new InvalidDataException($"Invalid instance weight \"{weightToken}\".");

                    instanceWeight = weight;

                    ReadToken(expectedToken: "}", endOfLine: false, quoting: false, textReader: textReader);
                    ReadToken(endOfLine: true, textReader: textReader);
                }
                else
                    throw new InvalidDataException($"Unexpected token \"{token}\". Expected \",\" or end-of-line.");

            return instance;
        }

        private object[] ReadSparseInstance(IReadOnlyList<ArffAttribute> attributes, TextReader textReader)
        {
            object[] instance = new object[attributes.Count];

            for (int i = 0; i < instance.Length; i++)
                if (attributes[i].Type is ArffNumericAttribute)
                    instance[i] = 0.0;
                else if (attributes[i].Type is ArffNominalAttribute)
                    instance[i] = 0;

            ReadToken(expectedToken: "{", endOfLine: false, quoting: false, textReader : textReader);

            string token = ReadToken(endOfLine: false, quoting: false, textReader: textReader);

            if (token == "}")
                return instance;

            while (true)
            {
                if (!int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out int index))
                    throw new InvalidDataException($"Unexpected token \"{token}\". Expected index.");

                if (index < 0 || index >= instance.Length)
                    throw new InvalidDataException($"Out-of-range index \"{token}\".");

                string value = ReadToken(out bool quoted, endOfLine: false, textReader: textReader);

                instance[index] = ParseValue(value, quoted, attributes[index].Type);

                token = ReadToken(endOfLine: false, quoting: false, textReader: textReader);

                if (token == "}")
                    break;
                else if (token != ",")
                    throw new InvalidDataException($"Unexpected token \"{token}\". Expected \",\" or \"}}\".");

                token = ReadToken(endOfLine: false, quoting: false, textReader: textReader);
            }

            return instance;
        }

        private object ParseValue(string value, bool quoted, ArffAttributeType attributeType)
        {
            if (!quoted && value == "?")
                return null;

            if (attributeType == ArffAttributeType.Numeric)
            {
                if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                    throw new InvalidDataException($"Unrecognized data value: \"{value}\"");

                return d;
            }
            else if (attributeType == ArffAttributeType.String)
                return value;
            else if (attributeType is ArffNominalAttribute nominalAttribute)
            {
                int index = nominalAttribute.Values.IndexOf(value);

                if (index == -1)
                    throw new InvalidDataException($"Unrecognized data value: \"{value}\"");

                return index;
            }
            else if (attributeType is ArffDateAttribute dateAttribute)
            {
                if (!DateTime.TryParseExact(value, dateAttribute.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.NoCurrentDateDefault, out DateTime d))
                    throw new InvalidDataException($"Unrecognized data value: \"{value}\"");

                return d;
            }
            else if (attributeType is ArffRelationalAttribute relationalAttribute)
            {
                List<object[]> relationalInstances = new List<object[]>();

                using (StringReader stringReader = new StringReader(value))
                    while (true)
                    {
                        // weights for relational instances are currently discarded
                        object[] instance = ReadInstance(out double? instanceWeight, relationalAttribute.ChildAttributes, stringReader);

                        if (instance == null)
                            break;

                        relationalInstances.Add(instance);         
                    }

                return relationalInstances.ToArray();
            }
            else
                throw new ArgumentException("Unsupported ArffAttributeType.", nameof(attributeType));
        }

        /// <summary>
        /// Reads data of all instances.
        /// </summary>
        /// <returns>Array with data of all instances.</returns>
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="InvalidOperationException"/>
        /// <exception cref="InvalidDataException"/>
        /// <seealso cref="ReadInstance()"/>
        public object[][] ReadAllInstances()
        {
            List<object[]> instances = new List<object[]>();

            object[] instance;

            while ((instance = ReadInstance()) != null)
                instances.Add(instance);

            return instances.ToArray();
        }

        /// <summary>
        /// Returns an enumerable that reads data of all instances during enumeration.
        /// </summary>
        /// <returns>Enumerable with data of all instances.</returns>
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="InvalidOperationException"/>
        /// <exception cref="InvalidDataException"/>
        /// <seealso cref="ReadAllInstances()"/>
        public IEnumerable<object[]> ReadInstances()
        {
            object[] instance;

            while ((instance = ReadInstance()) != null)
                yield return instance;
        }

        /// <summary>
        /// Releases all resources used by the <see cref="ArffReader"/> object.
        /// </summary>
        /// <param name="disposing">Whether this method is called from <see cref="IDisposable.Dispose"/>.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                    streamReader.Dispose();

                streamReader = null;
                arffHeader = null;

                disposed = true;
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="ArffReader"/> object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
    }
}
