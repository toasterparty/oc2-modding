@echo off

set SRC_DIR="%~dp0\..\OC2Modding"
set DIST_DIR="%~dp0\..\dist"
set BUILD_DIR="%~dp0\..\OC2Modding\bin\Debug"
set TOOLS_DIR="%~dp0\..\tools"

if not exist %DIST_DIR% mkdir %DIST_DIR%

cd %SRC_DIR% || exit 1

xcopy %SRC_DIR%\OC2Modding.csproj.epic %SRC_DIR%\OC2Modding.csproj /y /q || exit 1
dotnet restore || exit 1
dotnet build || exit 1

xcopy %SRC_DIR%\OC2Modding.csproj.steam %SRC_DIR%\OC2Modding.csproj /y /q || exit 1
dotnet restore || exit 1
dotnet build || exit 1

xcopy %BUILD_DIR%\net46\com.github.toasterparty.oc2modding.epic.dll %DIST_DIR% /y /q || exit 1
xcopy %BUILD_DIR%\net35\com.github.toasterparty.oc2modding.steam.dll %DIST_DIR% /y /q || exit 1

echo.
echo Successfully built 'OC2 Modding'
echo.
