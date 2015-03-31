using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CitiesCompilerExtender
{
	public static class Program
	{
		public static void Main(string[] args)
		{
			Console.WriteLine("CitiesCompilerExtender 0.1.1");

			if(args.Length < 1 || args[0] == "--help" || args[0] == "/help")
			{
				ShowUsage();
				return;
			}

			var installDir = args[0].Trim('"');

			if(!Directory.Exists(installDir))
			{
				Console.WriteLine("Directory '{0}' does not exist.", installDir);
				return;
			}

			var baseDir = string.Empty;

			var os = DetectOperatingSystem();

			switch(os)
			{
				case OperatingSystem.Windows:
				case OperatingSystem.Linux:
					baseDir = Path.Combine(installDir, "Cities_Data");
					break;

				case OperatingSystem.OSX:
					baseDir = Path.Combine(installDir, "Cities.app", "Contents", "Resources", "Data");
					break;

				case OperatingSystem.Unknown:
					Console.WriteLine("Warning: Unsupported operating system!");
					// Handle unknown operating systems like Windows & Linux
					goto case OperatingSystem.Linux;
			}

			var managed = Path.Combine(baseDir, "Managed");
			var gameBinary = Path.Combine(managed, "Assembly-CSharp.dll");
			var outputBinary = Path.Combine(managed, "Assembly-CSharp.mod.dll");

			if(!File.Exists(gameBinary))
			{
				Console.WriteLine("Assembly-CSharp.dll not found at '{0}'.", gameBinary);
				Console.WriteLine("You must give the path to steamapps/common/Cities_Skylines.");
				return;
			}

			// The list of assemblies to inject into the compiler's reference list
			var assemblies = new List<string>
			{
				"Assembly-CSharp.dll",
				"ColossalManaged.dll"
			};

			ModifyAssembly(gameBinary, outputBinary, assemblies);
		}

		private static void ModifyAssembly(string filePath, string targetPath, List<string> assemblies)
		{
			var module = ModuleDefinition.ReadModule(filePath);
			Console.WriteLine("Read assembly: {0}", module.Assembly.FullName);

			var pluginManager = module.Types.Single(t => t.Name == "Starter");
			Console.WriteLine("Found type: {0}", pluginManager);

			var compileMethod = pluginManager.Methods.Single(m => m.Name == "Awake");
			Console.WriteLine("Found method: {0}", compileMethod);

			var instructions = compileMethod.Body.Instructions;

			var injectionLocation = instructions.Single(i => (i.OpCode == OpCodes.Call) && ((MethodReference) i.Operand).Name == "SetAdditionalAssemblies");
			Console.WriteLine("Found injection point #1: {0}", injectionLocation);

			var arraySizeInjection = instructions.Single(i => (i.OpCode == OpCodes.Call) && ((MethodReference) i.Operand).Name == "add_eventLogMessage").Next;
			Console.WriteLine("Found injection point #2: {0}", arraySizeInjection);

			var ilProcessor = compileMethod.Body.GetILProcessor();

			Console.WriteLine("Resizing reference array...");
			ilProcessor.Replace(arraySizeInjection, ilProcessor.Create(OpCodes.Ldc_I4, assemblies.Count + 1));

			Console.WriteLine("Adding references...");
			var index = 1;
			foreach(var assembly in assemblies)
			{
				// Duplicate array reference
				ilProcessor.InsertBefore(injectionLocation, ilProcessor.Create(OpCodes.Dup));
				// Push index to stack
				ilProcessor.InsertBefore(injectionLocation, ilProcessor.Create(OpCodes.Ldc_I4, index));
				// Push assembly name to stack
				ilProcessor.InsertBefore(injectionLocation, ilProcessor.Create(OpCodes.Ldstr, assembly));
				// Store assembly name at index
				ilProcessor.InsertBefore(injectionLocation, ilProcessor.Create(OpCodes.Stelem_Ref));
				index++;
			}

			Console.WriteLine("Injection done. Writing modified assembly to {0}.", targetPath);
			module.Write(targetPath);
		}

		private static void ShowUsage()
		{
			Console.WriteLine();
			Console.WriteLine("Usage: CCE cities-install-path");
			Console.WriteLine("This utility modifies the compiler internally used by Cities: Skylines to enable");
			Console.WriteLine("code-only mods to use additional assemblies.");
			Console.WriteLine();
			Console.WriteLine("Additional arguments:");
			Console.WriteLine();
			Console.WriteLine("--help OR /help\t\tShow this dialog.");
		}

		private static OperatingSystem DetectOperatingSystem()
		{
			var platform = Environment.OSVersion.Platform;

			switch (platform)
			{
			    case PlatformID.Win32NT:
			        return OperatingSystem.Windows;
			    case PlatformID.Unix:
			    case PlatformID.MacOSX:
			        var uname = GetProcessOutput("uname");

			        if (uname.Contains("Darwin"))
			        {
			            return OperatingSystem.OSX;
			        }
			        if (uname.Contains("Linux"))
			        {
			            return OperatingSystem.Linux;
			        }
			        break;
			}

		    return OperatingSystem.Unknown;
		}

		private enum OperatingSystem
		{
			Windows,
			Linux,
			OSX,
			Unknown
		}

		//Modified from https://blez.wordpress.com/2012/09/17/determine-os-with-netmono/
		private static string GetProcessOutput(string name)
		{
			try
			{
				var pInfo = new ProcessStartInfo(name)
				{
					UseShellExecute = false,
					RedirectStandardOutput = true
				};

				var p = Process.Start(pInfo);

			    if (p == null)
			    {
			        return string.Empty;;
			    }

				// Do not wait for the child process to exit before reading to
				// the end of its redirected stream.
				// Read the output stream first and then wait.
				var output = p.StandardOutput.ReadToEnd();
				p.WaitForExit();

				return output.Trim();
			}
			catch(FileNotFoundException)
			{
				// The executable couldn't be found -> return empty string
				return string.Empty;
			}
		}
	}
}