<# : install.bat
:: Based on https://stackoverflow.com/a/15885133/1683264

@echo off
setlocal

set DIST_DIR="%~dp0"
set NO_PAUSE=%~1
set BEPINEX_VER=BepInEx_x86_5.4.21.0

echo.
echo Overcooked! 2 Mod Installer
echo.

if not exist %DIST_DIR%\com.github.toasterparty.OC2Modding.dll echo Error: Must run from 'dist' dir, not 'tools' dir
if not exist %DIST_DIR%\com.github.toasterparty.OC2Modding.dll echo.
if not exist %DIST_DIR%\com.github.toasterparty.OC2Modding.dll exit 1

if "%~2" == "" goto blank
call :install %2
goto :EOF

:blank

echo Please select your game executable
echo Typically ".../steam/steamapps/common/Overcooked! 2/Overcooked2.exe"
echo.

for /f "delims=" %%I in ('powershell -noprofile "iex (${%~f0} | out-string)"') do call :install "%%~I"
goto :EOF

:install
set GAME_DIR="%~dp1"
set BEPINEX_DIR="%~dp1\BepInEx\"
set PLUGINS_DIR="%~dp1\BepInEx\plugins"

if not exist %PLUGINS_DIR% mkdir %PLUGINS_DIR%

echo Installing 'OC2 Modding' into %PLUGINS_DIR%...
echo.

xcopy %DIST_DIR%\*.dll %PLUGINS_DIR% /y /q
xcopy %DIST_DIR%\oc2-modding-uninstall.bat %GAME_DIR% /y /q
xcopy %DIST_DIR%\%BEPINEX_VER% %GAME_DIR% /y /q /s /e
xcopy %DIST_DIR%\curl %GAME_DIR% /y /q /s /e

echo.
echo Successfully installed 'OC2 Modding'
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
