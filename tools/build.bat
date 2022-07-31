@echo off

set SRC_DIR="%~dp0\..\OC2Modding"
set DIST_DIR="%~dp0\..\dist"
set BUILD_DIR="%~dp0\..\OC2Modding\bin\Debug\net35\"
set TOOLS_DIR="%~dp0\..\tools"

if not exist %DIST_DIR% mkdir %DIST_DIR%

cd %SRC_DIR% || exit 1
dotnet build || exit 1

xcopy %BUILD_DIR%\com.github.toasterparty.OC2Modding.dll %DIST_DIR% /y /q || exit 1
xcopy %TOOLS_DIR%\oc2-modding-install.bat %DIST_DIR% /y /q || exit 1
xcopy %TOOLS_DIR%\oc2-modding-uninstall.bat %DIST_DIR% /y /q || exit 1

echo.
echo Successfully built 'OC2 Modding'
echo.
