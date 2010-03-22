mkdir Release
mkdir Release\lib
mkdir Release\Properties
mkdir Release\samples

mkdir Release\tests
mkdir Release\testdata

del Release\*.* /Q /S

copy lib\*.* Release\lib\
copy Properties\*.* Release\Properties\

copy samples\*.* Release\samples\
copy tests\*.* Release\tests\
copy testdata\*.* Release\testdata\


copy *.config Release\
copy *.cs Release\
copy *.csproj Release\
copy *.resx Release\
copy *.sln Release\
copy *.ico Release\
copy *.bat Release\

copy license.txt Release\
copy readme.txt Release\
copy grammar.txt Release\
copy primitives.txt Release\
copy codemodel.txt Release\

copy HeronEngine.exe Release\

copy config.xml Release\config.xml