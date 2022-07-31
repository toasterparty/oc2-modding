# Build

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
