using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PdfAnalyzer
{
    public class PdfDocument
    {
        public PdfDocument(string pdf)
        {
            PdfParser parser;
            using (var fs = new FileStream(pdf, FileMode.Open))
            {
                parser = new PdfParser(fs);
                parser.ReadXref(this);
            }
        }

        private Dictionary<int, PdfObject> objs = new Dictionary<int, PdfObject>();

        public PdfObject this[int no]
        {
            get
            {
                return objs.ContainsKey(no) ? objs[no] : null;
            }
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

        private Dictionary<string, object> trailer = new Dictionary<string, object>();

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

        public bool ContainsTrailer(string key)
        {
            return trailer.ContainsKey(key);
        }

        public void AddTrailer(string key, object value)
        {
            trailer.Add(key, value);
        }

        public object GetTrailer(string key)
        {
            return trailer[key];
        }
    }
}
