using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ArffTools
{
    /// <summary>
    /// Represents an attribute in an ARFF file.
    /// </summary>
    public class ArffAttribute
    {
        /// <summary>
        /// Gets the name of the attribute.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the type of the attribute.
        /// </summary>
        public ArffAttributeType Type { get; }

        /// <summary>
        /// Initializes a new <see cref="ArffAttribute"/> instance with the specified name and attribute type.
        /// </summary>
        /// <param name="name">The name of the attribute to create.</param>
        /// <param name="type">The type of the attribute to create.</param>
        /// <exception cref="ArgumentNullException"/>
        public ArffAttribute(string name, ArffAttributeType type)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            Name = name;
            Type = type;
        }

        /// <summary>
        /// Determines whether this object is equal to another object (an <see cref="ArffAttribute"/> with the same name and type).
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            ArffAttribute other = obj as ArffAttribute;

            if (other == null)
                return false;

            return other.Name == Name && other.Type.Equals(Type);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ Type.GetHashCode();
        }

        /// <summary>
        /// Returns a string representation of this instance.
        /// </summary>
        /// <returns>The string representation of the current object.</returns>
        public override string ToString()
        {
            return $"@attribute {ArffWriter.QuoteAndEscape(Name)} {Type}";
        }
    }

    /// <summary>
    /// Abstract base class for all ARFF attribute types.
    /// </summary>
    public abstract class ArffAttributeType
    {
        /// <summary>
        /// Numeric attribute type.
        /// </summary>
        public static readonly ArffNumericAttribute Numeric = new ArffNumericAttribute();

        /// <summary>
        /// String attribute type.
        /// </summary>
        public static readonly ArffStringAttribute String = new ArffStringAttribute();

        static readonly ArffDateAttribute date = new ArffDateAttribute();

        internal ArffAttributeType()
        {
        }

        /// <summary>
        /// Nominal attribute type with the specified nominal values.
        /// </summary>
        /// <param name="values">Nominal values of the attribute to create.</param>
        /// <returns>An <see cref="ArffNominalAttribute"/> instance representing the attribute type.</returns>
        /// <exception cref="ArgumentNullException"/>
        public static ArffNominalAttribute Nominal(params string[] values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            return new ArffNominalAttribute(values);
        }

        /// <summary>
        /// Nominal attribute type with the specified nominal values.
        /// </summary>
        /// <param name="values">Nominal values of the attribute to create.</param>
        /// <returns>An <see cref="ArffNominalAttribute"/> instance representing the attribute type.</returns>
        /// <exception cref="ArgumentNullException"/>
        public static ArffNominalAttribute Nominal(IList<string> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            return new ArffNominalAttribute(values);
        }

        /// <summary>
        /// Date attribute type.
        /// </summary>
        /// <returns></returns>
        public static ArffDateAttribute Date()
        {
            return date;
        }

        /// <summary>
        /// Date attribute type using the specified date format.
        /// </summary>
        /// <param name="dateFormat">Date format pattern as required by Java class <c>java.text.SimpleDateFormat</c>.</param>
        /// <returns>An <see cref="ArffDateAttribute"/> instance representing the attribute type.</returns>
        /// <exception cref="ArgumentNullException"/>
        public static ArffDateAttribute Date(string dateFormat)
        {
            if (dateFormat == null)
                throw new ArgumentNullException(nameof(dateFormat));

            return new ArffDateAttribute(dateFormat);
        }

        /// <summary>
        /// Relational attribute type combining the specified child attributes.
        /// </summary>
        /// <param name="childAttributes">The child attributes of the relational attribute type.</param>
        /// <returns>An <see cref="ArffRelationalAttribute"/> instance representing the attribute type.</returns>
        /// <exception cref="ArgumentNullException"/>
        public static ArffRelationalAttribute Relational(params ArffAttribute[] childAttributes)
        {
            if (childAttributes == null)
                throw new ArgumentNullException(nameof(childAttributes));

            return new ArffRelationalAttribute(childAttributes);
        }

        /// <summary>
        /// Relational attribute type combining the specified child attributes.
        /// </summary>
        /// <param name="childAttributes">The child attributes of the relational attribute type.</param>
        /// <returns>An <see cref="ArffRelationalAttribute"/> instance representing the attribute type.</returns>
        /// <exception cref="ArgumentNullException"/>
        public static ArffRelationalAttribute Relational(IList<ArffAttribute> childAttributes)
        {
            if (childAttributes == null)
                throw new ArgumentNullException(nameof(childAttributes));

            return new ArffRelationalAttribute(childAttributes);
        }
    }

    /// <summary>
    /// Represents the numeric attribute type.
    /// </summary>
    public sealed class ArffNumericAttribute : ArffAttributeType
    {
        internal ArffNumericAttribute()
        {
        }

        /// <summary>
        /// Determines whether this object is equal to another object (an <see cref="ArffNumericAttribute"/> with the same name).
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return obj is ArffNumericAttribute;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return GetType().GetHashCode();
        }

        /// <summary>
        /// Returns a string representation of this instance.
        /// </summary>
        /// <returns>The string representation of the current object.</returns>
        public override string ToString()
        {
            return "numeric";
        }
    }

    /// <summary>
    /// Represents the string attribute type.
    /// </summary>
    public sealed class ArffStringAttribute : ArffAttributeType
    {
        internal ArffStringAttribute()
        {
        }

        /// <summary>
        /// Determines whether this object is equal to another object (an <see cref="ArffStringAttribute"/> with the same name).
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return obj is ArffStringAttribute;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return GetType().GetHashCode();
        }

        /// <summary>
        /// Returns a string representation of this instance.
        /// </summary>
        /// <returns>The string representation of the current object.</returns>
        public override string ToString()
        {
            return "string";
        }
    }

    /// <summary>
    /// Represents the nominal attribute type.
    /// </summary>
    public sealed class ArffNominalAttribute : ArffAttributeType
    {
        /// <summary>
        /// Gets the nominal values of this nominal attribute type.
        /// </summary>
        public ReadOnlyCollection<string> Values { get; }

        internal ArffNominalAttribute(IList<string> values)
        {
            Values = new ReadOnlyCollection<string>(values);
        }

        /// <summary>
        /// Determines whether this object is equal to another object (an <see cref="ArffNominalAttribute"/> with the same name and nominal values).
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            ArffNominalAttribute other = obj as ArffNominalAttribute;

            if (other == null)
                return false;

            return other.Values.SequenceEqual(Values);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code for the current object.</returns>
        public override int GetHashCode()
        {
            int hashCode = 19;

            foreach (string value in Values)
                hashCode = unchecked(hashCode * 31 + value.GetHashCode());

            return hashCode;
        }

        /// <summary>
        /// Returns a string representation of this instance.
        /// </summary>
        /// <returns>The string representation of the current object.</returns>
        public override string ToString()
        {
            return "{" + string.Join(",", Values.Select(ArffWriter.QuoteAndEscape)) + "}";
        }
    }

    /// <summary>
    /// Represents the date attribute type.
    /// </summary>
    public sealed class ArffDateAttribute : ArffAttributeType
    {
        /// <summary>
        /// Gets the date format that this date attribute type is using.
        /// </summary>
        public string DateFormat { get; }

        internal const string DefaultDateFormat = "yyyy-MM-dd'T'HH:mm:ss";

        internal ArffDateAttribute()
        {
            DateFormat = DefaultDateFormat;
        }

        internal ArffDateAttribute(string dateFormat)
        {
            DateFormat = dateFormat;
        }

        /// <summary>
        /// Determines whether this object is equal to another object (an <see cref="ArffDateAttribute"/> with the same name and date format).
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            ArffDateAttribute other = obj as ArffDateAttribute;

            if (other == null)
                return false;

            return other.DateFormat == DateFormat;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return DateFormat.GetHashCode();
        }

        /// <summary>
        /// Returns a string representation of this instance.
        /// </summary>
        /// <returns>The string representation of the current object.</returns>
        public override string ToString()
        {
            if (DateFormat == DefaultDateFormat)
                return "date";
            else
                return "date " + ArffWriter.QuoteAndEscape(DateFormat);
        }
    }

    /// <summary>
    /// Represents the relational attribute type.
    /// </summary>
    public sealed class ArffRelationalAttribute : ArffAttributeType
    {
        /// <summary>
        /// Gets the child attributes of this relational attribute type.
        /// </summary>
        public ReadOnlyCollection<ArffAttribute> ChildAttributes { get; }

        internal ArffRelationalAttribute(IList<ArffAttribute> childAttributes)
        {
            ChildAttributes = new ReadOnlyCollection<ArffAttribute>(childAttributes);
        }

        /// <summary>
        /// Determines whether this object is equal to another object (an <see cref="ArffRelationalAttribute"/> with the same name and child attributes).
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            ArffRelationalAttribute other = obj as ArffRelationalAttribute;

            if (other == null)
                return false;

            return other.ChildAttributes.SequenceEqual(ChildAttributes);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code for the current object.</returns>
        public override int GetHashCode()
        {
            int hashCode = 19;

            foreach (ArffAttribute attribute in ChildAttributes)
                hashCode = unchecked(hashCode * 31 + attribute.GetHashCode());

            return hashCode;
        }

        /// <summary>
        /// Returns a string representation of this instance.
        /// </summary>
        /// <returns>The string representation of the current object.</returns>
        public override string ToString()
        {
            return "relational";
        }
    }
}
