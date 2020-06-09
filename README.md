# BepInEx.MonoMod.Loader

This is loader for [MonoMod.MonoModPatches](https://github.com/MonoMod/MonoMod/blob/master/README.md#using-monomod) 
suited for use in BepInEx. 

Main features

* **No permanent patching** -- all patches are applied at runtime without permanent changes to DLLs
* **Easy install and uninstall** -- simply add/remove patch DLLs to install/uninstall patches

## Installation

1. Download the latest version from [releases](https://github.com/BepInEx/BepInEx.MonoMod.Loader/releases)
2. Extract the contents of the archive into the game folder (the folder that contains `BepInEx` folder)
3. Install MonoMod patches into `BepInEx/monomod` folder

## Notes about writing MonoMod patches

* [Write MonoMod patches normally](https://github.com/MonoMod/MonoMod/blob/master/README.md#using-monomod)
* **Name your patch DLL as follows:** `<Assembly>.<ModName>.mm.dll` where 
    * `<Assembly>` is the *name of the assembly you want to patch*
    * `<ModName>` is the *name of your mod*
* **NOTE:** Because of the naming convention, you can also *patch a single assembly per DLL*. If you need to patch multiple assemblies, write multiple DLLs.
