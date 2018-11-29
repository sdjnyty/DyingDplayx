using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;
using System.Reflection;

namespace DllExporter
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1 || !File.Exists(args[0]))
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("1. Add reference to DllExporter.exe to your project;");
                Console.WriteLine("2. Add DllExporter.DllExport attribute to static methods that will be exported;");
                Console.WriteLine("3. Add following post-build command to project properties:");
                Console.WriteLine("    DllExporter.exe $(TargetFileName)");
                Console.WriteLine("    move $(TargetName).Exports$(TargetExt) $(TargetFileName)");
                Console.WriteLine("4. Build project;");
                Console.WriteLine("5. ???");
                Console.WriteLine("6. PROFIT!");

                return;
            }

            try
            {
                var workDir = GetWorkingDirectory();
                var assemblerPath = GetAssemblerPath();
                var disassemblerPath = GetDisassemblerPath();

                var methods = GetMethods(args[0]);
                var sourcePath = Disassemble(disassemblerPath, args[0], workDir.FullName);
                var sourceOutPath = workDir + @"\output.il";
                ProcessSource(sourcePath, sourceOutPath, methods);
                var outPath = Assemble(assemblerPath, args[0], workDir.FullName);

                var newPath = Path.Combine(Path.GetDirectoryName(args[0]), Path.GetFileNameWithoutExtension(outPath) + ".Exports" + Path.GetExtension(outPath));
                File.Delete(newPath);
                File.Move(outPath, newPath);

                workDir.Delete(true);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                Environment.Exit(1);
            }
        }

        static DirectoryInfo GetWorkingDirectory()
        {
            var path = Environment.ExpandEnvironmentVariables(@"%TEMP%\DllExporter");

            var directory = new DirectoryInfo(path);

            if (!directory.Exists)
                directory.Create();

            return directory;
        }

        static string GetDisassemblerPath()
        {

            return @"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.7.2 Tools\ildasm.exe";
        }

        static string GetAssemblerPath()
        {
            var path = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\Microsoft.NET\Framework\v2.0.50727\ilasm.exe");       

            if (!File.Exists(path))
                throw new Exception("Cannot locate ilasm.exe.");

            return path;
        }

        static string Disassemble(string disassemblerPath, string assemblyPath, string workDir)
        {
            var sourcePath = string.Format(@"{0}\input.il", workDir);
            var startInfo = new ProcessStartInfo(disassemblerPath, string.Format(@"""{0}"" /out:""{1}""", assemblyPath, sourcePath)) { WindowStyle = ProcessWindowStyle.Hidden };
            var process = Process.Start(startInfo);

            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new Exception(string.Format("ildasm.exe has failed disassembling {0}.", assemblyPath));

            return sourcePath;
        }

        static string Assemble(string assemblerPath, string assemblyPath, string workDir)
        {
            var outPath = string.Format(@"{0}\{1}", workDir, Path.GetFileName(assemblyPath));
            var resourcePath = string.Format(@"{0}\{1}", workDir, "input.res");

            var args = new StringBuilder();
            args.AppendFormat(@"""{0}\output.il"" /out:""{1}""", workDir, outPath);
            if (Path.GetExtension(assemblyPath) == ".dll")
                args.Append(" /dll");
            if (File.Exists(resourcePath))
                args.AppendFormat(@" /res:""{0}""", resourcePath);

            var startInfo = new ProcessStartInfo(assemblerPath, args.ToString()) { WindowStyle = ProcessWindowStyle.Hidden };
            var process = Process.Start(startInfo);

            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new Exception("ilasm.exe has failed assembling generated source.");

            return outPath;
        }

        static List<string> GetMethods(string assemblyPath)
        {
            var methods = new List<string>();

            var assembly = Assembly.LoadFrom(assemblyPath);

            foreach (var type in assembly.GetTypes())
            {
                foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    var attributes = method.GetCustomAttributes(typeof(DllExportAttribute), false);
                    if (attributes.Length != 1)
                        continue;

                    var attribute = (DllExportAttribute)attributes[0];

                    methods.Add(attribute.EntryPoint ?? method.Name);
                }
            }

            return methods;
        }

        static void ProcessSource(string sourcePath, string outPath, List<string> methods)
        {
            using (var output = new StreamWriter(outPath, false, Encoding.Default))
            {
                var methodIndex = 0;
                var skipLines = 0;
                var openBraces = 0;
                var isMethodStatic = false;

                foreach (var line in File.ReadAllLines(sourcePath, Encoding.Default))
                {
                    if (skipLines > 0)
                    {
                        skipLines--;
                        continue;
                    }

                    if (line.TrimStart(' ').StartsWith(".assembly extern DllExporter"))
                    {
                        skipLines = 3;
                        continue;
                    }

                    if (line.TrimStart(' ').StartsWith(".corflags"))
                    {
                        output.WriteLine(".corflags 0x00000002");

                        for (int i = 1; i <= methods.Count; i++)
                            output.WriteLine(".vtfixup [1] int32 fromunmanaged at VT_{0}", i);

                        for (int i = 1; i <= methods.Count; i++)
                            output.WriteLine(".data VT_{0} = int32(0)", i);

                        continue;
                    }

                    if (line.TrimStart(' ').StartsWith(".method"))
                        isMethodStatic = line.Contains(" static ");

                    if (line.TrimStart(' ').StartsWith(".custom instance void [DllExporter]DllExporter.DllExportAttribute"))
                    {
                        foreach (var ch in line)
                        {
                            if (ch == '(')
                                openBraces++;
                            if (ch == ')')
                                openBraces--;
                        }

                        if (isMethodStatic)
                        {
                            output.WriteLine(".vtentry {0} : 1", methodIndex + 1);
                            output.WriteLine(".export [{0}] as {1}", methodIndex + 1, methods[methodIndex]);

                            methodIndex++;
                        }

                        continue;
                    }

                    if (openBraces > 0)
                    {
                        foreach (var ch in line)
                        {
                            if (ch == '(')
                                openBraces++;
                            if (ch == ')')
                                openBraces--;
                        }

                        continue;
                    }

                    output.WriteLine(line);
                }
            }
        }
    }
}
