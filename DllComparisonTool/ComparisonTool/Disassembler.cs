using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ComparisonTool
{
	public class Disassembler
	{
		public string DllFileName { get; private set; }

		public Disassembler(string fileName)
        {
            this.DllFileName = fileName;
        }
		
		public void DisassembleFile()
		{
			Assembly assembly = null;
			try
			{
				assembly = Assembly.LoadFile(this.DllFileName);
			}
			catch (BadImageFormatException ex)
			{
				throw new BadImageFormatException(ex.FileName);
			}

			if (assembly != null)
			{
				//Found assembly, proceed to decompilation method.
				this.DisassembleAndOutputFile(this.DllFileName);
			}
		}

		private void DisassembleAndOutputFile(string assemblyFilePath)
		{
			if (!File.Exists(assemblyFilePath))
			{
				throw new InvalidOperationException(string.Format("The file {0} does not exist!", assemblyFilePath));
			}

			
			string fileName = Path.GetFileName(assemblyFilePath);

			//Start a process which looks for the ILDasm File (This contains the commands which ignore specific tags within the DLL) see below:
			//ildasm /all /text %1 | find /v "Time-date stamp:" | find /v "MVID" | find /v "Checksum:" | find /v "Image base:"
			
			string tempFileName = Path.GetDirectoryName(assemblyFilePath) + @"\" + fileName + ".txt";
			ProcessStartInfo startInfo = new ProcessStartInfo(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"\ildasm.bat");
			startInfo.WindowStyle = ProcessWindowStyle.Hidden;
			startInfo.CreateNoWindow = true;
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardOutput = true;
			startInfo.Arguments = string.Format("{0}", assemblyFilePath);

			using (Process process = System.Diagnostics.Process.Start(startInfo))
			{
				string output = process.StandardOutput.ReadToEnd();
				process.WaitForExit();

				if (process.ExitCode > 0)
				{
					throw new InvalidOperationException(
						string.Format("Generating IL code for file {0} failed with exit code - {1}. Log: {2}",
						assemblyFilePath, process.ExitCode, output));
				}

				File.WriteAllText(tempFileName, output);
			}

			string[] fileContent = File.ReadAllLines(tempFileName);

			//remove comments
			RemoveLinesFromDecompiledDll(ref fileContent);
			RemoveIldasmInfoFromDecompiledDll(ref fileContent);

			/*Seperate and order the contents of the array.
			  We need to do this as sometimes visual studio builds the IL code
			  in a random order. Which results in the code being scrambled*/

			Array.Sort(fileContent);

			//write lines to text file, avoiding blank lines
			File.WriteAllLines(tempFileName, fileContent.Where(i => i != "").ToArray());


		}

		/// <summary>
		/// When Visual Studio builds a DLL it often places comments in the file
		/// which contain incremental information which causes a difference between files.
		/// We want our decompiler to ignore these characters.
		/// </summary>
		/// <param name="dllFileContent"></param>
		private void RemoveLinesFromDecompiledDll(ref string[] dllFileContent)
		{

			for (int i = 0; i < dllFileContent.Count(); i++)
			{
				//remove comments - // and /* */
				if (dllFileContent[i] != "")
				{
					dllFileContent[i] = Regex.Replace(dllFileContent[i], @"/\*.*\*/", String.Empty, RegexOptions.Singleline);
					dllFileContent[i] = Regex.Replace(dllFileContent[i], @"(/\*([^*]|[\r\n]|(\*+([^*/]|[\r\n])))*\*+/)|(//.*)", String.Empty, RegexOptions.Singleline);
				}

				//Remove PrivateImplementationDetails tag
				if (dllFileContent[i].Contains("<PrivateImplementationDetails>"))
				{
					dllFileContent[i] = String.Empty;
				}
			}

		}

		/// <summary>
		/// Removes the ILDasm commands from the outputted text file. This is added to the text file
		/// early on in the process, so we need to remove it.
		/// </summary>
		/// <param name="dllFileContent"></param>
		private void RemoveIldasmInfoFromDecompiledDll(ref string[] dllFileContent)
		{

			for (int i = 0; i < dllFileContent.Count(); i++)
			{
				//Find and remove ildasm command line
				if (dllFileContent[i].Contains("ildasm"))
				{
					dllFileContent[i] = String.Empty;
					break;
				}
			}

		}

	}

}
