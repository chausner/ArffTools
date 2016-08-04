using System.Collections.Generic;
using System.Collections.ObjectModel;

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
    }
}
