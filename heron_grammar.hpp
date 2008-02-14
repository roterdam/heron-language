// YARD Grammar for the C language
// Released under the MIT License by Christopher Diggins

#ifndef HERON_GRAMMAR_HPP
#define HERON_GRAMMAR_HPP

namespace heron_grammar
{
	using namespace jaction_grammar;

	struct CLASS		: Keyword<CharSeq<'c','l','a','s','s'> > { };
	struct CLASSES		: Keyword<CharSeq<'c','l','a','s','s','e','s'> > { };
	struct DOMAIN		: Keyword<CharSeq<'d','o','m','a','i','n'> > { };
	struct IMPORTS		: Keyword<CharSeq<'i','m','p','o','r','t','s'> > { };
	struct LINKS		: Keyword<CharSeq<'l','i','n','k','s'> > { };
	struct STATES		: Keyword<CharSeq<'s','t','a','t','e','s'> > { };
	struct ATTRIBUTES	: Keyword<CharSeq<'a','t','t','r','i','b','u','t','e','s'> > { };
	struct OPERATIONS	: Keyword<CharSeq<'o','p','e','r','a','t','i','o','n','s'> > { };
	struct TRANSITIONS	: Keyword<CharSeq<'t','r','a','n','s','i','t','i','o','n','s'> > { };
	struct INVARIANTS	: Keyword<CharSeq<'i','n','v','a','r','i','a','n','t','s'> > { };
	struct ENTRY		: Keyword<CharSeq<'e','n','t','r','y'> > { };
	struct ARROW		: Keyword<CharSeq<'-','>'> > { };
	
	struct Eos : CharTok<';'> { };

	struct EntryProc :
		NoFailSeq<ENTRY, CodeBlock> { };

	struct Transition : 
		NoFailSeq<Store<Sym>, ARROW, Store<Sym>, Eos > { };
		
	struct TransitionTable : 
		NoFailSeq<TRANSITIONS, StoreBracedList<Transition> > { };

	struct State :
		NoFailSeq<Store<Sym>, CharTok<'('>, Opt<Store<Arg> >, CharTok<')'>, CharTok<'{'>, Store<EntryProc>, Store<TransitionTable>, CharTok<'}'> > { };

	struct Attribute :
		NoFailSeq<Store<Sym>, Opt<TypeDecl>, Eos >  { };

	struct Attributes :
		NoFailSeq<ATTRIBUTES, StoreBracedList<Attribute> > { };
	
	struct Link :
		NoFailSeq<Store<Sym>, TypeDecl, Eos > { };
		//NoFailSeq<Log<Store<Sym> >, Log<TypeDecl>, Eos  > { };

	struct Links : 
		NoFailSeq<LINKS, StoreBracedList<Link> > { };
	
	struct Operation :
		NoFailSeq<Store<Sym>, Store<ArgList>, Opt<Store<TypeDecl> >, Opt<Store<CodeBlock> >, Opt<Eos> > { };
		//Seq<Log<Store<Sym> >, Log<Store<ArgList> >, Log<Opt<Store<TypeDecl> > >, Log<Opt<Store<Statement> > >, Log<Opt<Eos > > > { };

	struct Operations :
		NoFailSeq<OPERATIONS, StoreBracedList<Operation> > { };

	struct States :
		NoFailSeq<STATES, StoreBracedList<State> > { };		

	struct Invariant : 
		NoFailSeq<Store<Sym>, Store<CodeBlock> > { };

	struct Invariants :
		NoFailSeq<INVARIANTS, StoreBracedList<Invariant> > { };

	struct Class :
		NoFailSeq<
			CLASS, 
			Store<Sym>, 
			CharTok<'{'>, 
			Opt<Attributes>, 
			Opt<Operations>, 
			Opt<States>, 
			Opt<Links>, 
			Opt<Invariants>, 
			CharTok<'}'> 
		> { };

	struct Import :
		NoFailSeq<Store<Sym>, Eos > { };

	struct Imports :
		NoFailSeq<IMPORTS, StoreBracedList<Import> > { };

	struct Classes :
		NoFailSeq<CLASSES, StoreBracedList<Class> > { };

	struct Domain :
		NoFailSeq<DOMAIN, Store<Sym>, CharTok<'{'>, Opt<Imports>, Opt<Attributes>, Opt<Operations>, Opt<Classes>, CharTok<'}'> > { };
		//NoFailSeq<Log<DOMAIN>, Log<Store<Sym> >, Log< CharTok<'{'> >, Log< Opt<Imports> >, Log< Opt<Attributes> >, Log< Opt<Operations> >, Log< Opt<Classes> >, Log< CharTok<'}'> > > { };

	struct Program :
		Seq<WS, Star<Store<Domain> > > { };
}	

#endif