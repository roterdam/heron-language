#define _CRT_SECURE_NO_DEPRECATE

//#define YARD_LOGGING

#include <string>
#include <iostream>

#include "yard/yard.hpp"
#include "misc/io.hpp"

#include "jaction_grammar.hpp"
#include "heron_grammar.hpp"
#include "heron_misc.hpp"
#include "xml/yard_xml_grammar.hpp"
//#include "heron_xml_parser.hpp"

using namespace heron_grammar;

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
		Output("Eval(");
		Output(node->GetFirstChild());
		Output(")");
	}
	else if (ti == typeid(Literal))
	{
		Output("Eval(");
		Output(node);
		Output(")");
	}
	else if (ti == typeid(AnonFxn))
	{
		Node* args = node->GetFirstChild();
		Node* body = args->GetSibling();
		Output("new anon() { public Object Apply");
		OutputArgList(args);
		OutputLine(" {");
		OutputStatement(body);
		Output("} }");
	}
	else if (ti == typeid(ParanthesizedExpr))
	{
		//Node* expr = node->GetFirstChild();
		Output("(");
		OutputExpr(node);
		Output(")");
	}
	else 
	{
		printf("unhandled expression type: %s\n", ti.name());
	}
}

void OutputExpr(Node* node)
{
	// TODO: a b c .d => apply(a, b, c).d
	// TODO: a (b, c) . d => apply(a, b, c).d
	// so I have to do some grouping here.
	// I have to add an "Apply" function to the other guy.

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
		OutputStatementList(node);
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
		Output("if ((");
		OutputExpr(cond);
		Output(").equals(JATrue()))");
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
		Output("while (("); 
		OutputExpr(cond);
		OutputLine(").equals(JATrue()))");
		OutputStatement(body);
	}
	else if (ti == typeid(SwitchStatement))
	{
		Node* val = node->GetFirstChild();
		Node* cases = val->GetSibling();
		Output("Object _switch_value = ");
		Output(val);
		OutputLine(";");
		OutputLine("if (false) { }");
		while (cases != NULL)
		{
			OutputStatement(cases);
			cases = cases->GetSibling();
		}
	}
	else if (ti == typeid(CaseStatement))
	{
		Node* val = node->GetFirstChild();
		Node* body = val->GetSibling();
		Output("else if (_switch_value.equals(");
		Output(val);
		OutputLine("))");
		OutputStatement(body);
	}
	else if (ti == typeid(DefaultStatement))
	{
		Node* body = node->GetFirstChild();
		Output("else");
		OutputStatement(body);
	}
	else if (ti == typeid(ReturnStatement))
	{
		Node* expr = node->GetFirstChild();
		Output("return ");
		OutputExpr(expr);
		OutputLine(";");
	}
	else if (ti == typeid(AssignmentStatement))
	{
		Node* lvalue = node->GetFirstChild();
		Node* rvalue = lvalue->GetSibling();
		OutputExpr(lvalue);
		Output(" = ");
		OutputExpr(rvalue);
		OutputLine(";");
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

void OutputStatementList(Node* x)
{
	for (Node* child = x->GetFirstChild();
		child != NULL;
		child = child->GetSibling())
	{
		OutputStatement(child);
	}
}

template<typename T>
bool ParseString(const char* s)
{
	size_t len = strlen(s);
	Parser parser(s, s + len);
	bool b = parser.Parse<T>();

	if (!b) 
		return false;

	// Uncomment for debugging
	// OutputParseTree(parser.GetAstRoot());

	OutputStatementList(parser.GetAstRoot());
	return true;
}

void ParseFile(const char* file)
{
	char* input = ReadFile(file);
	ParseString<Program>(input);
	free(input);
}

void UnitTest(const char* x) 
{
	printf("input: %s\n", x);
	if (!ParseString<StatementList>(x))
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
	UnitTest("() => return 12");
	UnitTest("() => return 12;");
	UnitTest("(x) => return x");
	UnitTest("(x) => { return x }");
	UnitTest("(x : int) => { return x; }");
	UnitTest("(x, y) => return x + y");
	UnitTest("(x : int, y : int) => return x * y");
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

	//RunUnitTests();

	for (int i=1; i < argn; ++i) {
		ParseFile(argv[i]);
	}

	system("pause");
	return 1;
}



