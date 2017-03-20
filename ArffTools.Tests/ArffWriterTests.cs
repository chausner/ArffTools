using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace ArffTools.Tests
{
    [TestClass]
    public class ArffWriterTests
    {
        private void AssertWriter(string expectedOutput, Action<ArffWriter> action)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            using (ArffWriter arffWriter = new ArffWriter(memoryStream))
            {
                action(arffWriter);

                arffWriter.Flush();

                memoryStream.Seek(0, SeekOrigin.Begin);

                using (StreamReader streamReader = new StreamReader(memoryStream, new UTF8Encoding(false), true, 4096, true))
                    Assert.AreEqual(expectedOutput, streamReader.ReadToEnd());
            }
        }

        [TestMethod]
        public void RelationName()
        {
            string arff = "@relation relationName\r\n\r\n";

            AssertWriter(arff, arffWriter =>
            {
                arffWriter.WriteRelationName("relationName");
            });
        }

        [TestMethod]
        public void QuotedRelationName()
        {
            string arff = "@relation ' relation\\tName '\r\n\r\n";

            AssertWriter(arff, arffWriter =>
            {
                arffWriter.WriteRelationName(" relation\tName ");
            });
        }

        [TestMethod]
        public void SimpleAttributes()
        {
            string arff = @"@relation relationName

@attribute a1 numeric
@attribute a2 string
@attribute a3 {v1,v2,v3}
@attribute a4 date
@attribute a5 date hh:mm:ss
@attribute a6 relational
  @attribute a7 numeric
  @attribute a8 string
@end a6
";

            AssertWriter(arff, arffWriter =>
            {
                arffWriter.WriteRelationName("relationName");
                arffWriter.WriteAttribute(new ArffAttribute("a1", ArffAttributeType.Numeric));
                arffWriter.WriteAttribute(new ArffAttribute("a2", ArffAttributeType.String));
                arffWriter.WriteAttribute(new ArffAttribute("a3", ArffAttributeType.Nominal("v1", "v2", "v3")));
                arffWriter.WriteAttribute(new ArffAttribute("a4", ArffAttributeType.Date()));
                arffWriter.WriteAttribute(new ArffAttribute("a5", ArffAttributeType.Date("hh:mm:ss")));
                arffWriter.WriteAttribute(new ArffAttribute("a6", ArffAttributeType.Relational(
                    new ArffAttribute("a7", ArffAttributeType.Numeric),
                    new ArffAttribute("a8", ArffAttributeType.String))));
            });
        }

        [TestMethod]
        public void NestedRelationalAttributes()
        {
            string arff = @"@relation relationName

@attribute a1 relational
  @attribute a2 numeric
  @attribute a3 relational
    @attribute a4 string
    @attribute a5 numeric
  @end a3
  @attribute a4 string
@end a1
";

            AssertWriter(arff, arffWriter =>
            {
                arffWriter.WriteRelationName("relationName");
                arffWriter.WriteAttribute(new ArffAttribute("a1", ArffAttributeType.Relational(
                    new ArffAttribute("a2", ArffAttributeType.Numeric),
                    new ArffAttribute("a3", ArffAttributeType.Relational(
                        new ArffAttribute("a4", ArffAttributeType.String),
                        new ArffAttribute("a5", ArffAttributeType.Numeric))),
                    new ArffAttribute("a4", ArffAttributeType.String))));
            });
        }

        [TestMethod]
        public void InstancesWrittenCorrectly()
        {
            string arff = @"@relation relationName

@attribute a1 numeric
@attribute a2 string
@attribute a3 {v1,v2,v3}
@attribute a4 date
@attribute a5 date hh:mm:ss
@attribute a6 relational
  @attribute a7 numeric
  @attribute a8 numeric
@end a6

@data
1.5,'abc,def',v3,2017-01-29T18:39:18,06:39:18,'2,-3.5\r\n3,4.5'
";

            DateTime date = DateTime.ParseExact("2017-01-29T18:39:18", "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);

            object[] instance = { 1.5, "abc,def", 2, date, date, new[] { new object[] { 2.0, -3.5 }, new object[] { 3.0, 4.5 } } };

            AssertWriter(arff, arffWriter =>
            {
                arffWriter.WriteRelationName("relationName");
                arffWriter.WriteAttribute(new ArffAttribute("a1", ArffAttributeType.Numeric));
                arffWriter.WriteAttribute(new ArffAttribute("a2", ArffAttributeType.String));
                arffWriter.WriteAttribute(new ArffAttribute("a3", ArffAttributeType.Nominal("v1", "v2", "v3")));
                arffWriter.WriteAttribute(new ArffAttribute("a4", ArffAttributeType.Date()));
                arffWriter.WriteAttribute(new ArffAttribute("a5", ArffAttributeType.Date("hh:mm:ss")));
                arffWriter.WriteAttribute(new ArffAttribute("a6", ArffAttributeType.Relational(
                    new ArffAttribute("a7", ArffAttributeType.Numeric),
                    new ArffAttribute("a8", ArffAttributeType.Numeric))));
                arffWriter.WriteInstance(instance);
            });
        }

        [TestMethod]
        public void SparseInstancesWithInstanceWeightsWrittenCorrectly()
        {
            string arff = @"@relation relationName

@attribute a1 numeric
@attribute a2 string
@attribute a3 {v1,v2,v3}
@attribute a4 date
@attribute a5 date hh:mm:ss
@attribute a6 relational
  @attribute a7 numeric
  @attribute a8 numeric
@end a6

@data
{0 1.5,1 'abc,def',2 v3,3 2017-01-29T18:39:18,4 06:39:18,5 '2,-3.5\r\n3,4.5'},{0.44}
{1 '',3 1970-01-01T00:00:00,4 12:00:00,5 '0,0\r\n0,0'},{0.87}
";

            DateTime date1 = DateTime.ParseExact("2017-01-29T18:39:18", "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
            DateTime date2 = new DateTime(1970, 1, 1, 0, 0, 0);

            object[] instance1 = { 1.5, "abc,def", 2, date1, date1, new[] { new object[] { 2.0, -3.5 }, new object[] { 3.0, 4.5 } } };
            object[] instance2 = { 0.0, string.Empty, 0, date2, date2, new[] { new object[] { 0.0, 0.0 }, new object[] { 0.0, 0.0 } } };

            AssertWriter(arff, arffWriter =>
            {
                arffWriter.WriteRelationName("relationName");
                arffWriter.WriteAttribute(new ArffAttribute("a1", ArffAttributeType.Numeric));
                arffWriter.WriteAttribute(new ArffAttribute("a2", ArffAttributeType.String));
                arffWriter.WriteAttribute(new ArffAttribute("a3", ArffAttributeType.Nominal("v1", "v2", "v3")));
                arffWriter.WriteAttribute(new ArffAttribute("a4", ArffAttributeType.Date()));
                arffWriter.WriteAttribute(new ArffAttribute("a5", ArffAttributeType.Date("hh:mm:ss")));
                arffWriter.WriteAttribute(new ArffAttribute("a6", ArffAttributeType.Relational(
                    new ArffAttribute("a7", ArffAttributeType.Numeric),
                    new ArffAttribute("a8", ArffAttributeType.Numeric))));
                arffWriter.WriteInstance(instance1, true, 0.44);
                arffWriter.WriteInstance(instance2, true, 0.87);
            });
        }

        [TestMethod]
        public void NestedRelationalValues()
        {
            string arff = @"@relation relationName

@attribute a1 relational
  @attribute a2 numeric
  @attribute a3 relational
    @attribute a4 {v1,v2,v3}
    @attribute a5 numeric
  @end a3
  @attribute a6 string
@end a1
@attribute a7 numeric

@data
'1,\'v1,2\\r\\nv2,3\',abc\r\n4,\'v3,5\\r\\nv1,6\',def',7
";

            object[] instance = { new object[][] { new object[] { 1.0, new object[][] { new object[] { 0, 2.0 }, new object[] { 1, 3.0 } }, "abc" }, new object[] { 4.0, new object[][] { new object[] { 2, 5.0 }, new object[] { 0, 6.0 } }, "def" } }, 7.0 };

            AssertWriter(arff, arffWriter =>
            {
                arffWriter.WriteRelationName("relationName");
                arffWriter.WriteAttribute(new ArffAttribute("a1", ArffAttributeType.Relational(
                    new ArffAttribute("a2", ArffAttributeType.Numeric),
                    new ArffAttribute("a3", ArffAttributeType.Relational(
                        new ArffAttribute("a4", ArffAttributeType.Nominal("v1", "v2", "v3")),
                        new ArffAttribute("a5", ArffAttributeType.Numeric))),
                    new ArffAttribute("a6", ArffAttributeType.String))));
                arffWriter.WriteAttribute(new ArffAttribute("a7", ArffAttributeType.Numeric));
                arffWriter.WriteInstance(instance);
            });
        }

        [TestMethod]
        public void CommentsWrittenCorrectly()
        {
            string arff = @"% comment before header
@relation relationName

% comment in header
@attribute a1 numeric
% multi-line comment 1
% multi-line comment 2
% multi-line comment 3

@data
1
% comment between instances
2
";

            AssertWriter(arff, arffWriter =>
            {
                arffWriter.WriteComment("comment before header");
                arffWriter.WriteRelationName("relationName");
                arffWriter.WriteComment("comment in header");
                arffWriter.WriteAttribute(new ArffAttribute("a1", ArffAttributeType.Numeric));
                arffWriter.WriteComment("multi-line comment 1\nmulti-line comment 2\r\nmulti-line comment 3");
                arffWriter.WriteInstance(new object[] { 1.0 });
                arffWriter.WriteComment("comment between instances");
                arffWriter.WriteInstance(new object[] { 2.0 });
            });
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void WriteRelationNameTwice()
        {
            AssertWriter(null, arffWriter => {
                arffWriter.WriteRelationName("relationName1");
                arffWriter.WriteRelationName("relationName2");
            });
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void WriteAttributeBeforeRelationName()
        {
            AssertWriter(null, arffWriter => {
                arffWriter.WriteAttribute(new ArffAttribute("a1", ArffAttributeType.Numeric));
            });
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void WriteInstanceBeforeRelationName()
        {
            AssertWriter(null, arffWriter => {
                arffWriter.WriteInstance(new object[] { 1.0 });
            });
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void WriteInstanceBeforeAttribute()
        {
            AssertWriter(null, arffWriter => {
                arffWriter.WriteRelationName("relationName");
                arffWriter.WriteInstance(new object[] { 1.0 });
            });
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void WriteRelationNameAfterInstance()
        {
            AssertWriter(null, arffWriter => {
                arffWriter.WriteRelationName("relationName");
                arffWriter.WriteAttribute(new ArffAttribute("a1", ArffAttributeType.Numeric));
                arffWriter.WriteInstance(new object[] { 1.0 });
                arffWriter.WriteRelationName("relationName");
            });
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void WriteAttributeAfterInstance()
        {
            AssertWriter(null, arffWriter => {
                arffWriter.WriteRelationName("relationName");
                arffWriter.WriteAttribute(new ArffAttribute("a1", ArffAttributeType.Numeric));
                arffWriter.WriteInstance(new object[] { 1.0 });
                arffWriter.WriteAttribute(new ArffAttribute("a2", ArffAttributeType.Numeric));
            });
        }
    }
}
