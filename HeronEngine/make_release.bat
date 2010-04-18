del Release\*.* /Q /S

mkdir Release
mkdir Release\lib
mkdir Release\src
mkdir Release\src\HeronEdit
mkdir Release\src\HeronEngine
mkdir Release\src\HeronEdit\properties
mkdir Release\src\HeronEngine\properties
mkdir Release\samples
mkdir Release\tests
mkdir Release\testdata
mkdir Release\macros

copy lib\*.* Release\lib\
copy samples\*.* Release\samples\
copy tests\*.* Release\tests\
copy testdata\*.* Release\testdata\
copy macros\*.* Release\macros\

copy *.config Release\src\HeronEngine\
copy *.cs Release\src\src\HeronEngine\
copy *.csproj Release\src\HeronEngine\
copy *.resx Release\src\HeronEngine\
copy *.sln Release\src\HeronEngine\
copy *.ico Release\src\HeronEngine\
copy *.bat Release\src\HeronEngine\
copy Properties\*.* Release\src\HeronEngine\Properties\

copy ..\HeronEdit\*.config Release\src\HeronEdit\
copy ..\HeronEdit\*.cs Release\src\src\HeronEdit\
copy ..\HeronEdit\*.csproj Release\src\HeronEdit\
copy ..\HeronEdit\*.resx Release\src\HeronEdit\
copy ..\HeronEdit\*.sln Release\src\HeronEdit\
copy ..\HeronEdit\*.ico Release\src\HeronEdit\
copy ..\HeronEdit\*.bat Release\src\HeronEdit\
copy ..\HeronEdit\Properties\*.* Release\src\HeronEdit\Properties\

copy license.txt Release\
copy readme.txt Release\
copy grammar.txt Release\
copy primitives.txt Release\
copy codemodel.txt Release\

copy HeronEngine.exe Release\
copy HeronEdit.exe Release\

copy config.xml Release\config.xml