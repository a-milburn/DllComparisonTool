using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace DLLDecompilerTool
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

            RemoveLinesFromDecompiledDll(ref fileContent);
            RemoveIldasmInfoFromDecompiledDll(ref fileContent);

            Array.Sort(fileContent);

            File.WriteAllLines(tempFileName, fileContent.Where(i => i != "").ToArray());


        }

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

        private void RemoveIldasmInfoFromDecompiledDll(ref string[] dllFileContent)
        {

            for (int i = 0; i < dllFileContent.Count(); i++)
            {
                //remove ildasm command line
                if (dllFileContent[i].Contains("ildasm"))
                {
                    dllFileContent[i] = String.Empty;
                    break;
                }
            }

        }

    }
}
