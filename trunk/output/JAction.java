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

Symposium on Principles of Programming Languages, January 1416,  2004, Venice, Italy



P. Hudak, S. Peyton Jones, P. Wadler, Arvind, B. Bontel, J. Fairbairn, J. Fasel, M. Guzman, K. Hammond, J. Hughs, T. Johnson, R. Kieburtz, W. Partain, and J. Peterson. Report on the Functional Programming Language Haskell (version 1.2). ACM SIGPLAN Notices, 27(5), July 1992.

Bertrand Meyer, Object Oriented Software Construction Prentice-Hall, Englewood Cliffs, NJ, 1988