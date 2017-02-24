Introduction

The stmt2vec_datageneration folder is a container of the following key modules of the stmt2vec technique.
	
	 1. A Tokenizer for splitting boundless compound tokens as described in the stmt2vec paper.

         2. An implementation of the Program Dependence Graph (PDG) generator.
         
         


Installation

         1. Download the code and open the 2 solutions with Visual Studio 2015.
         
         2. Install Roslyn(http://roslyn.codeplex.com/) and Json(http://www.newtonsoft.com/json) following the instructions below:
         
                         Menu: Tools > Nuget Package Manager > Package Manager Console
                         Command: Install-Package Miscrosoft.CodeAnalysis
                         Command: Install-Package newtonsoft.json
         3. Unzip the input data at ExprData\sourcecode.
         
         4. Run the IntrinsicTokenizer project with Visual Studio 15 to generate the tokens split by the camel style and intrinsic style.
	 
	 5. Run the PDGGenerator project with Visual Studio 15 to generate the PDG data of source code.


Folder structure

            ExprData\sourcecode: (input data) 10 subject projects.
            ExprData\GenData: (output data) PDG-based representation of source code.
            ExprData\FilePath: (intermediate data) training set and testing set splitting.
            ExprData\SplitWord: (intermediate data) sub-tokens split by the IntrinsicTokenizer.
