using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ComparisonTool;
using System.Reflection;
using System.Diagnostics;

namespace TestApp
{
	class Program
	{
		public const string OldFolder = @"OldDllFolder";
		public const string NewFolder = @"NewDllFolder";

		static void Main(string[] args)
		{
			DisassembleFileAndWriteToFile(OldFolder);
			DisassembleFileAndWriteToFile(NewFolder);
		}

		/// <summary>
		/// Method which decompiles a dll file and writes it's contents to
		/// a text file
		/// </summary>
		/// <param name="directoryName"></param>
		public static void DisassembleFileAndWriteToFile(string directoryName)
		{
			DirectoryInfo directory = new DirectoryInfo(directoryName);

			List<string> listOfFileNames = new List<string>();

			foreach (FileInfo file in directory.GetFiles())
			{
				listOfFileNames.Add(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), directoryName + @"\" + file.Name));
			}
			
			//only compare dll files
			listOfFileNames = listOfFileNames.Where(s => s.EndsWith(".dll")).ToList();

			//loop through each dll file and disassemble
			foreach (string fileName in listOfFileNames)
			{
				Disassembler disassembler = new Disassembler(fileName);
				disassembler.DisassembleFile();
			}

		}
	}
}
