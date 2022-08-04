@echo off

set SRC_DIR="%~dp0\..\OC2Modding"
set DIST_DIR="%~dp0\..\dist"
set BUILD_DIR="%~dp0\..\OC2Modding\bin\Debug\net35\"
set TOOLS_DIR="%~dp0\..\tools"

if not exist %DIST_DIR% mkdir %DIST_DIR%

cd %SRC_DIR% || exit 1

ping 127.0.0.1 -n 1 > nul

dotnet build || exit 1

ping 127.0.0.1 -n 2 > nul

xcopy %BUILD_DIR%\com.github.toasterparty.OC2Modding.dll %DIST_DIR% /y /q || exit 1

echo.
echo Successfully built 'OC2 Modding'
echo.
