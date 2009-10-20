Heron Language Interpreter
by Christopher Diggins 
http://www.heron-language.com

Overview
--------
This is the C# source code for an interpreter for the Heron language.
It is open source software which can be used with the MIT license.
See license.txt for more information. 

If for some reason you would like a different license let me know:
cdiggins@gmail.com

Usage
-----
Use the interpreter as follows:

    HeronEngine.exe 
		-c config.xml 
		-x input.heron

Alternatively you can use the exe, or just the source code, as a 
library (.NET assembly) to construct your own interpreters or 
compilers either for Heron or other languages.

About the Heron language
------------------------
Heron is a general purpose high-level programming language that 
is designed to be used for large software development projects.
It is an object-oriented language influenced heavily by C++, 
Java, and Scala. 

Heron can be interpreted or compiled. Heron may also be dynamically 
or statically typed. 

Heron is designed with a relatively small core set of features,
but allows abstract syntax tree (AST) transformations at 
compile-time. This mechanism is intended to be used to allow
library support to be built to extend the language. 

About the Interpreter
---------------------
The HeronEngine.exe interpreter is a small non-optimizing interpreter.
It is intended to be used a way of testing and demonstrating the 
Heron language semantics. 

It is also intended to be read, analayzed, reused, reverse-engineering, 
and modified to build other interpreters or compilers for Heron. 

It is hoped that people interested in learning about parsers, and 
language tools may find the Heron interpreter of pedagogical value.

It is encouraged to adopt the source code for your own purposes,
whether they are commercial or otherwise.

About the Author
----------------
Christopher is a software developer, author, and programming language 
enthusiast, who has worked on Heron for over ten years. His goal has 
been to design a language which would make large scale software 
development easier for architects, programmers, testers, and all of 
those people who have to maintain code written by someone else.