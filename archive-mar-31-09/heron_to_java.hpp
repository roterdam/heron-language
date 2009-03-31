/*
	Authour: Christopher Diggins
	License: MIT Licence 1.0
	
	Translates an AST generated from Heron code into Java
*/

using namespace heron_grammar;

void OutputSym(Node* x)
{
	assert(x->is<Sym>());

	// HACK: this is just awful.
	// I need to remap operators to values.
	std::string s = NodeToStr(x->GetFirstChild());
	if (s.compare("Real") == 0)
		Output("double"); else
		Output(s);
}

void OutputDotSym(Node* x)
{
	assert(x->is<DotSym>());
	Output(".");
	OutputRawNode(x->GetFirstChild());
}

void OutputLiteral(Node* x)
{
	assert(x->is<Literal>());
	OutputRawNode(x->GetFirstChild());
}

void OutputType(Node* x, std::string def)
{
	if (x == NULL) {
		Output(def);
		return;
	}
	if (!x->is<TypeExpr>())
		assert(false);
	Node* child = x->GetFirstChild();
	if (child->is<Sym>())
		OutputSym(child);
	else if (child->is<Literal>())
		OutputLiteral(child);
	else 
		printf("unrecognized type expression %\n", child->GetRuleTypeInfo().name());
	child = child->GetSibling();
	if (child != NULL)
	{
		Output("<");
		child = child->GetFirstChild();
		OutputType(child, "???");
		child = child->GetSibling();
		for (;child != NULL; child = child->GetSibling()) {
			Output(", ");
			OutputType(child, "???");
		}
		Output(">");
	}
}

void OutputArg(Node* x)
{
	assert(x->is<Arg>());
	Node* name = x->GetFirstTypedChild<Sym>();
	Node* type = x->GetFirstTypedChild<TypeExpr>();
	Output("final ");
	OutputType(type, "Object");
	Output(" ");
	OutputSym(name);
}

void OutputArgList(Node* x)
{
	assert(x->is<ArgList>());
	Output("(");
	for (Node* child = x->GetFirstChild(); child != NULL; child = child->GetSibling())
	{
		OutputArg(child);
		if (child->GetSibling() != NULL)
			Output(", ");
	}
	Output(")");
}

void OutputFunction(std::string name, std::string className, Node* arglist, Node* type, Node* code, bool bStatic)
{
	if (bStatic)
		Output("static ");

	if (name.compare("constructor") == 0)
	{
		Output("public ");
		Output(className);
		OutputArgList(arglist);
		OutputLine("{");	
		//OutputLine("instances.add(this);");
		OutputStatement(code);
		OutputLine("}");
	}
	else if (code == NULL) 
	{
		// TODO: also mark classes as abstract
		Output("public abstract ");
		OutputType(type, "void"); 
		Output(" ");
		Output(name);
		OutputArgList(arglist);
		OutputLine(";");
	}
	else
	{
		Output("public ");
		OutputType(type, "void"); 
		Output(" ");
		Output(name);
		OutputArgList(arglist);
		OutputLine("{");	
		OutputStatement(code);
		OutputLine("}");
	}
}

void OutputParams(Node* node) 
{
	Output("(");
	Node* child = node->GetFirstChild();
	while (child != NULL) {
		OutputExpr(child);
		child = child->GetSibling();
		if (child != NULL)
			Output(", ");
	}
	Output(")");
}

void OutputArgListAsVars(Node* node)
{
	assert(node->is<ArgList>());
	Node* arg = node->GetFirstTypedChild<Arg>();
	for (int n = 0; arg != NULL; ++n, arg = arg->GetTypedSibling<Arg>())
	{
		Node* type = arg->GetFirstTypedChild<TypeExpr>();
		Node* sym = arg->GetFirstTypedChild<Sym>();
		OutputType(type, "Object");
		Output(" ");
		OutputSym(sym);
		Output(" = (");
		OutputType(type, "Object");
		Output(")_args.get(");
		OutputInt(n);
		OutputLine(");");
	}
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
		OutputType(type, "Object");
		OutputParams(params);
	}
	else if (ti == typeid(DelExpr))
	{
		Node* obj = node->GetFirstChild();
		Output("delete ");
		OutputExpr(obj);
	}
	else if (ti == typeid(Sym))
	{
		OutputSym(node);
	}
	else if (ti == typeid(Literal))
	{
		OutputLiteral(node);
	}
	else if (ti == typeid(AnonFxn))
	{
		// TODO: support multiple return results
		Node* args = node->GetFirstChild();
		Node* body = args->GetSibling();
		OutputLine("new AnonymousFunction() {");
		OutputLine("public Object apply(Collection<Object> _args) {");
		OutputArgListAsVars(args);
		OutputStatement(body);
		OutputLine("}");
		OutputLine("}");
	}
	else if (ti == typeid(ParanthesizedExpr))
	{
		Output("(");
		OutputExpr(node);
		Output(")");
	}
	else if (ti == typeid(DotSym))
	{
		OutputDotSym(node);
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
		Node* sym = node->GetFirstTypedChild<Sym>();
		Node* type = node->GetFirstTypedChild<TypeExpr>();
		Node* expr = node->GetFirstTypedChild<Expr>();
		OutputType(type, "Object");
		Output(" ");
		OutputSym(sym);
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
		OutputExpr(cond);
		Output(")");
		OutputStatement(onTrue);
		if (onFalse != NULL) {
			OutputLine("else");
			OutputStatement(onFalse);
		}	
	}
	else if (ti == typeid(ForEachStatement))
	{
		Node* sym = node->GetFirstTypedChild<Sym>();
		Node* coll = node->GetFirstTypedChild<Expr>();		
		Node* type = node->GetFirstTypedChild<TypeExpr>();
		Node* body = node->GetFirstTypedChild<CodeBlock>();
		Output("for (");
		OutputType(type, "Object");
		Output(" ");
		OutputSym(sym);
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
		Output("Object _switch_value = ");
		OutputSym(val);
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
		OutputSym(val);
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
	else if (ti == typeid(EmptyStatement))
	{
		OutputLine(";");
	}
	else 
	{		
		printf("unhandled statement type: %s\n", ti.name());
		throw 0;
	}
}

void OutputStatementList(Node* x)
{
	x->ForEach(OutputStatement);
}

void OutputAttribute(Node* x, bool bStatic) 
{
	Node* name = x->GetFirstChild();
	Node* type = name->GetSibling();
	Output("public ");
	if (bStatic) Output("static ");
	OutputType(type, "Object");
	Output(" ");
	OutputSym(name);	
	OutputLine(";");
}

void OutputOperation(Node* x, std::string className, bool bStatic)
{
	assert(x->is<Operation>());
	Node* name = x->GetFirstTypedChild<Sym>();
	Node* arglist = x->GetFirstTypedChild<ArgList>();
	Node* type = x->GetFirstTypedChild<TypeExpr>();
	Node* statement = x->GetFirstTypedChild<CodeBlock>();
	OutputFunction(NodeToStr(name), className, arglist, type, statement, bStatic);
}

void OutputOperations(Node* x, bool bStatic)
{
	assert(x->is<Class>() || x->is<Domain>());
	std::string name = NodeToStr(x->GetFirstChild()->GetFirstChild());

	Node* op = x->GetFirstTypedChild<Operation>();
	while (op != NULL) {
		OutputOperation(op, name, bStatic);
		op = op->GetTypedSibling<Operation>();
	}
}

void OutputAttributes(Node* x, bool bStatic)
{
	assert(x->is<Class>() || x->is<Domain>());

	Node* op = x->GetFirstTypedChild<Attribute>();
	while (op != NULL) {
		OutputAttribute(op, bStatic);
		op = op->GetTypedSibling<Attribute>();
	}
}

void OutputLink(Node* x) 
{
	// TODO:
}

void OutputInv(Node* x)
{
	// TODO:
}

int StateToInt(Node* x) {
	std::string name = NodeToStr(x->GetFirstChild()->GetFirstChild());
	if (name.compare("initial") == 0) {
		return 0;
	}
	return (int)x;
}

void OutputStateProc(Node* x) 
{
	assert(x->is<State>());
	Node* name = x->GetFirstTypedChild<Sym>();
	Node* arg = x->GetFirstTypedChild<Arg>();
	Node* code = x->GetFirstTypedChild<CodeBlock>();
	
	// There is no procedure associated with the "initial" state
	std::string sName = NodeToStr(name->GetFirstChild());
	if (sName.compare("initial") == 0)
	{
		assert(arg == NULL);
		assert(code == NULL);
		return; 
	}
	else
	{
		assert(arg != NULL);
	}

	Output("public void ");
	Output(sName);	
	OutputLine("(HeronSignal __event__) {");
	OutputType(arg->GetFirstTypedChild<TypeExpr>(), "Object");
	Output(" ");
	OutputSym(arg->GetFirstTypedChild<Sym>());
	Output(" = (");
	OutputType(arg->GetFirstTypedChild<TypeExpr>(), "Object");
	OutputLine(")__event__.data;");
	Output("__state__ = ");
	OutputInt(StateToInt(x));
	OutputLine(";");

	if (code != NULL)
		OutputStatement(code);	

	OutputLine("}");
}

void OutputTransition(Node* state, Node* transition) 
{
	assert(state->is<State>());
	assert(transition->is<Transition>());
	Node* signalType = transition->GetFirstChild();
	Node* stateName = signalType->GetSibling();
	Output("else if ((__state__ == ");
	printf("%d", StateToInt(state));
	Output(") && (signal.data instanceof ");
	OutputSym(signalType);
	OutputLine(")) {");

	// Call the procedure associate with the state transition
	OutputSym(stateName);
	OutputLine("(signal);");
	OutputLine("}");
}

void OutputDispatch(Node* x)
{
	assert(x->is<Class>());
	
	OutputLine("public void onSignal(HeronSignal signal) {");
	OutputLine("if (false) {");
	OutputLine("}");
	Node* state = x->GetFirstTypedChild<State>();
	while (state != NULL) {
		Node* transitions = state->GetFirstTypedChild<TransitionTable>();
		Node* transition = transitions->GetFirstTypedChild<Transition>();
		while (transition != NULL) {
			OutputTransition(state, transition);
			transition = transition->GetTypedSibling<Transition>();
		}
		state = state->GetTypedSibling<State>();
	}
	OutputLine("}");
}

void OutputClass(Node* x) 
{
	assert(x->is<Class>());
	Node* child = x->GetFirstChild();

	std::string name = NodeToStr(child->GetFirstChild());
	
	// used to output classes to new files
	RedirectOutput(name.c_str());

	Output("public class ");
	Output(name);
	Node* subclasses = x->GetFirstTypedChild<Subclasses>();
	if (subclasses != NULL && subclasses->GetNumChildren() > 0) {
		if (subclasses->GetNumChildren() > 1) {
			printf("multiple super-classes not currently supported\n");
			assert(false);
		}
		Node* subclass = subclasses->GetFirstChild();
		Node* subclassName = subclass->GetFirstChild();
		Output(" extends "); 
		OutputSym(subclassName);
		OutputLine("{");
	}
	else {
		OutputLine(" extends HeronObject {");
	}

	// static instances field
	//Output("public static Collection<");
	//OutputSym(child);
	//Output("> instances = new Collection<");
	//Output(name);
	//OutputLine(">();");
	
	// default empty constructor
	//Output("public ");
	//Output(name);
	//OutputLine("() {");
	//OutputLine("instances.add(this);");
	//OutputLine("}");

	OutputLine("// attributes");
	OutputAttributes(x, false);
	OutputLine("// operations");
	OutputOperations(x, false);
	OutputLine("// state entry procedures");
	x->ForEachTyped<State>(OutputStateProc);

	OutputLine("// dispatch function");
	OutputDispatch(x);

	OutputLine("}");
}

void OutputDomainImport(Node* x) {
	// TEMP: does nothing for now
}

void OutputDomainAttr(Node* x) {
	// TEMP: does nothing for now
}

void OutputDomainOp(Node* x) {
	// TEMP: does nothing for now
}

void OutputDomain(Node* x)
{
	assert(x->is<Domain>());
	
	x->ForEachTyped<Class>(OutputClass);
	
	Node* child = x->GetFirstChild();

	std::string name = NodeToStr(child->GetFirstChild());
	RedirectOutput(name.c_str());

	Output("public class ");
	Output(name);
	OutputLine(" extends HeronApplication {");

	OutputLine("// applet entry point");
	OutputLine("public void init() {");
	OutputLine("super.init();");
	OutputLine("initialize();");
	OutputLine("}");

	OutputLine("// application entry point");
	OutputLine("public static void main(String s[]) {");
	Output("baseMain(new ");
	Output(name);
	OutputLine("());");
	OutputLine("initialize();");
	OutputLine("theApp.dispatchNextSignal();");
	OutputLine("}");

	OutputLine("// attributes");
	OutputAttributes(x, true);

	OutputLine("// operations");
	OutputOperations(x, true);

	// TEMP: comment out these lines if we want to output java files on new classes
	//OutputLine("// classes");
	//x->ForEachTyped<Class>(OutputClass);

	OutputLine("}");

	fflush(stdout);
	fclose(stdout);
}

void OutputProgram(Node* x)
{
	x->ForEachTyped<Domain>(OutputDomain);
}

