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
#include "heron_to_java.hpp"

using namespace heron_grammar;

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

	OutputProgram(parser.GetAstRoot());
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

	// TODO: output standard output into a file.
	// then try to compile the file in Java.
	// then find bugs and fix them. 
	// Figure out what links are.
	// Figure out how to deal with signals in Java.
	// The simplest is a big state switch diagram.
	// OnEvent(Object signal) { }; 
	// Note: I can't avoid the fact that I am probably going to need to use threads.
	// Every object is going to be have to registered as a listener probably.
	// There is an event dispatcher. 
	// Another possibility to bundle stateFlag with eventDispatch.
	// Note that every state 

	return 1;
}



