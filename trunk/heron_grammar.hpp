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
	
	struct AttDecl :
		Seq<Store<Sym>, TypeDecl>  { };

	struct EntryProc :
		Seq<ENTRY, CodeBlock> { };

	struct Transition : 
		Seq<Store<Sym>, ARROW, Store<Sym> > { };
		
	struct TransitionTable : 
		Seq<TRANSITIONS, Braced<Store<Transition> > > { };

	struct State :
		Seq<Store<Sym>, CharTok<'('>, Opt<Arg>, CharTok<')'>, CharTok<'{'>, Store<EntryProc>, Store<TransitionTable>, CharTok<'}'> > { };

	struct Atts :
		Seq<ATTRIBUTES, Braced<AttDecl> > { };
	
	struct LinkDecl :
		Seq<Store<Sym>, TypeDecl> { };

	struct Links : 
		Seq<LINKS, Braced<LinkDecl> > { };
	
	struct OpDecl :
		Seq<Store<Sym>, Store<ArgList>, Store<Statement> > { };

	struct Ops :
		Seq<OPERATIONS, Braced<OpDecl> > { };

	struct States :
		Seq<STATES, Braced<Store<State> > > { };		

	struct Invariant : 
		Seq<Store<Sym>, Store<CodeBlock> > { };

	struct Invariants :
		Seq<INVARIANTS, Braced<Store<Invariant> > > { };

	struct Class :
		Seq<CLASS, Store<Sym>, CharTok<'{'>, Opt<Atts>, Opt<Ops>, Opt<States>, Opt<Links>, Opt<Invariants>, CharTok<'}'> > { };

	struct Imports :
		Seq<IMPORTS, Braced<Store<Sym> > > { };

	struct Classes :
		Seq<CLASSES, Braced<Store<Class> > > { };

	struct Domain :
		Seq<DOMAIN, Store<Sym>, CharTok<'{'>, Opt<Imports>, Opt<Atts>, Opt<Ops>, Opt<Classes>, CharTok<'}'> > { };

	struct Program :
		Seq<WS, Store<Store<Domain> > > { };
}

#endif