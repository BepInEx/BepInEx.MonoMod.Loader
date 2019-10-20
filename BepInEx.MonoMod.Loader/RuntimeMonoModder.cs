using System.IO;
using BepInEx.Logging;
using Mono.Cecil;
using MonoMod;

namespace BepInEx.MonoMod.Loader
{
	public class RuntimeMonoModder : MonoModder
	{
		protected ManualLogSource Logger { get; set; }

		public RuntimeMonoModder(AssemblyDefinition assembly, ManualLogSource logger)
		{
			Module = assembly.MainModule;
			Logger = logger;
		}

		public override void Log(object value)
			=> Logger.LogMessage(value);

		public override void Log(string value)
			=> Logger.LogMessage(value);

		public override void LogVerbose(object value)
		{
			if (!LogVerboseEnabled)
				return;

			Logger.LogDebug(value);
		}

		public override void LogVerbose(string value)
		{
			if (!LogVerboseEnabled)
				return;

			Logger.LogDebug(value);
		}

		public void PerformPatches(string modDirectory)
		{
			Read();

			Log("[Main] Scanning for mods in directory.");

			ReadMod(modDirectory);

			foreach (var directory in Directory.GetDirectories(modDirectory, "*", SearchOption.AllDirectories))
				ReadMod(directory);

			MapDependencies();

            Log($"[Main] Found {Mods.Count} mods");

			Log("[Main] mm.PatchRefs(); fixup pre-pass");
			PatchRefs();

			Log("[Main] mm.AutoPatch();");
			AutoPatch();

			Log("[Main] Done.");
		}

		public override void Dispose()
		{
			Module = null;

			base.Dispose();
		}
	}
}