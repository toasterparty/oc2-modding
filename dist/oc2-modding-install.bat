<# : install.bat
:: Based on https://stackoverflow.com/a/15885133/1683264

@echo off
setlocal

set DIST_DIR="%~dp0"
set NO_PAUSE=%~1

set STEAM_BEPINEX_VER=BepInEx_x86_5.4.21.0
set EPIC_BEPINEX_VER=BepInEx_UnityMono_x64_6.0.0-pre.1

echo.
echo Overcooked! 2 Mod Installer
echo.

if not exist %DIST_DIR%\com.github.toasterparty.OC2Modding.dll goto fail
if not exist %DIST_DIR%\curl\curl\curl.exe goto fail
if not exist %DIST_DIR%\%STEAM_BEPINEX_VER% goto fail
if not exist %DIST_DIR%\%EPIC_BEPINEX_VER% goto fail

goto check_params

:fail

echo Corrupt installation package. If you are a developer, please refer to the README.
echo.

pause
exit 1

:check_params

if "%~2" == "" goto blank
call :install %2
goto :EOF

:blank

echo Please find and double click your game executable...
echo Typically "C:\Program Files\Steam\steamapps\common\Overcooked! 2\Overcooked2.exe"
echo.

for /f "delims=" %%I in ('powershell -noprofile "iex (${%~f0} | out-string)"') do call :install "%%~I"
goto :EOF

:install
set GAME_DIR="%~dp1"
set BEPINEX_DIR="%~dp1\BepInEx\"
set PLUGINS_DIR="%~dp1\BepInEx\plugins"

echo Installing 'OC2 Modding' into %PLUGINS_DIR%...
echo.

if not exist %BEPINEX_DIR% mkdir %BEPINEX_DIR%
if not exist %PLUGINS_DIR% mkdir %PLUGINS_DIR%

if     exist "%~dp1\UnityCrashHandler64.exe" xcopy %DIST_DIR%\%EPIC_BEPINEX_VER%  %GAME_DIR% /y /q /s /e
if not exist "%~dp1\UnityCrashHandler64.exe" xcopy %DIST_DIR%\%STEAM_BEPINEX_VER% %GAME_DIR% /y /q /s /e

xcopy %DIST_DIR%\*.dll %PLUGINS_DIR% /y /q
xcopy %DIST_DIR%\oc2-modding-uninstall.bat %GAME_DIR% /y /q
xcopy %DIST_DIR%\curl %GAME_DIR% /y /q /s /e

if not exist "%~dp1\UnityCrashHandler64.exe" xcopy %DIST_DIR%\steam_doorstop_config.ini %GAME_DIR%\doorstop_config.ini /y /q

echo.
echo Successfully installed 'OC2 Modding'
echo (You may now close this window)
echo.

if "%NO_PAUSE%" == "nopause" goto :EOF

pause

goto :EOF

: end Batch portion / begin PowerShell hybrid chimera #>

Add-Type -AssemblyName System.Windows.Forms
$f = new-object Windows.Forms.OpenFileDialog
$f.InitialDirectory = pwd
$f.Filter = "Overcooked! 2 (*.exe)|*.exe|All Files (*.*)|*.*"
$f.ShowHelp = $true
$f.Multiselect = $true
[void]$f.ShowDialog()
if ($f.Multiselect) { $f.FileNames } else { $f.FileName }
