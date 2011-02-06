using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PdfAnalyzer
{
    public class PdfParser
    {
        private Stream stream;
        public PdfLexer Lexer { get; private set; }

        public PdfParser(Stream stream)
        {
            this.stream = stream;
            Lexer = new PdfLexer(stream);
            if (Lexer.ReadAscii(4) != "%PDF")
                throw new Exception("signature is not %PDF");
        }

        private long readStartXref()
        {
            stream.Position = Math.Max(0, stream.Length - 64);
            var last = Lexer.ReadAscii(64);
            var p = last.LastIndexOf("startxref");
            if (p < 0) return 0;
            p += 9;
            while (p < last.Length && last[p] <= ' ') p++;
            return PdfLexer.ReadInt64(last, p);
        }

        private Dictionary<int, long> xref = new Dictionary<int, long>();
        public Dictionary<int, long> Xref { get { return xref; } }

        private Dictionary<int, object> objs = new Dictionary<int, object>();

        public object GetObject(int no)
        {
            if (objs.ContainsKey(no)) return objs[no];
            if (!xref.ContainsKey(no)) return null;

            stream.Position = xref[no];
            Lexer.Clear();
            Lexer.ReadToken();
            var ret = Read();
            objs[no] = ret;
            return ret;
        }

        public void ReadXref()
        {
            var xr = readStartXref();
            if (xr == 0) throw new Exception("not find: startxref");

            stream.Position = xr;
            readXref();
        }

        private void readXref()
        {
            Lexer.ReadToken();
            if (Lexer.Current != "xref")
            {
                readXrefObject();
                return;
            }
            while (Lexer.Current != null)
            {
                Lexer.ReadToken();
                if (Lexer.Current == "trailer")
                {
                    readTrailer();
                    return;
                }
                var start = int.Parse(Lexer.Current);
                Lexer.ReadToken();
                var size = int.Parse(Lexer.Current);
                for (int i = 0; i < size; i++)
                {
                    int no = start + i;
                    Lexer.ReadToken();
                    int offset = int.Parse(Lexer.Current);
                    Lexer.ReadToken();
                    int t = int.Parse(Lexer.Current);
                    if (no == 0 && t != 65535)
                        throw Lexer.Abort("xref: 0 must be 65535");
                    Lexer.ReadToken();
                    if (no == 0)
                    {
                        if (Lexer.Current != "f")
                            throw Lexer.Abort("xref: 0 must be 'f'");
                    }
                    else if (Lexer.Current != "n")
                        throw Lexer.Abort("xref: must be 'n'");
                    if (!xref.ContainsKey(no))
                    {
                        xref.Add(no, offset);
                    }
                }
            }
        }

        private void readXrefObject()
        {
            var obj = new PdfObject(this);
            if (!xref.ContainsKey(obj.Number)) xref[obj.Number] = obj.Position;
            if (!objs.ContainsKey(obj.Number)) objs[obj.Number] = obj;
            if (obj.Type != "/XRef" || obj.StreamLength == 0)
                throw Lexer.Abort("required: xref");
            if (obj.Dictionary == null)
                throw Lexer.Abort("required: << ... >>");
            if (!obj.Dictionary.ContainsKey("/W"))
                throw Lexer.Abort("required: /W");
            var w = obj.Dictionary["/W"] as object[];
            if (w == null || w.Length != 3)
                throw Lexer.Abort("required: /W [ n n n ]");
            var ww = new int[3];
            for (int i = 0; i < 3; i++) ww[i] = (int)(double)w[i];
            int start = 0, size = 0;
            if (obj.Dictionary.ContainsKey("/Size"))
                size = (int)(double)obj.Dictionary["/Size"];
            if (obj.Dictionary.ContainsKey("/Index"))
            {
                var idx = obj.Dictionary["/Index"] as object[];
                if (idx == null || idx.Length != 2)
                    throw Lexer.Abort("required: /Index [ n n ]");
                start = (int)(double)idx[0];
                size = (int)(double)idx[1];
            }

            foreach (var key in new[] { "/Root", "/Size", "/Info", "/ID" })
            {
                if (obj.Dictionary.ContainsKey(key) && !trailer.ContainsKey(key))
                    trailer[key] = obj.Dictionary[key];
            }

            using (var s = obj.GetStream(stream))
            {
                for (int i = 0, no = start; i < size; i++, no++)
                {
                    var type = s.ReadByte();
                    var cr0 = ReadToInt64(s, ww[0]);
                    var cr1 = ReadToInt64(s, ww[1]);
                    var cr2 = ReadToInt64(s, ww[2]);
                    if (!xref.ContainsKey(no)) xref[no] = cr1;
                    System.Diagnostics.Debug.Print("{0}: {1} {2} {3} {4}", no, type, cr0, cr1, cr2);
                }
            }

            Lexer.Clear();
            if (obj.Dictionary.ContainsKey("/Prev"))
            {
                stream.Position = (long)(double)obj.Dictionary["/Prev"];
                readXref();
            }
        }

        public static long ReadToInt64(Stream s, int len)
        {
            long ret = 0;
            for (int i = 0; i < len; i++)
            {
                var b = s.ReadByte();
                if (b == -1) break;
                ret = (ret << 8) + b;
            }
            return ret;
        }

        private Dictionary<string, object> trailer = new Dictionary<string, object>();

        private void readTrailer()
        {
            Lexer.ReadToken();
            if (Lexer.Current != "<<")
                throw Lexer.Abort("required: <<");
            var dic = new PdfDictionary(this);
            long? prev = null, xrefstm = null;
            foreach (var key in dic.Keys)
            {
                if (key == "/Prev")
                    prev = (long)(double)dic[key];
                else if (key == "/XRefStm")
                    xrefstm = (long)(double)dic[key];
                else if (!trailer.ContainsKey(key))
                    trailer[key] = dic[key];
            }
            if (xrefstm != null)
            {
                stream.Position = (long)prev;
                Lexer.Clear();
                Lexer.ReadToken();
                readXrefObject();
            }
            if (prev != null)
            {
                stream.Position = (long)prev;
                Lexer.Clear();
                readXref();
            }
        }

        public Dictionary<int, string> GetTrailerReferences()
        {
            var ret = new Dictionary<int, string>();
            foreach (var key in trailer.Keys)
            {
                var r = trailer[key] as PdfReference;
                if (r != null) ret.Add(r.Number, key);
            }
            return ret;
        }

        private object cache;

        public object Read()
        {
            if (cache != null)
            {
                var ret = cache;
                cache = null;
                return ret;
            }
            if (Lexer.IsNumber)
            {
                var num = double.Parse(Lexer.Current);
                Lexer.ReadToken();
                if (Lexer.IsNumber)
                {
                    var num2 = double.Parse(Lexer.Current);
                    Lexer.ReadToken();
                    if (Lexer.Current == "R")
                    {
                        Lexer.ReadToken();
                        return new PdfReference((int)num, (int)num2);
                    }
                    else
                    {
                        cache = num2;
                        return num;
                    }
                }
                else
                    return num;
            }
            else if (Lexer.Current == "<<")
                return new PdfDictionary(this);
            else if (Lexer.Current == "[")
            {
                var list = new List<object>();
                Lexer.ReadToken();
                while (Lexer.Current != null)
                {
                    if (Lexer.Current == "]")
                    {
                        Lexer.ReadToken();
                        break;
                    }
                    list.Add(Read());
                }
                return list.ToArray();
            }
            else if (Lexer.Current == "<")
                return ReadTo('>');
            else if (Lexer.Current == "(")
                return ReadTo(')');
            else
            {
                var ret = Lexer.Current;
                Lexer.ReadToken();
                return ret;
            }
        }

        private string ReadTo(char end)
        {
            var sb = new StringBuilder(Lexer.Current);
            for (; ; )
            {
                int b = stream.ReadByte();
                if (b == -1) break;
                var ch = (char)b;
                sb.Append(ch);
                if (ch == end)
                {
                    Lexer.Clear();
                    Lexer.ReadToken();
                    break;
                }
            }
            return sb.ToString();
        }
    }
}
