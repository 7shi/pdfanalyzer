using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfAnalyzer
{
    public class PdfDictionary
    {
        private Dictionary<string, object> dic = new Dictionary<string, object>();

        public PdfDictionary(PdfParser parser)
        {
            var lexer = parser.Lexer;
            lexer.ReadToken();
            while (lexer.Current != null && lexer.Current != ">>")
            {
                var key = lexer.Current;
                lexer.ReadToken();
                if (lexer.IsNumber)
                {
                    var num = double.Parse(lexer.Current);
                    lexer.ReadToken();
                    if (lexer.IsNumber)
                    {
                        var num2 = int.Parse(lexer.Current);
                        lexer.ReadToken();
                        if (lexer.Current == "R")
                        {
                            dic[key] = new PdfReference((int)num, num2);
                            lexer.ReadToken();
                        }
                        else
                            throw lexer.Abort("required: R");
                    }
                    else
                        dic[key] = num;
                }
                else if (lexer.Current == "[")
                {
                    while (lexer.Current != null && lexer.Current != "]")
                        lexer.ReadToken();
                    lexer.ReadToken();
                }
                else
                {
                    dic[key] = lexer.Current;
                    lexer.ReadToken();
                }
            }
        }

        public object this[string key]
        {
            get { return dic[key]; }
        }

        public Dictionary<string, object>.KeyCollection Keys
        {
            get { return dic.Keys; }
        }

        public bool ContainsKey(string key)
        {
            return dic.ContainsKey(key);
        }
    }
}
