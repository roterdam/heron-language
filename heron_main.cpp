#define _CRT_SECURE_NO_DEPRECATE

//#define YARD_LOGGING

#include "yard/yard.hpp"
#include "heron_grammar.hpp"

#include "misc/io.hpp"

#include <string>
#include <iostream>

using namespace yard;
using namespace heron_grammar;
using namespace std;

typedef TreeBuildingParser<char> Parser;
typedef Parser::Tree Tree;
typedef Tree::AbstractNode Node;

void OutputExpr(Node* node);
void OutputStatement(Node* node);

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

void OutputArg(Node* node)
{
	Node* name = node->GetFirstChild();
	Node* type = node->GetSibling();
	if (type != NULL)
	{
		Output(type);
		Output(" ");
	}
	else
	{
		Output("Object ");
	}
	Output(name);
}

void OutputArgList(Node* node)
{
	Output("(");
	for (Node* child = node->GetFirstChild(); child != NULL; child = child->GetSibling())
	{
		OutputArg(child);
		if (child->GetSibling() != NULL)
			Output(", ");
	}
	Output(")");
}

void OutputSimpleExpr(Node* node)
{
	assert(node != NULL);
	const type_info& ti = node->GetRuleTypeInfo();

	if (ti == typeid(NewExpr))
	{
		Node* type = node->GetFirstChild();
		Node* params = type->GetSibling();
		Output("new ");
		OutputExpr(type);
		Output("(");
		OutputExpr(params);
		Output(")");
	}
	else if (ti == typeid(DelExpr))
	{
		Node* obj = node->GetFirstChild();
		Output("delete ");
		OutputExpr(obj);
	}
	else if (ti == typeid(Sym))
	{
		Output(node->GetFirstChild());
	}
	else if (ti == typeid(Literal))
	{
		Output("Object(");
		Output(node);
		Output(")");
	}
	else if (ti == typeid(AnonFxn))
	{
		Node* args = node->GetFirstChild();
		Node* body = args->GetSibling();
		Output("new anon() { public Object Apply");
		OutputArgList(args);
		OutputLine("");
		OutputStatement(body);
		Output("}");
	}
	else if (ti == typeid(Paranthesized<Expr>))
	{
		Node* expr = node->GetFirstChild();
		Output("(");
		Output(expr);
		Output(")");
	}
	else 
	{
		printf("unhandled expression type: %s\n", ti.name());
	}
}

void OutputExpr(Node* node)
{
	for (Node* child = node->GetFirstChild(); child != NULL; child = child->GetSibling())
	{
		OutputSimpleExpr(child);
		Output(" ");
	}
}

void OutputStatement(Node* node)
{
	assert(node != NULL);
	const type_info& ti = node->GetRuleTypeInfo();

	if (ti == typeid(CodeBlock))
	{		
		OutputLine("{");
		OutputStatement(node->GetFirstChild());
		OutputLine("}");
	}
	else if (ti == typeid(VarDecl))
	{
		Node* sym = node->GetFirstChild();
		Node* expr = sym->GetSibling();
		Output("Object ");
		Output(sym);
		if (expr != NULL) {
			Output(" = ");
			OutputExpr(expr);
		}
		OutputLine(";");
	}
	else if (ti == typeid(IfStatement))
	{
		Node* cond = node->GetFirstChild();
		Node* onTrue = cond->GetSibling();
		Node* onFalse = onTrue->GetSibling();
		Output("if (");
		OutputExpr(onTrue);
		Output(")");
		OutputStatement(onTrue);
		if (onFalse != NULL) {
			OutputLine("else");
			OutputStatement(onFalse);
		}	
	}
	else if (ti == typeid(ForEachStatement))
	{
		Node* sym = node->GetFirstChild();
		Node* coll = sym->GetSibling();
		Node* body = coll->GetSibling();
		Output("for (Object ");
		Output(sym);
		Output(" : ");
		OutputExpr(coll);
		OutputLine(")");
		OutputStatement(body);
	}
	else if (ti == typeid(WhileStatement))
	{
		Node* cond = node->GetFirstChild();
		Node* body = cond->GetSibling();
		Output("while ("); 
		OutputExpr(cond);
		OutputLine(")");
		OutputStatement(body);
	}
	else if (ti == typeid(SwitchStatement))
	{
		Node* val = node->GetFirstChild();
		Node* cases = val->GetSibling();
		Output("JActionObject _switch_value = ");
		Output(val);
		OutputLine(";");
		Output("if (false) { }");
	}
	else if (ti == typeid(CaseStatement))
	{
		Node* val = node->GetFirstChild();
		Node* body = val->GetSibling();
		Output("else if (_switch_value.equals(");
		Output(val);
		OutputLine(")");
		Output(body);
	}
	else if (ti == typeid(DefaultStatement))
	{
		Node* body = node->GetFirstChild();
		Output("else");
		Output(body);
	}
	else if (ti == typeid(ReturnStatement))
	{
		Output("return ");
		OutputExpr(node);
		OutputLine(";");
	}
	else if (ti == typeid(AssignmentStatement))
	{
		Node* lvalue = node->GetFirstChild();
		Node* rvalue = lvalue->GetSibling();
		OutputExpr(lvalue);
		Output(" = ");
		OutputExpr(rvalue);
	}
	else if (ti == typeid(ExprStatement))
	{
		Node* expr = node->GetFirstChild();
		OutputExpr(expr);
		OutputLine(";");
	}
	else 
	{		
		printf("unhandled statement type: %s\n", ti.name());
	}
}

void OutputStatements(Node* x)
{
	for (Node* child = x->GetFirstChild();
		child != NULL;
		child = child->GetSibling())
	{
		OutputStatement(child);
	}
}

bool ParseString(const char* input)
{
	size_t len = strlen(input);
	Parser parser(input, input + len);
	bool b = parser.Parse<Program>();

	if (!b) 
		return false;

	// Uncomment for debugging
	//OutputParseTree(parser.GetAstRoot());

	OutputStatements(parser.GetAstRoot());
	return true;
}

void ParseFile(const char* file)
{
	char* input = ReadFile(file);
	ParseString(input);
	free(input);
}

void UnitTest(const char* x) 
{
	printf("input: %s\n", x);
	if (!ParseString(x))
		printf("\nfailed to parse\n");
	printf("\n");
}

void RunUnitTests() {
	UnitTest("42");
	UnitTest("hello");
	UnitTest("\"hello\"");
	UnitTest("'q'");
	UnitTest("1 2 3");
	UnitTest("1 , 2");
	UnitTest("1,2,3");
	UnitTest("(1,2,3)");
	UnitTest("()");
	UnitTest("((1,2),3)");
	UnitTest("(1)");
	UnitTest("(((1)))");
	UnitTest("2 . 3");
	UnitTest("2.3");
	UnitTest("ab.cd.f()");
	UnitTest(",");
	UnitTest("x+y");
	UnitTest("x + y");
	UnitTest("x = 3");
	UnitTest("x.a = 4");
	UnitTest("x . a = 4");
	UnitTest("!x");
	UnitTest("x()");
	UnitTest("f(1)");
	UnitTest("f(a,b.c)");
	UnitTest("var x");
	UnitTest("var x = 12");
	UnitTest("() => 12");
	UnitTest("return 12");
	UnitTest("new x()");
	UnitTest("delete x");
	UnitTest("a; b");
	UnitTest("a; b;");
	UnitTest("{ }");
	UnitTest("{ a }");
	UnitTest("{ a; }");
	UnitTest("{ a; b }");
	UnitTest("{ a; b; }");
	UnitTest("if (b) { x }");
	UnitTest("if (b) { x } else { y }");
	UnitTest("switch (x) { }");
	UnitTest("switch (x) { case (a) { a; } }");
	UnitTest("switch (x) { default { b; } }");
	UnitTest("switch (x) { case (a) { a; } case(b) { b; } }");
	UnitTest("switch (x) { case (a) { a; } case(b) { b; } default { c } }");
	UnitTest("while (b) { x }");
	UnitTest("foreach (x in y) { a }");
}

int main(int argn, char* argv[])
{	
	printf("running %s\n", argv[0]);

	RunUnitTests();

	for (int i=1; i < argn; ++i) {
		ParseFile(argv[i]);
	}

	return 1;
}



