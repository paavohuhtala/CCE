# Cities Compiler Extender - CCE

A tool for [Cities: Skylines](http://www.citiesskylines.com/) for easier mod development.

## TL;DR
Allows you to use more stuff in scripting mods without having to use an external compiler.

## Why?
Cities: Skylines comes with a built-in C# compiler, which allows you to hot-compile and hot-load mod scripts while running the game. Unfortunately you can only use certain assemblies, which means that the mods developed this way are heavily limited, since the most powerful & useful APIs are not available.

Luckily, you can compile your mods with an external compiler to bypass the limitations, but by default the game won't reload your mods without a restart. You can delete your mod binary and change the assembly version every time you recompile it ([which can be automated in an IDE](https://www.reddit.com/r/CitiesSkylinesModding/comments/2ypcl5/guide_using_visual_studio_2013_to_develop_mods/)) to enable hot-loading, but it is not ideal.

This tool gives you the ability to use additional assembly references in code-only mods. This is accomplished by modifying one of the game's assemblies (namely Assembly-CSharp.dll) to change the internal compiler's configuration to include Assembly-CSharp.dll and ColossalManaged.dll into the list of referenced assemblies.

## Caveats
You have to (most likely) run this tool again after each game update. This tool might (and probably will) stop working at some point in the future, because it relies on the bytecode of Starter.Awake() not changing too much.

## How to use
```
.NET
CCE path-to-the-game-directory

Mono
mono CCE path-to-the-game-directory
```

The path must be an absolute path to the game's installation directory (for example "C:/Program Files (x86)/Steam/steamapps/common/Cities_Skylines").
The tool creates a new file, which is called Assembly-CSharp.mod.dll, in Cities_Data/Managed. **This tool won't overwrite the existing Assembly-CSharp.dll. You have to backup the current version of the file and replace it yourself with the modified version.**

## How to build
If you have installed make and Mono, just run 'make' in the root directory of the project. If you haven't, you can create a Visual Studio/Monodevelop solution yourself.
The project requires [Mono.Cecil](https://github.com/jbevain/cecil), which is included in /Dependencies.

## Prebuilt binaries
Not currently available.

## Todo
* Automatic backup of Assembly-CSharp.dll
* A Visual Studio project file
* Better error handling
  * For example, a warning if the .dll has been already modified

## License
See LICENSE.txt.