@echo off
setlocal
set "DIR=%~dp0"
if exist "%DIR%ScrcpyManager-Portable.exe" (
    start "" "%DIR%ScrcpyManager-Portable.exe" %*
) else (
    start "" powershell.exe -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File "%DIR%ScrcpyApp.ps1" %*
)
endlocal