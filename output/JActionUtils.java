package jaction;

import java.util.*;

public class JActionUtils 
{
	static long gnAssocId = 0;
	
	public JABool JATrue;
	public JABool JAFalse;
	
	public JActionUtils()
	{
		JATrue = new JABool(true);
		JAFalse = new JABool(false);
	}
	
	public class JACollection extends ArrayList<Object>
	{
	}
	
	public class JAList extends JACollection
	{
	}
	
	public interface JAIEnvironment
	{
		public Object Lookup(String s);
	}
	
	public class JANamedElement 
	{
		public String name;		
		
		public JANamedElement(String name)
		{
			this.name = name;
		}
	}
	
	public class JAElementMap extends HashMap<String, JANamedElement>
	{
		public void Add(JANamedElement e)
		{
			this.put(e.name, e);
		}
	}
	
	public class JAVariable extends JANamedElement
	{
		public JAVariable(String name)
		{
			super(name);
		}
	}
	
	public class JALiteral
	{
		public Object value;
	}
	
	public class JABool extends JALiteral 
	{
		public JABool(boolean b)
		{
			value = b;
		}		
	}
	
	public class JAInt extends JALiteral
	{
		
	}
	
	public abstract class JACodeBlock implements JAIEnvironment
	{
		public JAElementMap vars;
		public JAIEnvironment parent;
		
		public JACodeBlock(JAIEnvironment parent)
		{
			this.parent = parent;
		}
		
		public Object Eval(String s)
		{
			return Lookup(s);
		}
		
		public Object Eval(JALiteral x)
		{
			return x.value;
		}
		
		public void CreateLink(JAClassifier source, String role, JAClassifier dest)
		{
			// TODO: update the correct ends. 
		}
		
		public void Assignment(JAClassifier qualifier, String lvalue, Object rvalue) 
		{
			// TODO: this could be a link creation, 
			// or a variable assignment
			// or an attribute assignment
		}
			
		public void DeclareVar(String name)
		{
			vars.Add(new JAVariable(name));
		}
		
		public Object Lookup(String s)
		{
			if (vars.containsKey(s))
				return vars.get(s);
			if (parent != null)
				return parent.Lookup(s);
			return null;
		}		
		
		public abstract void Evaluate();
	}
	
	// TODO: discuss whether an Association class could be simply a specialization of 
	// JAAssociation
	public class JAAssociation extends JANamedElement
	{
		public JAElementMap ends;
		
		public JAAssociation(String name)
		{
			super(name);
		}
				
			public JAAssociation()
			{
				super("R" + gnAssocId++);
			}
	}
	
	public class JAClassifier extends JANamedElement implements JAIEnvironment
	{	
		public JAElementMap atts;
		public JAElementMap ops;
		public JAElementMap links;
		public JAClassifier parent;
		
		public JAClassifier(String name, JAClassifier parent)
		{
			super(name);
			this.parent = parent;
			atts = new JAElementMap();
			ops = new JAElementMap();
			links = new JAElementMap();
		}
		
		public JANamedElement Lookup(String s)
		{
			if (atts.containsKey(s)) 
				return atts.get(s);
			if (ops.containsKey(s)) 
				return atts.get(s);
			if (links.containsKey(s)) 
				return links.get(s);
			if (parent != null)
				return parent.Lookup(s);
			return null;
		}
	}
	
	public abstract class JAOperation extends JANamedElement
	{
		public JAOperation(String name) {
			super(name);
		}
		
		public abstract JACollection Apply(JACollection args);
	}
	
	public class JAAttribute extends JANamedElement
	{
		Object value;
		
		public JAAttribute(String name)
		{
			super(name);
		}
	}
}



