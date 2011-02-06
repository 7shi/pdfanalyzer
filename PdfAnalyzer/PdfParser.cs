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
                var obj = new PdfObject(Lexer);
                throw new NotImplementedException();
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

        private Dictionary<string, object> trailer = new Dictionary<string, object>();

        private void readTrailer()
        {
            Lexer.ReadToken();
            if (Lexer.Current != "<<")
                throw Lexer.Abort("required: <<");
            var dic = new PdfDictionary(this);
            long? prev = null;
            foreach (var key in dic.Keys)
            {
                if (key == "/Prev")
                    prev = (long)(double)dic[key];
                else if (!trailer.ContainsKey(key))
                    trailer[key] = dic[key];
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

        public object Read()
        {
            if (Lexer.IsNumber)
            {
                var num = double.Parse(Lexer.Current);
                Lexer.ReadToken();
                if (Lexer.IsNumber)
                {
                    var num2 = int.Parse(Lexer.Current);
                    Lexer.ReadToken();
                    if (Lexer.Current == "R")
                    {
                        Lexer.ReadToken();
                        return new PdfReference((int)num, num2);
                    }
                    else
                        throw Lexer.Abort("required: R");
                }
                else
                    return num;
            }
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
                return ReadUntil('>');
            else if (Lexer.Current == "(")
                return ReadUntil(')');
            else
            {
                var ret = Lexer.Current;
                Lexer.ReadToken();
                return ret;
            }
        }

        private string ReadUntil(char end)
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
