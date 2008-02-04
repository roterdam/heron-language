using namespace std;
using namespace yard;

typedef TreeBuildingParser<char> Parser;
typedef Parser::Tree Tree;
typedef Tree::AbstractNode Node;

void OutputExpr(Node* node);
void OutputStatement(Node* node);
void OutputStatementList(Node* node);

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
	return string(pNode->GetFirstToken(), pNode->GetLastToken());
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
	std::cout << s;
}

void Output(Node* pNode)
{
	Output(NodeToStr(pNode));
}

void OutputLine()
{
	std::cout << endl;
}

void OutputLine(string s)
{
	std::cout << s << endl;
}

void OutputLine(Node* node)
{
	assert(node != NULL);
	OutputLine(NodeToStr(node));
}

