using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Stmt2Vec
{
    class Program
    {
        static void Main(string[] args)
        {
            string curDir = System.IO.Directory.GetCurrentDirectory();
            
            string rootDir = System.IO.Directory.GetCurrentDirectory() + "\\..\\..\\..\\..";
            Common.initTrainTestFile(rootDir);
            SplitWord.init(rootDir);
            Statistics sc = new Statistics(rootDir);
            sc.summaryTokens();
            sc.summaryScales();
            sc.summarySplit();
            
            generateExprData ged = new generateExprData(rootDir);
            ged.initPDGs();
            ged.writeJsonResult();
            
        }
    }
}
