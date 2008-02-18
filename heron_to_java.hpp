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
		Output("double");
	else
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

void OutputType(Node* x)
{
	if (x == NULL) {
		Output("void");
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
		OutputType(child);
		child = child->GetSibling();
		for (;child != NULL; child = child->GetSibling()) {
			Output(", ");
			OutputType(child);
		}
		Output(">");
	}
}

void OutputArg(Node* x)
{
	assert(x->is<Arg>());
	Node* name = x->GetFirstTypedChild<Sym>();
	Node* type = x->GetFirstTypedChild<TypeExpr>();
	if (type == NULL)
		Output("Object"); 
	else
		OutputType(type);
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
		OutputStatement(code);
		OutputLine("}");
	}
	else if (code == NULL) 
	{
		// TODO: also mark classes as abstract
		Output("public abstract ");
		OutputType(type); 
		Output(" ");
		Output(name);
		OutputArgList(arglist);
		OutputLine(";");
	}
	else
	{
		Output("public ");
		OutputType(type); 
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
		if (type == NULL)
			Output("Object");
		else
			OutputType(type);
		Output(" ");
		OutputSym(sym);
		Output(" = (");
		if (type == NULL)
			Output("Object");
		else
			OutputType(type);
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
		OutputType(type);
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
		OutputLine("public Object Apply(Collection<Object> _args) {");
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
		
		if (type == NULL) {
			Output("Object ");
		}
		else {
			OutputType(type);
			Output(" ");
		}

		OutputSym(sym);
		if (expr != NULL) {
			Output(" = ");
			OutputExpr(expr);
		}
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
		Node* sym = node->GetFirstTypedChild<Sym>();
		Node* coll = node->GetFirstTypedChild<Expr>();		
		Node* type = node->GetFirstTypedChild<TypeExpr>();
		Node* body = node->GetFirstTypedChild<CodeBlock>();
		Output("for (");
		if (type == NULL)
			Output("Object");
		else
			OutputType(type);
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
	}
	else if (ti == typeid(EmptyStatement))
	{
		OutputLine(";");
	}
	else 
	{		
		printf("unhandled statement type: %s\n", ti.name());
	}
}

void OutputStatementList(Node* x)
{
	x->ForEach(OutputStatement);
}

void OutputAttribute(Node* x) 
{
	Node* name = x->GetFirstChild();
	Node* type = name->GetSibling();
	OutputType(type);
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

void OutputLink(Node* x) 
{
	// TODO:
}

void OutputInv(Node* x)
{
	// TODO:
}

int StateToInt(Node* x) {
	return (int)x;
}

void OutputStateProc(Node* x) 
{
	assert(x->is<State>());
	Node* name = x->GetFirstTypedChild<Sym>();
	Node* arg = x->GetFirstTypedChild<Arg>();
	Node* code = x->GetFirstTypedChild<CodeBlock>();
	Output("public void ");
	OutputSym(name);
	Output("(");
	OutputArg(arg);
	OutputLine(") {");
	Output("__state__ = ");
	OutputInt(StateToInt(x));
	OutputLine(";");
	OutputStatement(code);	
	OutputLine("}");
}

void OutputClass(Node* x) 
{
	assert(x->is<Class>());
	Node* child = x->GetFirstChild();

	std::string name = NodeToStr(child->GetFirstChild());
	RedirectOutput(name.c_str());

	Output("public class ");
	Output(name);
	OutputLine(" extends HeronObject {");

	// static instances field
	Output("public static Collection<");
	OutputSym(child);
	Output("> instances = new Collection<");
	Output(name);
	OutputLine(">();");
	
	// constructor
	Output("public ");
	Output(name);
	OutputLine("() {");
	OutputLine("instances.add(this);");
	OutputLine("}");

	OutputLine("// attributes");
	x->ForEachTyped<Attribute>(OutputAttribute);
	OutputLine("// operations");
	OutputOperations(x, false);
	OutputLine("// state entry procedures");
	x->ForEachTyped<State>(OutputStateProc);
	OutputLine("}");
	fflush(stdout);
	fclose(stdout);
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

	OutputLine("// main entry point");
	OutputLine("public static void main(String s[]) {");
	Output("baseMain(new ");
	Output(name);
	OutputLine("());");
	Output("initialize();");
	OutputLine("}");

	// TODO: output domain attributes 
	// OutputLine("// attributes");
	// OutputAttributes(x, true);

	OutputLine("// operations");
	OutputOperations(x, true);

	OutputLine("}");

	fflush(stdout);
	fclose(stdout);
}

void OutputTransition(int nState, Node* state, Node* transition) 
{
	assert(state->is<State>());
	assert(transition->is<Transition>());
	Node* signalType = transition->GetFirstChild();
	Node* stateName = signalType->GetSibling();
	Output("else if ((__state__ == ");
	printf("%d", nState);
	Output("&& signal.type.equals(");
	OutputSym(signalType);
	OutputLine(".getClass<Object>())) {");

	// Call the procedure associate with the state transition
	OutputSym(stateName);
	Output("(signal);");
	OutputLine("}");
}

void OutputDispatch(Node* x)
{
	assert(x->is<Class>());
	
	OutputLine("public void onSignal(HeronSignal signal) {");
	OutputLine("if (false) {");
	OutputLine("}");
	Node* state = x->GetFirstTypedChild<State>();
	int nState = 0;
	while (state != NULL) {
		Node* transitions = state->GetFirstTypedChild<Transition>();
		Node* transition = transitions->GetFirstChild();
		while (transition != NULL) {
			OutputTransition(nState, state, transition);
		}
		state = state->GetTypedSibling<State>();
		++nState;
	}
	OutputLine("}");
}

void OutputProgram(Node* x)
{
	x->ForEachTyped<Domain>(OutputDomain);
}

