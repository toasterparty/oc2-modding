# Overcooked! 2 Mods

OC2-Modding is a general-purpose modding framework for Team 17's hit game *Overcooked! 2*.

There are two main uses for it right now:
- [Archipelago Randomizer/MultiWorld](https://archipelago.gg/games/Overcooked!%202/info/en)
    - Visit the Archipealgo Site for setup/usage
- Playing the normal game with quality of life improvements
    - Edit `OC2Modding.cfg` after installation

This has only been tested on Steam, but in theory it *should* work with Epic.

# How To Install

1. Download and extract the [Latest Release](https://github.com/toasterparty/oc2-modding/releases)

2. Double click `oc2-modding-install.bat` and use the file picker window to select your Game's .exe file

3. Run the game once, wait until you reach the main menu, and then close it

4. Open `<path-to-game>\Overcooked! 2\BepInEx\config\OC2Modding.cfg` with your favorite text editor and configure your game to taste

# How To Build (read only if you are a developer)

1. Install latest [.NET sdk](https://dotnet.microsoft.com/en-us/)

2. Copy the following DLLs to `/lib/`:

```
UnityEngine.dll
UnityEngine.CoreModule.dll 
UnityEngine.PhysicsModule.dll 
UnityEngine.TextRenderingModule.dll 
UnityEngine.UI.dll 
UnityEngine.UIModule.dll 
Assembly-CSharp.dll
```

3. Build
```
tools\build.bat
```

This will create a folder called `dist` and populate it with the plugin dll file. 

4. Extract the contents of [BepInEx_x64_5.4.21.0](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.21) into the `dist` folder

5. Extract the contents of [BepInEx_x64_5.4.21.0](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.21) into the `dist` folder

6. Extract the contents of [curl/bin](https://curl.se/download.html) into the `dist` folder

If done right, your directory should look like this:

```
\dist\
    BepInEx_x86_5.4.21.0
    BepInEx_UnityMono_x64_6.0.0-pre.1
    curl
    com.github.toasterparty.OC2Modding.dll
    steam_doorstop_config.ini
    oc2-modding-install.bat
    oc2-modding-uninstall.bat

\dist\BepInEx_*\
    BepInEx
    changelog.txt
    doorstop_config.ini
    winhttp.dll

\dist\curl\
    curl

\dist\curl\curl\
    curl-ca-bundle.crt
    curl.exe
    libcurl-x64.def
    libcurl-x64.dll
```

6. Install
```
dist\oc2-modding-install.bat
```

and follow the instructions.

7. Enable Logging:

Edit `...\Overcooked! 2\BepInEx\config\BepInEx.cfg` to enable console logging. This is very helpful for debugging.
