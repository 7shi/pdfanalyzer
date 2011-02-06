using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfAnalyzer
{
    public class PdfObject
    {
        public int Number { get; private set; }
        public int Number2 { get; private set; }

        public PdfObject(PdfLexer lexer)
        {
            if (!lexer.IsNumber)
                throw lexer.Abort("required: number");
            Number = int.Parse(lexer.Current);
            lexer.ReadToken();
            Number2 = int.Parse(lexer.Current);
            lexer.ReadToken();
            if (lexer.Current != "obj")
                throw lexer.Abort("required: obj");
        }
    }
}
