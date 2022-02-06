using System.IO;

namespace ArffTools
{
    class LineColumnTextReader : TextReader
    {
        TextReader baseTextReader;
        int line = 1;
        int column = 1;

        public int Line { get { return line; } }
        public int Column { get { return column; } }

        public LineColumnTextReader(TextReader baseTextReader)
        {
            this.baseTextReader = baseTextReader;
        }

        public override int Read()
        {
            int c = baseTextReader.Read();

            if (c == '\n')
            {
                line++;
                column = 1;
            }
            else if (c != -1)
                column++;

            return c;
        }

        public override int Peek()
        {
            return baseTextReader.Peek();
        }

        public override void Close()
        {
            baseTextReader.Close();
            base.Close();
        }

        protected override void Dispose(bool disposing)
        {
            baseTextReader.Dispose();
            base.Dispose(disposing);
        }
    }
}
