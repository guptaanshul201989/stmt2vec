using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDGGenerator
{
    class SplitWord
    {

        public static Dictionary<string, List<string>> camelStyleWords = new Dictionary<string, List<string>>();
        public static Dictionary<string, List<string>> intrinsicWords = new Dictionary<string, List<string>>();
        public static Dictionary<string, int> split_words = new Dictionary<string, int>();
        public static void init(string dir)
        {
            string splitDir = dir + "\\ExprData\\SplitWord";
            var camelLines = File.ReadAllLines(splitDir + "\\words_camel.txt");
            foreach (var line in camelLines)
            {
                var split_line = line.Split('\t');
                List<string> words = new List<string>();
                for (int i = 1; i < split_line.Count(); i++)
                {
                    words.Add(split_line[i]);
                }
                camelStyleWords.Add(split_line[0], words);
            }

            var intrinsicLines = File.ReadAllLines(splitDir + "\\words_intrinsic.txt");
            foreach (var line in intrinsicLines)
            {
                var split_line = line.Split('\t');
                List<string> words = new List<string>();
                for (int i = 1; i < split_line.Count(); i++)
                {
                    words.Add(split_line[i]);
                }
                intrinsicWords.Add(split_line[0], words);
            }



        }
        public static List<string> getCamelWords(string word) { return camelStyleWords[word]; }
        public static List<string> getIntrinsicWords(string word) { return intrinsicWords[word]; }

    } 
}
