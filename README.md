# ArffTools
.NET library for reading and writing Weka attribute-relation file format (ARFF) files

[NuGet Package](https://www.nuget.org/packages/ArffTools/)

Features
--------
* Read and write ARFF files
* Supports relational attributes
* Supports sparse instances
* Supports instance weights
* Proper quoting and escaping of special characters

Usage
-----
Reading ARFF files:
```csharp
using (ArffReader arffReader = new ArffReader("glass.arff"))
{
    ArffHeader header = arffReader.ReadHeader();    
    object[] instance;    
    while ((instance = arffReader.ReadInstance()) != null)
    {
        // process instance
    }    
}
```
Reading all instances at a time:
```csharp
object[][] instances = arffReader.ReadAllInstances();
```
Writing ARFF files:
```csharp
using (ArffWriter arffWriter = new ArffWriter("iris.arff"))
{
    arffWriter.WriteRelationName("iris");
    arffWriter.WriteAttribute(new ArffAttribute("sepallength", ArffAttributeType.Numeric));
    arffWriter.WriteAttribute(new ArffAttribute("sepalwidth", ArffAttributeType.Numeric));
    arffWriter.WriteAttribute(new ArffAttribute("petallength", ArffAttributeType.Numeric));
    arffWriter.WriteAttribute(new ArffAttribute("petalwidth", ArffAttributeType.Numeric));
    arffWriter.WriteAttribute(new ArffAttribute("class", ArffAttributeType.Nominal("Iris-setosa", "Iris-versicolor", "Iris-virginica")));
    arffWriter.WriteInstance(new object[] { 5.1, 3.5, 1.4, 0.2, 0 });
}
```

License
-------
MIT, see LICENSE.md
