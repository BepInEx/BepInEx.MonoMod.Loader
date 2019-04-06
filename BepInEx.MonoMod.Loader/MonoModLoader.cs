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
			string monoModPath = Path.Combine(Paths.BepInExRootPath, "monomod");

			if (!Directory.Exists(monoModPath))
				Directory.CreateDirectory(monoModPath);

			using (var monoModder = new RuntimeMonoModder(assembly, Logger))
			{
				monoModder.LogVerboseEnabled = false;
				monoModder.PerformPatches(monoModPath);
			}
		}
	}
}