using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Preloader.Core.Patching;
using Mono.Cecil;

namespace BepInEx.MonoMod.Loader
{
    [PatcherPluginInfo("io.bepinex.monomodloader", "MonoMod Loader", "2.0")]
    public class Patcher : BasePatcher
    {
        public List<string> ResolveDirectories { get; set; } = new List<string>
        {
            Paths.BepInExAssemblyDirectory,
            Paths.PatcherPluginPath,
            Paths.PluginPath
        };
        
        private static readonly HashSet<string> UnpatchableAssemblies =
            new HashSet<string>(StringComparer.CurrentCultureIgnoreCase) { "mscorlib" };

        private HashSet<string> TargetAssemblies { get; set; }

        private readonly string monoModPath = Path.Combine(Paths.BepInExRootPath, "monomod");

        public override void Initialize()
        {
	        if (Directory.Exists(Paths.ManagedPath))
		        ResolveDirectories.Add(Paths.ManagedPath);

	        if (!Directory.Exists(monoModPath))
		        Directory.CreateDirectory(monoModPath);

	        TargetAssemblies = CollectTargetDLLs();
        }

        private HashSet<string> CollectTargetDLLs()
        {
            Log.LogInfo("Collecting target assemblies from mods");

            var result = new HashSet<string>();

            foreach (var modDll in Directory.GetFiles(monoModPath, "*.mm.dll", SearchOption.AllDirectories))
            {
	            Log.LogDebug($"Found '{modDll}'");

                var fileName = Path.GetFileNameWithoutExtension(modDll);
                try
                {
	                using (var ass = AssemblyDefinition.ReadAssembly(modDll))
	                {
		                foreach (var assRef in ass.MainModule.AssemblyReferences)
		                {
			                if (!UnpatchableAssemblies.Contains(assRef.Name) &&
			                    (fileName.StartsWith(assRef.Name, StringComparison.InvariantCultureIgnoreCase) ||
			                     fileName.StartsWith(assRef.Name.Replace(" ", ""),
				                     StringComparison.InvariantCultureIgnoreCase)))
			                {
				                result.Add($"{assRef.Name}.dll");
				                result.Add($"{assRef.Name}.exe");
			                }
		                }
	                }
                }
                catch (Exception ex)
                {
                    Log.LogDebug($"Ran into a problem scanning '{modDll}'; will skip. Exception: {ex}");
                }
            }

            return result;
        }

        [TargetAssembly(TargetAssemblyAttribute.AllAssemblies)]
        public bool Patch(ref AssemblyDefinition assembly, string targetDll)
        {
            if (!TargetAssemblies.Contains(targetDll))
                return false;

            using (var monoModder = new RuntimeMonoModder(assembly, Log))
            {
                monoModder.LogVerboseEnabled = false;

                monoModder.DependencyDirs.AddRange(ResolveDirectories);

                var resolver = (BaseAssemblyResolver)monoModder.AssemblyResolver;
                var moduleResolver = (BaseAssemblyResolver)monoModder.Module.AssemblyResolver;

                foreach (var dir in ResolveDirectories)
                    resolver.AddSearchDirectory(dir);

                resolver.ResolveFailure += ResolverOnResolveFailure;
                // Add our dependency resolver to the assembly resolver of the module we are patching
                moduleResolver.ResolveFailure += ResolverOnResolveFailure;

                monoModder.PerformPatches(monoModPath);

                // Then remove our resolver after we are done patching to not interfere with other patchers
                moduleResolver.ResolveFailure -= ResolverOnResolveFailure;
            }

            return true;
        }

        private AssemblyDefinition ResolverOnResolveFailure(object sender, AssemblyNameReference reference)
        {
            foreach (var directory in ResolveDirectories)
            {
                var potentialDirectories = new List<string> { directory };

                potentialDirectories.AddRange(Directory.GetDirectories(directory, "*", SearchOption.AllDirectories));

                var potentialFiles = potentialDirectories.Select(x => Path.Combine(x, $"{reference.Name}.dll"))
                                                         .Concat(potentialDirectories.Select(
                                                                     x => Path.Combine(x, $"{reference.Name}.exe")));

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