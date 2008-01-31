// Dedicated to the public domain by Christopher Diggins

#ifndef IO_HPP
#define IO_HPP

int FileSize(const char* filename)
{
	int r = 0;
	FILE* f = fopen(filename, "r");
	while (!feof(f))
	{
		fgetc(f);
		++r;
	}
	fclose(f);
	return r;
}

char* ReadFile(const char* filename)
{	
	int n = FileSize(filename);
	char* result = (char*)malloc(n + 1);
	char* p = result;
	FILE* f = fopen(filename, "r");
	if (f == NULL)
		return NULL;
	char c = fgetc(f);
	while (!feof(f))
	{
		*p++ = c;
		c = fgetc(f);
	}		
	*p = '\0';
	fclose(f);
	return result;
}		

#endif
