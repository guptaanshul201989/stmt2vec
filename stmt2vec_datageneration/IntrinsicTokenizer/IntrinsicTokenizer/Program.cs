using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntrinsicTokenizer
{
    class Program
    {
        static void Main(string[] args)
        {
            string curDir = System.IO.Directory.GetCurrentDirectory();

            string rootDir = System.IO.Directory.GetCurrentDirectory() + "\\..\\..\\..\\..\\..";

            SplitWord.generate(rootDir);

            Statistics sc = new Statistics(rootDir);
            sc.summaryTokens();
            sc.summaryScales();
        }
    }
}
