try {
    Add-Type -AssemblyName System.Windows.Forms
    Add-Type -AssemblyName System.Drawing

    # --- KONFIGURACJA ---
    # PIN jest pobierany ze zmiennej środowiskowej, aby nie trafiał do repozytorium.
    $adbPin = $env:SCRCPY_ADB_PIN

    # Domyślny rozmiar wirtualnego ekranu dla pojedynczych aplikacji (szer x wys)
    $appDisplaySize = "1080x2200"

    # Plik stanu do bezpiecznego zapamiętania pierwotnego limitu wygaszania ekranu
    $stateFile = Join-Path $env:TEMP "scrcpy_manager_original_timeout.txt"
    $script:launchedProcesses = New-Object 'System.Collections.Generic.List[System.Diagnostics.Process]'
    $script:originalTimeout = 30000 # Domyślny limit zapasowy (30 sekund)

    # --- FUNKCJE POMOCNICZE ---

    function Test-AdbDeviceSilent {
        $devices = adb devices 2>$null | Select-String "device$"
        return [bool]$devices
    }

    function Test-AdbDevice {
        if (-not (Test-AdbDeviceSilent)) {
            [System.Windows.Forms.MessageBox]::Show(
                "Nie wykryto podłączonego telefonu (adb devices nie zwraca urządzenia). Sprawdź kabel/USB debugging.",
                "Brak urządzenia",
                [System.Windows.Forms.MessageBoxButtons]::OK,
                [System.Windows.Forms.MessageBoxIcon]::Warning
            )
            return $false
        }
        return $true
    }

    function Initialize-ScreenTimeoutSettings {
        # 1. Sprawdź, czy w pliku tymczasowym mamy już zapisany pierwotny timeout
        if (Test-Path $stateFile) {
            $saved = Get-Content $stateFile -ErrorAction SilentlyContinue
            if ($saved -as [int] -and [int]$saved -gt 0 -and [int]$saved -lt 2147483647) {
                $script:originalTimeout = [int]$saved
                return
            }
        }

        # 2. Odczytaj bieżący limit z telefonu, jeśli podłączony
        if (Test-AdbDeviceSilent) {
            $current = (adb shell settings get system screen_off_timeout 2>$null)
            if ($current -and $current.Trim() -as [int]) {
                $val = [int]$current.Trim()
                if ($val -gt 0 -and $val -lt 2147483647) {
                    $script:originalTimeout = $val
                    Set-Content -Path $stateFile -Value $script:originalTimeout -ErrorAction SilentlyContinue
                }
            }
        }
    }

    function Invoke-AdbUnlock {
        if (-not (Test-AdbDeviceSilent)) { return }

        # 1. Wybudzenie ekranu
        adb shell input keyevent 224 2>$null

        # 2. Sprawdzenie, czy ekran jest zablokowany (Keyguard)
        $isLocked = [bool](adb shell dumpsys window 2>$null | Select-String "isKeyguardShowing=true")
        if ($isLocked -and -not [string]::IsNullOrWhiteSpace($adbPin)) {
            Start-Sleep -Milliseconds 400
            # Odsłonięcie pola na PIN (przesunięcie w górę)
            adb shell input swipe 500 1500 500 300 200 2>$null
            Start-Sleep -Milliseconds 400
            # Wpisanie PINu i zatwierdzenie
            adb shell input text $adbPin 2>$null
            adb shell input keyevent 66 2>$null
            Start-Sleep -Milliseconds 200
        }
    }

    function Enable-ScreenLockPrevention {
        if (-not (Test-AdbDeviceSilent)) { return }

        # Ustawienie maksymalnego limitu wygaszania (~24.8 dni)
        adb shell settings put system screen_off_timeout 2147483647 2>$null
        # Czuwanie przy podłączonym zasilaniu/USB
        adb shell svc power stayon true 2>$null

        # Wybudzenie i odblokowanie telefonu, jeśli zablokowany
        Invoke-AdbUnlock
    }

    function Restore-ScreenLockSettings {
        if (-not (Test-AdbDeviceSilent)) { return }

        $timeoutToRestore = if ($script:originalTimeout -gt 0) { $script:originalTimeout } else { 30000 }
        adb shell settings put system screen_off_timeout $timeoutToRestore 2>$null
        adb shell svc power stayon false 2>$null

        if (Test-Path $stateFile) {
            Remove-Item $stateFile -Force -ErrorAction SilentlyContinue
        }
    }

    function Start-Taskbar {
        if (-not (Test-AdbDeviceSilent)) { return }
        # Włączenie freeform i skalowalnych okien
        adb shell settings put global enable_freeform_support 1 2>$null
        adb shell settings put secure force_resizable_activities 1 2>$null
        # Uruchomienie Taskbara
        adb shell monkey -p com.farmerbb.taskbar 1 2>$null | Out-Null
    }

    function Optimize-RdcClipboard {
        if (-not (Test-AdbDeviceSilent)) { return }
        # Zezwolenie na odczyt schowka w tle oraz okna systemowe dla Windows App (Remote Desktop)
        adb shell cmd appops set com.microsoft.rdc.androidx READ_CLIPBOARD allow 2>$null
        adb shell cmd appops set com.microsoft.rdc.androidx SYSTEM_ALERT_WINDOW allow 2>$null
    }

    function Register-ScrcpyProcess {
        param([System.Diagnostics.Process]$Process)
        if ($Process -and -not $Process.HasExited) {
            $script:launchedProcesses.Add($Process)
        }
    }

    function Clean-ExitedProcesses {
        $alive = New-Object 'System.Collections.Generic.List[System.Diagnostics.Process]'
        foreach ($proc in $script:launchedProcesses) {
            if ($proc -and -not $proc.HasExited) {
                $alive.Add($proc)
            }
        }
        $script:launchedProcesses = $alive
    }

    function Start-ScrcpyApp {
        param(
            [Parameter(Mandatory = $true)][string]$PackageName,
            [string]$DisplaySize = $appDisplaySize,
            [switch]$UseUhidKeyboard,
            [switch]$ForwardAllClicks
        )

        if (-not (Test-AdbDevice)) { return }

        # Upewnienie się, że telefon nie śpi i nie jest zablokowany
        Enable-ScreenLockPrevention

        # Dla aplikacji Windows App upewnij się, że uprawnienia schowka w Androidzie są włączone
        if ($PackageName -eq "com.microsoft.rdc.androidx") {
            Optimize-RdcClipboard
        }

        $argListItems = @(
            "--new-display=$DisplaySize",
            "--start-app=$PackageName",
            "--no-vd-system-decorations",
            "-x",
            "-w"
        )

        # Włącz tryb sprzętowej klawiatury UHID dla Windows App lub na żądanie
        if ($UseUhidKeyboard -or $PackageName -eq "com.microsoft.rdc.androidx") {
            $argListItems += "-K"
        }

        # Włącz pełne przekazywanie kliknięć myszy (prawy przycisk myszy = menu kontekstowe/wklejanie, a nie 'Wstecz')
        if ($ForwardAllClicks -or $PackageName -eq "com.microsoft.rdc.androidx") {
            $argListItems += "--mouse-bind=++++"
        }

        $argList = $argListItems -join " "

        try {
            $proc = Start-Process "scrcpy" -ArgumentList $argList -PassThru -ErrorAction Stop
            Register-ScrcpyProcess -Process $proc
        }
        catch {
            [System.Windows.Forms.MessageBox]::Show(
                "Nie udało się uruchomić scrcpy dla pakietu '$PackageName': $($_.Exception.Message)`n`nSprawdź, czy scrcpy.exe jest w PATH oraz czy pakiet jest poprawny.",
                "Błąd",
                [System.Windows.Forms.MessageBoxButtons]::OK,
                [System.Windows.Forms.MessageBoxIcon]::Error
            )
        }
    }

    # --- INTERFEJS GRAFICZNY ---

    # Główne okno aplikacji
    $form = New-Object System.Windows.Forms.Form
    $form.Text = "Scrcpy Manager"
    $form.Size = New-Object System.Drawing.Size(380, 650)
    $form.StartPosition = "CenterScreen"
    $form.FormBorderStyle = "FixedDialog"
    $form.MaximizeBox = $false

    # Etykieta
    $label = New-Object System.Windows.Forms.Label
    $label.Text = "Zarządzanie ekranem (Pixel):"
    $label.Location = New-Object System.Drawing.Point(18, 12)
    $label.AutoSize = $true
    $form.Controls.Add($label)

    # Przycisk 1: Tryb Desktop
    $btnDesktop = New-Object System.Windows.Forms.Button
    $btnDesktop.Location = New-Object System.Drawing.Point(18, 32)
    $btnDesktop.Size = New-Object System.Drawing.Size(335, 38)
    $btnDesktop.Text = "1. Włącz Tryb Desktop (Małe DPI)"
    $btnDesktop.Add_Click({
        if (-not (Test-AdbDevice)) { return }

        Enable-ScreenLockPrevention

        adb shell wm density 250
        adb shell settings put global window_animation_scale 0.5
        adb shell settings put global transition_animation_scale 0.5
        adb shell settings put global animator_duration_scale 0.5

        Start-Taskbar

        [System.Windows.Forms.MessageBox]::Show(
            "Zastosowano małe DPI, przyspieszono animacje, włączono freeform i uruchomiono Taskbar.`n`nJeśli okna aplikacji w Taskbarze nadal nie mają ramki do przeciągania/zmiany rozmiaru - zrestartuj telefon.",
            "Gotowe"
        )
    })
    $form.Controls.Add($btnDesktop)

    # Przycisk 2: Uruchom scrcpy (z automatycznym obracaniem i logowaniem)
    $btnScrcpy = New-Object System.Windows.Forms.Button
    $btnScrcpy.Location = New-Object System.Drawing.Point(18, 75)
    $btnScrcpy.Size = New-Object System.Drawing.Size(335, 48)
    $btnScrcpy.Text = "2. URUCHOM SCRCPY"
    $btnScrcpy.BackColor = [System.Drawing.Color]::LightGreen
    $btnScrcpy.Font = New-Object System.Drawing.Font("Arial", [float]9, [System.Drawing.FontStyle]::Bold)
    $btnScrcpy.Add_Click({
        if (-not (Test-AdbDevice)) { return }

        Enable-ScreenLockPrevention

        if ($chkAutoTaskbar.Checked) {
            Start-Taskbar
        }

        # 1. Wymuszenie orientacji poziomej
        adb shell settings put system accelerometer_rotation 0
        adb shell settings put system user_rotation 1

        # 2. Wybudzenie ekranu i odblokowanie
        Invoke-AdbUnlock

        # 3. Uruchomienie scrcpy
        try {
            $proc = Start-Process "scrcpy" -ArgumentList "-S -w -K -M" -PassThru -ErrorAction Stop
            Register-ScrcpyProcess -Process $proc
        }
        catch {
            [System.Windows.Forms.MessageBox]::Show(
                "Nie udało się uruchomić scrcpy: $($_.Exception.Message)`n`nSprawdź, czy scrcpy.exe jest w PATH.",
                "Błąd",
                [System.Windows.Forms.MessageBoxButtons]::OK,
                [System.Windows.Forms.MessageBoxIcon]::Error
            )
        }
    })
    $form.Controls.Add($btnScrcpy)

    # Przycisk 3: Tryb Normalny (Reset)
    $btnNormal = New-Object System.Windows.Forms.Button
    $btnNormal.Location = New-Object System.Drawing.Point(18, 128)
    $btnNormal.Size = New-Object System.Drawing.Size(335, 36)
    $btnNormal.Text = "3. Przywróć Tryb Normalny (Reset)"
    $btnNormal.Add_Click({
        if (-not (Test-AdbDevice)) { return }

        # Resetowanie DPI i animacji
        adb shell wm density reset
        adb shell settings put global window_animation_scale 1.0
        adb shell settings put global transition_animation_scale 1.0
        adb shell settings put global animator_duration_scale 1.0

        # Przywrócenie orientacji pionowej i auto-obracania
        adb shell settings put system user_rotation 0
        adb shell settings put system accelerometer_rotation 1

        # Przywrócenie domyślnego wygaszania ekranu i wyłączenie wymuszonego czuwania
        Restore-ScreenLockSettings

        # Wymuszone zamknięcie Taskbara i powrót na domyślny ekran główny
        adb shell am force-stop com.farmerbb.taskbar 2>$null
        adb shell input keyevent 3

        [System.Windows.Forms.MessageBox]::Show("Przywrócono domyślne ustawienia telefonu (w tym wygaszanie ekranu) i zamknięto Taskbar.", "Gotowe")
    })
    $form.Controls.Add($btnNormal)

    # Przycisk 4: Napraw schowek (3 maszyny)
    $btnClipFix = New-Object System.Windows.Forms.Button
    $btnClipFix.Location = New-Object System.Drawing.Point(18, 169)
    $btnClipFix.Size = New-Object System.Drawing.Size(335, 32)
    $btnClipFix.Text = "Napraw schowek (3 maszyny)"
    $btnClipFix.BackColor = [System.Drawing.Color]::LightSkyBlue
    $btnClipFix.Font = New-Object System.Drawing.Font("Arial", [float]8.5, [System.Drawing.FontStyle]::Bold)
    $btnClipFix.Add_Click({
        if (Test-AdbDeviceSilent) {
            Optimize-RdcClipboard
        }

        # Skopiowanie polecenia naprawy rdpclip na zdalnym PC do schowka użytkownika
        $remoteFixCmd = "taskkill /f /im rdpclip.exe & start rdpclip.exe"
        try {
            [System.Windows.Forms.Clipboard]::SetText($remoteFixCmd)
            $copiedNotice = "Polecenie naprawcze dla ZDALNEJ maszyny zostało skopiowane do Twojego schowka:`n`n    $remoteFixCmd`n`nMożesz je wkleić (Win+R lub cmd) na zdalnym komputerze, jeśli schowek RDP się zablokuje."
        }
        catch {
            $copiedNotice = "Polecenie naprawcze dla zdalnego PC (w razie zawieszenia):`n    $remoteFixCmd"
        }

        [System.Windows.Forms.MessageBox]::Show(
            "Zastosowano optymalizacje schowka dla telefonu (READ_CLIPBOARD: allow).`n`n" +
            "Skróty scrcpy do sterowania schowkiem w oknie Windows App:`n" +
            "• Alt + V : Wymuś wysłanie schowka z tego komputera do telefonu/RDP i wklejenie`n" +
            "• Alt + C : Wymuś pobranie schowka z telefonu/RDP do tego komputera`n`n" +
            "Upewnij się również:`n" +
            "1. W aplikacji Windows App na telefonie w ustawieniach połączenia (Devices & Audio) opcja 'Clipboard' (Schowek) jest WŁĄCZONA.`n" +
            "2. $copiedNotice",
            "Diagnostyka i pomoc schowka (3 maszyny)",
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Information
        )
    })
    $form.Controls.Add($btnClipFix)

    # Przełącznik automatycznego włączania Taskbara
    $chkAutoTaskbar = New-Object System.Windows.Forms.CheckBox
    $chkAutoTaskbar.Text = "Automatycznie włączaj Taskbar (pasek zadań)"
    $chkAutoTaskbar.Checked = $true
    $chkAutoTaskbar.AutoSize = $true
    $chkAutoTaskbar.Location = New-Object System.Drawing.Point(20, 206)
    $form.Controls.Add($chkAutoTaskbar)

    # --- Sekcja: uruchamianie konkretnych aplikacji w osobnym oknie (2 KOLUMNY, ALFABETYCZNIE) ---

    $groupApps = New-Object System.Windows.Forms.GroupBox
    $groupApps.Text = "Uruchom aplikację w osobnym oknie"
    $groupApps.Location = New-Object System.Drawing.Point(18, 230)
    $groupApps.Size = New-Object System.Drawing.Size(335, 335)
    $form.Controls.Add($groupApps)

    # Lista aplikacji posortowana alfabetycznie
    $appButtons = @(
        @{ Text = "Claude";              Package = "com.anthropic.claude";                  Flags = @() },
        @{ Text = "ConneckBot";          Package = "org.connectbot";                        Flags = @("-UseUhidKeyboard") },
        @{ Text = "Gmail (Wszystkie)";   Package = "com.google.android.gm";                 Flags = @() },
        @{ Text = "TurboTel";            Package = "ellipi.messenger";                      Flags = @() },
        @{ Text = "Ustawienia";          Package = "com.android.settings";                  Flags = @() },
        @{ Text = "Vivaldi";             Package = "com.vivaldi.browser";                   Flags = @("-ForwardAllClicks") },
        @{ Text = "WhatsApp";            Package = "com.whatsapp";                          Flags = @() },
        @{ Text = "Wiadomości (Google)"; Package = "com.google.android.apps.messaging";     Flags = @() },
        @{ Text = "Windows App";         Package = "com.microsoft.rdc.androidx";           Flags = @("-UseUhidKeyboard", "-ForwardAllClicks") }
    )

    # Generowanie przycisków w 2 kolumnach
    $colWidth = 148
    $btnHeight = 35
    $rowPitch = 41
    $colPitch = 157
    $startX = 15
    $startY = 24

    for ($i = 0; $i -lt $appButtons.Count; $i++) {
        $app = $appButtons[$i]
        $row = [int][math]::Floor($i / 2)
        $col = [int]($i % 2)

        $posX = [int]($startX + $col * $colPitch)
        $posY = [int]($startY + $row * $rowPitch)

        $btn = New-Object System.Windows.Forms.Button
        $btn.Text = $app.Text
        $btn.Font = New-Object System.Drawing.Font("Arial", [float]8.5)
        $btn.Location = New-Object System.Drawing.Point($posX, $posY)
        $btn.Size = New-Object System.Drawing.Size($colWidth, $btnHeight)

        $pkg = $app.Package
        $useUhid = $app.Flags -contains "-UseUhidKeyboard"
        $forwardClicks = $app.Flags -contains "-ForwardAllClicks"

        $btn.Add_Click({
            if ($chkAutoTaskbar.Checked) { Start-Taskbar }
            Start-ScrcpyApp -PackageName $pkg -UseUhidKeyboard:$useUhid -ForwardAllClicks:$forwardClicks
        }.GetNewClosure())

        $groupApps.Controls.Add($btn)
    }

    # Dowolny inny pakiet - pole tekstowe + przycisk
    $lblCustom = New-Object System.Windows.Forms.Label
    $lblCustom.Text = "Inny pakiet (np. com.spotify.music):"
    $lblCustom.Location = New-Object System.Drawing.Point(15, 233)
    $lblCustom.AutoSize = $true
    $groupApps.Controls.Add($lblCustom)

    $txtCustom = New-Object System.Windows.Forms.TextBox
    $txtCustom.Location = New-Object System.Drawing.Point(15, 253)
    $txtCustom.Size = New-Object System.Drawing.Size(305, 20)
    $groupApps.Controls.Add($txtCustom)

    $btnCustom = New-Object System.Windows.Forms.Button
    $btnCustom.Location = New-Object System.Drawing.Point(15, 278)
    $btnCustom.Size = New-Object System.Drawing.Size(305, 32)
    $btnCustom.Text = "Uruchom"
    $btnCustom.Add_Click({
        $pkg = $txtCustom.Text.Trim()
        if ([string]::IsNullOrWhiteSpace($pkg)) {
            [System.Windows.Forms.MessageBox]::Show("Wpisz nazwę pakietu aplikacji.", "Brak pakietu")
            return
        }
        if ($chkAutoTaskbar.Checked) { Start-Taskbar }
        Start-ScrcpyApp -PackageName $pkg
    })
    $groupApps.Controls.Add($btnCustom)

    # Etykieta informacyjna o stanie blokady ekranu
    $lblStatus = New-Object System.Windows.Forms.Label
    $lblStatus.Text = "Blokada ekranu: wyłączona (czuwanie aktywne)"
    $lblStatus.Location = New-Object System.Drawing.Point(18, 574)
    $lblStatus.Size = New-Object System.Drawing.Size(335, 25)
    $lblStatus.ForeColor = [System.Drawing.Color]::DarkGreen
    $lblStatus.Font = New-Object System.Drawing.Font("Arial", [float]8, [System.Drawing.FontStyle]::Italic)
    $lblStatus.TextAlign = [System.Drawing.ContentAlignment]::MiddleCenter
    $form.Controls.Add($lblStatus)

    # --- CYKLICZNY TIMER PODTRZYMUJĄCY CZUWANIE ---
    $keepAwakeTimer = New-Object System.Windows.Forms.Timer
    $keepAwakeTimer.Interval = 8000 # Sprawdzanie co 8 sekund
    $keepAwakeTimer.Add_Tick({
        Clean-ExitedProcesses

        if (Test-AdbDeviceSilent) {
            # Upewnienie się, że timeout nadal jest ustawiony na brak wygaszania
            adb shell settings put system screen_off_timeout 2147483647 2>$null
            adb shell svc power stayon true 2>$null

            # Jeśli telefon jest uśpiony lub zablokowany, wybudź i odblokuj
            $isLocked = [bool](adb shell dumpsys window 2>$null | Select-String "isKeyguardShowing=true")
            if ($isLocked) {
                Invoke-AdbUnlock
            }
        }
    })

    # Zdarzenie załadowania formularza
    $form.Add_Load({
        Initialize-ScreenTimeoutSettings
        Enable-ScreenLockPrevention
        Optimize-RdcClipboard
        if ($chkAutoTaskbar.Checked) {
            Start-Taskbar
        }
        $keepAwakeTimer.Start()
    })

    # Zdarzenie zamknięcia formularza
    $form.Add_FormClosing({
        $keepAwakeTimer.Stop()
        Clean-ExitedProcesses

        $runningPids = @($script:launchedProcesses | Where-Object { -not $_.HasExited } | Select-Object -ExpandProperty Id)

        if ($runningPids.Count -eq 0) {
            # Brak działających aplikacji potomnych - od razu przywracamy stan pierwotny telefonu
            Restore-ScreenLockSettings
        }
        else {
            # Któraś aplikacja z przycisków nadal działa!
            # Uruchamiamy ukryty proces PowerShell, który czuwa dopóki procesy scrcpy nie zostaną zamknięte.
            $pidArrayStr = ($runningPids -join ",")
            $timeoutVal = if ($script:originalTimeout -gt 0) { $script:originalTimeout } else { 30000 }

            $watcherScript = @"
`$pids = @($pidArrayStr)
while ((Get-Process -Id `$pids -ErrorAction SilentlyContinue).Count -gt 0) {
    adb shell settings put system screen_off_timeout 2147483647 2>`$null
    adb shell svc power stayon true 2>`$null
    Start-Sleep -Seconds 5
}
adb shell settings put system screen_off_timeout $timeoutVal 2>`$null
adb shell svc power stayon false 2>`$null
if (Test-Path '$stateFile') {
    Remove-Item '$stateFile' -Force -ErrorAction SilentlyContinue
}
"@
            $bytes = [System.Text.Encoding]::Unicode.GetBytes($watcherScript)
            $encoded = [Convert]::ToBase64String($bytes)
            Start-Process "powershell" -ArgumentList "-WindowStyle", "Hidden", "-NoProfile", "-EncodedCommand", $encoded -WindowStyle Hidden
        }
    })

    # Uruchomienie okna
    $form.ShowDialog() | Out-Null
}
catch {
    [System.Windows.Forms.MessageBox]::Show(
        "Wystąpił błąd podczas uruchamiania Scrcpy Manager:`n`n$($_.Exception.ToString())",
        "Błąd Scrcpy Manager",
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Error
    )
}