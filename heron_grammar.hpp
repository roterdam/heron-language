// YARD Grammar for the C language
// Released under the MIT License by Christopher Diggins

#ifndef HERON_GRAMMAR_HPP
#define HERON_GRAMMAR_HPP

namespace heron_grammar
{
	using namespace jaction_grammar;

	struct CLASS : Keyword<CharSeq<'c','l','a','s','s'> > { };
	//struct OPS : Keyword<CharSeq<'o','p','s'> > { };
	//struct ATTS : Keyword<CharSeq<'a','t','t','s'> > { };
	//struct SIGS : Keyword<CharSeq<'s','i','g','s'> > { };
	//struct DEFS : Keyword<CharSeq<'d','e','f','s'> > { };
	//struct VARS : Keyword<CharSeq<'v','a','r','s'> > { };
	struct SIGNALS : Keyword<CharSeq<'s','i','g','n','a','l','s'> > { };
	struct LINKS : Keyword<CharSeq<'l','i','n','k','s'> > { };
	struct ATTRIBUTES : Keyword<CharSeq<'a','t','t','r','i','b','u','t','e','s'> > { };
	struct OPERATIONS : Keyword<CharSeq<'o','p','e','r','a','t','i','o','n','s'> > { };

	struct Atts :
		Seq<ATTRIBUTES, Braced<AttDecl> > { };
	
	struct Links : 
		Seq<LINKS, Braced<LinkDecl> > { };
	
	struct Ops :
		Seq<OPERATIONS, Braced<OpDecl> > { };

	struct Sigs :
		Seq<SIGNALS, Braced<Signal> > { };		

	struct Class :
		Seq<Opt<Atts>, Opt<AOpt<Links> > { };

	struct Program :
		StatementList { Class };
}

#endif