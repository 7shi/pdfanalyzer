using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace PdfLib
{
    public class PdfObject
    {
        public double Value;
        public string Text;
        public PdfObject[] Objects;

        private Dictionary<string, PdfObject> dict;
        private PdfDocument doc;

        public long Position;
        public int Number;
        public int Index;
        public int ObjStm;
        public string Details = "";

        public string Type { get; private set; }
        public long Length { get; private set; }
        public long StreamStart { get; private set; }
        public long StreamLength { get; private set; }

        public PdfObject() { }

        public PdfObject(PdfDocument doc)
        {
            this.doc = doc;
        }

        public bool HasRead { get; private set; }

        public void Read(PdfParser parser)
        {
            if (HasRead) return;
            HasRead = true;
            if (ObjStm != 0)
            {
                doc.GetObject(ObjStm).Read(parser);
                return;
            }
            var lexer = parser.Lexer;
            if (Position == 0)
                Position = lexer.Position;
            else
            {
                lexer.Stream.Position = Position;
                lexer.Clear();
                lexer.ReadToken();
            }
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
                ReadDictionary(parser);
            if (lexer.Current == "stream")
            {
                if (dict == null | !dict.ContainsKey("/Length"))
                    throw lexer.Abort("not found: /Length");
                var pos = lexer.Stream.Position;
                StreamLength = (long)this["/Length"].Value;
                lexer.Stream.Position = pos;
                StreamStart = lexer.SkipStream(StreamLength);
                lexer.ReadToken();
                if (lexer.Current != "endstream")
                    throw lexer.Abort("required: endstream");
                lexer.ReadToken();
            }
            if (lexer.Current != "endobj")
                parser.Read(this);
            if (lexer.Current != "endobj")
                throw lexer.Abort("required: endobj");
            Length = lexer.Position + 6 - Position;
            lexer.ReadToken();
            if (Type == "/ObjStm")
                readObjStm(parser.Lexer);
        }

        public Stream GetStream(Stream stream)
        {
            var filter = this["/Filter"];
            stream.Position = StreamStart;
            var s = new SubStream(stream, StreamLength);
            if (filter != null && filter.Text == "/FlateDecode")
                return new PdfDeflateStream(s, this);
            return s;
        }

        private void readObjStm(PdfLexer lexer)
        {
            using (var s = GetStream(lexer.Stream))
            {
                if (!dict.ContainsKey("/N"))
                    throw new Exception(string.Format(
                        "{0:x} [ObjStm] required: /N", Position));
                var n = (int)this["/N"].Value;
                if (!dict.ContainsKey("/First"))
                    throw new Exception(string.Format(
                        "{0:x} [ObjStm] required: /First", Position));
                var first = (int)this["/First"].Value;
                var objno = new int[n];
                var objpos = new long[n];
                var parser = new PdfParser(doc, s);
                var lexer2 = parser.Lexer;
                for (int i = 0; i < n; i++)
                {
                    lexer2.ReadToken();
                    objno[i] = int.Parse(lexer2.Current);
                    lexer2.ReadToken();
                    objpos[i] = int.Parse(lexer2.Current);
                }
                lexer2.ReadToken();
                for (int i = 0; i < n; i++)
                {
                    var obj = doc[objno[i]];
                    obj.HasRead = true;
                    obj.ReadDictionary(parser);
                    obj.Position = first + objpos[i];
                    if (i < n - 1)
                        obj.Length = objpos[i + 1] - objpos[i];
                    else
                        obj.Length = s.Position - obj.Position;
                }
            }
        }

        public byte[] GetStreamBytes(Stream stream)
        {
            var ms = new MemoryStream();
            using (var s = GetStream(stream))
            {
                var buf = new byte[4096];
                int len;
                while ((len = s.Read(buf, 0, buf.Length)) > 0)
                    ms.Write(buf, 0, len);
            }
            return ms.ToArray();
        }

        public void ReadDictionary(PdfParser parser)
        {
            dict = new Dictionary<string, PdfObject>();
            var lexer = parser.Lexer;
            lexer.ReadToken();
            while (lexer.Current != null)
            {
                var key = lexer.Current;
                lexer.ReadToken();
                if (key == ">>") break;
                dict.Add(key, parser.Read(null));
            }
            var type = this["/Type"];
            if (type != null) Type = type.Text;
        }

        public PdfObject this[string key]
        {
            get
            {
                if (!ContainsKey(key)) return null;
                var ret = dict[key];
                if (ret.Position > 0) ret = doc[ret.Number];
                return ret;
            }
        }

        public Dictionary<string, PdfObject>.KeyCollection Keys
        {
            get { return dict.Keys; }
        }

        public bool ContainsKey(string key)
        {
            return dict != null ? dict.ContainsKey(key) : false;
        }
    }
}
