void OutputArg(Node* node)
{
	Node* name = node->GetFirstChild();
	Node* type = node->GetSibling();
	if (type != NULL)
	{
		Output(type);
		Output(" ");
	}
	else
	{
		Output("Object ");
	}
	Output(name);
}

void OutputArgList(Node* node)
{
	Output("(");
	for (Node* child = node->GetFirstChild(); child != NULL; child = child->GetSibling())
	{
		OutputArg(child);
		if (child->GetSibling() != NULL)
			Output(", ");
	}
	Output(")");
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
		Output(node->GetFirstChild());
		Output(")");
	}
	else if (ti == typeid(Literal))
	{
		Output("Eval(");
		Output(node);
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
		Output(sym);
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
		Node* sym = node->GetFirstChild();
		Node* coll = sym->GetSibling();
		Node* body = coll->GetSibling();
		Output("for (Object ");
		Output(sym);
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
		Output(val);
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
		Output(val);
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
	}
}

void OutputStatementList(Node* x)
{
	x->ForEach(OutputStatement);
}

void OutputType(Node* x)
{
	// TODO: make sure everyone calls this when they should
	Output(x);
}

void OutputAttr(Node* x) 
{
	Node* name = x->GetFirstChild();
	Node* type = name->GetSibling();
	OutputType(type);
	Output(" ");
	Output(name);	
	OutputLine(";");
}

void OutputOp(Node* x)
{
	Node* name = x->GetFirstChild();
	Node* arglist = name->GetSibling();
	Node* typedecl = arglist->GetSibling();
	Node* statement = null;
	if (typedecl->GetRuleTypeInfo() != typeid(TypeDecl)) {
		statement = typedecl;
	}
	else {
		statement == typedecl->GetSibling();
	}

	// TODO: finish
}

void OutputState(Node* x) 
{

}

void OutputLink(Node* x) 
{
}

void OutputInv(Node* x)
{
}

void OutputClassElement(Node* x)
{
	assert(node != NULL);
	const type_info& ti = node->GetRuleTypeInfo();

	if (ti == typeid(Attribute))
		OutputAttr(x);
	else if (ti == typeid(Operation))
		OutputOp(x);
	else if (ti == typeid(State))
		OutputState(x);
	else if (ti == typeid(Link))
		OutputLink(x);
	else if (ti == typeid(Inv))
		OutputInv(x);
	else  
		printf("Unrecognized class type\n");

}

void OutputClass(Node* x) 
{
	Node* child = x->GetFirstChild();
	Output("class ");
	Output(child);
	OutputLine(" {");
	
	for (;child != NULL; child = child->GetSibling())
		OutputClassElement(child)

	OutputLine("}");
}

void OutputDomainImport(Node* x) {
}

void OutputDomainAttr(Node* x) {
}

void OutputDomainOp(Node* x) {
}

void OutputDomain(Node* x)
{
	Node* child = x->GetFirstChild();
	Output("package ");
	Output(child);
	OutputLine(";");

	// imports
	child = child->GetSibling();
	child->ForEach(OutputDomainImport);
	
	// attributes
	child = child->GetSibling();
	child->ForEach(OutputDomainAttr);

	// operations
	child = child->GetSibling();
	child->ForEach(OutputDomainOp);

	// classes
	child = child->GetSibling();
	child->ForEach(OutputClass);
}

void OutputProgram(Node* x)
{
	x->ForEach(OutputDomain(child));
}

