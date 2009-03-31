class Scratch
{
  public static void main(String[] args)
  {
    Local.Evaluate();
  }

  class MyClass
  {
    void Evaluate()
    {

    }
  }

  class JAContext
  {
    private JAContext parent;
    public Map env;

    public JAContext(JAContext p)
    {
      parent = p;
    }
    public JAContext()
    {
      parent = NULL;
    }
    public JAContext GetParent()
    {
      return parent;
    }
    public object Lookup(string s)
    {
      if (env.containsKey(s))
        return env.get(s);
      else if (parent != NULL)
        return parent.Lookup(s);
      else
        return NULL;
    }
    public void Register(string s, object o)
    {
      env.put(s, o);
    }
  }

  class JAStatement : JAContext
  {
    public JAStatement(JAContext c)
    {
      super(c);
    }
    abstract void Evaluate();
  }

  class JAVarDecl : JAContext
  {
    private string name;

    public JAVarDecl(JAContext c, string s)
    {
      super(c);
      name = s;
    }

    public string GetName()
    {

    }

    public void Evaluate()
    {
      parent.Register(s, null);
    }
  }

  class JAAssignment
  {
    public JAAssignment(JAContext c, string s)
    {
    }

    public void Evaluate()
    {
    }
  }
}

////

JActionObjectWrapper
{
  object value;
}


// Variable declaration
// Creates an object wrapper in the current environment
VariableDeclaration(string "xxx");

// "{"
PushNewEnv();

// "}"
PopEnv();

// lvalue = rvalue
// Checks if lvalue is a variable, attribute, or role-name
Assignment(JActionObject lvalue, JActionObject rvalue);

// a . b
Evaluate(Lookup("a")).Lookup("b");

// a(b)
Evaluate(Lookup("a")).Apply(Evaluate(Lookup("b")));

// a b
Evaluate(Lookup("a")).Apply(Evaluate(Lookup("b"))));

// a = b
Lookup("a").value = b;

// simply saying "a" is meaningless ... it has to be resolved .
// a problem is that "b" is context dependent. It is simply a string.
// what does "a" . "b" mean.

Evaluate(JALiteral l)
{
  return l;
}

Evaluate(JAFunction f)
{
  return f;
}

Evaluate(JAWrapper w)
{
  return w.value;
}