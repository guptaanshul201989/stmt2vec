using System;
using System.Collections.Generic;
using System.Linq;


using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace PDGGenerator
{
    enum PDGNodeType
    {
        statement, predicate, return_statement, entry, exit, if_clause, else_clause, while_header, while_body, while_exit,
        for_header, for_body, for_exit, foreach_header, foreach_body, foreach_exit, switch_header, switch_section,
        loop_break, loop_continue, try_clause, catch_clause, summary, label
    };
    enum CDSEdgeType { FALSE, TRUE, ALLTRUE, CASE };
    class PDGNode
    {
        public int ID;
        public PDGNodeType type;
        public SyntaxReference info = null;

        public List<CDSEdge> CDEdges = new List<CDSEdge>();
        public List<CDSEdge> CFEdges = new List<CDSEdge>();

        public List<int> cfunreslit = new List<int>();
        public List<int> loopBreakUnreslit = new List<int>();
        public List<int> loopContinueUnreslit = new List<int>();

        public List<KeyValuePair<int, bool>> transferBreakList = new List<KeyValuePair<int, bool>>();
        public List<List<KeyValuePair<int, bool>>> transferContinueList = new List<List<KeyValuePair<int, bool>>>();
        public List<List<KeyValuePair<int, bool>>> transferReturnList = new List<List<KeyValuePair<int, bool>>>();

        //public List<PDGEdge> DDEdges = new List<PDGEdge>();

        public PDGNode(int i) { ID = i; }
        public PDGNode(int i, PDGNodeType t) { ID = i; type = t; }
        public override string ToString()
        {
            string result = "";
            if (type == PDGNodeType.statement)
                result = "S" + ID + ":\t" + info.GetSyntax().ToString();
            else if (type == PDGNodeType.predicate)
            {
                result = "P" + ID + ":\t" + info.GetSyntax().ToString();
            }
            else if (type == PDGNodeType.return_statement)
            {
                result = "S" + ID + ":\t" + info.GetSyntax().ToString();
            }
            else
                result = "R" + ID + "_" + type.ToString();


            return result;
        }


        public List<string> processTokens(IEnumerable<SyntaxToken> tokens)
        {
            List<string> result = new List<string>();
            foreach (var token in tokens)
            {
                var tKind = token.Kind();
                if (tKind == SyntaxKind.IdentifierToken)
                {
                    result.Add("<id>");
                    foreach (var word in SplitWord.getCamelWords(token.ToString()))
                        //foreach (var word in Program.splitWords2(token.ToString()))
                        result.Add(word);
                    result.Add("</id>");
                }
                else result.Add(tKind.ToString());
            }
            return result;
        }
        public List<string> parse()
        {
            List<string> result = new List<string>();
            if (type == PDGNodeType.statement)
            {
                result.AddRange(processTokens(info.GetSyntax().DescendantTokens()));
            }
            else if (type == PDGNodeType.predicate)
            {
                var kind = info.GetSyntax().Kind();
                if (kind == SyntaxKind.IfStatement)
                {
                    result.Add("IfKeyword");
                    result.Add("OpenParenToken");
                    var cond = ((IfStatementSyntax)info.GetSyntax()).Condition;
                    result.AddRange(processTokens(cond.DescendantTokens()));
                    result.Add("CloseParenToken");
                }
                else if (kind == SyntaxKind.WhileStatement)
                {
                    result.Add("WhileKeyword");
                    result.Add("OpenParenToken");
                    var cond = ((WhileStatementSyntax)info.GetSyntax()).Condition;
                    result.AddRange(processTokens(cond.DescendantTokens()));
                    result.Add("CloseParenToken");
                }
                else if (kind == SyntaxKind.ForStatement)
                {
                    result.Add("ForKeyword");
                    result.Add("OpenParenToken");
                    var cond = ((ForStatementSyntax)info.GetSyntax()).Condition;
                    if (cond != null)
                        result.AddRange(processTokens(cond.DescendantTokens()));
                    result.Add("CloseParenToken");
                }
                else if (kind == SyntaxKind.ForEachStatement)
                {
                    var forNode = (ForEachStatementSyntax)info.GetSyntax();
                    var bodyNode = forNode.ChildNodes().Last();

                    List<SyntaxToken> tokens = new List<SyntaxToken>();
                    int count = 0;
                    foreach (var child in forNode.DescendantTokens())
                    {
                        if (count == forNode.DescendantTokens().Count() - bodyNode.DescendantTokens().Count()) break;
                        tokens.Add(child);
                        count++;
                    }
                    result.AddRange(processTokens(tokens));
                }
            }
            else if (type == PDGNodeType.loop_break || type == PDGNodeType.loop_continue || type == PDGNodeType.return_statement)
                result.AddRange(processTokens(info.GetSyntax().DescendantTokens()));
            return result;
        }
        public List<string> processTokens2(IEnumerable<SyntaxToken> tokens)
        {
            List<string> result = new List<string>();
            foreach (var token in tokens)
            {
                var tKind = token.Kind();
                if (tKind == SyntaxKind.IdentifierToken)
                {
                    result.Add("<id>");
                    foreach (var word in SplitWord.getIntrinsicWords(token.ToString()))
                        result.Add(word);
                    result.Add("</id>");
                }
                else result.Add(tKind.ToString());
            }
            return result;
        }
        public List<string> parse2()
        {
            List<string> result = new List<string>();
            if (type == PDGNodeType.statement)
            {
                result.AddRange(processTokens2(info.GetSyntax().DescendantTokens()));
            }
            else if (type == PDGNodeType.predicate)
            {
                var kind = info.GetSyntax().Kind();
                if (kind == SyntaxKind.IfStatement)
                {
                    result.Add("IfKeyword");
                    result.Add("OpenParenToken");
                    var cond = ((IfStatementSyntax)info.GetSyntax()).Condition;
                    result.AddRange(processTokens2(cond.DescendantTokens()));
                    result.Add("CloseParenToken");
                }
                else if (kind == SyntaxKind.WhileStatement)
                {
                    result.Add("WhileKeyword");
                    result.Add("OpenParenToken");
                    var cond = ((WhileStatementSyntax)info.GetSyntax()).Condition;
                    result.AddRange(processTokens2(cond.DescendantTokens()));
                    result.Add("CloseParenToken");
                }
                else if (kind == SyntaxKind.ForStatement)
                {
                    result.Add("ForKeyword");
                    result.Add("OpenParenToken");
                    var cond = ((ForStatementSyntax)info.GetSyntax()).Condition;
                    if (cond != null)
                        result.AddRange(processTokens2(cond.DescendantTokens()));
                    result.Add("CloseParenToken");
                }
                else if (kind == SyntaxKind.ForEachStatement)
                {
                    var forNode = (ForEachStatementSyntax)info.GetSyntax();
                    var bodyNode = forNode.ChildNodes().Last();

                    List<SyntaxToken> tokens = new List<SyntaxToken>();
                    int count = 0;
                    foreach (var child in forNode.DescendantTokens())
                    {
                        if (count == forNode.DescendantTokens().Count() - bodyNode.DescendantTokens().Count()) break;
                        tokens.Add(child);
                        count++;
                    }
                    result.AddRange(processTokens2(tokens));
                }
            }
            else if (type == PDGNodeType.loop_break || type == PDGNodeType.loop_continue || type == PDGNodeType.return_statement)
                result.AddRange(processTokens2(info.GetSyntax().DescendantTokens()));
            return result;
        }
    }

    class CDSEdge
    {
        int ID;
        public CDSEdgeType type;
        public int fromID;
        public int toID;

        public CDSEdge(int i, int f, int t, CDSEdgeType tp)
        {
            ID = i;
            fromID = f;
            toID = t;
            type = tp;
        }
        public override string ToString()
        {
            string result = ID + "_(" + fromID + ", " + toID + ")";
            if (type == CDSEdgeType.FALSE) result += "->F";
            else if (type == CDSEdgeType.TRUE) result += "->T";
            else if (type == CDSEdgeType.ALLTRUE) result += "->A";
            else result += "->C";
            return result;
        }
    }
    class DDSEdge
    {
        int ID;
        public int fromID;
        public int toID;

        public DDSEdge(int i, int f, int t)
        {
            ID = i;
            fromID = f;
            toID = t;
        }
    }

    class Def
    {
        public int nodeID;
        public ISymbol variable;
        public Def(int i, ISymbol v) { nodeID = i; variable = v; }
        public override bool Equals(object obj)
        {
            Def d = obj as Def;
            if (d == null) return false;
            if (nodeID == d.nodeID && variable == d.variable)
                return true;
            else
                return false;
        }
        public override int GetHashCode()
        {
            var hash = variable.GetHashCode() + nodeID;
            return hash;
        }
    }
    class PDG
    {
        public string location;
        public string funcName;

        public Dictionary<int, PDGNode> PDNodes = new Dictionary<int, PDGNode>();

        public Dictionary<int, HashSet<int>> CFPredList = new Dictionary<int, HashSet<int>>();
        public Dictionary<int, HashSet<int>> CFSuccList = new Dictionary<int, HashSet<int>>();
        public Dictionary<int, HashSet<int>> CDPredList = new Dictionary<int, HashSet<int>>();
        public Dictionary<int, HashSet<int>> CDSuccList = new Dictionary<int, HashSet<int>>();

        public Dictionary<int, HashSet<int>> DDPredList = new Dictionary<int, HashSet<int>>();
        public Dictionary<int, HashSet<int>> DDSuccList = new Dictionary<int, HashSet<int>>();

        public PDGNode getNode(int ID)
        {
            if (PDNodes.ContainsKey(ID)) return PDNodes[ID];
            else return null;
        }
        public HashSet<int> getCDPreds(int ID)
        {
            if (CDPredList.ContainsKey(ID))
                return CDPredList[ID];
            else
                return null;
        }
        public HashSet<int> getCDSuccs(int ID)
        {
            if (CDSuccList.ContainsKey(ID))
                return CDSuccList[ID];
            else
                return null;
        }
        public HashSet<int> getCFPreds(int ID)
        {
            if (CFPredList.ContainsKey(ID))
                return CFPredList[ID];
            else
                return null;
        }
        public HashSet<int> getCFSuccs(int ID)
        {
            if (CFSuccList.ContainsKey(ID))
                return CFSuccList[ID];
            else
                return null;
        }
        public HashSet<int> getDDPreds(int ID)
        {
            if (DDPredList.ContainsKey(ID))
                return DDPredList[ID];
            else
                return null;
        }
        public HashSet<int> getDDSuccs(int ID)
        {
            if (DDSuccList.ContainsKey(ID))
                return DDSuccList[ID];
            else
                return null;
        }
        public HashSet<int> getCDSuccWithoutRegion(int ID)
        {
            var result = new HashSet<int>();
            if (CDSuccList.ContainsKey(ID))
            {
                foreach (var succ in CDSuccList[ID])
                {
                    var nodeType = getNode(succ).type;
                    if(nodeType > PDGNodeType.predicate && nodeType != PDGNodeType.return_statement && nodeType != PDGNodeType.loop_break && nodeType != PDGNodeType.loop_continue)
                    {
                        result.UnionWith(getCDSuccWithoutRegion(succ));
                    }
                    else
                        result.Add(succ);
                }
            }
            return result;
        }
        public HashSet<int> getCFSuccWithoutRegion(int ID)
        {
            var result = new HashSet<int>();
            if (CFSuccList.ContainsKey(ID))
            {
                foreach (var succ in CFSuccList[ID])
                {
                    var nodeType = getNode(succ).type;
                    if (nodeType > PDGNodeType.predicate && nodeType != PDGNodeType.return_statement && nodeType != PDGNodeType.loop_break && nodeType != PDGNodeType.loop_continue)
                        result.UnionWith(getCFSuccWithoutRegion(succ));
                    else
                        result.Add(succ);
                }
            }
            return result;
        }

        public void addNode(PDGNode node)
        {
            PDNodes.Add(node.ID, node);
        }
        public void addDDEdge(int fID, int tID)
        {
            if (!DDPredList.ContainsKey(tID))
                DDPredList.Add(tID, new HashSet<int>());
            DDPredList[tID].Add(fID);

            if (!DDSuccList.ContainsKey(fID))
                DDSuccList.Add(fID, new HashSet<int>());
            DDSuccList[fID].Add(tID);

        }
        public void addCDPredList(int key, int fID)
        {
            if (CDPredList.ContainsKey(key))
                CDPredList[key].Add(fID);
            else
            {
                HashSet<int> tSet = new HashSet<int>();
                tSet.Add(fID);
                CDPredList.Add(key, tSet);
            }
        }
        public void addCDSuccList(int key, int fID)
        {
            if (CDSuccList.ContainsKey(key))
                CDSuccList[key].Add(fID);
            else
            {
                HashSet<int> tSet = new HashSet<int>();
                tSet.Add(fID);
                CDSuccList.Add(key, tSet);
            }
        }
        public void addCFPredList(int key, int fID)
        {
            if (CFPredList.ContainsKey(key))
                CFPredList[key].Add(fID);
            else
            {
                HashSet<int> tSet = new HashSet<int>();
                tSet.Add(fID);
                CFPredList.Add(key, tSet);
            }
        }
        public void addCFSuccList(int key, int fID)
        {
            if (CFSuccList.ContainsKey(key))
                CFSuccList[key].Add(fID);
            else
            {
                HashSet<int> tSet = new HashSet<int>();
                tSet.Add(fID);
                CFSuccList.Add(key, tSet);
            }
        }
        public void initEdgeList()
        {
            var dicSort = from objeDic in PDNodes orderby objeDic.Key ascending select objeDic;
            foreach (var node in dicSort)
            {
                int key = node.Key;
                foreach (var edge in node.Value.CDEdges)
                {
                    int fID = edge.fromID;
                    int tID = edge.toID;
                    if (tID == key)
                        addCDPredList(key, fID);
                    if (fID == key)
                        addCDSuccList(key, tID);
                }
                foreach (var edge in node.Value.CFEdges)
                {
                    int fID = edge.fromID;
                    int tID = edge.toID;
                    if (tID == key)
                        addCFPredList(key, fID);
                    if (fID == key)
                        addCFSuccList(key, tID);
                }
            }
            foreach (var node in dicSort)
            {
                var nodeID = node.Key;
                //string result = nodeID + ":\t";\
                if (getNode(nodeID).type == PDGNodeType.predicate)
                {
                    foreach (var succ in getCDSuccs(nodeID))
                    {
                        addCFPredList(succ, nodeID);
                        addCFSuccList(nodeID, succ);
                    }
                }
                var children = getCDSuccs(node.Key);
                if (children != null)
                {
                    int pre = -1;
                    foreach (var child in children)
                    {
                        if (child < nodeID) continue;
                        if (pre == -1)
                        {
                            pre = child;
                            addCFPredList(child, nodeID);
                            addCFSuccList(nodeID, child);
                            continue;
                        }
                        var nodeType = getNode(pre).type;

                        if (nodeType == PDGNodeType.statement || nodeType == PDGNodeType.loop_break || nodeType == PDGNodeType.loop_continue)
                        {
                            addCFPredList(child, pre);
                            addCFSuccList(pre, child);
                        }
                        pre = child;

                    }
                }
                //Console.WriteLine(result);
            }


        }
        public override string ToString()
        {
            string result = "";
            var dicSort = from objeDic in PDNodes orderby objeDic.Key ascending select objeDic;

            foreach (var node in dicSort)
            {
                result += node.Value.ToString();
                result += "\n\tCFSucc:\t";
                if (getCFSuccs(node.Key) != null)
                    foreach (var cfsucc in getCFSuccs(node.Key))
                    {
                        result += cfsucc;
                        result += "\t";
                    }
                result += "\n\tDDSucc:\t";
                if (getDDSuccs(node.Key) != null)
                    foreach (var ddsucc in getDDSuccs(node.Key))
                    {
                        result += ddsucc;
                        result += "\t";
                    }
                result += "\n\tCDSucc:\t";
                if (getCDSuccs(node.Key) != null)
                    foreach (var cdsucc in getCDSuccs(node.Key))
                    {
                        result += cdsucc;
                        result += "\t";
                    }
                result += "\n";
                result += "\n\tCDSuccWithouRegion:\t";
                if (getCDSuccWithoutRegion(node.Key) != null)
                    foreach (var cdsucc in getCDSuccWithoutRegion(node.Key))
                    {
                        result += cdsucc;
                        result += "\t";
                    }
                result += "\n";

            }
            return result;
        }
    }



    class ConstructMethodPDG : CSharpSyntaxWalker
    {
        public PDG pdg = new PDG();

        public Stack<PDGNode> pdgStack = new Stack<PDGNode>();  //store region Node
        public Stack<PDGNode> loopStack = new Stack<PDGNode>(); //store for/foreach/while PDGNode

        public HashSet<int> exitSet = new HashSet<int>();  //store return statement ID

        public PDGNode mostRecentNode = null;
        public List<KeyValuePair<int, bool>> predictPath = new List<KeyValuePair<int, bool>>();
        public Dictionary<int, List<KeyValuePair<int, bool>>> regionTable = new Dictionary<int, List<KeyValuePair<int, bool>>>();

        public int nodeID = 0;
        public int cdsEdgeID = 0;

        public void constructCDS(SyntaxNode root) { Visit(root); }
        public void constructDDS(SyntaxNode root, SemanticModel model)
        {

            pdg.initEdgeList();

            bool changes = true;
            Dictionary<int, HashSet<Def>> IN = new Dictionary<int, HashSet<Def>>();
            Dictionary<int, HashSet<Def>> OUT = new Dictionary<int, HashSet<Def>>();


            var nodeSort = from objeDic in pdg.PDNodes orderby objeDic.Key ascending select objeDic;

            foreach (var node in nodeSort)
            {
                IN.Add(node.Key, new HashSet<Def>());
                OUT.Add(node.Key, new HashSet<Def>());
            }
            while (changes)
            {
                changes = false;
                foreach (var node in nodeSort)
                {

                    var pdgNode = node.Value;

                    if (pdgNode.type == PDGNodeType.statement)
                    {

                        var IN_CUR = IN[pdgNode.ID];
                        var IN_PRE = new HashSet<Def>(IN_CUR);
                        IN_CUR.Clear();
                        var predList = pdg.getCFPreds(pdgNode.ID);
                        if (predList != null)
                        {
                            foreach (var pred in predList)
                            {
                                IN_CUR.UnionWith(OUT[pred]);
                            }
                        }

                        var OUT_CUR = OUT[pdgNode.ID];
                        var OUT_PRE = new HashSet<Def>(OUT_CUR);
                        OUT_CUR.Clear();
                        var GEN = new HashSet<Def>();
                        var UN_KILL = new HashSet<Def>();
                        if (pdgNode.info.GetSyntax().Kind() == SyntaxKind.VariableDeclaration)
                        {
                            continue;
                        }
                        DataFlowAnalysis data = model.AnalyzeDataFlow(pdgNode.info.GetSyntax());
                        var defs = data.WrittenInside.Union(data.VariablesDeclared);
                        if (defs.Count() != 0)
                        {
                            foreach (var v in defs)
                            {
                                Def d = new Def(pdgNode.ID, v);
                                GEN.Add(d);
                                foreach (var k in IN_CUR)
                                {
                                    if (k.variable != v) UN_KILL.Add(k);
                                }
                            }
                        }
                        else
                        {
                            foreach (var k in IN_CUR) UN_KILL.Add(k);
                        }
                        OUT_CUR.UnionWith(GEN);
                        OUT_CUR.UnionWith(UN_KILL);

                        IN_PRE.SymmetricExceptWith(IN_CUR);
                        OUT_PRE.SymmetricExceptWith(OUT_CUR);
                        if (IN_PRE.Count() != 0 || OUT_PRE.Count() != 0) changes = true;
                    }
                    else
                    {
                        var OUT_CUR = OUT[pdgNode.ID];
                        var OUT_PRE = new HashSet<Def>(OUT_CUR);
                        OUT_CUR.Clear();
                        var predList = pdg.getCFPreds(pdgNode.ID);
                        if (predList != null)
                        {
                            foreach (var pred in predList)
                                OUT_CUR.UnionWith(OUT[pred]);
                        }
                        //Add the for_each iteration variable
                        if (pdgNode.info != null && pdgNode.info.GetSyntax().Kind() == SyntaxKind.ForEachStatement)
                        {
                            Def d = new Def(pdgNode.ID, model.GetDeclaredSymbol(pdgNode.info.GetSyntax()));
                            OUT_CUR.Add(d);
                        }
                        OUT_PRE.SymmetricExceptWith(OUT_CUR);
                        if (OUT_PRE.Count() != 0) changes = true;
                    }
                }


            }
            foreach (var node in nodeSort)
            {
                int nodeID = node.Key;
                var pdgNode = node.Value;
                if (pdgNode.type == PDGNodeType.statement)
                {
                    if (pdgNode.info.GetSyntax().Kind() == SyntaxKind.VariableDeclaration)
                        continue;
                    DataFlowAnalysis data = model.AnalyzeDataFlow(pdgNode.info.GetSyntax());
                    var uses = data.ReadInside;
                    if (uses.Count() != 0)
                    {
                        foreach (var def in IN[nodeID])
                        {
                            foreach (var use in uses)
                            {
                                if (use == def.variable)
                                {
                                    int fID = def.nodeID;
                                    int tID = nodeID;
                                    pdg.addDDEdge(fID, tID);
                                }
                            }
                        }
                    }

                }
                else if (pdgNode.type == PDGNodeType.predicate || pdgNode.type == PDGNodeType.return_statement)
                {
                    //if (pdgNode.info == null) continue;
                    var kind = pdgNode.info.GetSyntax().Kind();
                    SyntaxNode nodeInfo = null;
                    if (kind == SyntaxKind.IfStatement)
                        nodeInfo = ((IfStatementSyntax)pdgNode.info.GetSyntax()).Condition;
                    else if (kind == SyntaxKind.WhileStatement)
                        nodeInfo = ((WhileStatementSyntax)pdgNode.info.GetSyntax()).Condition;
                    else if (kind == SyntaxKind.ForStatement)
                        nodeInfo = ((ForStatementSyntax)pdgNode.info.GetSyntax()).Condition;
                    else if (kind == SyntaxKind.ReturnStatement || kind == SyntaxKind.YieldReturnStatement)
                        nodeInfo = pdgNode.info.GetSyntax();
                    if (nodeInfo == null) continue;
                    DataFlowAnalysis data = model.AnalyzeDataFlow(nodeInfo);
                    var uses = data.ReadInside;
                    if (uses.Count() != 0)
                    {
                        foreach (var def in OUT[nodeID])
                        {
                            foreach (var use in uses)
                            {
                                if (use == def.variable)
                                {
                                    int fID = def.nodeID;
                                    int tID = nodeID;
                                    pdg.addDDEdge(fID, tID);
                                }
                            }
                        }
                    }
                }

            }
        }

        public void addNodeToCDS(PDGNode node, CDSEdgeType type)
        {
            var aliveNode = pdgStack.Peek();
            CDSEdge e = new CDSEdge(cdsEdgeID++, aliveNode.ID, node.ID, type);
            aliveNode.CDEdges.Add(e);
            node.CDEdges.Add(e);
            return;
        }

        public void popPDGStack()
        {
            var prePeek = pdgStack.Peek();
            pdgStack.Pop();
            pdgStack.Peek().cfunreslit.AddRange(new List<int>(prePeek.cfunreslit));
            /*Console.Write(prePeek.ID + ":\t");
            foreach (var i in prePeek.cfunreslit)
                Console.Write(i+",");
            Console.WriteLine();*/
            prePeek.cfunreslit.Clear();
        }
        public void popLoopStack(int hID, int eID)
        {
            var prePeek = loopStack.Peek();
            loopStack.Pop();
            var breakList = prePeek.loopBreakUnreslit;
            var continueList = prePeek.loopContinueUnreslit;
            if (breakList.Count() != 0)
            {
                foreach (var bID in breakList)
                {
                    CDSEdge e = new CDSEdge(cdsEdgeID++, bID, eID, CDSEdgeType.ALLTRUE);
                    pdg.getNode(bID).CFEdges.Add(e);
                    pdg.getNode(eID).CFEdges.Add(e);
                }
            }
            if (continueList.Count() != 0)
            {
                foreach (var cID in continueList)
                {
                    CDSEdge e = new CDSEdge(cdsEdgeID++, cID, hID, CDSEdgeType.ALLTRUE);
                    pdg.getNode(cID).CFEdges.Add(e);
                    pdg.getNode(eID).CFEdges.Add(e);
                }
            }
            prePeek.loopBreakUnreslit.Clear();
            prePeek.loopContinueUnreslit.Clear();
        }
        public void addMostRecentNodeToUnsolved()
        {
            var aliveNode = pdgStack.Peek();
            var mType = mostRecentNode.type;
            if (mType == PDGNodeType.loop_break || mType == PDGNodeType.loop_continue || mType == PDGNodeType.return_statement)
                return;
            if (!aliveNode.cfunreslit.Contains(mostRecentNode.ID))
                aliveNode.cfunreslit.Add(mostRecentNode.ID);
        }

        public void resolveNodes(int toID)
        {
            foreach (var fromID in pdgStack.Peek().cfunreslit)
            {
                CDSEdge e = new CDSEdge(cdsEdgeID++, fromID, toID, CDSEdgeType.ALLTRUE);
                pdg.getNode(fromID).CFEdges.Add(e);
                pdg.getNode(toID).CFEdges.Add(e);
            }
            pdgStack.Peek().cfunreslit.Clear();
        }
        public void resolveReturnNodes(int toID)
        {
            foreach (var fromID in exitSet)
            {
                CDSEdge e = new CDSEdge(cdsEdgeID++, fromID, toID, CDSEdgeType.ALLTRUE);
                pdg.getNode(fromID).CFEdges.Add(e);
                pdg.getNode(toID).CFEdges.Add(e);
            }
            exitSet.Clear();
        }
        public int getEnclosingPredictPath()
        {
            int top = predictPath.Count() - 1;
            return predictPath.ElementAt(top).Key;
        }

        public List<KeyValuePair<int, bool>> copyPredictPath()
        {
            List<KeyValuePair<int, bool>> result = new List<KeyValuePair<int, bool>>();
            foreach (var p in predictPath)
                result.Add(new KeyValuePair<int, bool>(p.Key, p.Value));
            return result;
        }

        /***********************VISTIT THE SYNTAX TREE NODE*************************/
        public override void Visit(SyntaxNode node)
        {
            string kind = node.Kind().ToString();
            //if(kind.EndsWith("Statement"))
            //Console.WriteLine(kind);
            base.Visit(node);
        }
        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            PDGNode entry = new PDGNode(nodeID++, PDGNodeType.entry);
            entry.info = node.GetReference();
            //Console.WriteLine(node.Identifier.ToString());
            mostRecentNode = entry;
            regionTable.Add(entry.ID, copyPredictPath());
            pdgStack.Push(entry);

            base.VisitMethodDeclaration(node);

            pdg.addNode(entry);
            PDGNode exit = new PDGNode(nodeID++, PDGNodeType.exit);
            addNodeToCDS(exit, CDSEdgeType.ALLTRUE);
            mostRecentNode = exit;
            pdg.addNode(exit);
            resolveNodes(exit.ID);
            resolveReturnNodes(exit.ID);
            //Console.WriteLine("Exit");
        }
        public override void VisitIfStatement(IfStatementSyntax node)
        {
            //if-else-start Node Visit
            PDGNode pNode = new PDGNode(nodeID++, PDGNodeType.predicate);
            pNode.info = node.GetReference();
            pdg.addNode(pNode);
            resolveNodes(pNode.ID);
            addNodeToCDS(pNode, CDSEdgeType.ALLTRUE);
            mostRecentNode = pNode;
            pdgStack.Push(pNode);
            //Console.WriteLine(pNode);

            //if-caluse Node Visit 

            predictPath.Add(new KeyValuePair<int, bool>(pdgStack.Peek().ID, true));

            PDGNode ifNode = new PDGNode(nodeID++, PDGNodeType.if_clause);
            addNodeToCDS(ifNode, CDSEdgeType.TRUE);
            mostRecentNode = ifNode;
            regionTable.Add(ifNode.ID, copyPredictPath());
            pdgStack.Push(ifNode);

            base.VisitIfStatement(node);

            pdg.addNode(ifNode);

            if (node.Else == null)
            { // No else-clause 
                addMostRecentNodeToUnsolved();
                popPDGStack();

                predictPath.RemoveAt(predictPath.Count() - 1);
                predictPath.Add(new KeyValuePair<int, bool>(pdgStack.Peek().ID, false));

                PDGNode elseNode = new PDGNode(nodeID++, PDGNodeType.else_clause);
                addNodeToCDS(elseNode, CDSEdgeType.FALSE);
                mostRecentNode = elseNode;
                regionTable.Add(elseNode.ID, copyPredictPath());
                pdgStack.Push(elseNode);
                pdg.addNode(elseNode);
                addMostRecentNodeToUnsolved();
                predictPath.RemoveAt(predictPath.Count() - 1);
                popPDGStack();
            }
            //if-else-end Node Visit
            popPDGStack();

        }
        public override void VisitElseClause(ElseClauseSyntax node)
        {
            //Pop the if-clause region node
            addMostRecentNodeToUnsolved();
            popPDGStack();

            predictPath.RemoveAt(predictPath.Count() - 1);
            predictPath.Add(new KeyValuePair<int, bool>(pdgStack.Peek().ID, false));

            PDGNode elseNode = new PDGNode(nodeID++, PDGNodeType.else_clause);
            addNodeToCDS(elseNode, CDSEdgeType.FALSE);
            mostRecentNode = elseNode;
            regionTable.Add(elseNode.ID, copyPredictPath());
            pdgStack.Push(elseNode);


            //Console.WriteLine(elseNode);
            base.VisitElseClause(node);
            pdg.addNode(elseNode);
            addMostRecentNodeToUnsolved();
            predictPath.RemoveAt(predictPath.Count() - 1);

            popPDGStack();
        }
        public override void VisitSwitchStatement(SwitchStatementSyntax node)
        {

            PDGNode pNode = new PDGNode(nodeID++, PDGNodeType.predicate);
            pNode.info = node.GetReference();
            pdg.addNode(pNode);
            resolveNodes(pNode.ID);
            addNodeToCDS(pNode, CDSEdgeType.ALLTRUE);
            mostRecentNode = pNode;
            pdgStack.Push(pNode);

            foreach (var section in node.Sections)
            {
                PDGNode sectionNode = new PDGNode(nodeID++, PDGNodeType.switch_section);
                sectionNode.info = section.GetReference();
                pdg.addNode(sectionNode);
                addNodeToCDS(sectionNode, CDSEdgeType.CASE);
                mostRecentNode = sectionNode;
                pdgStack.Push(sectionNode);

                base.Visit(section);
                addMostRecentNodeToUnsolved();
                popPDGStack();
            }
            popPDGStack();
            //base.VisitSwitchStatement(node);
        }
        public override void VisitTryStatement(TryStatementSyntax node)
        {
            PDGNode pNode = new PDGNode(nodeID++, PDGNodeType.predicate);
            pNode.info = node.GetReference();
            pdg.addNode(pNode);
            resolveNodes(pNode.ID);
            addNodeToCDS(pNode, CDSEdgeType.ALLTRUE);
            mostRecentNode = pNode;
            pdgStack.Push(pNode);

            //create try clause
            PDGNode tryNode = new PDGNode(nodeID++, PDGNodeType.try_clause);
            pdg.addNode(tryNode);
            addNodeToCDS(tryNode, CDSEdgeType.TRUE);
            mostRecentNode = tryNode;
            pdgStack.Push(tryNode);
            base.Visit(node.Block);
            addMostRecentNodeToUnsolved();
            popPDGStack();

            //create catch clause
            foreach (var catch_clause in node.Catches)
            {
                PDGNode catchNode = new PDGNode(nodeID++, PDGNodeType.catch_clause);
                pdg.addNode(catchNode);
                addNodeToCDS(catchNode, CDSEdgeType.FALSE);
                mostRecentNode = catchNode;
                pdgStack.Push(catchNode);

                base.Visit(catch_clause);
                addMostRecentNodeToUnsolved();
                popPDGStack();
            }

            popPDGStack();
        }
        public override void VisitWhileStatement(WhileStatementSyntax node)
        {
            PDGNode whileHeaderNode = new PDGNode(nodeID++, PDGNodeType.while_header);
            addNodeToCDS(whileHeaderNode, CDSEdgeType.ALLTRUE);
            pdg.addNode(whileHeaderNode);
            resolveNodes(whileHeaderNode.ID);
            mostRecentNode = whileHeaderNode;
            regionTable.Add(whileHeaderNode.ID, copyPredictPath());
            pdgStack.Push(whileHeaderNode);
            loopStack.Push(whileHeaderNode);

            PDGNode pNode = new PDGNode(nodeID++, PDGNodeType.predicate);
            pNode.info = node.GetReference();
            addNodeToCDS(pNode, CDSEdgeType.ALLTRUE);
            pdg.addNode(pNode);
            mostRecentNode = pNode;

            pdgStack.Push(pNode);

            predictPath.Add(new KeyValuePair<int, bool>(pNode.ID, true));

            PDGNode whileBodyNode = new PDGNode(nodeID++, PDGNodeType.while_body);
            addNodeToCDS(whileBodyNode, CDSEdgeType.TRUE);
            pdg.addNode(whileBodyNode);
            //connect while-body and while-header
            CDSEdge e = new CDSEdge(cdsEdgeID++, whileBodyNode.ID, whileHeaderNode.ID, CDSEdgeType.ALLTRUE);
            whileBodyNode.CDEdges.Add(e);
            whileHeaderNode.CDEdges.Add(e);

            mostRecentNode = whileBodyNode;
            regionTable.Add(whileBodyNode.ID, copyPredictPath());
            pdgStack.Push(whileBodyNode);

            //Console.WriteLine(pNode);
            base.VisitWhileStatement(node);
            addMostRecentNodeToUnsolved();
            popPDGStack();  //pop while-body

            //resolve all nodes to while-header
            resolveNodes(whileHeaderNode.ID);

            predictPath.RemoveAt(predictPath.Count() - 1);
            predictPath.Add(new KeyValuePair<int, bool>(pNode.ID, false));

            PDGNode whileExitNode = new PDGNode(nodeID++, PDGNodeType.while_exit);
            mostRecentNode = whileExitNode;
            addNodeToCDS(whileExitNode, CDSEdgeType.FALSE);
            pdg.addNode(whileExitNode);
            addMostRecentNodeToUnsolved();
            //pdgStack.Peek().cfunreslit.AddRange(new List<int>());
            regionTable.Add(whileExitNode.ID, copyPredictPath());
            popPDGStack(); // pop while-predicate
            popPDGStack(); // pop while-header

            popLoopStack(whileHeaderNode.ID, whileExitNode.ID);
        }
        public override void VisitDoStatement(DoStatementSyntax node)
        {
            PDGNode whileHeaderNode = new PDGNode(nodeID++, PDGNodeType.while_header);
            addNodeToCDS(whileHeaderNode, CDSEdgeType.ALLTRUE);
            pdg.addNode(whileHeaderNode);
            resolveNodes(whileHeaderNode.ID);
            mostRecentNode = whileHeaderNode;
            regionTable.Add(whileHeaderNode.ID, copyPredictPath());
            pdgStack.Push(whileHeaderNode);
            loopStack.Push(whileHeaderNode);

            base.VisitDoStatement(node);

            PDGNode pNode = new PDGNode(nodeID++, PDGNodeType.predicate);
            pNode.info = node.GetReference();
            addNodeToCDS(pNode, CDSEdgeType.ALLTRUE);
            pdg.addNode(pNode);
            mostRecentNode = pNode;
            CDSEdge e = new CDSEdge(cdsEdgeID++, pNode.ID, whileHeaderNode.ID, CDSEdgeType.TRUE);
            pNode.CFEdges.Add(e);
            whileHeaderNode.CFEdges.Add(e);

            pdgStack.Push(pNode);
            //addMostRecentNodeToUnsolved();

            PDGNode whileExitNode = new PDGNode(nodeID++, PDGNodeType.while_exit);
            mostRecentNode = whileExitNode;
            addNodeToCDS(whileExitNode, CDSEdgeType.FALSE);
            pdg.addNode(whileExitNode);
            addMostRecentNodeToUnsolved();

            popPDGStack(); // pop while-predicate
            popPDGStack(); // pop while-header

            popLoopStack(whileHeaderNode.ID, whileExitNode.ID);
        }
        public override void VisitForStatement(ForStatementSyntax node)
        {

            //add init nodes to Region
            foreach (var init in node.Initializers)
            {
                //Console.WriteLine(init.ToString());
                PDGNode initNode = new PDGNode(nodeID++, PDGNodeType.statement);
                initNode.info = init.GetReference();
                addNodeToCDS(initNode, CDSEdgeType.ALLTRUE);
                pdg.addNode(initNode);
            }
            if (node.Declaration != null)
            {
                PDGNode decNode = new PDGNode(nodeID++, PDGNodeType.statement);
                decNode.info = node.Declaration.GetReference();
                addNodeToCDS(decNode, CDSEdgeType.ALLTRUE);
                pdg.addNode(decNode);
            }


            //create the for header Region
            PDGNode forHeaderNode = new PDGNode(nodeID++, PDGNodeType.for_header);
            addNodeToCDS(forHeaderNode, CDSEdgeType.ALLTRUE);
            pdg.addNode(forHeaderNode);
            resolveNodes(forHeaderNode.ID);
            mostRecentNode = forHeaderNode;
            regionTable.Add(forHeaderNode.ID, copyPredictPath());
            pdgStack.Push(forHeaderNode);
            loopStack.Push(forHeaderNode);

            PDGNode pNode = new PDGNode(nodeID++, PDGNodeType.predicate);
            pNode.info = node.GetReference();
            addNodeToCDS(pNode, CDSEdgeType.ALLTRUE);
            pdg.addNode(pNode);
            mostRecentNode = pNode;
            pdgStack.Push(pNode);

            PDGNode forBodyNode = new PDGNode(nodeID++, PDGNodeType.for_body);
            addNodeToCDS(forBodyNode, CDSEdgeType.TRUE);
            pdg.addNode(forBodyNode);
            CDSEdge e = new CDSEdge(cdsEdgeID++, forBodyNode.ID, forHeaderNode.ID, CDSEdgeType.ALLTRUE);
            forBodyNode.CDEdges.Add(e);
            forHeaderNode.CDEdges.Add(e);

            mostRecentNode = forBodyNode;
            regionTable.Add(forBodyNode.ID, copyPredictPath());
            pdgStack.Push(forBodyNode);

            //Visit for body Node
            base.VisitForStatement(node);


            bool first_statement = true;
            foreach (var incre in node.Incrementors)
            {
                PDGNode increNode = new PDGNode(nodeID++, PDGNodeType.statement);
                increNode.info = incre.GetReference();
                addNodeToCDS(increNode, CDSEdgeType.ALLTRUE);
                pdg.addNode(increNode);
                if (first_statement)
                {
                    resolveNodes(increNode.ID);
                    first_statement = false;
                }
                mostRecentNode = increNode;
            }
            addMostRecentNodeToUnsolved();
            popPDGStack();  //pop for-body
            resolveNodes(forHeaderNode.ID);

            PDGNode forExitNode = new PDGNode(nodeID++, PDGNodeType.for_exit);
            mostRecentNode = forExitNode;
            addNodeToCDS(forExitNode, CDSEdgeType.FALSE);
            pdg.addNode(forExitNode);
            addMostRecentNodeToUnsolved();
            //pdgStack.Peek().cfunreslit.AddRange(new List<int>());
            regionTable.Add(forExitNode.ID, copyPredictPath());
            popPDGStack(); // pop for-predicate
            popPDGStack(); // pop for-header

            popLoopStack(forHeaderNode.ID, forExitNode.ID);




        }
        public override void VisitForEachStatement(ForEachStatementSyntax node)
        {
            //Console.WriteLine(node.Expression.ToString());

            PDGNode foreachHeaderNode = new PDGNode(nodeID++, PDGNodeType.foreach_header);
            addNodeToCDS(foreachHeaderNode, CDSEdgeType.ALLTRUE);
            pdg.addNode(foreachHeaderNode);
            resolveNodes(foreachHeaderNode.ID);
            mostRecentNode = foreachHeaderNode;
            regionTable.Add(foreachHeaderNode.ID, copyPredictPath());
            pdgStack.Push(foreachHeaderNode);
            loopStack.Push(foreachHeaderNode);

            PDGNode pNode = new PDGNode(nodeID++, PDGNodeType.predicate);

            pNode.info = node.GetReference();   //forstatement save
            addNodeToCDS(pNode, CDSEdgeType.ALLTRUE);
            mostRecentNode = pNode;
            pdgStack.Push(pNode);

            predictPath.Add(new KeyValuePair<int, bool>(pNode.ID, true));

            PDGNode foreachBodyNode = new PDGNode(nodeID++, PDGNodeType.foreach_body);
            addNodeToCDS(foreachBodyNode, CDSEdgeType.TRUE);
            CDSEdge e = new CDSEdge(cdsEdgeID++, foreachBodyNode.ID, foreachHeaderNode.ID, CDSEdgeType.ALLTRUE);
            foreachBodyNode.CDEdges.Add(e);
            foreachHeaderNode.CDEdges.Add(e);
            mostRecentNode = foreachBodyNode;
            regionTable.Add(foreachBodyNode.ID, copyPredictPath());
            pdgStack.Push(foreachBodyNode);

            base.VisitForEachStatement(node);
            addMostRecentNodeToUnsolved();
            popPDGStack();  //pop while-body

            //resolve all nodes to while-header
            resolveNodes(foreachHeaderNode.ID);

            predictPath.RemoveAt(predictPath.Count() - 1);
            predictPath.Add(new KeyValuePair<int, bool>(pNode.ID, false));

            PDGNode foreachExitNode = new PDGNode(nodeID++, PDGNodeType.foreach_exit);
            mostRecentNode = foreachExitNode;
            addNodeToCDS(foreachExitNode, CDSEdgeType.FALSE);
            addMostRecentNodeToUnsolved();
            //pdgStack.Peek().cfunreslit.AddRange(new List<int>());
            regionTable.Add(foreachExitNode.ID, copyPredictPath());
            popPDGStack(); // pop while-predicate
            popPDGStack(); // pop while-header


            pdg.addNode(pNode);
            pdg.addNode(foreachBodyNode);
            pdg.addNode(foreachExitNode);

            popLoopStack(foreachHeaderNode.ID, foreachExitNode.ID);
        }
        public override void VisitReturnStatement(ReturnStatementSyntax node)
        {
            PDGNode rNode = new PDGNode(nodeID++, PDGNodeType.return_statement);
            rNode.info = node.GetReference();
            addNodeToCDS(rNode, CDSEdgeType.ALLTRUE);
            pdg.addNode(rNode);
            resolveNodes(rNode.ID);
            mostRecentNode = rNode;

            exitSet.Add(rNode.ID);
            base.VisitReturnStatement(node);
        }
        public override void VisitYieldStatement(YieldStatementSyntax node)
        {
            if (node.Kind() == SyntaxKind.YieldBreakStatement)
            {
                PDGNode bNode = new PDGNode(nodeID++, PDGNodeType.loop_break);
                bNode.info = node.GetReference();
                addNodeToCDS(bNode, CDSEdgeType.ALLTRUE);
                pdg.addNode(bNode);
                resolveNodes(bNode.ID);
                mostRecentNode = bNode;
                exitSet.Add(bNode.ID);
                //loopStack.Peek().loopBreakUnreslit.Add(bNode.ID);
            }
            else if (node.Kind() == SyntaxKind.YieldReturnStatement)
            {
                PDGNode rNode = new PDGNode(nodeID++, PDGNodeType.return_statement);
                rNode.info = node.GetReference();
                addNodeToCDS(rNode, CDSEdgeType.ALLTRUE);
                pdg.addNode(rNode);
                resolveNodes(rNode.ID);
                mostRecentNode = rNode;

                exitSet.Add(rNode.ID);
            }
            base.VisitYieldStatement(node);
        }
        public override void VisitBreakStatement(BreakStatementSyntax node)
        {
            PDGNode bNode = new PDGNode(nodeID++, PDGNodeType.loop_break);
            bNode.info = node.GetReference();
            addNodeToCDS(bNode, CDSEdgeType.ALLTRUE);
            pdg.addNode(bNode);
            resolveNodes(bNode.ID);
            mostRecentNode = bNode;

            if (loopStack.Count() != 0)
                loopStack.Peek().loopBreakUnreslit.Add(bNode.ID);

            base.VisitBreakStatement(node);
        }
        public override void VisitContinueStatement(ContinueStatementSyntax node)
        {
            PDGNode cNode = new PDGNode(nodeID++, PDGNodeType.loop_continue);
            cNode.info = node.GetReference();
            addNodeToCDS(cNode, CDSEdgeType.ALLTRUE);
            pdg.addNode(cNode);
            resolveNodes(cNode.ID);
            mostRecentNode = cNode;

            loopStack.Peek().loopContinueUnreslit.Add(cNode.ID);
            base.VisitContinueStatement(node);
        }
        public override void VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            PDGNode sNode = new PDGNode(nodeID++, PDGNodeType.statement);
            sNode.info = node.GetReference();
            addNodeToCDS(sNode, CDSEdgeType.ALLTRUE);
            pdg.addNode(sNode);
            resolveNodes(sNode.ID);
            mostRecentNode = sNode;


            //Console.WriteLine(sNode);
            base.VisitLocalDeclarationStatement(node);

        }
        public override void VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            PDGNode sNode = new PDGNode(nodeID++, PDGNodeType.statement);
            sNode.info = node.GetReference();
            addNodeToCDS(sNode, CDSEdgeType.ALLTRUE);
            pdg.addNode(sNode);
            resolveNodes(sNode.ID);
            mostRecentNode = sNode;


            //Console.WriteLine(sNode);
            //base.VisitExpressionStatement(node);

        }
        public override void VisitThrowStatement(ThrowStatementSyntax node)
        {
            PDGNode sNode = new PDGNode(nodeID++, PDGNodeType.statement);
            sNode.info = node.GetReference();
            addNodeToCDS(sNode, CDSEdgeType.ALLTRUE);
            pdg.addNode(sNode);
            resolveNodes(sNode.ID);
            mostRecentNode = sNode;
        }

        public PDG getPDG()
        {
            /*string res = "";
            foreach (var r in regionTable) {
                res += r.Key;
                foreach (var p in r.Value) {
                    res += "\t" + p.Key;
                    if (p.Value) res += "T";
                    else res += "F";
                }
                res += "\n";
            }
            Console.WriteLine(res);*/
            return pdg;
        }
    }
}
