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


int main(int argn, char* argv[])
{	
	if (argn != 3)
	{
		printf("Usage\n\n");
		printf("heron <outputpath> <inputfile>\n\n");
		return 1;
	}

	printf("running %s\n", argv[0]);

	//RunUnitTests();

	freopen(argv[1], "w", stdout);

	printf("public class Output extends HeronBaseApplication {\n");
	for (int i=1; i < argn; ++i) {
		ParseFile(argv[i]);
	}
	printf("}\n");
	fflush(stdout);
	fclose(stdout);

	char buffer[255];
	sprintf(buffer, "javac %s", argv[1]);
	system(buffer);
	
	return 0;
}




