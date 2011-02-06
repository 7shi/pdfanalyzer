using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfAnalyzer
{
    public class PdfReference
    {
        public int Number { get; private set; }
        public int Number2 { get; private set; }

        public PdfReference(int num, int num2)
        {
            Number = num;
            Number2 = num2;
        }
    }
}
