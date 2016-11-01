using System;
using System.Collections.Generic;
using System.Linq;


using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.MSBuild;

namespace PDGGenerator
{
    class generateExprData
    {
        public string rootDir;

        public Dictionary<string, List<string>> trainPath = new Dictionary<string, List<string>>();
        public Dictionary<string, List<string>> testPath = new Dictionary<string, List<string>>();

        public Dictionary<string, List<PDG>> trainPDG = new Dictionary<string, List<PDG>>();
        public Dictionary<string, List<PDG>> testPDG = new Dictionary<string, List<PDG>>();

        public generateExprData(string path)
        {
            rootDir = path;
            foreach (var p in Directory.GetDirectories(path + "\\ExprData\\FilePath"))
            {
                string proName = p.Split('\\').Last();
                List<string> trainSet = new List<string>();
                var trainLines = File.ReadAllLines(p + "\\trainPath.txt");

                foreach (var line in trainLines) trainSet.Add(line);
                trainPath.Add(proName, trainSet);
                //Console.WriteLine(proName + "\t" + trainLines.Count() + "\t" + trainSet.Count());
                List<string> testSet = new List<string>();
                var testLines = File.ReadAllLines(p + "\\testPath.txt");
                foreach (var line in testLines) testSet.Add(line);
                testPath.Add(proName, testSet);
                //Console.WriteLine(proName + "\t" + testLines.Count() + "\t" + testSet.Count());
                //break;
            }


        }
        public string getMethodFullName(MethodDeclarationSyntax method)
        {
            string methodName = method.Identifier.ToString();
            string argName = "";
            var parList = method.ParameterList.DescendantNodes().OfType<ParameterSyntax>();
            foreach (var par in parList)
            {
                var nodes = par.DescendantNodes();
                if (nodes.Count() == 0) continue;
                var tokens = nodes.First().DescendantTokens();
                foreach (var token in tokens)
                {
                    if (token.Kind() == SyntaxKind.IdentifierToken)
                    {
                        foreach (var word in SplitWord.getCamelWords(token.ToString()))
                        {
                            argName += word;
                            argName += "\t";
                        }
                    }
                    else
                    {
                        argName += token.Kind().ToString();
                        argName += "\t";
                    }
                }
            }
            return methodName + argName;
        }
        public void initPDGs()
        {
            string programDir = rootDir + "\\ExprData\\sourcecode";

            foreach (var proPath in Directory.GetDirectories(programDir))
            {

                string proName = proPath.Split('\\').Last();
                //if (proName != "Exceptionless-master") continue;
                Console.WriteLine("Constructing PDG of " + proName + "...");
                var trainPathSet = trainPath[proName];
                List<PDG> trainList = new List<PDG>();
                List<PDG> testList = new List<PDG>();
                int count1 = 0;
                int count2 = 0;
                HashSet<string> filePaths = new HashSet<string>();
                foreach (var slnPath in Common.scanCSharpSln(proPath))
                {
                    var msWorkspace = MSBuildWorkspace.Create();
                    var sol = msWorkspace.OpenSolutionAsync(slnPath).Result;
                    foreach (var pro in sol.Projects)
                    {
                        HashSet<string> funcNames = new HashSet<string>();
                        foreach (var doc in pro.Documents)
                        {
                            if (filePaths.Contains(doc.FilePath)) continue;
                            filePaths.Add(doc.FilePath);
                            bool isTrainFile = true;
                            if (trainPathSet.Contains(doc.FilePath)) { isTrainFile = true; count1++; }
                            else { isTrainFile = false; count2++; }

                            var rootNode = doc.GetSyntaxRootAsync().Result;
                            var model = doc.GetSemanticModelAsync().Result;
                            var methodNodes = rootNode.DescendantNodes().OfType<MethodDeclarationSyntax>();
                            foreach (var method in methodNodes)
                            {
                                if (method.ChildNodes().Last().Kind() != SyntaxKind.Block) continue;
                                string methodName = getMethodFullName(method);
                                if (funcNames.Contains(methodName)) continue;
                                else funcNames.Add(methodName);

                                var methodPDG = new ConstructMethodPDG();
                                methodPDG.constructCDS(method);
                                methodPDG.constructDDS(method, model);
                                var mPDG = methodPDG.getPDG();
                                mPDG.location = doc.FilePath;
                                mPDG.funcName = method.Identifier.ToString();
                                //Console.WriteLine(method.ToString());
                                //Console.WriteLine(mPDG.ToString());
                                //Console.Read();
                                if (isTrainFile) trainList.Add(mPDG);
                                else testList.Add(mPDG);
                            }
                        }
                    }
                }
                //Console.WriteLine(trainList.Count() + "\t" + testList.Count());
                //Console.WriteLine(count1 + "\t" + count2);
                trainPDG.Add(proName, trainList);
                testPDG.Add(proName, testList);
                //break;
            }
        }
        public void writeJsonResult()
        {
            string programDir = rootDir + "\\ExprData\\sourcecode";
            string genDir = rootDir + "\\ExprData\\GenData";
            if (!Directory.Exists(genDir)) Directory.CreateDirectory(genDir);
            foreach (var proPath in Directory.GetDirectories(programDir))
            {
                //if (proPath.Split('\\').Last() != "Entity") continue;
                Console.WriteLine("Writing Json of " + proPath.Split('\\').Last() + "...");
                string proDir = genDir + "\\" + proPath.Split('\\').Last();
                if (!Directory.Exists(proDir)) Directory.CreateDirectory(proDir);
                writeFuncCamelData(proDir);
                writePDGCamelData(proDir);
                writeFuncIntrinsicData(proDir);
                writePDGIntrinsicData(proDir);
                //break;
            }
        }
        public void writeFuncCamelData(string proDir)
        {
            string proName = proDir.Split('\\').Last();
            string path = proDir + "\\FuncCamelData";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            string pathTrain = path + "\\train";
            if (!Directory.Exists(pathTrain)) Directory.CreateDirectory(pathTrain);
            string pathTest = path + "\\test";
            if (!Directory.Exists(pathTest)) Directory.CreateDirectory(pathTest);

            writeFuncCamelJson(trainPDG[proName], pathTrain + "\\train.json");
            writeFuncCamelJson(testPDG[proName], pathTest + "\\test.json");
        }
        public void writePDGCamelData(string proDir)
        {
            string proName = proDir.Split('\\').Last();
            string path = proDir + "\\PDGCamelData";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            string pathTrain = path + "\\train";
            if (!Directory.Exists(pathTrain)) Directory.CreateDirectory(pathTrain);
            string pathTest = path + "\\test";
            if (!Directory.Exists(pathTest)) Directory.CreateDirectory(pathTest);

            writePDGCamelJson(trainPDG[proName], pathTrain + "\\train.json");
            writePDGCamelJson(testPDG[proName], pathTest + "\\test.json");
        }
        public void writeFuncIntrinsicData(string proDir)
        {
            string proName = proDir.Split('\\').Last();
            string path = proDir + "\\FuncIntrinsicData";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            string pathTrain = path + "\\train";
            if (!Directory.Exists(pathTrain)) Directory.CreateDirectory(pathTrain);
            string pathTest = path + "\\test";
            if (!Directory.Exists(pathTest)) Directory.CreateDirectory(pathTest);

            writeFuncIntrinsicJson(trainPDG[proName], pathTrain + "\\train.json");
            writeFuncIntrinsicJson(testPDG[proName], pathTest + "\\test.json");
        }
        public void writePDGIntrinsicData(string proDir)
        {
            string proName = proDir.Split('\\').Last();
            string path = proDir + "\\PDGIntrinsicData";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            string pathTrain = path + "\\train";
            if (!Directory.Exists(pathTrain)) Directory.CreateDirectory(pathTrain);
            string pathTest = path + "\\test";
            if (!Directory.Exists(pathTest)) Directory.CreateDirectory(pathTest);

            writePDGIntrinsicJson(trainPDG[proName], pathTrain + "\\train.json");
            writePDGIntrinsicJson(testPDG[proName], pathTest + "\\test.json");
        }

        public void writeFuncCamelJson(List<PDG> pdgs, string path)
        {
            var sw1 = new StringWriter();
            JsonWriter writer1 = new JsonTextWriter(sw1);
            writer1.Formatting = (Formatting.Indented);

            writer1.WriteStartArray();
            foreach (var pdg in pdgs)
            {
                writer1.WriteStartObject();
                writer1.WritePropertyName("name");
                writer1.WriteStartArray();

                foreach (var n in SplitWord.getCamelWords(pdg.funcName))
                    writer1.WriteValue(n);

                writer1.WriteEndArray();

                writer1.WritePropertyName("tokens");
                writer1.WriteStartArray();
                var method = pdg.getNode(0).info.GetSyntax();
                foreach (var token in method.ChildNodes().Last().DescendantTokens())
                {
                    if (token.Kind() == SyntaxKind.IdentifierToken)
                    {
                        writer1.WriteValue("<id>");
                        foreach (var word in SplitWord.getCamelWords(token.ToString()))
                            writer1.WriteValue(word);
                        writer1.WriteValue("</id>");
                    }
                    else writer1.WriteValue(token.Kind().ToString());
                }
                writer1.WriteEndArray();


                string funcBody = method.ToString();
                writer1.WritePropertyName("body");
                writer1.WriteStartArray();
                foreach (var stmt in funcBody.Split('\n'))
                {
                    writer1.WriteValue(stmt);
                }
                writer1.WriteEndArray();
                writer1.WriteEndObject();
            }

            writer1.WriteEndArray();

            writer1.Flush();

            File.WriteAllText(path, sw1.ToString());
        }
        public void writeFuncIntrinsicJson(List<PDG> pdgs, string path)
        {
            var sw1 = new StringWriter();
            JsonWriter writer1 = new JsonTextWriter(sw1);
            writer1.Formatting = (Formatting.Indented);

            writer1.WriteStartArray();
            foreach (var pdg in pdgs)
            {
                writer1.WriteStartObject();
                writer1.WritePropertyName("name");
                writer1.WriteStartArray();

                foreach (var n in SplitWord.getIntrinsicWords(pdg.funcName))
                    writer1.WriteValue(n);

                writer1.WriteEndArray();

                writer1.WritePropertyName("tokens");
                writer1.WriteStartArray();
                var method = pdg.getNode(0).info.GetSyntax();
                foreach (var token in method.ChildNodes().Last().DescendantTokens())
                {
                    if (token.Kind() == SyntaxKind.IdentifierToken)
                    {
                        writer1.WriteValue("<id>");
                        foreach (var word in SplitWord.getIntrinsicWords(token.ToString()))
                            writer1.WriteValue(word);
                        writer1.WriteValue("</id>");
                    }
                    else writer1.WriteValue(token.Kind().ToString());
                }
                writer1.WriteEndArray();


                string funcBody = method.ToString();
                writer1.WritePropertyName("body");
                writer1.WriteStartArray();
                foreach (var stmt in funcBody.Split('\n'))
                {
                    writer1.WriteValue(stmt);
                }
                writer1.WriteEndArray();
                writer1.WriteEndObject();
            }

            writer1.WriteEndArray();

            writer1.Flush();

            File.WriteAllText(path, sw1.ToString());
        }
        public void writePDGCamelJson(List<PDG> pdgs, string path)
        {
            var sw = new StringWriter();
            JsonWriter writer = new JsonTextWriter(sw);
            writer.Formatting = (Formatting.Indented);

            writer.WriteStartArray();
            foreach (var pdg in pdgs)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("name");

                writer.WriteStartArray();

                foreach (var n in SplitWord.getCamelWords(pdg.funcName))
                    writer.WriteValue(n);
                writer.WriteEndArray();

                writer.WritePropertyName("tokens");
                writer.WriteStartArray();

                var dicSort = from objeDic in pdg.PDNodes orderby objeDic.Key ascending select objeDic;
                foreach (var node in dicSort)
                {
                    if (node.Value.type > PDGNodeType.predicate && node.Value.type != PDGNodeType.loop_break
                        && node.Value.type != PDGNodeType.loop_continue && node.Value.type != PDGNodeType.return_statement) continue;
                    if (node.Value.info == null) continue;
                    if (node.Value.info.GetSyntax().Kind() == SyntaxKind.TryStatement) continue;
                    writer.WriteStartArray();
                    writer.WriteValue(node.Key);  //statemtent ID
                    var tokens = node.Value.parse();

                    foreach (var token in tokens)
                    {
                        writer.WriteValue(token);
                    }
                    writer.WriteEndArray();
                }
                writer.WriteEndArray();

                writer.WritePropertyName("cdedges");
                writer.WriteStartArray();
                foreach (var node in dicSort)
                {
                    var nodeType = node.Value.type;
                    if (nodeType > PDGNodeType.predicate && nodeType != PDGNodeType.loop_break && nodeType != PDGNodeType.loop_continue) continue;

                    foreach (var e in pdg.getCDSuccWithoutRegion(node.Key))
                    {
                        writer.WriteValue(node.Key + "->" + e);
                    }
                }
                writer.WriteEndArray();
                writer.WritePropertyName("cfedges");
                writer.WriteStartArray();
                foreach (var node in dicSort)
                {
                    var nodeType = node.Value.type;
                    if (nodeType > PDGNodeType.predicate && nodeType != PDGNodeType.loop_break && nodeType != PDGNodeType.loop_continue) continue;

                    //writer.WriteStartArray();
                    foreach (var e in pdg.getCFSuccWithoutRegion(node.Key))
                    {
                        writer.WriteValue(node.Key + "->" + e);
                    }
                    //writer.WriteEndArray();
                }
                writer.WriteEndArray();
                writer.WritePropertyName("ddedges");
                writer.WriteStartArray();
                foreach (var node in dicSort)
                {
                    var nodeType = node.Value.type;
                    if (nodeType > PDGNodeType.predicate && nodeType != PDGNodeType.loop_break && nodeType != PDGNodeType.loop_continue) continue;

                    //writer.WriteStartArray();
                    if (pdg.getDDSuccs(node.Key) == null) continue;
                    foreach (var e in pdg.getDDSuccs(node.Key))
                    {
                        writer.WriteValue(node.Key + "->" + e);
                    }
                    //writer.WriteEndArray();
                }
                writer.WriteEndArray();

                writer.WritePropertyName("body");

                var method = pdg.getNode(0).info.GetSyntax();
                string funcBody = method.ToString();
                writer.WriteStartArray();
                foreach (var stmt in funcBody.Split('\n'))
                    writer.WriteValue(stmt);
                writer.WriteEndArray();

                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            writer.Flush();

            File.WriteAllText(path, sw.ToString());
        }
        public void writePDGIntrinsicJson(List<PDG> pdgs, string path)
        {
            var sw = new StringWriter();
            JsonWriter writer = new JsonTextWriter(sw);
            writer.Formatting = (Formatting.Indented);

            writer.WriteStartArray();
            foreach (var pdg in pdgs)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("name");

                writer.WriteStartArray();

                foreach (var n in SplitWord.getIntrinsicWords(pdg.funcName))
                    writer.WriteValue(n);
                writer.WriteEndArray();

                writer.WritePropertyName("tokens");
                writer.WriteStartArray();

                var dicSort = from objeDic in pdg.PDNodes orderby objeDic.Key ascending select objeDic;
                foreach (var node in dicSort)
                {
                    if (node.Value.type > PDGNodeType.predicate && node.Value.type != PDGNodeType.loop_break
                        && node.Value.type != PDGNodeType.loop_continue && node.Value.type != PDGNodeType.return_statement) continue;
                    if (node.Value.info == null) continue;
                    if (node.Value.info.GetSyntax().Kind() == SyntaxKind.TryStatement) continue;
                    writer.WriteStartArray();
                    writer.WriteValue(node.Key);  //statemtent ID
                    var tokens = node.Value.parse2();

                    foreach (var token in tokens)
                    {
                        writer.WriteValue(token);
                    }
                    writer.WriteEndArray();
                }
                writer.WriteEndArray();

                writer.WritePropertyName("cdedges");
                writer.WriteStartArray();
                foreach (var node in dicSort)
                {
                    var nodeType = node.Value.type;
                    if (nodeType > PDGNodeType.predicate && nodeType != PDGNodeType.loop_break && nodeType != PDGNodeType.loop_continue) continue;

                    foreach (var e in pdg.getCDSuccWithoutRegion(node.Key))
                    {
                        writer.WriteValue(node.Key + "->" + e);
                    }
                }
                writer.WriteEndArray();
                writer.WritePropertyName("cfedges");
                writer.WriteStartArray();
                foreach (var node in dicSort)
                {
                    var nodeType = node.Value.type;
                    if (nodeType > PDGNodeType.predicate && nodeType != PDGNodeType.loop_break && nodeType != PDGNodeType.loop_continue) continue;
                    //writer.WriteStartArray();
                    foreach (var e in pdg.getCFSuccWithoutRegion(node.Key))
                    {
                        writer.WriteValue(node.Key + "->" + e);
                    }
                    //writer.WriteEndArray();
                }
                writer.WriteEndArray();
                writer.WritePropertyName("ddedges");
                writer.WriteStartArray();
                foreach (var node in dicSort)
                {
                    var nodeType = node.Value.type;
                    if (nodeType > PDGNodeType.predicate && nodeType != PDGNodeType.loop_break && nodeType != PDGNodeType.loop_continue) continue;
                    //writer.WriteStartArray();
                    if (pdg.getDDSuccs(node.Key) == null) continue;
                    foreach (var e in pdg.getDDSuccs(node.Key))
                    {
                        writer.WriteValue(node.Key + "->" + e);
                    }
                    //writer.WriteEndArray();
                }
                writer.WriteEndArray();

                writer.WritePropertyName("body");

                var method = pdg.getNode(0).info.GetSyntax();
                string funcBody = method.ToString();
                writer.WriteStartArray();
                foreach (var stmt in funcBody.Split('\n'))
                    writer.WriteValue(stmt);
                writer.WriteEndArray();

                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            writer.Flush();

            File.WriteAllText(path, sw.ToString());
        }

    }
}
