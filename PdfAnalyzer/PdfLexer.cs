using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PdfAnalyzer
{
    public class PdfLexer
    {
        private Stream stream;

        public PdfLexer(Stream stream)
        {
            this.stream = stream;
        }

        public void Clear()
        {
            cur = 0;
            token2 = token1 = current = null;
            IsNumber = false;
        }

        private int cur, objno;
        private string token2, token1, current;

        public long Position { get; private set; }
        public bool IsNumber { get; private set; }

        public int ObjNo { get { return objno; } }
        public string Current { get { return current; } }

        public void ReadToken(bool isStream = false)
        {
            if (!isStream)
            {
                token2 = token1;
                token1 = current;
                current = ReadTokenInternal();
                if (current == "obj") objno = int.Parse(token2);
            }
            else
                current = ReadTokenInternal();
        }

        private string ReadTokenInternal()
        {
            IsNumber = false;
            Position = stream.Position;
            if (cur == 0) cur = stream.ReadByte();
            if (cur == -1) return null;

            var sb = new StringBuilder();
            for (; cur != -1; cur = stream.ReadByte())
            {
                var ch = (char)cur;
                if (char.IsDigit(ch))
                {
                    for (; cur != -1; cur = stream.ReadByte())
                    {
                        var ch2 = (char)cur;
                        if (ch2 == '.' || char.IsDigit(ch2))
                            sb.Append(ch2);
                        else
                            break;
                    }
                    IsNumber = true;
                    break;
                }
                else if (ch == '/' || char.IsLetter(ch))
                {
                    do
                    {
                        sb.Append((char)cur);
                        cur = stream.ReadByte();
                    }
                    while (cur != -1 && (cur == '_' || char.IsLetterOrDigit((char)cur)));
                    break;
                }
                else if (ch > ' ')
                {
                    sb.Append(ch);
                    cur = stream.ReadByte();
                    if ((ch == '<' || ch == '>') && ch == cur)
                    {
                        sb.Append(ch);
                        cur = stream.ReadByte();
                    }
                    break;
                }
            }
            return sb.ToString();
        }

        public string ReadAscii(int len)
        {
            var buf = new byte[len];
            var readlen = stream.Read(buf, 0, len);
            var cbuf = new char[readlen];
            for (int i = 0; i < readlen; i++)
                cbuf[i] = (char)buf[i];
            return new string(cbuf);
        }

        public static long ReadInt64(string s, int pos)
        {
            long ret = 0;
            for (int p = pos; '0' <= s[p] && s[p] <= '9'; p++)
            {
                ret *= 10;
                ret += s[p] - '0';
            }
            return ret;
        }

        public Exception Abort(string format, params object[] args)
        {
            var msg = string.Format(format, args);
            return new Exception(string.Format("{0:x} [{1}] {2}", Position, current, msg));
        }

        public long SkipStream(long len)
        {
            if (cur == 0x0d) stream.ReadByte();
            Clear();
            var ret = stream.Position;
            stream.Position += len;
            return ret;
        }
    }
}
