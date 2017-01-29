using System.IO;

namespace ArffTools
{
    internal static class ExtensionMethods
    {
        public static bool EndOfStream(this TextReader textReader)
        {
            return textReader.Peek() == -1;
        }
    }
}
