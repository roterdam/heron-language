/*
	Authour: Christopher Diggins
	License: MIT Licence 1.0
	
	Utility functions
*/

using namespace std;
using namespace yard;

typedef TreeBuildingParser<char> Parser;
typedef Parser::Tree Tree;
typedef Tree::AbstractNode Node;

void OutputExpr(Node* node);
void OutputStatement(Node* node);
void OutputStatementList(Node* node);

int nIndent = 0;
const char* outputPath = "";

void RedirectOutput(const char* x)
{
	fflush(stdout);
	char buffer[255];
	sprintf(buffer, "%s\\%s.java", outputPath, x);
	freopen(buffer, "w", stdout);
}

void Unimplemented()
{
	assert(false);
}

void AstError(const char* x)
{
	perror(x);
	assert(false);
}

string NodeToStr(Node* pNode)
{
	string ret = string(pNode->GetFirstToken(), pNode->GetLastToken());
	for (size_t i=0; i < ret.length(); ++i) {
		if (ret[i] == ' ')
			ret[i] = '#';
	}
	return ret;
}

void OutputParseTree(Node* pNode, int n = 0)
{
	for (int i=0; i < n; ++i) printf("  ");
	printf("%s\n", pNode->GetRuleTypeInfo().name());
	for (Node* p = pNode->GetFirstChild(); p != NULL; p = p->GetSibling())
		OutputParseTree(p, n + 1);
}

void Output(const string& s)
{
	for (size_t i=0; i < s.length(); ++i) {
		if (s[i] == '{')
			++nIndent;
		else if (s[i] == '}')
			--nIndent;
	}
	printf("%s", s.c_str());
}

void OutputInt(int n)
{
	printf("%d", n);
}

void OutputRawNode(Node* pNode)
{
	if (pNode == NULL)
		printf("missing node!\n");
	Output(NodeToStr(pNode));
}

void OutputLine()
{
	printf("\n");
	for (int i=0; i < nIndent; ++i)
		printf("  ");
}

void OutputLine(string s)
{
	Output(s);
	OutputLine();
}

void OutputLine(Node* node)
{
	assert(node != NULL);
	OutputLine(NodeToStr(node));
}

