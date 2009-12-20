mkdir Release
mkdir Release\tests
mkdir Release\src
mkdir Release\src\Properties
mkdir Release\src\Tests
del Release\*.* /Q /S
copy tests\*.* Release\tests
copy tests\*.* Release\src\tests
copy Properties\*.cs Release\src\Properties
copy *.cs Release\src
copy Properties\*.cs Release\src
copy *.csproj Release\src
copy *.resx Release\src
copy *.sln Release\src
copy license.txt Release\src
copy readme.txt Release\src
copy grammar.txt Release\
copy primitives.txt Release\
copy codemodel.txt Release\
copy HeronEngine.exe Release\
copy run_tests.bat Release\
copy config-release.xml Release\config.xml