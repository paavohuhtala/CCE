using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.CodeDom.Compiler;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CitiesCompilerExtender
{
	public static class Program
	{
		public static void Main(string[] args)
		{
			Console.WriteLine("CitiesCompilerExtender 0.1.0");

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

			var gameBinary = Path.Combine(installDir, "Cities_Data", "Managed", "Assembly-CSharp.dll");
			var targetBinary = Path.Combine(installDir, "Cities_Data", "Managed", "Assembly-CSharp.mod.dll");

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

			ModifyAssembly(gameBinary, targetBinary, assemblies);
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
			int index = 1;
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
	}
}