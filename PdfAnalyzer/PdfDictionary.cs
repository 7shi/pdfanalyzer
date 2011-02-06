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
                dic[key] = parser.Read();
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
