# Overcooked! 2 Mods

OC2-Modding is a general-purpose modding framework for PC version of *Overcooked! 2*.

There are two main uses for it right now:
- [Archipelago Randomizer/MultiWorld](https://archipelago.gg/games/Overcooked!%202/info/en)
    - Visit the Archipelago Site for setup/usage
- Playing the normal game with quality of life improvements
    - Edit `OC2Modding.cfg` after installation

# How To Install

1. Download and extract the [Latest Release](https://github.com/toasterparty/oc2-modding/releases)

2. Double click `oc2-modding-install.bat` and use the file picker window to select your game's .exe file

3. Run the game once, wait until you reach the main menu, and then close it

4. Open `<path-to-game>\Overcooked! 2\BepInEx\config\OC2Modding.cfg` with your favorite text editor and configure your game to taste

# How To Build (read only if you are a developer)

1. Install latest [.NET sdk](https://dotnet.microsoft.com/en-us/)

2. Copy the following DLLs from `...\Overcooked! 2\Overcooked2_Data\Managed` to `lib\`:

```
Assembly-CSharp.dll
Assembly-CSharp-firstpass.dll
UnityEngine.CoreModule.dll
UnityEngine.dll
UnityEngine.PhysicsModule.dll
UnityEngine.TextRenderingModule.dll
UnityEngine.UI.dll
UnityEngine.UIModule.dll
```

3. Build

Run

```
tools\build.bat
```

This will create a folder called `dist` and populate it's contents with the mod distributables.

4. Install

Run

```
\dist\oc2-modding-install.bat
```

and follow the instructions.

5. Enable Logging:

Edit `...\Overcooked! 2\BepInEx\config\BepInEx.cfg` to enable console logging. This is very helpful for debugging.
