using System.Collections.Generic;
using System.IO;
using BepInEx.Logging;
using Mono.Cecil;

namespace BepInEx.MonoMod.Loader
{
	public static class Patcher
	{
		public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };

		private static ManualLogSource Logger = Logging.Logger.CreateLogSource("MonoMod");

		public static void Patch(AssemblyDefinition assembly)
		{
			string assemblyFolder = new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath;

			string monoModPath = Path.Combine(Paths.BepInExRootPath, "monomod");

			if (!Directory.Exists(monoModPath))
				Directory.CreateDirectory(monoModPath);

			string[] resolveDirectories =
			{
				Paths.BepInExAssemblyDirectory,
				Paths.ManagedPath,
				Paths.PatcherPluginPath,
				assemblyFolder,
				Paths.PluginPath
			};

			using (var monoModder = new RuntimeMonoModder(assembly, Logger))
			{
				monoModder.LogVerboseEnabled = false;

				monoModder.DependencyDirs.AddRange(resolveDirectories);

				var resolver = (BaseAssemblyResolver)monoModder.AssemblyResolver;

				foreach (var dir in resolveDirectories)
					resolver.AddSearchDirectory(dir);

				monoModder.PerformPatches(monoModPath);
			}
		}
	}
}