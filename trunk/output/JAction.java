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

class Set : : JActionObject, ICollection
{
}

class Range : JActionObject, ICollection
{
}

class JActionVariable : JActionObject
{
}

class JActionProcedure : JActionObject
{
}

class JActionScope : JActionObject
{
  public void Evaluate(JActionObject[] args)
  {
    if (args.Count == 0)
  }
}

class

class JActionAttribute : JActionObject
{
}

class JActionOperation : JActionProcedure
{

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

//

a . b = c;

