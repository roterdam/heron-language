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

void OutputLine(Node* pNode)
{
	OutputLine(NodeToStr(pNode));
}

void ProcessExpression(Node* node)
{
	Unimplemented();
}

void ProcessStatement(Node* node)
{
	const type_info& ti = node->GetRuleTypeInfo();

	if (ti == typeid(CodeBlock))
	{		
		OutputLine("{");
		for (Node* child = node->GetFirstChild(); 
				child != NULL; 
				child = child->GetSibling())
			ProcessStatement(child);
		OutputLine("}");
	}
	else if (ti == typeid(WriteVarOrProp))
	{
		Node* q = node->GetFirstTypedChild<Qualifier>();
		Node* id = node->GetFirstTypedChild<Ident>();
		Node* init = node->GetFirstTypedChild<Initializer>();
	}
	else if (ti == typeid(VarDecl))
	{
		Node* id = node->GetFirstTypedChild<Ident>();
		Node* init = node->GetFirstTypedChild<Initializer>();
	}
	else if (ti == typeid(IfStatement))
	{
		Node* cond = node->GetFirstChild();
		Node* onTrue = cond->GetSibling();
		Node* onFalse = onTrue->GetSibling();
	}
	else if (ti == typeid(ForEachStatement))
	{
		Unimplemented();
	}
	else if (ti == typeid(WhileStatement))
	{
		Unimplemented();
	}
	else if (ti == typeid(ExprStatement))
	{
		Unimplemented();
	}
	else 
	{
		Unimplemented();
	}
}


int main(int argn, char* argv[])
{	
	printf("running %s\n", argv[0]);

	//RunUnitTests();

	for (int i=1; i < argn; ++i)
	{
		char* input = ReadFile(argv[i]);
		bool b = true;
		size_t len = strlen(input);
		int cnt = 1000; 
		for (int j=0; j < cnt; ++j)
		{
			Parser parser(input, input + len);
			b &= parser.Parse<StatementList>();
		}
		
		if (b)
			printf("successfullly parsed file %s %d times\n", argv[i], cnt); else
			printf("failed to parse file %s\n", argv[i]);

		free(input);
	}
	return 1;
}



