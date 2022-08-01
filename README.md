# Overcooked! 2 Mods

🤠

# How To Install

1. Download and extract the [Latest Release](https://github.com/toasterparty/oc2-modding/releases)

2. Double click `oc2-modding-install.bat` and use the file picker window to select your Game's .exe file

3. Run the game once, wait until you reach the main menu, and then close it

4. Open `<path-to-game>\Overcooked! 2\BepInEx\config\OC2Modding.cfg` with your favorite text editor and configure your game to taste

# How To Build (Read only if you are a developer)

1. Install the contents of [BepInEx_x64_5.4.21.0](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.21) into the game folder

2. Install latest [.NET sdk](https://dotnet.microsoft.com/en-us/)

3. Copy the following DLLs to `/lib/`:

```
UnityEngine.dll
UnityEngine.CoreModule.dll
Assembly-CSharp.dll
```

4. Build
```
tools\build.bat
```

5. Install
```
dist\oc2-modding-install.bat
```
