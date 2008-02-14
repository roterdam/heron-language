using namespace heron_grammar;

void OutputSym(Node* x)
{
	assert(x->is<Sym>());
	OutputRawNode(x->GetFirstChild());
}

void OutputDotSym(Node* x)
{
	assert(x->is<DotSym>());
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
		Output("Object");
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
		OutputType(child);
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

void OutputFunction(Node* name, Node* arglist, Node* type, Node* code)
{
	if (code == NULL) 
	{
		Output("abstract ");
		OutputType(type); 
		Output(" ");
		OutputSym(name);
		OutputArgList(arglist);
		OutputLine(";");
	}
	else
	{
		OutputType(type); 
		Output(" ");
		OutputSym(name);
		OutputArgList(arglist);
		OutputLine("{");	
		OutputStatement(code);
		OutputLine("}");
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
		OutputSym(node);
		Output(")");
	}
	else if (ti == typeid(Literal))
	{
		Output("Eval(");
		OutputLiteral(node);
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
		Node* sym = node->GetFirstChild();
		Node* expr = sym->GetSibling();
		Output("Object ");
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
		Node* type = node->GetFirstTypedChild<TypeDecl>();
		Node* body = node->GetFirstTypedChild<CodeBlock>();
		Output("for (");
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

void OutputAttr(Node* x) 
{
	Node* name = x->GetFirstChild();
	Node* type = name->GetSibling();
	OutputType(type);
	Output(" ");
	OutputSym(name);	
	OutputLine(";");
}

void OutputOp(Node* x)
{
	Node* name = x->GetFirstTypedChild<Sym>();
	Node* arglist = x->GetFirstTypedChild<ArgList>();
	Node* type = x->GetFirstTypedChild<TypeExpr>();
	Node* statement = x->GetFirstTypedChild<CodeBlock>();
	OutputFunction(name, arglist, type, statement);
}

void OutputState(Node* x) 
{
	// TODO:
}

void OutputLink(Node* x) 
{
	// TODO:
}

void OutputInv(Node* x)
{
	// TODO:
}

void OutputClassElement(Node* x)
{
	assert(x != NULL);
	const type_info& ti = x->GetRuleTypeInfo();

	if (ti == typeid(Attribute))
		OutputAttr(x);
	else if (ti == typeid(Operation))
		OutputOp(x);
	else if (ti == typeid(State))
		OutputState(x);
	else if (ti == typeid(Link))
		OutputLink(x);
	else if (ti == typeid(Invariant))
		OutputInv(x);
	else  
		printf("Unrecognized class element %s\n", ti.name());
}

void OutputClass(Node* x) 
{
	assert(x->is<Class>());
	Node* child = x->GetFirstChild();
	Output("class ");
	OutputSym(child);
	OutputLine(" {");
	for (child = child->GetSibling(); child != NULL; child = child->GetSibling())
		OutputClassElement(child);
	OutputLine("}");
}

void OutputDomainImport(Node* x) {
	// TEMP: does nothing
}

void OutputDomainAttr(Node* x) {
	// TEMP: does nothing	
}

void OutputDomainOp(Node* x) {
	// TEMP: does nothing
}

void OutputDomainElement(Node* x)
{
	assert(x != NULL);
	const type_info& ti = x->GetRuleTypeInfo();

	if (ti == typeid(Attribute))
		OutputDomainAttr(x);
	else if (ti == typeid(Operation))
		OutputDomainOp(x);
	else if (ti == typeid(Import))
		OutputDomainImport(x);
	else if (ti == typeid(Class))
		OutputClass(x);
	else  
		printf("Unrecognized domain element %s\n", ti.name());
}

void OutputDomain(Node* x)
{
	assert(x->is<Domain>());
	Node* child = x->GetFirstChild();
	Output("package ");
	OutputSym(child);
	OutputLine(";");	
	
	// imports
	for (child = child->GetSibling(); child != NULL; child = child->GetSibling())
		OutputDomainElement(child);
}

void OutputProgram(Node* x)
{
	x->ForEach(OutputDomain);
}

