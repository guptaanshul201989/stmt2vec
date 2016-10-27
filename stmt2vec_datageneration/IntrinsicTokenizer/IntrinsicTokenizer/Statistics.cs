
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace IntrinsicTokenizer
{
    class Statistics
    {
        public string desDir;
        public string srcDir;
        public Statistics(string path) { srcDir = path; }

        public string getTokenNums(Dictionary<string, int> dic)
        {
            int countKind = 0;
            int countNums = 0;
            foreach (var d in dic)
            {
                countKind++;
                countNums += d.Value;
            }
            string res = countKind + "\t\t" + countNums;
            return res;
        }
        public void writeTokenNums(Dictionary<string, int> dic, string fName)
        {
            StringBuilder sb = new StringBuilder();
            int countNums = 0;
            foreach (var d in dic)
            {
                sb.AppendLine(d.Key + "\t" + d.Value);
                countNums += d.Value;
            }
            sb.AppendLine("Sum\t" + countNums);
            sb.AppendLine("Dis\t" + dic.Count());
            File.WriteAllText(fName, sb.ToString());
        }
        public void summaryTokens()
        {
            string programDir = srcDir + "\\ExprData\\sourcecode";
            string tokenDir = srcDir + "\\ExprData\\summaryTokens";
            if (!Directory.Exists(tokenDir)) Directory.CreateDirectory(tokenDir);

            Dictionary<string, int> sumKeyword = new Dictionary<string, int>();
            Dictionary<string, int> sumSymbol = new Dictionary<string, int>();
            Dictionary<string, int> sumLitteral = new Dictionary<string, int>();
            Dictionary<string, int> sumName = new Dictionary<string, int>();

            Dictionary<string, int> sumOthers = new Dictionary<string, int>();

            foreach (var proPath in Directory.GetDirectories(programDir))
            {

                Dictionary<string, int> countKeyword = new Dictionary<string, int>();
                Dictionary<string, int> countSymbol = new Dictionary<string, int>();
                Dictionary<string, int> countLitteral = new Dictionary<string, int>();
                Dictionary<string, int> countName = new Dictionary<string, int>();

                Dictionary<string, int> countOthers = new Dictionary<string, int>();

                string proName = proPath.Split('\\').Last();
                string proDir = tokenDir + "\\" + proName;
                Console.WriteLine("Summary " + proName + "...");
                HashSet<string> filePath = new HashSet<string>();
                if (!Directory.Exists(proDir)) Directory.CreateDirectory(proDir);
                foreach (var slnPath in Common.scanCSharpSln(proPath))
                {
                    var msWorkspace = MSBuildWorkspace.Create();
                    var sol = msWorkspace.OpenSolutionAsync(slnPath).Result;
                    foreach (var pro in sol.Projects)
                    {
                        foreach (var doc in pro.Documents)
                        {
                            if (filePath.Contains(doc.FilePath)) continue;
                            filePath.Add(doc.FilePath);
                            var rootNode = doc.GetSyntaxRootAsync().Result;
                            var tokens = rootNode.DescendantTokens();
                            foreach (var token in tokens)
                            {
                                var str = token.ToString();
                                var kind = token.Kind();
                                if (kind >= SyntaxKind.BoolKeyword && kind <= SyntaxKind.LoadKeyword)
                                {
                                    if (!countKeyword.ContainsKey(str)) countKeyword.Add(str, 1);
                                    else countKeyword[str]++;

                                    if (!sumKeyword.ContainsKey(str)) sumKeyword.Add(str, 1);
                                    else sumKeyword[str]++;
                                }
                                else if (kind >= SyntaxKind.TildeToken && kind <= SyntaxKind.PercentEqualsToken)
                                {
                                    if (!countSymbol.ContainsKey(str)) countSymbol.Add(str, 1);
                                    else countSymbol[str]++;

                                    if (!sumSymbol.ContainsKey(str)) sumSymbol.Add(str, 1);
                                    else sumSymbol[str]++;
                                }
                                else if (kind >= SyntaxKind.NumericLiteralToken && kind <= SyntaxKind.XmlTextLiteralToken)
                                {
                                    if (!countLitteral.ContainsKey(str)) countLitteral.Add(str, 1);
                                    else countLitteral[str]++;

                                    if (!sumLitteral.ContainsKey(str)) sumLitteral.Add(str, 1);
                                    else sumLitteral[str]++;
                                }
                                else if (kind == SyntaxKind.IdentifierToken)
                                {
                                    if (!countName.ContainsKey(str)) countName.Add(str, 1);
                                    else countName[str]++;

                                    if (!sumName.ContainsKey(str)) sumName.Add(str, 1);
                                    else sumName[str]++;
                                }
                                else
                                {
                                    if (!countOthers.ContainsKey(str)) countOthers.Add(str, 1);
                                    else countOthers[str]++;

                                    if (!sumOthers.ContainsKey(str)) sumOthers.Add(str, 1);
                                    else sumOthers[str]++;
                                }
                            }
                        }
                    }
                }
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Type\t\tDis\t\tNums");
                sb.AppendLine("keyword\t\t" + getTokenNums(countKeyword));
                sb.AppendLine("symbol\t\t" + getTokenNums(countSymbol));
                sb.AppendLine("literal\t\t" + getTokenNums(countLitteral));
                sb.AppendLine("name\t\t" + getTokenNums(countName));
                File.WriteAllText(proDir + "\\sum.txt", sb.ToString());
                writeTokenNums(countKeyword, proDir + "\\Keyword.txt");
                writeTokenNums(countSymbol, proDir + "\\Symbol.txt");
                writeTokenNums(countLitteral, proDir + "\\Litteral.txt");
                writeTokenNums(countName, proDir + "\\Name.txt");
                writeTokenNums(countOthers, proDir + "\\Others.txt");
            }


            if (!Directory.Exists(tokenDir + "\\Summary")) Directory.CreateDirectory(tokenDir + "\\Summary");

            StringBuilder sb_s = new StringBuilder();
            sb_s.AppendLine("Type\t\tDis\t\tNums");
            sb_s.AppendLine("keyword\t\t" + getTokenNums(sumKeyword));
            sb_s.AppendLine("symbol\t\t" + getTokenNums(sumSymbol));
            sb_s.AppendLine("literal\t\t" + getTokenNums(sumLitteral));
            sb_s.AppendLine("name\t\t" + getTokenNums(sumName));
            File.WriteAllText(tokenDir + "\\Summary\\Sum.txt", sb_s.ToString());
            writeTokenNums(sumKeyword, tokenDir + "\\Summary\\Keyword.txt");
            writeTokenNums(sumSymbol, tokenDir + "\\Summary\\Symbol.txt");
            writeTokenNums(sumLitteral, tokenDir + "\\Summary\\Litteral.txt");
            writeTokenNums(sumName, tokenDir + "\\Summary\\Name.txt");
            writeTokenNums(sumOthers, tokenDir + "\\Summary\\Others.txt");
        }
        public void summaryScales()
        {
            string programDir = srcDir + "\\ExprData\\sourcecode";
            string summaryDir = srcDir + "\\ExprData\\summaryScales";
            if (!Directory.Exists(summaryDir)) Directory.CreateDirectory(summaryDir);
            int sumFiles = 0;
            int sumClasses = 0;
            int sumMethods = 0;
            int sumLines = 0;
            StringBuilder sb_files = new StringBuilder();
            StringBuilder sb_classes = new StringBuilder();
            StringBuilder sb_methods = new StringBuilder();
            StringBuilder sb_lines = new StringBuilder();
            foreach (var proPath in Directory.GetDirectories(programDir))
            {

                string proName = proPath.Split('\\').Last();

                Console.WriteLine("Summary " + proName + "...");

                int countFiles = 0;
                int countClasses = 0;
                int countMethods = 0;
                int countLines = 0;
                HashSet<string> filePath = new HashSet<string>();
                foreach (var slnPath in Common.scanCSharpSln(proPath))
                {
                    var msWorkspace = MSBuildWorkspace.Create();
                    var sol = msWorkspace.OpenSolutionAsync(slnPath).Result;
                    foreach (var pro in sol.Projects)
                    {
                        foreach (var doc in pro.Documents)
                        {
                            if (filePath.Contains(doc.FilePath)) continue;
                            filePath.Add(doc.FilePath);
                            countFiles++;
                            var rootNode = doc.GetSyntaxRootAsync().Result;
                            var classNodes = rootNode.DescendantNodes().OfType<ClassDeclarationSyntax>();
                            var methodNodes = rootNode.DescendantNodes().OfType<MethodDeclarationSyntax>();
                            countClasses += classNodes.Count();
                            countMethods += methodNodes.Count();
                            var text = doc.GetTextAsync().Result.ToString();
                            countLines += text.Split('\n').Length;
                        }
                    }
                }
                sumFiles += countFiles;
                sumClasses += countClasses;
                sumMethods += countMethods;
                sumLines += countLines;

                sb_files.AppendLine(proName + "\t" + countFiles);
                sb_classes.AppendLine(proName + "\t" + countClasses);
                sb_methods.AppendLine(proName + "\t" + countMethods);
                sb_lines.AppendLine(proName + "\t" + countLines);

            }
            File.WriteAllText(summaryDir + "\\Files.txt", sb_files.ToString());
            File.WriteAllText(summaryDir + "\\Classes.txt", sb_classes.ToString());
            File.WriteAllText(summaryDir + "\\Methods.txt", sb_methods.ToString());
            File.WriteAllText(summaryDir + "\\Lines.txt", sb_lines.ToString());
            StringBuilder sb1 = new StringBuilder();
            sb1.AppendLine("Files\t\t" + sumFiles);
            sb1.AppendLine("Classes\t\t" + sumClasses);
            sb1.AppendLine("Methods\t\t" + sumMethods);
            sb1.AppendLine("Lines\t\t" + sumLines);
            File.WriteAllText(summaryDir + "\\Sum.txt", sb1.ToString());
        }
       
    }
}
