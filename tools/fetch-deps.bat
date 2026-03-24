@echo off
setlocal EnableExtensions

set "DIST_DIR=%~dp0..\dist"
if not exist "%DIST_DIR%" mkdir "%DIST_DIR%"

set "TMP_DIR=%TEMP%\oc2-modding-bepinex"
if not exist "%TMP_DIR%" mkdir "%TMP_DIR%"

call :fetch_and_extract ^
  "https://github.com/BepInEx/BepInEx/releases/download/v5.4.21/BepInEx_x86_5.4.21.0.zip" ^
  "BepInEx_x86_5.4.21.0.zip" ^
  "BepInEx_x86_5.4.21.0" || exit /b 1

call :fetch_and_extract ^
  "https://github.com/BepInEx/BepInEx/releases/download/v6.0.0-pre.1/BepInEx_UnityMono_x64_6.0.0-pre.1.zip" ^
  "BepInEx_UnityMono_x64_6.0.0-pre.1.zip" ^
  "BepInEx_UnityMono_x64_6.0.0-pre.1" || exit /b 1

call :fetch_and_extract ^
  "https://curl.se/windows/dl-8.19.0_4/curl-8.19.0_4-win64-mingw.zip" ^
  "curl-8.19.0_4-win64-mingw.zip" ^
  "curl" || exit /b 1

if exist "%DIST_DIR%\curl\" del /f /q "%ZIP_PATH%" >nul 2>&1

if exist "%DIST_DIR%\curl\curl-8.19.0_4-win64-mingw" (
  for /f "delims=" %%I in ('dir /b /a "%DIST_DIR%\curl\curl-8.19.0_4-win64-mingw"') do (
    if /i not "%%~nxI"=="bin" (
      if exist "%DIST_DIR%\curl\curl-8.19.0_4-win64-mingw\%%~nxI\" (
        rd /s /q "%DIST_DIR%\curl\curl-8.19.0_4-win64-mingw\%%~nxI"
      ) else (
        del /f /q "%DIST_DIR%\curl\curl-8.19.0_4-win64-mingw\%%~nxI" >nul 2>&1
      )
    )
  )

  if exist "%DIST_DIR%\curl\curl" rd /s /q "%DIST_DIR%\curl\curl"
  ren "%DIST_DIR%\curl\curl-8.19.0_4-win64-mingw" "curl"

  if exist "%DIST_DIR%\curl\curl\bin" (
    xcopy "%DIST_DIR%\curl\curl\bin\*" "%DIST_DIR%\curl\curl\" /E /I /Y >nul
    rd /s /q "%DIST_DIR%\curl\curl\bin"
  )
)

exit /b 0

:fetch_and_extract
set "URL=%~1"
set "ZIP_NAME=%~2"
set "EXTRACT_DIR=%DIST_DIR%\%~3"
set "ZIP_PATH=%TMP_DIR%\%~2"

if exist "%EXTRACT_DIR%" (
  exit /b 0
)

echo Fetch %ZIP_NAME%...

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "Invoke-WebRequest -Uri '%URL%' -OutFile '%ZIP_PATH%'"
if errorlevel 1 (
  if exist "%ZIP_PATH%" del /f /q "%ZIP_PATH%" >nul 2>&1
  exit /b 1
)

if not exist "%EXTRACT_DIR%" mkdir "%EXTRACT_DIR%"

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "Expand-Archive -Path '%ZIP_PATH%' -DestinationPath '%EXTRACT_DIR%' -Force"
if errorlevel 1 (
  if exist "%ZIP_PATH%" del /f /q "%ZIP_PATH%" >nul 2>&1
  exit /b 1
)

if exist "%ZIP_PATH%" del /f /q "%ZIP_PATH%" >nul 2>&1

exit /b 0
