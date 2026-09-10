@echo off
setlocal
set "DIR=%~dp0"
echo Tworzenie skrotu na Pulpicie...
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$ws = New-Object -ComObject WScript.Shell; $target = if (Test-Path '%DIR%ScrcpyManager-Portable.exe') { '%DIR%ScrcpyManager-Portable.exe' } else { '%DIR%ScrcpyApp.cmd' }; $s = $ws.CreateShortcut([IO.Path]::Combine([Environment]::GetFolderPath('Desktop'), 'scrcpy Manager.lnk')); $s.TargetPath = $target; $s.WorkingDirectory = '%DIR%'; if (Test-Path '%DIR%app.ico') { $s.IconLocation = '%DIR%app.ico,0' }; $s.Description = 'scrcpy Manager'; $s.Save(); [System.Windows.Forms.MessageBox]::Show('Prawidlowo utworzono skrot do scrcpy Manager na Twoim Pulpicie!', 'scrcpy Manager', [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Information) | Out-Null"
endlocal