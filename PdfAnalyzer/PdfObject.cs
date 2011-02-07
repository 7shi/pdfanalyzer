using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace PdfAnalyzer
{
    public class PdfObject
    {
        public long Position { get; private set; }
        public long Length { get; private set; }
        public string Type { get; private set; }
        public int Number { get; private set; }
        public int Index { get; private set; }
        public PdfDictionary Dictionary { get; private set; }
        public long StreamStart { get; private set; }
        public long StreamLength { get; private set; }
        public object Object { get; private set; }
        public int ObjStm { get; private set; }

        public PdfObject(long position, int no, int index)
        {
            Position = position;
            Number = no;
            Index = index;
        }

        public PdfObject(int objstm, int index)
        {
            ObjStm = objstm;
            Index = index;
        }

        public PdfObject(PdfParser parser)
        {
            Read(parser);
        }

        public void Read(PdfParser parser)
        {
            var lexer = parser.Lexer;
            Position = lexer.Position;
            if (!lexer.IsNumber)
                throw lexer.Abort("required: number");
            Number = int.Parse(lexer.Current);
            lexer.ReadToken();
            Index = int.Parse(lexer.Current);
            lexer.ReadToken();
            if (lexer.Current != "obj")
                throw lexer.Abort("required: obj");
            lexer.ReadToken();
            if (lexer.Current == "<<")
            {
                Dictionary = new PdfDictionary(parser);
                if (Dictionary.ContainsKey("/Type"))
                    Type = Dictionary["/Type"] as string;
            }
            if (lexer.Current == "stream")
            {
                if (Dictionary == null | !Dictionary.ContainsKey("/Length"))
                    throw lexer.Abort("not found: /Length");
                var len = Dictionary["/Length"];
                if (len is double)
                    StreamLength = (long)(double)len;
                else if (len is PdfReference)
                    throw new NotImplementedException();
                else
                    throw lexer.Abort("unexpected /Length: {0}", len);
                StreamStart = lexer.SkipStream(StreamLength);
                lexer.ReadToken();
                if (lexer.Current != "endstream")
                    throw lexer.Abort("required: endstream");
                lexer.ReadToken();
            }
            if (lexer.Current != "endobj")
                Object = parser.Read();
            if (lexer.Current != "endobj")
                throw lexer.Abort("required: endobj");
            Length = lexer.Position + 6 - Position;
            lexer.ReadToken();
        }

        public Stream GetStream(Stream stream)
        {
            stream.Position = StreamStart;
            var s = new SubStream(stream, StreamLength);
            if (Dictionary.ContainsKey("/Filter"))
            {
                var filter = Dictionary["/Filter"] as string;
                if (filter == "/FlateDecode")
                    return new PdfDeflateStream(s, this);
            }
            return s;
        }
    }
}
