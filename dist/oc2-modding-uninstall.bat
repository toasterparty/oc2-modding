@echo off
del /f /q changelog.txt 
del /f /q winhttp.dll
del /f /q doorstop_config.ini
del /f /q leaderboard_scores.csv
del /f /q output_log.txt
del /f /q /s BepInEx
del /f /q /s curl
rmdir BepInEx /S /Q
rmdir curl /S /Q