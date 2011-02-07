using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfAnalyzer
{
    public class PdfReference
    {
        public int Number { get; private set; }
        public int Index { get; private set; }

        public PdfReference(int num, int num2)
        {
            Number = num;
            Index = num2;
        }
    }
}
