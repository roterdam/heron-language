Parser parser;

template<typename T>
Node* ParseString(const char* s)
{
	size_t len = strlen(s);	
	bool b = parser.Parse<T>(s, s + len);

	if (!b) 
 		return NULL;

	// Uncomment for debugging
	// OutputParseTree(parser.GetAstRoot());

	return parser.GetAstRoot();
}
