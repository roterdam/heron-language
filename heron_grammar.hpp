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
		NoFailSeq<ENTRY, CodeBlock> { };

	struct Transition : 
		Seq<Store<Sym>, ARROW, Store<Sym> > { };
		
	struct TransitionTable : 
		NoFailSeq<TRANSITIONS, Braced<Store<Transition> > > { };

	struct State :
		Seq<Store<Sym>, CharTok<'('>, Opt<Arg>, CharTok<')'>, CharTok<'{'>, Store<EntryProc>, Store<TransitionTable>, CharTok<'}'> > { };

	struct Atts :
		NoFailSeq<ATTRIBUTES, Braced<AttDecl> > { };
	
	struct LinkDecl :
		Seq<Store<Sym>, TypeDecl> { };

	struct Links : 
		NoFailSeq<LINKS, Braced<LinkDecl> > { };
	
	struct OpDecl :
		Seq<Store<Sym>, Store<ArgList>, Opt<Store<TypeDecl> >, Or<CharTok<';'>, Store<Statement> > > { };

	struct Ops :
		NoFailSeq<OPERATIONS, Braced<OpDecl> > { };

	struct States :
		NoFailSeq<STATES, Braced<Store<State> > > { };		

	struct Invariant : 
		Seq<Store<Sym>, Store<CodeBlock> > { };

	struct Invariants :
		NoFailSeq<INVARIANTS, Braced<Store<Invariant> > > { };

	struct Class :
		NoFailSeq<CLASS, Store<Sym>, CharTok<'{'>, Opt<Atts>, Opt<Ops>, Opt<States>, Opt<Links>, Opt<Invariants>, CharTok<'}'> > { };

	struct Imports :
		NoFailSeq<IMPORTS, Braced<Store<Sym> > > { };

	struct Classes :
		NoFailSeq<CLASSES, Braced<Store<Class> > > { };

	struct Domain :
		NoFailSeq<Log<DOMAIN>, Log<Store<Sym> >, Log< CharTok<'{'> >, Log< Opt<Imports> >, Log< Opt<Atts> >, Log< Opt<Ops> >, Log< Opt<Classes> >, Log< CharTok<'}'> > > { };
		//NoFailSeq<DOMAIN, Store<Sym>, CharTok<'{'>, Opt<Imports>, Opt<Atts>, Opt<Ops>, Opt<Classes>, CharTok<'}'> > { };

	struct Program :
		Seq<WS, Star<Log<Store<Domain> > > > { };
}

#endif