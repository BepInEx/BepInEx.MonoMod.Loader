using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using Mono.Cecil;

namespace BepInEx.MonoMod.Loader
{
	public static class Patcher
	{
		public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };

		private static ManualLogSource Logger = Logging.Logger.CreateLogSource("MonoMod");

		public static string[] ResolveDirectories { get; set; } =
		{
			Paths.BepInExAssemblyDirectory,
			Paths.ManagedPath,
			Paths.PatcherPluginPath,
			Paths.PluginPath
		};

		public static void Patch(AssemblyDefinition assembly)
		{
			Environment.SetEnvironmentVariable("MONOMOD_DMD_TYPE", "Cecil");
			
			string monoModPath = Path.Combine(Paths.BepInExRootPath, "monomod");

			if (!Directory.Exists(monoModPath))
				Directory.CreateDirectory(monoModPath);
			

			using (var monoModder = new RuntimeMonoModder(assembly, Logger))
			{
				monoModder.LogVerboseEnabled = false;

				monoModder.DependencyDirs.AddRange(ResolveDirectories);

				var resolver = (BaseAssemblyResolver)monoModder.AssemblyResolver;

				foreach (var dir in ResolveDirectories)
					resolver.AddSearchDirectory(dir);

				resolver.ResolveFailure += ResolverOnResolveFailure;

				monoModder.PerformPatches(monoModPath);
			}
		}

		private static AssemblyDefinition ResolverOnResolveFailure(object sender, AssemblyNameReference reference)
		{
			foreach (var directory in ResolveDirectories)
			{
				var potentialDirectories = new List<string> { directory };

				potentialDirectories.AddRange(Directory.GetDirectories(directory, "*", SearchOption.AllDirectories));

				var potentialFiles = potentialDirectories.Select(x => Path.Combine(x, $"{reference.Name}.dll"))
					.Concat(potentialDirectories.Select(x => Path.Combine(x, $"{reference.Name}.exe")));

				foreach (string path in potentialFiles)
				{
					if (!File.Exists(path))
						continue;

					var assembly = AssemblyDefinition.ReadAssembly(path, new ReaderParameters(ReadingMode.Deferred));

					if (assembly.Name.Name == reference.Name)
						return assembly;

					assembly.Dispose();
				}
			}

			return null;
		}
	}
}