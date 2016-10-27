Introduction

The current IntrinsicTokenizer folder is a container of the following key components of the stmt2vec technique. We will refactor the components into different modules soon.

         1. An implementation of the Program Dependence Graph (PDG) generator.
         
         2. The IntrinsicTokenizer as described in the stmt2vec paper.


Installation

         1. Download the code and open the solution with Visual Studio 2015.
         
         2. Install Roslyn(http://roslyn.codeplex.com/) and Json(http://www.newtonsoft.com/json) following the instructions below:
         
                         Menu: Tools > Nuget Package Manager > Package Manager Console
                         Command: Install-Package Miscrosoft.CodeAnalysis
                         Command: Install-Package newtonsoft.json
         3. Unzip the input data at ExprData\sourcecode.
         
         4. Run the Visual Studio project.


Folder structure

            ExprData\sourcecode: (input data) 10 subject projects.
            ExprData\GenData: (output data) PDG-based representation of source code.
            ExprData\FilePath: (intermediate data) training set and testing set splitting.
            ExprData\SplitWord: (intermediate data) sub-tokens split by the IntrinsicTokenizer.
