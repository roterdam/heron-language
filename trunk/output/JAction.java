package JAction;

interface JActionObject
{
}

interface ICollection
{

  void AddValue(JActionObject o);
  void RemoveValue(JActionObject o);
}

class List : ICollection
{
  void Add(JActionObject o)
  {
  }
  void InsertAt(int n, JActionObject o)
  {
  }
  void RemoveAt(int n)
  {
  }
  void Remove(JActionObject o)
  {
  }
}

class JActionVariable : JActionObject
{
}

class JActionVariableDeclaration : JActionOjbect

class JActionProcedure : JActionObject
{
}

class JActionFunction
{
  public JActionICollection Apply(JActionICollection args)
  {
    foreach (
  }
}



class JActionIClassifier
{
  public JActionICollection GetClassMethods() { return NULL; }
  public JActionICollection GetAttributes() { return NULL; }
  public JActionICollection GetOperations() { return NULL; }
}

class JActionAttribute
{
  public JActionObject Read()
  {
  }

  public void Write(JActionObject)
  {
  }
}



class JActionClassifierInstance
{
}

class JActionScope : JActionObject
{
  public void Evaluate(JActionObject[] args)
  {
    switch (args.Count)
    {
      case (0)
      {
        throw Error();
      }
      case (1)
      {
        switch (args[0].TypeName)
        {
          case "IntLiteral"
          {
          }
          case "CharLiteral"
          {
          }
        }
      }
      default
      {
        if (args[1] is JActionFunction)
        {
        }
        else if (args[2] is JActionFunction)
        {
        }
        else
        {
          Error("Can't evaluate expression" is JActionFunction);
        }
      }
      default
      {
      }
    }
  }
}

class test2
{
  public static void main(String[] args) {
    System.out.println("Hello World!"); // Display the string.
  }
}


/**
 * The HelloWorldApp class implements an application that
 * simply prints "Hello World!" to standard output.
 */
class output
{
  class test {
    int doNothing;
  };

  public static void main(String[] args) {
    System.out.println("Hello World!"); // Display the string.
  }
}


