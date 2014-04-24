DLLComparisonTool

- Use this tool to compare DLLs from two different folders

1) The tool will decompile DLLs using the ILdasm.exe file to strip out unwanted file tags (which may have been created by Visual Studio).
2) The tool will then write the output to a text file where the contents can then be compared.

This is especially useful when deploying projects as it's very difficult to detect changes within the DLLs. The visual studio build process
tends to put unwanted comments and file tags in the DLLs. It also places the IL code in a random order, so it's very difficult to tell what has changed
between builds.

I hope people find this useful.