/*
	Authour: Christopher Diggins
	License: MIT Licence 1.0
	
	Heron unit tests
*/

template<typename T>
void UnitTest(const char* x) 
{	
	if (!ParseString<T>(x)) {
		printf("FAILED");
	}
	else {
		printf("PASSED");
	}
	printf(" unit test: %s\n", x);	
}

void RunUnitTests() {
	UnitTest<heron_grammar::Expr>("42");
	UnitTest<heron_grammar::Expr>("hello");
	UnitTest<heron_grammar::Expr>("\"hello\"");
	UnitTest<heron_grammar::Expr>("'q'");
	UnitTest<heron_grammar::Expr>("1 2 3");
	UnitTest<heron_grammar::Expr>("1 , 2");
	UnitTest<heron_grammar::Expr>("1,2,3");
	UnitTest<heron_grammar::Expr>("(1,2,3)");
	UnitTest<heron_grammar::Expr>("()");
	UnitTest<heron_grammar::Expr>("((1,2),3)");
	UnitTest<heron_grammar::Expr>("(1)");
	UnitTest<heron_grammar::Expr>("(((1)))");
	UnitTest<heron_grammar::Expr>("2 . 3");
	UnitTest<heron_grammar::Expr>("2.3");
	UnitTest<heron_grammar::Expr>("ab.cd.f()");
	UnitTest<heron_grammar::Expr>(",");
	UnitTest<heron_grammar::Expr>("a b");
	UnitTest<heron_grammar::Expr>("a b ,");
	UnitTest<heron_grammar::Expr>("x+y");
	UnitTest<heron_grammar::Expr>("x + y");
	UnitTest<heron_grammar::Expr>("x = 3");
	UnitTest<heron_grammar::Expr>("x.a = 4");
	UnitTest<heron_grammar::Expr>("x . a = 4");
	UnitTest<heron_grammar::Expr>("!x");
	UnitTest<heron_grammar::Expr>("x()");
	UnitTest<heron_grammar::Expr>("f(1)");
	UnitTest<heron_grammar::Expr>("f(a,b.c)");
	UnitTest<heron_grammar::Expr>("var x");
	UnitTest<heron_grammar::Expr>("var x = 12");
	UnitTest<heron_grammar::Expr>("() => { return 12; }");
	UnitTest<heron_grammar::Expr>("(x) => { return x; }");
	UnitTest<heron_grammar::Expr>("(x : int) => { return x; }");
	UnitTest<heron_grammar::Expr>("(x, y) => { return x + y; }");
	UnitTest<heron_grammar::Expr>("(x : int, y : int) => { return x * y; }");
	UnitTest<heron_grammar::Expr>("new x()");
	UnitTest<heron_grammar::Expr>("delete x");
	UnitTest<heron_grammar::Expr>("-12");
	UnitTest<heron_grammar::Expr>("-(12)");
	UnitTest<heron_grammar::Expr>("-(1 + 3)");
	UnitTest<heron_grammar::Expr>("(-(1-2))");
	UnitTest<heron_grammar::Expr>("(-(1-2),x + 4)");
	UnitTest<heron_grammar::Expr>("new Point(-(width / 2), (length / 2))");

	UnitTest<heron_grammar::Statement>(";");
	UnitTest<heron_grammar::Statement>(";;;");
	UnitTest<heron_grammar::Statement>("return 12;");
	UnitTest<heron_grammar::Statement>("a; b;");
	UnitTest<heron_grammar::Statement>("a; b;");
	UnitTest<heron_grammar::Statement>("{ }");
	UnitTest<heron_grammar::Statement>("{ a; }");
	UnitTest<heron_grammar::Statement>("{ a; b; }");
	UnitTest<heron_grammar::Statement>("if (b) { x; }");
	UnitTest<heron_grammar::Statement>("if (b) { x; } else { y; }");
	UnitTest<heron_grammar::Statement>("switch (x) { }");
	UnitTest<heron_grammar::Statement>("switch (x) { case (a) { a; } }");
	UnitTest<heron_grammar::Statement>("switch (x) { default { b; } }");
	UnitTest<heron_grammar::Statement>("switch (x) { case (a) { a; } case(b) { b; } }");
	UnitTest<heron_grammar::Statement>("switch (x) { case (a) { a; } case(b) { b; } default { c; } }");
	UnitTest<heron_grammar::Statement>("while (b) { a; }");
	UnitTest<heron_grammar::Statement>("foreach (x in y) { a; }");
	UnitTest<heron_grammar::Statement>("foreach (x : X in y) { a; }");
	UnitTest<heron_grammar::Statement>("var pt1 : Point;");
	UnitTest<heron_grammar::Statement>("var pt1 : Point = new Point();");
	UnitTest<heron_grammar::Statement>("pt1 = new Point(-(width / 2), (length / 2));");
	UnitTest<heron_grammar::Statement>("pt1 = Point(-(width / 2), (length / 2));");
	UnitTest<heron_grammar::Statement>("var pt1 : Point = new Point(-(width / 2), (length / 2));");
}

