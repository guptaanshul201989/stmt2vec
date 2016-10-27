using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDGGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            string curDir = System.IO.Directory.GetCurrentDirectory();

            string rootDir = System.IO.Directory.GetCurrentDirectory() + "\\..\\..\\..\\..\\..";
            Common.initTrainTestFile(rootDir);
            SplitWord.init(rootDir);
            
            generateExprData ged = new generateExprData(rootDir);
            ged.initPDGs();
            ged.writeJsonResult();
        }
    }
}
