Heron Language Interpreter
by Christopher Diggins 
http://www.heron-language.com

--------
Overview
--------
This is an interpreter with C# source code for the Heron language.

-----
Usage
-----
Use the interpreter as follows:

    HeronEngine.exe input.heron

Alternatively you can use the exe as a library, or just the source code,
to construct your own interpreters or compilers for Heron, or any other 
language.

If you have a file "config.xml" in your root directory, this can be used 
to control additional options of the interpreter.

If Heron is installed via MSI the installer will creates a file association
for files with the extension .heron. 

------------------------
About the Heron language
------------------------
Heron is a simple general-purpose programming language that 
is designed to be used for small scripting tasks, but that 
can scale up easily to large software development projects.

Heron is an object-oriented language influenced heavily by C++, C#,
and Java. Other influential languages include Scala, Pascal, JavaScript,
and Eiffel. 

The language syntax is officially defined in the 
file grammar.txt as a parsing expression grammar (PEG). 

---------------------
About the Interpreter
---------------------
The HeronEngine.exe interpreter is a small non-optimizing interpreter.
It is intended to be used a way of testing and demonstrating the 
Heron language semantics. 

It is also intended to be read, analayzed, reused, reverse-engineering, 
and modified to build other interpreters or compilers for Heron. 

People interested in learning about or implementing parsers and other language 
tools may find the Heron interpreter of value. Heron uses an efficient 
and simple recursive descent parser, and executes the code from an AST format.

-------
License
-------
Heron is open source software which can be used with the MIT license.
See license.txt for more information. If for some reason you need a 
different license send an email to: cdiggins@gmail.com
 