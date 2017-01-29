using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;

namespace ArffTools.Tests
{
    [TestClass]
    public class ArffReaderTests
    {
        private ArffReader CreateArffReader(string arff)
        {
            MemoryStream memoryStream = new MemoryStream();

            using (StreamWriter streamWriter = new StreamWriter(memoryStream, Encoding.Unicode, 4096, true))
                streamWriter.Write(arff);

            memoryStream.Seek(0, SeekOrigin.Begin);

            return new ArffReader(memoryStream);
        }

        private class InstanceComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                object[] ax = x as object[];
                object[] ay = y as object[];

                if (ax != null && ay != null)
                {
                    if (ax.Length < ay.Length)
                        return -1;
                    else if (ax.Length > ay.Length)
                        return 1;
                    else
                    {
                        for (int i = 0; i < ax.Length; i++)
                        {
                            int c = Compare(ax[i], ay[i]);
                            if (c != 0)
                                return c;
                        }

                        return 0;
                    }
                }
                else
                    return Comparer.DefaultInvariant.Compare(x, y);
            }
        }

        private void AssertReader(string arff, string expectedRelationName = null, ICollection expectedAttributes = null, object[][] expectedInstances = null)
        {
            ArffReader arffReader = CreateArffReader(arff);

            ArffHeader arffHeader = arffReader.ReadHeader();

            if (expectedRelationName != null)
                Assert.AreEqual(expectedRelationName, arffHeader.RelationName, "Unexpected relation name.");

            if (expectedAttributes != null)
                CollectionAssert.AreEqual(expectedAttributes, arffHeader.Attributes, "Unexpected attributes.");

            object[][] instances = arffReader.ReadAllInstances();

            if (expectedInstances != null)
                CollectionAssert.AreEqual(expectedInstances, instances, new InstanceComparer(), "Unexpected instances.");
        }

        [TestMethod]
        public void RelationNameReadCorrectly()
        {
            string arff = @"@relation relationName
                            @attribute a1 numeric
                            @data";

            AssertReader(arff, expectedRelationName: "relationName");
        }

        [TestMethod]
        public void QuotedRelationNameReadCorrectly()
        {
            string arff = @"@relation 'relation\\\tName'
                            @attribute a1 numeric
                            @data";

            AssertReader(arff, expectedRelationName: "relation\\\tName");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void MissingRelationName()
        {
            string arff = @"@attribute a1 numeric
                            @data";

            ArffReader arffReader = CreateArffReader(arff);

            ArffHeader arffHeader = arffReader.ReadHeader();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void MissingAttributes()
        {
            string arff = @"@relation relationName
                            @data";

            ArffReader arffReader = CreateArffReader(arff);

            ArffHeader arffHeader = arffReader.ReadHeader();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void MissingData()
        {
            string arff = @"@relation relationName
                            @attribute a1 numeric
                            ";

            ArffReader arffReader = CreateArffReader(arff);

            ArffHeader arffHeader = arffReader.ReadHeader();
        }

        [TestMethod]
        public void KeywordCaseInsensitivity()
        {
            string arff = @"@RElatION relationName
                            @ATTRiBUTE a1 nuMERic
                            @DaTa";

            ArffReader arffReader = CreateArffReader(arff);

            ArffHeader arffHeader = arffReader.ReadHeader();
        }

        [TestMethod]
        public void CRLFLineEndings()
        {
            string arff = "@relation relationName\r\n@attribute a1 numeric\r\n@data";

            ArffReader arffReader = CreateArffReader(arff);

            ArffHeader arffHeader = arffReader.ReadHeader();
        }

        [TestMethod]
        public void CRLineEndings()
        {
            string arff = "@relation relationName\r@attribute a1 numeric\r@data";

            ArffReader arffReader = CreateArffReader(arff);

            ArffHeader arffHeader = arffReader.ReadHeader();
        }

        [TestMethod]
        public void Whitespace()
        {
            string arff = "\r\n\n\t @relation\t'\\trelation Name'\n\r\n \r\n@attribute      a1\t \tnumeric\t\r@attribute a2{ 'v1'  , v2,v3  }\n @data \r\n \r\n";

            AssertReader(arff,
                expectedRelationName: "\trelation Name",
                expectedAttributes: new[] {
                    new ArffAttribute("a1", ArffAttributeType.Numeric),
                    new ArffAttribute("a2", ArffAttributeType.Nominal("v1", "v2", "v3"))
                });
        }

        [TestMethod]
        public void EmptyNominalAttribute()
        {
            string arff = @"@relation relationName
                            @attribute a1 {}
                            @data";

            AssertReader(arff,
                expectedRelationName: "relationName",
                expectedAttributes: new[] {
                    new ArffAttribute("a1", ArffAttributeType.Nominal())
                });
        }

        [TestMethod]
        public void Comments()
        {
            string arff = @"% comment before header
                            @relation relationName
                            % comment in header % comment in comment
                            @attribute a1 numeric%comment at end-of-line
                            @attribute a2 string
                            @data % comment at end-of-line
                            % comment between instances
                            1,abc% comment at end-of-line
                            % comment between instances
                            2,'def\%'
                            % comment at end-of-file";

            AssertReader(arff,
                expectedRelationName: "relationName",
                expectedAttributes: new[] {
                    new ArffAttribute("a1", ArffAttributeType.Numeric),
                    new ArffAttribute("a2", ArffAttributeType.String)
                },
                expectedInstances: new[] {
                    new object[] { 1.0, "abc" },
                    new object[] { 2.0, "def%" }
                });
        }

        [TestMethod]
        public void StringQuotingAndEscaping()
        {
            string arff = @"@relation relationName
                            @attribute 'a1' numeric
                            @attribute ""a2"" numeric
                            @attribute \\a3\\ numeric
                            @attribute '\\' numeric
                            @attribute ""\t"" numeric
                            @data";

            AssertReader(arff,
                expectedRelationName: "relationName",
                expectedAttributes: new[] {
                    new ArffAttribute("a1", ArffAttributeType.Numeric),
                    new ArffAttribute("a2", ArffAttributeType.Numeric),
                    new ArffAttribute("\\\\a3\\\\", ArffAttributeType.Numeric),
                    new ArffAttribute("\\", ArffAttributeType.Numeric),
                    new ArffAttribute("\t", ArffAttributeType.Numeric)
                });
        }

        [TestMethod]
        public void AttributesReadCorrectly()
        {
            string arff = @"@relation relationName
                            @attribute a1 numeric
                            @attribute a2 integer
                            @attribute a3 real
                            @attribute a4 string
                            @attribute a5 date
                            @attribute a6 date yyyy-MM-dd
                            @attribute a7 {v1,'v2',""v3""}
                            @attribute a8 relational
                              @attribute a9 numeric
                              @attribute a10 relational
                                @attribute a11 string
                              @end a10
                            @end a8
                            @data";

            AssertReader(arff,
                expectedRelationName: "relationName",
                expectedAttributes: new[] {
                    new ArffAttribute("a1", ArffAttributeType.Numeric),
                    new ArffAttribute("a2", ArffAttributeType.Numeric),
                    new ArffAttribute("a3", ArffAttributeType.Numeric),
                    new ArffAttribute("a4", ArffAttributeType.String),
                    new ArffAttribute("a5", ArffAttributeType.Date()),
                    new ArffAttribute("a6", ArffAttributeType.Date("yyyy-MM-dd")),
                    new ArffAttribute("a7", ArffAttributeType.Nominal("v1", "v2", "v3")),
                    new ArffAttribute("a8", ArffAttributeType.Relational(
                        new ArffAttribute("a9", ArffAttributeType.Numeric),
                        new ArffAttribute("a10", ArffAttributeType.Relational(
                            new ArffAttribute("a11", ArffAttributeType.String)))))
                });
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ReadHeaderTwice()
        {
            string arff = @"@relation relationName
                            @attribute a1 numeric
                            @data";

            ArffReader arffReader = CreateArffReader(arff);

            arffReader.ReadHeader();
            arffReader.ReadHeader();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ReadInstanceBeforeHeader()
        {
            string arff = @"@relation relationName
                            @attribute a1 numeric
                            @data";

            ArffReader arffReader = CreateArffReader(arff);

            arffReader.ReadInstance();
        }

        [TestMethod]
        public void NoInstances()
        {
            string arff = @"@relation relationName
                            @attribute a1 numeric
                            @data";

            ArffReader arffReader = CreateArffReader(arff);

            arffReader.ReadHeader();

            Assert.IsNull(arffReader.ReadInstance());
            Assert.IsNull(arffReader.ReadInstance());
        }

        [TestMethod]
        public void InstancesReadCorrectly()
        {
            string arff = @"@relation relationName
                            @attribute a1 numeric
                            @attribute a2 integer
                            @attribute a3 real
                            @attribute a4 string
                            @attribute a5 date
                            @attribute a6 date yyyy-MM-dd
                            @attribute a7 {v1,'v2',""v3""}
                            @data
                            -6.54,42,0.0,string,2016-06-11T19:30:05,2016-06-11,v3";

            AssertReader(arff,
                expectedRelationName: "relationName",
                expectedAttributes: new[] {
                    new ArffAttribute("a1", ArffAttributeType.Numeric),
                    new ArffAttribute("a2", ArffAttributeType.Numeric),
                    new ArffAttribute("a3", ArffAttributeType.Numeric),
                    new ArffAttribute("a4", ArffAttributeType.String),
                    new ArffAttribute("a5", ArffAttributeType.Date()),
                    new ArffAttribute("a6", ArffAttributeType.Date("yyyy-MM-dd")),
                    new ArffAttribute("a7", ArffAttributeType.Nominal("v1", "v2", "v3")) },
                expectedInstances: new object[][] {
                    new object[] { -6.54, 42.0, 0.0, "string", DateTime.ParseExact("2016-06-11T19:30:05", ArffDateAttribute.DefaultDateFormat, CultureInfo.InvariantCulture), DateTime.ParseExact("2016-06-11", "yyyy-MM-dd", CultureInfo.InvariantCulture), 2 }
                    });
        }

        [TestMethod]
        public void RelationalInstancesReadCorrectly()
        {
            string arff = @"@relation relationName
                            @attribute a1 relational
                              @attribute a2 {v1,v2,v3}
                              @attribute a3 relational
                                @attribute a4 string
                                @attribute a5 {v4,v5,v6}
                              @end a3
                            @end a1
                            @data
                            'v1,\'abc,v6,{1.5}\\r\\ndef,v5\'\r\nv2,\'ghi,v4\\r\\njkl,v6\',{2.5}'";

            AssertReader(arff,
                expectedRelationName: "relationName",
                expectedAttributes: new[] {
                    new ArffAttribute("a1", ArffAttributeType.Relational(
                        new ArffAttribute("a2", ArffAttributeType.Nominal("v1", "v2", "v3")),
                        new ArffAttribute("a3", ArffAttributeType.Relational(
                            new ArffAttribute("a4", ArffAttributeType.String),
                            new ArffAttribute("a5", ArffAttributeType.Nominal("v4", "v5", "v6")))))) },
                expectedInstances: new object[][] {
                    new object[] { new object[][] { new object[] { 0, new object[][] { new object[] { "abc", 2 }, new object[] { "def", 1 } }  }, new object[] { 1, new object[][] { new object[] { "ghi", 0 }, new object[] { "jkl", 2 } } } } }
                });
        }

        [TestMethod]
        public void MissingValues()
        {
            string arff = @"@relation relationName
                            @attribute a1 numeric
                            @attribute a2 integer
                            @attribute a3 real
                            @attribute a4 string
                            @attribute a5 date
                            @attribute a6 date yyyy-MM-dd
                            @attribute a7 {v1,'v2',""v3""}
                            @attribute a8 string
                            @data
                            ?,?,?,?,?,?,?,'?'";

            AssertReader(arff,
                expectedRelationName: "relationName",
                expectedAttributes: new[] {
                    new ArffAttribute("a1", ArffAttributeType.Numeric),
                    new ArffAttribute("a2", ArffAttributeType.Numeric),
                    new ArffAttribute("a3", ArffAttributeType.Numeric),
                    new ArffAttribute("a4", ArffAttributeType.String),
                    new ArffAttribute("a5", ArffAttributeType.Date()),
                    new ArffAttribute("a6", ArffAttributeType.Date("yyyy-MM-dd")),
                    new ArffAttribute("a7", ArffAttributeType.Nominal("v1", "v2", "v3")),
                    new ArffAttribute("a8", ArffAttributeType.String) },
                expectedInstances: new object[][] {
                    new object[] { null, null, null, null, null, null, null, "?" }
                });
        }

        [TestMethod]
        public void QuotingHandledCorrectly()
        {
            string arff = @"@relation relationName
                            @attribute a1 {',',""{"",'}','?'}
                            @attribute a2 {',',""{"",'}','?'}
                            @attribute a3 string
                            @attribute a4 string
                            @data
                            ',',""?"","","",' '
                            ?,?,'?',""?""";

            AssertReader(arff,
                expectedRelationName: "relationName",
                expectedAttributes: new[] {
                    new ArffAttribute("a1", ArffAttributeType.Nominal(",", "{", "}", "?")),
                    new ArffAttribute("a2", ArffAttributeType.Nominal(",", "{", "}", "?")),
                    new ArffAttribute("a3", ArffAttributeType.String),
                    new ArffAttribute("a4", ArffAttributeType.String) },
                expectedInstances: new object[][] {
                    new object[] { 0, 3, ",", " " },
                    new object[] { null, null, "?", "?" }
                });
        }

        [TestMethod]
        public void InstanceWeightsReadCorrectly()
        {
            string arff = @"@relation relationName
                            @attribute a1 numeric
                            @attribute a2 integer
                            @attribute a3 real
                            @data
                            -6.54,42,0.0
                            -6.54,42,0.0,{5}
                            -6.54,42,0.0,{0.476}
                            {0 -6.54,1 42}
                            {0 -6.54,1 42},{0.476}";

            ArffReader arffReader = CreateArffReader(arff);

            arffReader.ReadHeader();

            double? instanceWeight;

            arffReader.ReadInstance(out instanceWeight);

            Assert.IsNull(instanceWeight);

            arffReader.ReadInstance(out instanceWeight);

            Assert.AreEqual(5.0, instanceWeight);

            arffReader.ReadInstance(out instanceWeight);

            Assert.AreEqual(0.476, instanceWeight);

            arffReader.ReadInstance(out instanceWeight);

            Assert.IsNull(instanceWeight);

            arffReader.ReadInstance(out instanceWeight);

            Assert.AreEqual(0.476, instanceWeight);
        }
    }
}
