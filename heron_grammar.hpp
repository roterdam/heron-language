// YARD Grammar for the C language
// Released under the MIT License by Christopher Diggins

#ifndef JACTION_GRAMMAR_HPP
#define JACTION_GRAMMAR_HPP

namespace heron_grammar
{
	using namespace yard;
	using namespace text_grammar;

	struct Expr;
	struct SimpleExpr;
	struct Statement;
	struct StatementList;

	struct SymChar :
		CharSetParser<CharSet<'~','!','@','#','$','%','^','&','*','-','+','|','\\','<','>','/','?',','> > { };

	struct NewLine : 
		Or<CharSeq<'\r', '\n'>, CharSeq<'\n'> > { };

	struct LineComment : 
		Seq<CharSeq<'/', '/'>, UntilPast<NewLine> > { };

	struct FullComment : 
		Seq<CharSeq<'/', '*'>, UntilPast<CharSeq<'*', '/'> > >	{ };

	struct Comment :
		Or<LineComment, FullComment> { };

	struct WS : 
		Star<Or<Char<' '>, Char<'\t'>, NewLine, Comment, Char<'\r'>, Char<'\v'>, Char<'\f'> > >	{ };

	template<typename R>
	struct Tok : 
		Seq<R, WS> { };

	struct Sym :
		Seq<Or<Store<Ident>, Store<Plus<SymChar> > >, WS> { };

	struct DotSym :
		Seq<Char<'.'>, Sym> { };


	struct Id : 
		Tok<Sym> { };

	template<typename T>
	struct Keyword : 
		Tok<Seq<T, NotAlphaNum > > { };

	template<char C>
    struct CharTok :
		Seq<Char<C>, WS> { };

	template<typename R, typename D>
	struct DelimitedList : 
	   Or<Opt<R>, Seq<R, Plus<Seq<D, R> > > >
	{ };

	template<typename R>
	struct CommaList : 
	   DelimitedList<R, CharTok<','> > { };

	template<typename R>
	struct Braced : 
		Seq<CharTok<'{'>, R, CharTok<'}'> > { };

	template<typename R>
	struct BracedCommaList : 
		Braced<CommaList<R> > { };

	template<typename R>
	struct Paranthesized  : 
		Seq<CharTok<'('>, R, CharTok<')'> > { };

	template<typename R>
	struct ParanthesizedCommaList : 
		Paranthesized<CommaList<R> > { };

	template<typename R>
	struct Bracketed : 
		Seq<CharTok<'['>, R, CharTok<']'> >	{ };

	template<typename R>
	struct BracketedCommaList : 
		Bracketed<CommaList<R> > { };

	struct StringCharLiteral : 
		Or<Seq<Char<'\\'>, NotChar<'\n'> >, AnyCharExcept<CharSet<'\n', '\"', '\'' > > > { };

	struct CharLiteral : 
		Seq<Char<'\''>, StringCharLiteral, Char<'\''> > { };

	struct StringLiteral : 
		Seq<Char<'\"'>, Star<StringCharLiteral>, Char<'\"'> > { };

	struct BinaryDigit : 
		Or<Char<'0'>, Char<'1'> > { };

	struct BinNumber : 
		Seq<CharSeq<'0', 'b'>, Plus<BinaryDigit>, NotAlphaNum, WS> { };

	struct HexNumber : 
		Seq<CharSeq<'0', 'x'>, Plus<HexDigit>, NotAlphaNum, WS> { };

	struct DecNumber : 
		Seq<Opt<Char<'-'> >, Plus<Digit>, Opt<Seq<Char<'.'>, Star<Digit> > >, NotAlphaNum, WS> { };

	struct NEW : Keyword<CharSeq<'n','e','w'> > { };
	struct DELETE : Keyword<CharSeq<'d','e','l','e','t','e'> > { };
	struct VAR : Keyword<CharSeq<'v','a','r'> > { };
	struct ELSE : Keyword<CharSeq<'e','l','s','e'> > { };
	struct IF : Keyword<CharSeq<'i','f'> > { };
	struct FOREACH : Keyword<CharSeq<'f','o','r','e','a','c','h'> > { };
	struct WHILE : Keyword<CharSeq<'w','h','i','l','e'> > { };
	struct IN : Keyword<CharSeq<'i','n'> > { };
	struct CASE : Keyword<CharSeq<'c','a','s','e'> > { };
	struct SWITCH : Keyword<CharSeq<'s','w','i','t','c','h'> > { };
	struct RETURN : Keyword<CharSeq<'r','e','t','u','r','n'> > { };
	struct DEFAULT : Keyword<CharSeq<'d','e','f','a','u','l','t'> > { };

	struct Literal :
		Tok<Or<BinNumber, HexNumber, DecNumber, CharLiteral, StringLiteral > > { };

	struct ParamList :
		ParanthesizedCommaList<Store<Expr> > { };

	// TODO: allow more sophisticated types
	struct TypeExpr : 
		Store<Sym> { };

	struct TypeDecl :
		Seq<CharTok<':'>, Store<TypeExpr> > { };

	struct Arg :
		Seq<Store<Sym>, Opt<TypeDecl> > { };

	struct ArgList :
		ParanthesizedCommaList<Store<Arg> > { };

	struct AnonFxn :
		Seq<Store<ArgList>, Char<'='>, Char<'>'>, Opt<WS>, Statement> { };

	struct NewExpr :
		Seq<NEW, Store<Expr>, Store<ParamList> > { };

	struct DelExpr :
		Seq<DELETE, Store<Expr> > { };

	struct ParanthesizedExpr :
		Paranthesized<Opt<Expr> > { };

	struct SimpleExpr :
		Or<
            Store<NewExpr>,
            Store<DelExpr>,
            Store<Sym>,
			Store<DotSym>,
            Store<Literal>,
            Store<AnonFxn>,
			Store<ParanthesizedExpr>
		> { };

	struct Expr :
		Plus<SimpleExpr> { };

	struct Initializer :
		Seq<CharTok<'='>, Store<Expr> > { };

	struct CodeBlock :
		Braced<StatementList> { };

	struct VarDecl :
		Seq<VAR, Store<Sym>, Opt<Initializer> > { };

	struct ElseStatement :
		Seq<ELSE, Store<CodeBlock> > { };

	struct IfStatement :
        Seq<IF, Paranthesized<Store<Expr> >, Store<CodeBlock>, Opt<ElseStatement> > { };

	struct ForEachStatement :
		Seq<FOREACH, CharTok<'('>, Store<Sym>,
			IN, Store<Expr>, CharTok<')'>, Store<CodeBlock> > { };

	struct ExprStatement :
		Store<Expr> { };

	struct ReturnStatement :
		Seq<RETURN, Store<Expr> > { };	

	struct CaseStatement :
		Seq<CASE, Paranthesized<Store<Expr> >, Store<CodeBlock> > { };

	struct DefaultStatement :
		Seq<DEFAULT, Store<CodeBlock> > { };

	struct SwitchStatement :
		Seq<SWITCH, Paranthesized<Store<Expr> >, CharTok<'{'>, Star<Store<CaseStatement> >, 
			Opt<Store<DefaultStatement> >, CharTok<'}'> > { };

	struct WhileStatement :
		Seq<WHILE, Paranthesized<Store<Expr> >, Store<CodeBlock> > { };
	
	struct AssignmentStatement :
		Seq<Store<Expr>, Initializer> { };

    struct Statement :
       Or<
           Store<CodeBlock>,
           Store<VarDecl>,
           Store<IfStatement>,
           Store<SwitchStatement>,
           Store<ForEachStatement>,
           Store<WhileStatement>,
		   Store<ReturnStatement>,
		   Store<AssignmentStatement>,
           Store<ExprStatement>
       > { };

	struct StatementList :
        Seq<Opt<Statement>, Star<Seq<CharTok<';'>, Statement> >, Opt<CharTok<';'> > > { };

	struct Program :
		StatementList { };
}

#endif