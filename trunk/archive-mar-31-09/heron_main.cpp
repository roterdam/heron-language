/*
	Authour: Christopher Diggins
	License: MIT Licence 1.0
*/

#define _CRT_SECURE_NO_DEPRECATE

//#define YARD_LOGGING

#include <string>
#include <iostream>

#include "yard/yard.hpp"
#include "yard/yard_io.hpp"

#include "jaction_grammar.hpp"
#include "heron_grammar.hpp"
#include "heron_misc.hpp"
#include "heron_parser.hpp"
#include "heron_unit_tests.hpp"
#include "xml/yard_xml_grammar.hpp"
#include "heron_to_java.hpp"

using namespace heron_grammar;

int main(int argn, char* argv[])
{	
	printf("Heron to Java Compiler version 0.2\n");
	printf("written by Christopher Diggins\n");
	printf("licensed under the MIT license 1.0\n");
	
	if (argn != 3)
	{
		printf("Usage\n\n");
		printf("heron <outputfolder> <inputfile>\n\n");
		return 1;
	}

	printf("running %s\n", argv[0]);

	RunUnitTests();

	outputPath = argv[1];

	char* input = ReadFile(argv[2]);
	Node* tree = ParseString<Program>(input);

	if (tree != NULL) {
		OutputProgram(tree);
	}
	else {
		getchar();
	}

	free(input);
	fflush(stdout);
	fclose(stdout);

	//char buffer[255];
	//sprintf(buffer, "javac %s", argv[1]);
	//system(buffer);
	
	return 0;
}




