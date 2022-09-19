@echo off

if not exist Overcooked2.exe echo Error: Uninstall script is not in game folder
if not exist Overcooked2.exe pause
if not exist Overcooked2.exe exit 1

del /f /q changelog.txt 
del /f /q winhttp.dll
del /f /q doorstop_config.ini
del /f /q leaderboard_scores.json
del /f /q leaderboard_scores.csv
del /f /q OC2Modding.json
del /f /q output_log.txt
del /f /q /s BepInEx
del /f /q /s curl
rmdir BepInEx /S /Q
rmdir curl /S /Q
del "%~f0"
