using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ArffTools
{
    /// <summary>
    /// Provides methods for writing ARFF (attribute-relation file format) data into a stream or file.
    /// </summary>
    public class ArffWriter : IDisposable
    {
        StreamWriter streamWriter;       

        int step = 0;

        List<ArffAttribute> writtenAttributes = new List<ArffAttribute>();

        bool disposed = false;

        /// <summary>
        /// Initializes a new <see cref="ArffWriter"/> instance that writes to the specified stream using UTF-8 encoding.
        /// </summary>
        /// <param name="stream">The underlying stream that the <see cref="ArffWriter"/> should write to.</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException"/>
        public ArffWriter(Stream stream) : this(stream, new UTF8Encoding(false))
        {
        }

        /// <summary>
        /// Initializes a new <see cref="ArffWriter"/> instance that writes to the specified stream using the specified encoding.
        /// </summary>
        /// <param name="stream">The underlying stream that the <see cref="ArffWriter"/> should write to.</param>
        /// <param name="encoding">The character encoding that should be used.</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException"/>
        public ArffWriter(Stream stream, Encoding encoding)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            if (!stream.CanWrite)
                throw new ArgumentException("The specified stream is not writeable.", nameof(stream));

            streamWriter = new StreamWriter(stream, encoding);
        }

        /// <summary>
        /// Initializes a new <see cref="ArffWriter"/> instance that writes to the specified file path using UTF-8 encoding.
        /// </summary>
        /// <param name="path">The file path that the <see cref="ArffWriter"/> should write to.</param>
        /// <exception cref="UnauthorizedAccessException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="DirectoryNotFoundException"/>
        /// <exception cref="IOException"/>
        /// <exception cref="PathTooLongException"/>
        /// <exception cref="System.Security.SecurityException"/>
        public ArffWriter(string path) : this(path, new UTF8Encoding(false))
        {            
        }

        /// <summary>
        /// Initializes a new <see cref="ArffWriter"/> instance that writes to the specified file path using the specified encoding.
        /// </summary>
        /// <param name="path">The file path that the <see cref="ArffWriter"/> should write to.</param>
        /// <param name="encoding">The character encoding that should be used.</param>
        /// <exception cref="UnauthorizedAccessException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="DirectoryNotFoundException"/>
        /// <exception cref="IOException"/>
        /// <exception cref="PathTooLongException"/>
        /// <exception cref="System.Security.SecurityException"/>
        public ArffWriter(string path, Encoding encoding)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));

            streamWriter = new StreamWriter(path, false, encoding);
        }

        private string QuoteAndEscape(string s)
        {
            if (s == string.Empty)
                return "''";
            if (s == "?")
                return "'?'";

            StringBuilder stringBuilder = new StringBuilder(s.Length + 2);

            bool quote = false;

            foreach (char c in s)
                switch (c)
                {
                    case '"':
                        stringBuilder.Append("\\\"");
                        quote = true;
                        break;
                    case '\'':
                        stringBuilder.Append("\\'");
                        quote = true;
                        break;
                    case '%':
                        stringBuilder.Append("\\%");
                        quote = true;
                        break;
                    case '\\':
                        stringBuilder.Append("\\\\");
                        quote = true;
                        break;
                    case '\r':
                        stringBuilder.Append("\\r");
                        quote = true;
                        break;
                    case '\n':
                        stringBuilder.Append("\\n");
                        quote = true;
                        break;
                    case '\t':
                        stringBuilder.Append("\\t");
                        quote = true;
                        break;
                    case '\u001E':
                        stringBuilder.Append("\\u001E");
                        quote = true;
                        break;
                    case ' ':
                    case ',':
                    case '{':
                    case '}':                                   
                        stringBuilder.Append(c);
                        quote = true;
                        break;
                    default:
                        stringBuilder.Append(c);
                        break;
                }

            if (quote)
            {
                stringBuilder.Insert(0, '\'');
                stringBuilder.Append('\'');
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Writes the ARFF header (@relation &lt;relation-name&gt;) with the relation name.
        /// Must be called before any other data can be written.
        /// </summary>
        /// <param name="relationName">The name of the relation that is to be written.</param>
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="InvalidOperationException"/>
        /// <exception cref="IOException"/>
        public void WriteRelationName(string relationName)
        {
            if (disposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (relationName == null)
                throw new ArgumentNullException(nameof(relationName));
            if (step != 0)
                throw new InvalidOperationException("The relation name must be the first data in the file and must appear exactly once.");

            streamWriter.WriteLine("@relation {0}", QuoteAndEscape(relationName));
            streamWriter.WriteLine();

            step = 1;
        }

        /// <summary>
        /// Writes an attribute declaration (@attribute &lt;...&gt;) for the specified <see cref="ArffAttribute"/>.
        /// Must be called after the relation name has been written and before any data instances are written.
        /// </summary>
        /// <param name="attribute">An <see cref="ArffAttribute"/> object representing the attribute.</param>
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="InvalidOperationException"/>
        /// <exception cref="IOException"/>
        public void WriteAttribute(ArffAttribute attribute)
        {
            if (disposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (attribute == null)
                throw new ArgumentNullException(nameof(attribute));
            if (step != 1 && step != 2)
                throw new InvalidOperationException("All attributes must be written after the relation name and before any instances.");

            WriteAttribute(attribute, 0);

            writtenAttributes.Add(attribute);

            step = 2;
        }

        private void WriteAttribute(ArffAttribute attribute, int indent)
        {
            string type;

            if (attribute.Type == ArffAttributeType.Numeric)
                type = "numeric";
            else if (attribute.Type == ArffAttributeType.String)
                type = "string";
            else if (attribute.Type is ArffNominalAttribute)
                type = "{" + string.Join(",", ((ArffNominalAttribute)attribute.Type).Values.Select(v => QuoteAndEscape(v))) + "}";
            else if (attribute.Type is ArffDateAttribute)
            {
                string dateFormat = ((ArffDateAttribute)attribute.Type).DateFormat;
                if (dateFormat == ArffDateAttribute.DefaultDateFormat)
                    type = "date";
                else
                    type = "date " + QuoteAndEscape(dateFormat);
            }
            else if (attribute.Type is ArffRelationalAttribute)
                type = "relational";
            else
                throw new ArgumentException("Unsupported attribute type.", nameof(attribute));

            if (indent != 0)
                streamWriter.Write(new string(' ', indent));

            streamWriter.WriteLine("@attribute {0} {1}", QuoteAndEscape(attribute.Name), type);

            if (type == "relational")
            {
                foreach (ArffAttribute childAttribute in ((ArffRelationalAttribute)attribute.Type).ChildAttributes)
                    WriteAttribute(childAttribute, indent + 2);

                if (indent != 0)
                    streamWriter.Write(new string(' ', indent));

                streamWriter.WriteLine("@end {0}", QuoteAndEscape(attribute.Name));
            }
        }

        /// <summary>
        /// Writes the relation name and all attribute declarations from the specified <see cref="ArffHeader"/>.
        /// </summary>
        /// <param name="header">The <see cref="ArffHeader"/> object containing relation name and attributes to write.</param>
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="InvalidOperationException"/>
        /// <exception cref="IOException"/>
        public void WriteHeader(ArffHeader header)
        {
            if (header == null)
                throw new ArgumentNullException(nameof(header));

            WriteRelationName(header.RelationName);

            foreach (ArffAttribute attribute in header.Attributes)
                WriteAttribute(attribute);
        }

        /// <summary>
        /// Writes the data of the specified instance.
        /// May be called only after the relation name and all attribute declarations have been written.
        /// </summary>
        /// <param name="instance">The instance data to write. Length and element types must conform to all previously written attribute declarations.
        /// <para>
        /// The expected element types depend on the type of their corresponding attribute:
        /// <see cref="double"/> (numeric attribute),
        /// <see cref="string"/> (string attribute),
        /// <see cref="int"/> (nominal attribute, index into nominal values array),
        /// <see cref="DateTime"/> (date attribute),
        /// <see cref="object"/>[] (relational attribute).
        /// Missing values should be represented as <c>null</c>.</para>
        /// </param>
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="InvalidOperationException"/>
        /// <exception cref="IOException"/>
        public void WriteInstance(object[] instance)
        {
            WriteInstance(instance, false, double.NaN);
        }

        /// <summary>
        /// Writes the data of the specified instance.
        /// May be called only after the relation name and all attribute declarations have been written.
        /// </summary>
        /// <param name="instance">The instance data to write. Length and element types must conform to all previously written attribute declarations.
        /// <para>
        /// The expected element types depend on the type of their corresponding attribute:
        /// <see cref="double"/> (numeric attribute),
        /// <see cref="string"/> (string attribute),
        /// <see cref="int"/> (nominal attribute, index into nominal values array),
        /// <see cref="DateTime"/> (date attribute),
        /// <see cref="object"/>[] (relational attribute).
        /// Missing values should be represented as <c>null</c>.</para>
        /// </param>
        /// <param name="sparse">True, if the instance should be written in sparse format.</param>
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="InvalidOperationException"/>
        /// <exception cref="IOException"/>
        public void WriteInstance(object[] instance, bool sparse)
        {
            WriteInstance(instance, sparse, double.NaN);
        }

        /// <summary>
        /// Writes the data of the specified instance with an associated instance weight.
        /// May be called only after the relation name and all attribute declarations have been written.
        /// </summary>
        /// <param name="instance">The instance data to write. Length and element types must conform to all previously written attribute declarations.
        /// <para>The expected element types depend on the type of their corresponding attribute:
        /// <see cref="double"/> (numeric attribute),
        /// <see cref="string"/> (string attribute),
        /// <see cref="int"/> (nominal attribute, index into nominal values array),
        /// <see cref="DateTime"/> (date attribute),
        /// <see cref="object"/>[] (relational attribute).
        /// Missing values should be represented as <c>null</c>.</para>
        /// </param>
        /// <param name="sparse">True, if the instance should be written in sparse format.</param>
        /// <param name="instanceWeight">The instance weight.</param>
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="InvalidOperationException"/>
        /// <exception cref="IOException"/>
        public void WriteInstance(object[] instance, bool sparse, double instanceWeight)
        {
            if (disposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            if (instance.Length != writtenAttributes.Count)
                throw new ArgumentException("Instance does not have the same number of entries as attributes have been written.", nameof(instance));
            if (step < 2)
                throw new InvalidOperationException("The relation name and at least one attribute must have been written before any instances can be written.");

            if (step == 2)
            {
                streamWriter.WriteLine();
                streamWriter.WriteLine("@data");

                step = 3;
            }

            if (sparse)
                streamWriter.Write('{');

            WriteInstanceData(instance, sparse, writtenAttributes, streamWriter);

            if (sparse)
                streamWriter.Write('}');

            if (!double.IsNaN(instanceWeight))
                streamWriter.Write(",{" + instanceWeight.ToString(CultureInfo.InvariantCulture) + "}");

            streamWriter.WriteLine();
        }

        private void WriteInstanceData(object[] instance, bool sparse, IReadOnlyList<ArffAttribute> attributes, TextWriter textWriter)
        {
            int numAttributesWritten = 0;

            for (int i = 0; i < instance.Length; i++)
            {
                object value = instance[i];

                if (sparse)
                {
                    if ((value is double && (double)value == 0.0) || (value is int && (int)value == 0))
                        continue;

                    if (numAttributesWritten != 0)
                        textWriter.Write("," + i + " ");
                    else
                        textWriter.Write(i + " ");
                }
                else if (numAttributesWritten != 0)
                    textWriter.Write(",");

                WriteValue(value, attributes[i], textWriter);

                numAttributesWritten++;
            }
        }

        /// <summary>
        /// Writes the data of all specified instances.
        /// May be called only after the relation name and all attribute declarations have been written.
        /// </summary>
        /// <param name="instances">Enumerable of instances to write.</param>
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="InvalidOperationException"/>
        /// <exception cref="IOException"/>
        /// <seealso cref="WriteInstance(object[])"/>
        public void WriteAllInstances(IEnumerable<object[]> instances)
        {
            WriteAllInstances(instances, false);
        }

        /// <summary>
        /// Writes the data of all specified instances.
        /// May be called only after the relation name and all attribute declarations have been written.
        /// </summary>
        /// <param name="instances">Enumerable of instances to write.</param>
        /// <param name="sparse">True, if the instances should be written in sparse format.</param>
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="InvalidOperationException"/>
        /// <exception cref="IOException"/>
        /// <seealso cref="WriteInstance(object[], bool)"/>
        public void WriteAllInstances(IEnumerable<object[]> instances, bool sparse)
        {
            if (instances == null)
                throw new ArgumentNullException(nameof(instances));

            foreach (object[] instance in instances)
                WriteInstance(instance, sparse);
        }

        private void WriteValue(object value, ArffAttribute attribute, TextWriter textWriter)
        {
            if (value == null)
                textWriter.Write("?");
            else if (value is double)
                textWriter.Write(((double)value).ToString(CultureInfo.InvariantCulture));
            else if (value is string)
                textWriter.Write(QuoteAndEscape((string)value));
            else if (value is int)
            {
                ReadOnlyCollection<string> values = (attribute.Type as ArffNominalAttribute)?.Values;

                if (values == null || values.Count <= (int)value)
                    throw new ArgumentException("Instance is incompatible with types of written attributes.", "instance");

                textWriter.Write(QuoteAndEscape(values[(int)value]));
            }
            else if (value is DateTime)
            {
                string dateFormat = (attribute.Type as ArffDateAttribute)?.DateFormat;

                if (dateFormat == null)
                    throw new ArgumentException("Instance is incompatible with types of written attributes.", "instance");

                textWriter.Write(QuoteAndEscape(((DateTime)value).ToString(dateFormat, CultureInfo.InvariantCulture)));
            }
            else if (value is object[][])
            {
                ReadOnlyCollection<ArffAttribute> relationalAttributes = (attribute.Type as ArffRelationalAttribute)?.ChildAttributes;

                if (relationalAttributes == null)
                    throw new ArgumentException("Instance is incompatible with types of written attributes.", "instance");

                using (StringWriter stringWriter = new StringWriter())
                {
                    object[][] relationalInstances = (object[][])value;

                    for (int j = 0; j < relationalInstances.Length; j++)
                    {
                        // sparse format does not seem to be supported in relational values
                        // instance weights seem to be supported in the format but they cannot be represented with an object[] alone, so we don't support them for now
                        WriteInstanceData(relationalInstances[j], false, relationalAttributes, stringWriter); 

                        if (j != relationalInstances.Length - 1)
                            stringWriter.WriteLine();
                    }

                    textWriter.Write(QuoteAndEscape(stringWriter.GetStringBuilder().ToString()));
                }
            }
            else
                throw new ArgumentException("Unsupported data type in instance.", "instance");
        }

        /// <summary>
        /// Writes a comment.
        /// </summary>
        /// <param name="comment">The comment to write.</param>
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="IOException"/>
        public void WriteComment(string comment)
        {
            if (disposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (comment == null)
                throw new ArgumentNullException(nameof(comment));

            if (comment.Contains("\r") || comment.Contains("\n"))
            {
                StringReader stringReader = new StringReader(comment);

                string line;

                while ((line = stringReader.ReadLine()) != null)
                    streamWriter.Write("% {0}", line);
            }
            else
                streamWriter.Write("% {0}", comment);
        }

        /// <summary>
        /// Flushes any buffered data to the underlying stream or file.
        /// </summary>
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="IOException"/>
        /// <exception cref="EncoderFallbackException"/>
        public void Flush()
        {
            if (disposed)
                throw new ObjectDisposedException(GetType().FullName);

            streamWriter.Flush();
        }

        /// <summary>
        /// Writes an ARFF file with the specified path, relation name, attributes and instances.
        /// </summary>
        /// <param name="path">The path of the file that should be written.</param>
        /// <param name="relationName">The name of the relation that is to be written.</param>
        /// <param name="attributes">The attributes of the data to be written.</param>
        /// <param name="instances">The instance data to write. Length and element types must conform to the attributes.
        /// <para>
        /// The expected element types depend on the type of their corresponding attribute:
        /// <see cref="double"/> (numeric attribute),
        /// <see cref="string"/> (string attribute),
        /// <see cref="int"/> (nominal attribute, index into nominal values array),
        /// <see cref="DateTime"/> (date attribute),
        /// <see cref="object"/>[] (relational attribute).
        /// Missing values should be represented as <c>null</c>.</para>
        /// </param>
        /// <exception cref="UnauthorizedAccessException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="DirectoryNotFoundException"/>
        /// <exception cref="IOException"/>
        /// <exception cref="PathTooLongException"/>
        /// <exception cref="System.Security.SecurityException"/>
        public static void Write(string path, string relationName, IEnumerable<ArffAttribute> attributes, IEnumerable<object[]> instances)
        {
            Write(path, relationName, attributes, instances, false);
        }

        /// <summary>
        /// Writes an ARFF file with the specified path, relation name, attributes and instances.
        /// </summary>
        /// <param name="path">The path of the file that should be written.</param>
        /// <param name="relationName">The name of the relation that is to be written.</param>
        /// <param name="attributes">The attributes of the data to be written.</param>
        /// <param name="instances">The instance data to write. Length and element types must conform to the attributes.
        /// <para>
        /// The expected element types depend on the type of their corresponding attribute:
        /// <see cref="double"/> (numeric attribute),
        /// <see cref="string"/> (string attribute),
        /// <see cref="int"/> (nominal attribute, index into nominal values array),
        /// <see cref="DateTime"/> (date attribute),
        /// <see cref="object"/>[] (relational attribute).
        /// Missing values should be represented as <c>null</c>.</para>
        /// </param>
        /// <param name="sparse">True, if the instances should be written in sparse format.</param>
        /// <exception cref="UnauthorizedAccessException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="DirectoryNotFoundException"/>
        /// <exception cref="IOException"/>
        /// <exception cref="PathTooLongException"/>
        /// <exception cref="System.Security.SecurityException"/>
        public static void Write(string path, string relationName, IEnumerable<ArffAttribute> attributes, IEnumerable<object[]> instances, bool sparse)
        {
            if (attributes == null)
                throw new ArgumentNullException(nameof(attributes));

            using (ArffWriter arffWriter = new ArffWriter(path))
            {
                arffWriter.WriteRelationName(relationName);

                foreach (ArffAttribute attribute in attributes)
                    arffWriter.WriteAttribute(attribute);

                arffWriter.WriteAllInstances(instances, sparse);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                    streamWriter.Dispose();

                streamWriter = null;
                writtenAttributes = null;

                disposed = true;
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="ArffWriter"/> object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
    }
}
