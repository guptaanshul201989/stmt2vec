using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntrinsicTokenizer
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
    }
}
