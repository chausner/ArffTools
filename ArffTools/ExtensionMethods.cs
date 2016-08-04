using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
