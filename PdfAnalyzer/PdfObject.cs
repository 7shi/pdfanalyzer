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

        public PdfObject(int no, int objstm, int index, long position = 0)
        {
            Position = position;
            Number = no;
            ObjStm = objstm;
            Index = index;
        }

        public PdfObject(PdfDocument doc, PdfParser parser)
        {
            Read(doc, parser);
        }

        public bool HasRead { get; private set; }

        public void Read(PdfDocument doc, PdfParser parser)
        {
            if (ObjStm != 0)
            {
                doc[ObjStm].Read(doc, parser);
                return;
            }
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
                readDictionary(parser);
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
            if (Type == "/ObjStm")
                readObjStm(doc, parser.Lexer.Stream);
            HasRead = true;
        }

        private void readDictionary(PdfParser parser)
        {
            Dictionary = new PdfDictionary(parser);
            if (Dictionary.ContainsKey("/Type"))
                Type = Dictionary["/Type"] as string;
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

        private void readObjStm(PdfDocument doc, Stream stream)
        {
            using (var s = GetStream(stream))
            {
                if (!Dictionary.ContainsKey("N"))
                    throw new Exception(string.Format(
                        "{0:x} [ObjStm] required: N", Position));
                var n = (int)(double)Dictionary["N"];
                var objno = new int[n];
                var parser = new PdfParser(s);
                var lexer = parser.Lexer;
                for (int i = 0; i < n; i++)
                {
                    lexer.ReadToken();
                    objno[i] = int.Parse(lexer.Current);
                    lexer.ReadToken();
                }
                for (int i = 0; i < n; i++)
                    doc[objno[i]].readFromStream(parser);
            }
        }

        private void readFromStream(PdfParser parser)
        {
            readDictionary(parser);
            HasRead = true;
        }
    }
}
