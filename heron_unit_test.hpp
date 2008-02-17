/*
	Authour: Christopher Diggins
	License: MIT Licence 1.0
	
	Heron unit tests
*/

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

