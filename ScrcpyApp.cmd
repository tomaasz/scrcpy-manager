@echo off
setlocal
set "DIR=%~dp0"
if exist "%DIR%ScrcpyApp.exe" (
    start "" "%DIR%ScrcpyApp.exe" %*
) else (
    start "" powershell.exe -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File "%DIR%ScrcpyApp.ps1" %*
)
endlocal