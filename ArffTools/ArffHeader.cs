using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ArffTools
{
    /// <summary>
    /// Represents the header of an ARFF file consisting of relation name and attribute declarations.
    /// </summary>
    public class ArffHeader
    {
        /// <summary>
        /// Gets the relation name.
        /// </summary>
        public string RelationName { get; }

        /// <summary>
        /// Gets the declared attributes.
        /// </summary>
        public ReadOnlyCollection<ArffAttribute> Attributes { get; }

        internal ArffHeader(string relationName, IList<ArffAttribute> attributes)
        {
            RelationName = relationName;
            Attributes = new ReadOnlyCollection<ArffAttribute>(attributes);
        }

        /// <summary>
        /// Determines whether this object is equal to another object (an <see cref="ArffHeader"/> with the same relation name and attributes).
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            ArffHeader other = obj as ArffHeader;

            if (other == null)
                return false;

            return other.RelationName == RelationName && other.Attributes.SequenceEqual(Attributes);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code for the current object.</returns>
        public override int GetHashCode()
        {
            int hashCode = RelationName.GetHashCode();

            foreach (ArffAttribute attribute in Attributes)
                hashCode = unchecked(hashCode * 31 + attribute.GetHashCode());

            return hashCode;
        }
    }
}
