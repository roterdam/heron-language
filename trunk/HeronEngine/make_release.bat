mkdir Release
mkdir Release\tests
mkdir Release\src
mkdir Release\src\Properties
del Release\*.* /Q /S
copy tests\*.* Release\tests
copy Properties\*.cs Release\src\Properties
copy *.cs Release\src
copy Properties\*.cs Release\src
copy *.csproj Release\src
copy *.resx Release\src
copy *.sln Release\src
copy license.txt Release\src
copy readme.txt Release\src
copy HeronEngine.exe Release\
copy Config.xml Release\