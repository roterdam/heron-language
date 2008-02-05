
using namespace xml_grammar;

/*
	Notes:
		- I will probably need to add an XMI parser file
		- classses 

	Everything seems to have a name attribute and an xmi.id attribute.

	Classifiers have attributes and operations
	Attributes have multiplicity and type
	Operation have Parameters 

<XMI>
  <XMI.content>
    <UML:Model name = 'untitledModel'>
      <UML:Namespace.ownedElement>
        <UML:Class name = 'MyClass'>
          <UML:Namespace.ownedElement>
            <UML:Class name = 'int'>
            <UML:Class name = 'void'>
          </UML:Namespace.ownedElement>
          <UML:Classifier.feature>
            <UML:Attribute name = 'newAttr'> 
              <UML:StructuralFeature.multiplicity>
                <UML:Multiplicity>
                  <UML:Multiplicity.range>
                    <UML:MultiplicityRange lower = '1' upper = '1'/>
                  </UML:Multiplicity.range>
                </UML:Multiplicity>
              </UML:StructuralFeature.multiplicity>
              <UML:StructuralFeature.type>
                <UML:Class xmi.idref = '-64--88-2-11--4523963b:117d8758866:-8000:0000000000000721'/>
              </UML:StructuralFeature.type>
            </UML:Attribute>
            <UML:Operation name = 'newOperation'>
              <UML:BehavioralFeature.parameter>
                <UML:Parameter name = 'return'>
                  <UML:Parameter.type>
                    <UML:Class/>
                  </UML:Parameter.type>
                </UML:Parameter>
              </UML:BehavioralFeature.parameter>
            </UML:Operation>
          </UML:Classifier.feature>
        </UML:Class>
        <UML:ActivityGraph>
          <UML:StateMachine.top>
            <UML:CompositeState name = 'top'>
          </UML:StateMachine.top>
        </UML:ActivityGraph>
      </UML:Namespace.ownedElement>
    </UML:Model>
  </XMI.content>
</XMI>


*/

void ProcessXmlNode(Node* node)
{
	const type_info& ti = node->GetRuleTypeInfo();

	if (ti == typeid(Element))
	{
		ProcessXmlNode(node->GetFirstChild());
	}
	else if (ti == typeid(TaggedContent))
	{	
		Node* stag = node->GetFirstChild();		
		Node* content = stag->GetSibling();
		Node* etag = content->GetSibling();
		ProcessXmlNode(stag);
		for (Node* child = content->GetFirstChild(); child != NULL; child = child->GetSibling())
			ProcessXmlNode(child);
		ProcessXmlNode(etag);		
	}
	else if (ti == typeid(EmptyElemTag) || ti == typeid(STag))
	{
		Node* name = node->GetFirstChild();
		Node* attributes = name->GetSibling();
		Output("<");
		Output(name);
		Output(" ");
		ProcessXmlNode(attributes);

		if (ti == typeid(EmptyElemTag))
			Output("/");
		
		OutputLine(">");
	}
	else if (ti == typeid(ETag))
	{
		Node* name = node->GetFirstChild();
		Output("<");
		Output(name);
		OutputLine(">");
	}
	else if (ti == typeid(Attributes))
	{
		for (Node* child = node->GetFirstChild(); child != NULL; child = child->GetSibling())
			ProcessXmlNode(child);
	}
	else if (ti == typeid(Attribute))
	{
		Node* name = node->GetFirstChild();
		Node* value = name->GetSibling();
		Output(name);
		Output(" = ");
		Output(value);
	}
}

bool ParseXmlString(const char* s)
{
	size_t len = strlen(s);
	Parser parser(s, s + len);
	bool b = parser.Parse<xml_grammar::Document>();

	if (!b) 
		return false;
	
	ProcessXmlNode(parser.GetAstRoot());

	return true;
}

