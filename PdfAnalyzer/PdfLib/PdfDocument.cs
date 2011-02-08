using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PdfLib
{
    public class PdfDocument : IDisposable
    {
        private PdfParser parser;
        private List<PdfObject> pages = new List<PdfObject>();

        public PdfDocument(string pdf)
        {
            var fs = new FileStream(pdf, FileMode.Open);
            var parser = new PdfParser(this, fs);
            if (parser.Lexer.ReadAscii(4) != "%PDF")
                throw new Exception("signature is not %PDF");
            parser.ReadXref();
            this.parser = parser;

            var tr = GetTrailerObjects();
            foreach (var key in tr.Keys)
                this[key].Details = tr[key];

            var root = GetTrailer("/Root") as PdfObject;
            if (root == null)
                throw new Exception("not found: /Root");
            root.Read(parser);

            var pages = root["/Pages"] as PdfObject;
            if (pages == null)
                throw new Exception("not found: /Pages");
            pages.Read(parser);
            addPages(pages);
        }

        private void addPages(PdfObject p1)
        {
            p1.Details = "/Pages";
            var kids = p1["/Kids"].Objects;
            if (kids == null)
                throw new Exception("obj " + p1.Number + ": not found: /Kids");
            for (int i = 0; i < kids.Length; i++)
            {
                var p2 = kids[i] as PdfObject;
                p2.Read(parser);
                if (p2.Type == "/Pages")
                    addPages(p2);
                else if (p2.Type == "/Page")
                {
                    pages.Add(p2);
                    p2.Details = "/Page " + pages.Count;
                }
            }
        }

        public int PageCount { get { return pages.Count; } }

        public PdfObject GetPage(int p)
        {
            return pages[p - 1];
        }

        public void Dispose()
        {
            parser.Lexer.Stream.Dispose();
            parser = null;
        }

        private Dictionary<int, PdfObject> objs = new Dictionary<int, PdfObject>();

        public PdfObject this[int no]
        {
            get
            {
                var ret = GetObject(no);
                if (ret != null && !ret.HasRead && parser != null)
                    ret.Read(parser);
                return ret;
            }
        }

        public PdfObject GetObject(int no)
        {
            PdfObject ret = null;
            if (objs.ContainsKey(no))
                ret = objs[no];
            else if (parser == null)
            {
                ret = new PdfObject(this) { Number = no };
                objs.Add(no, ret);
            }
            return ret;
        }

        public void Add(PdfObject obj)
        {
            objs[obj.Number] = obj;
        }

        public bool ContainsKey(int no)
        {
            return objs.ContainsKey(no);
        }

        public Dictionary<int, PdfObject>.KeyCollection Keys
        {
            get { return objs.Keys; }
        }

        private Dictionary<string, PdfObject> trailer = new Dictionary<string, PdfObject>();

        public Dictionary<int, string> GetTrailerObjects()
        {
            var ret = new Dictionary<int, string>();
            foreach (var key in trailer.Keys)
            {
                var r = trailer[key];
                if (r.Position > 0) ret.Add(r.Number, key);
            }
            return ret;
        }

        public bool ContainsTrailer(string key)
        {
            return trailer.ContainsKey(key);
        }

        public void AddTrailer(string key, PdfObject value)
        {
            trailer.Add(key, value);
        }

        public object GetTrailer(string key)
        {
            return trailer.ContainsKey(key) ? trailer[key] : null;
        }

        public string ReadObject(int key)
        {
            var sw = new StringWriter();
            var stream = parser.Lexer.Stream;
            var obj = this[key];
            if (obj.ObjStm == 0)
            {
                if (obj.Position > 0)
                {
                    stream.Position = obj.Position;
                    if (obj.StreamStart == 0)
                        sw.WriteLine(parser.Lexer.ReadAscii((int)obj.Length));
                    else
                    {
                        int len1 = (int)(obj.StreamStart - obj.Position);
                        int len2 = (int)(obj.Length - obj.StreamLength - len1);
                        var s1 = parser.Lexer.ReadAscii(len1);
                        stream.Position += obj.StreamLength;
                        var s2 = parser.Lexer.ReadAscii(len2);
                        if (s2.StartsWith("\r\n")) s2 = s2.Substring(2);
                        sw.Write(s1);
                        if (!s1.EndsWith("\r\n")) sw.WriteLine();
                        var filter = obj["/Filter"];
                        if (filter == null ||
                            (filter.Text == "/FlateDecode" && !obj.ContainsKey("/DecodeParms")))
                        {
                            using (var s = obj.GetStream(stream))
                            {
                                var lexer2 = new PdfLexer(s);
                                var bytes = obj.GetStreamBytes(stream);
                                var s3 = PdfLexer.GetString(bytes, 0, bytes.Length, Encoding.Default);
                                sw.Write(s3);
                                if (!s3.EndsWith("\r\n")) sw.WriteLine();
                            }
                        }
                        else
                            sw.WriteLine("...");
                        sw.Write(s2);
                        if (!s2.EndsWith("\r\n")) sw.WriteLine();
                    }
                }
            }
            else
            {
                var objstm = this[obj.ObjStm];
                using (var s = objstm.GetStream(stream))
                {
                    var skip = new byte[obj.Position];
                    s.Read(skip, 0, skip.Length);
                    var buf = new byte[obj.Length];
                    int rlen = s.Read(buf, 0, buf.Length);
                    var s1 = PdfLexer.GetAscii(buf, 0, rlen);
                    sw.Write(s1);
                    if (!s1.EndsWith("\r\n")) sw.WriteLine();
                }
            }
            return sw.ToString();
        }

        public byte[] GetStreamBytes(PdfObject obj)
        {
            return obj.GetStreamBytes(parser.Lexer.Stream);
        }
    }
}
