using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
namespace PDGGenerator
{
    class Common
    {
        public static List<string> scanCSharpSln(string dir)
        {

            List<string> slns = new List<string>();
            Stack<string> folders = new Stack<string>();
            folders.Push(dir);
            List<string> Files = new List<string>();
            string curFolder;

            StringBuilder unsln = new StringBuilder();
            while (folders.Count > 0)
            {
                curFolder = folders.Peek();
                folders.Pop();
                foreach (string dirPath in Directory.GetDirectories(curFolder))
                    folders.Push(dirPath);

                foreach (string slnPath in Directory.GetFiles(curFolder, "*.sln"))
                {
                    slns.Add(slnPath);
                }

            }

            return slns;
        }
        public static void initTrainTestFile(string rootDir)
        {
            string programDir = rootDir + "\\ExprData\\sourcecode";
            string fileDir = rootDir + "\\ExprData\\FilePath";
            if (!Directory.Exists(fileDir)) Directory.CreateDirectory(fileDir);
            foreach (var proPath in Directory.GetDirectories(programDir))
            {
                
                string proDir = fileDir + "\\" + proPath.Split('\\').Last();
                if (!Directory.Exists(proDir)) Directory.CreateDirectory(proDir);

                HashSet<string> paths = new HashSet<string>();
                StringBuilder train = new StringBuilder();
                StringBuilder test = new StringBuilder();
                int count_1 = 0;
                int count_2 = 0;
                int count_3 = 0;
                Console.WriteLine("Split train and test data of " + proPath.Split('\\').Last() + "...");
                Random r = new Random();
                foreach (var slnPath in scanCSharpSln(proPath))
                {
                    var msWorkspace = MSBuildWorkspace.Create();
                    var sol = msWorkspace.OpenSolutionAsync(slnPath).Result;
                    foreach (var pro in sol.Projects)
                    {
                        foreach (var doc in pro.Documents)
                        {
                            if (paths.Contains(doc.FilePath))
                            {
                                count_3++; continue;
                            }
                            paths.Add(doc.FilePath);
                            int val = r.Next(0, 99);
                            if (val < 70)
                            {
                                train.AppendLine(doc.FilePath);
                                count_1++;
                            }
                            else
                            {
                                test.AppendLine(doc.FilePath);
                                count_2++;
                            }
                        }
                    }
                }
                //Console.WriteLine(count_1 + "\t" + count_2 + "\t" + count_3);
                File.WriteAllText(proDir + "\\trainPath.txt", train.ToString());
                File.WriteAllText(proDir + "\\testPath.txt", test.ToString());
                //break;
            }

        }
    }
}
