# Overcooked! 2 Mods

🤠

# How To Install

1. Download and extract the [Latest Release](https://github.com/toasterparty/oc2-modding/releases)

2. Double click `oc2-modding-install.bat` and use the file picker window to select your Game's .exe file

3. Run the game once, wait until you reach the main menu, and then close it

4. Open `<path-to-game>\Overcooked! 2\BepInEx\config\OC2Modding.cfg` with your favorite text editor and configure your game to taste

# How To Build (Read only if you are a developer)

1. Install latest [.NET sdk](https://dotnet.microsoft.com/en-us/)

2. Copy the following DLLs to `/lib/`:

```
UnityEngine.dll
UnityEngine.CoreModule.dll
Assembly-CSharp.dll
```

3. Build
```
tools\build.bat
```

This will create a folder called `dist` and populate it with the plugin dll file. 

4. Extract the contents of [BepInEx_x64_5.4.21.0](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.21) into the `dist` folder

5. Extract the contents of [curl/bin](https://curl.se/download.html) into the `dist` folder

If done right, your directory should look like this:

```
> dir -r

    Directory: ...\dist
d-----         7/31/2022   9:24 AM                BepInEx_x86_5.4.21.0
d-----          8/3/2022   8:28 AM                curl
-a----          8/3/2022   8:26 AM          19456 com.github.toasterparty.OC2Modding.dll
-a----          8/3/2022   8:27 AM           1722 oc2-modding-install.bat
-a----          8/3/2022   8:26 AM            132 oc2-modding-uninstall.bat


    Directory: ...\dist\BepInEx_x86_5.4.21.0
d-----         7/31/2022   9:24 AM                BepInEx
------         7/19/2022   8:28 PM            375 changelog.txt
------         4/11/2021   9:29 AM            891 doorstop_config.ini
------         7/19/2022   8:28 PM          20992 winhttp.dll


    Directory: ...\dist\BepInEx_x86_5.4.21.0\BepInEx
d-----         7/31/2022   9:24 AM                core


    Directory: ...\dist\BepInEx_x86_5.4.21.0\BepInEx\core
------          2/2/2022   6:28 PM         204800 0Harmony.dll
------             ...redacted for brevity...

    Directory: ...\dist\curl
d-----          8/3/2022   8:28 AM                curl


    Directory: ...\dist\curl\curl
-a----         7/19/2022   3:12 AM         222477 curl-ca-bundle.crt
-a----         6/27/2022   6:12 AM        5918280 curl.exe
-a----         6/27/2022   6:12 AM           2206 libcurl-x64.def
-a----         6/27/2022   6:12 AM        5716040 libcurl-x64.dll
```

6. Install
```
dist\oc2-modding-install.bat
```

and follow the instructions.
