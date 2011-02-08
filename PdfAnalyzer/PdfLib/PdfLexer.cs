﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PdfLib
{
    public class PdfLexer
    {
        public Stream Stream { get; private set; }

        private PdfLexer() { }

        public PdfLexer(Stream stream)
        {
            Stream = stream;
        }

        public void Clear()
        {
            cur = 0;
            token2 = token1 = current = null;
            isNumber = false;
        }

        public PdfLexer Save()
        {
            var ret = new PdfLexer();
            ret.Load(this);
            return ret;
        }

        public void Load(PdfLexer lexer)
        {
            cur = lexer.cur;
            objno = lexer.objno;
            token2 = lexer.token2;
            token1 = lexer.token1;
            current = lexer.current;
            position = lexer.position;
            isNumber = lexer.isNumber;
            if (Stream == null)
                spos = lexer.Stream.Position;
            else
                Stream.Position = lexer.spos;

        }

        private int cur, objno;
        private string token2, token1, current;
        private long position, spos;
        private bool isNumber;

        public long Position { get { return position; } }
        public bool IsNumber { get { return isNumber; } }
        public int ObjNo { get { return objno; } }
        public string Current { get { return current; } }

        public void ReadToken()
        {
            token2 = token1;
            token1 = current;
            current = ReadTokenInternal();
            if (current == "obj") objno = int.Parse(token2);
        }

        private string ReadTokenInternal()
        {
            isNumber = false;
            position = Stream.Position;
            if (cur == 0) cur = Stream.ReadByte();
            if (cur == -1) return null;

            var sb = new StringBuilder();
            for (; cur != -1; cur = Stream.ReadByte())
            {
                var ch = (char)cur;
                if (ch == '-' || ch == '.' || char.IsDigit(ch))
                {
                    bool dot = false;
                    position = Stream.Position - 1;
                    do
                    {
                        sb.Append((char)cur);
                        if (cur == '.') dot = true;
                        cur = Stream.ReadByte();
                    }
                    while (cur != -1 && ((!dot && cur == '.') || char.IsDigit((char)cur)));
                    isNumber = true;
                    break;
                }
                else if (ch == '/' || char.IsLetter(ch))
                {
                    position = Stream.Position - 1;
                    do
                    {
                        sb.Append((char)cur);
                        cur = Stream.ReadByte();
                    }
                    while (cur != -1 &&
                        (cur == '-' || cur == '_' || cur == '.' || cur == '#'
                        || char.IsLetterOrDigit((char)cur)));
                    break;
                }
                else if (ch == '%')
                {
                    while (cur != -1 && cur != '\r' && cur != '\n')
                        cur = Stream.ReadByte();
                }
                else if (ch == '(')
                {
                    position = Stream.Position - 1;
                    int prev2 = -1, prev = -1;
                    do
                    {
                        sb.Append((char)cur);
                        prev2 = prev;
                        prev = cur;
                        cur = Stream.ReadByte();
                    }
                    while (cur != -1 && !(prev == ')' && prev2 != '\\'));
                    break;
                }
                else if (ch > ' ')
                {
                    position = Stream.Position - 1;
                    sb.Append(ch);
                    cur = Stream.ReadByte();
                    if ((ch == '<' || ch == '>') && ch == cur)
                    {
                        sb.Append(ch);
                        cur = Stream.ReadByte();
                    }
                    else if (ch == '<' && cur != -1)
                    {
                        int prev = -1;
                        do
                        {
                            sb.Append((char)cur);
                            prev = cur;
                            cur = Stream.ReadByte();
                        }
                        while (cur != -1 && prev != '>');
                    }
                    break;
                }
            }
            return sb.ToString();
        }

        public string ReadAscii(int len)
        {
            var buf = new byte[len];
            var rlen = Stream.Read(buf, 0, len);
            return GetAscii(buf, 0, rlen);
        }

        public static string GetAscii(byte[] buf, int start, int count)
        {
            var sb = new StringBuilder();
            char prev = '\0';
            for (int i = 0; i < count; i++)
            {
                var ch = (char)buf[start + i];
                if (prev == '\r' && ch != '\n')
                    sb.Append('\n');
                else if (prev != '\r' && ch == '\n')
                    sb.Append('\r');
                sb.Append(ch);
                prev = ch;
            }
            return sb.ToString();
        }

        public string ReadString(int len, Encoding enc)
        {
            var buf = new byte[len];
            var rlen = Stream.Read(buf, 0, len);
            return GetString(buf, 0, rlen, enc);
        }

        public static string GetString(byte[] buf, int start, int count, Encoding enc)
        {
            var list = new List<byte>();
            byte prev = 0;
            for (int i = 0; i < count; i++)
            {
                var ch = buf[start + i];
                if (prev == '\r' && ch != '\n')
                    list.Add((byte)'\n');
                else if (prev != '\r' && ch == '\n')
                    list.Add((byte)'\r');
                list.Add(ch);
                prev = ch;
            }
            return enc.GetString(list.ToArray());
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
            if (cur == 0x0d) Stream.ReadByte();
            Clear();
            var ret = Stream.Position;
            Stream.Position += len;
            return ret;
        }
    }
}
