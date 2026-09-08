try {
    Add-Type -AssemblyName System.Windows.Forms
    Add-Type -AssemblyName System.Drawing

    $scriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
    if (-not $scriptDir) { $scriptDir = (Get-Location).Path }

    # --- KONFIGURACJA BEZPIECZEŃSTWA ---
    # PIN jest pobierany ze zmiennej środowiskowej SCRCPY_ADB_PIN, aby nie trafiał do repozytorium.
    $adbPin = $env:SCRCPY_ADB_PIN

    # Domyślny rozmiar wirtualnego ekranu (będzie dynamicznie aktualizowany na podstawie podłączonego telefonu)
    $script:appDisplaySize = "1080x2400"
    $script:deviceModel = "Android"
    $script:deviceManufacturer = ""
    $script:isWifiConnected = $false

    # Plik stanu do bezpiecznego zapamiętania pierwotnego limitu wygaszania ekranu
    $stateFile = Join-Path $env:TEMP "scrcpy_manager_original_timeout.txt"
    $script:launchedProcesses = New-Object 'System.Collections.Generic.List[System.Diagnostics.Process]'
    $script:originalTimeout = 30000 # Domyślny limit zapasowy (30 sekund)

    # --- FUNKCJE POMOCNICZE ADB I URZĄDZENIA ---

    function Test-AdbDeviceSilent {
        $devices = adb devices 2>$null | Select-String "device$"
        return [bool]$devices
    }

    function Test-AdbDevice {
        if (-not (Test-AdbDeviceSilent)) {
            [System.Windows.Forms.MessageBox]::Show(
                "Nie wykryto podłączonego telefonu (adb devices nie zwraca urządzenia).`n`nSprawdź kabel USB, debugowanie USB w telefonie lub połączenie Wi-Fi.",
                "Brak urządzenia",
                [System.Windows.Forms.MessageBoxButtons]::OK,
                [System.Windows.Forms.MessageBoxIcon]::Warning
            )
            return $false
        }
        return $true
    }

    function Update-DeviceInfo {
        if (-not (Test-AdbDeviceSilent)) {
            $script:deviceModel = "Brak urządzenia"
            return
        }

        try {
            $model = (adb shell getprop ro.product.model 2>$null)
            $mfg = (adb shell getprop ro.product.manufacturer 2>$null)
            if ($model) { $script:deviceModel = $model.Trim() }
            if ($mfg) { $script:deviceManufacturer = $mfg.Trim() }

            # Odczyt fizycznej rozdzielczości ekranu
            $wmSize = (adb shell wm size 2>$null)
            if ($wmSize -match "Physical size:\s*(\d+)x(\d+)") {
                $w = $Matches[1]
                $h = $Matches[2]
                $script:appDisplaySize = "$($w)x$($h)"
            }
        }
        catch {
            # Błąd odczytu parametrów urządzenia nie przerywa działania
        }
    }

    function Get-DeviceBatteryStatus {
        if (-not (Test-AdbDeviceSilent)) { return "Odłączony" }
        try {
            $dump = (adb shell dumpsys battery 2>$null)
            $level = 0
            $charging = $false

            if ($dump -match "level:\s*(\d+)") { $level = [int]$Matches[1] }
            if ($dump -match "status:\s*(2|5)") { $charging = $true }

            $chargeStr = if ($charging) { " (Ładowanie)" } else { "" }
            return "$level%$chargeStr"
        }
        catch {
            return "Nieznany"
        }
    }

    function Initialize-ScreenTimeoutSettings {
        if (Test-Path $stateFile) {
            $saved = Get-Content $stateFile -ErrorAction SilentlyContinue
            if ($saved -as [int] -and [int]$saved -gt 0 -and [int]$saved -lt 2147483647) {
                $script:originalTimeout = [int]$saved
                return
            }
        }

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

        # 2. Sprawdzenie, czy ekran jest zablokowany (Keyguard / Lockscreen)
        $isLocked = [bool](adb shell dumpsys window 2>$null | Select-String "isKeyguardShowing=true|mShowing=true")
        if ($isLocked -and -not [string]::IsNullOrWhiteSpace($adbPin)) {
            Start-Sleep -Milliseconds 350
            # Uniwersalne współrzędne gestu swipe w górę (niezależne od rozdzielczości telefonu)
            adb shell input swipe 500 1500 500 300 200 2>$null
            Start-Sleep -Milliseconds 350
            # Wpisanie PINu i zatwierdzenie (Enter)
            adb shell input text $adbPin 2>$null
            adb shell input keyevent 66 2>$null
            Start-Sleep -Milliseconds 200
        }
    }

    function Enable-ScreenLockPrevention {
        if (-not (Test-AdbDeviceSilent)) { return }

        adb shell settings put system screen_off_timeout 2147483647 2>$null
        adb shell svc power stayon true 2>$null
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
        adb shell settings put global enable_freeform_support 1 2>$null
        adb shell settings put secure force_resizable_activities 1 2>$null
        adb shell am start -n com.farmerbb.taskbar/.activity.StartTaskbarActivity 2>$null | Out-Null
    }

    function Stop-Taskbar {
        if (-not (Test-AdbDeviceSilent)) { return }
        adb shell am force-stop com.farmerbb.taskbar 2>$null | Out-Null
        adb shell input keyevent 3 2>$null
    }

    function Optimize-RdcClipboard {
        if (-not (Test-AdbDeviceSilent)) { return }
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

    function Switch-ToWirelessAdb {
        if (-not (Test-AdbDevice)) { return }

        # Odczytanie adresu IP telefonu z interfejsu wlan0
        $ip = $null
        $ipRoute = (adb shell ip route 2>$null)
        if ($ipRoute -match "src\s+(\d+\.\d+\.\d+\.\d+)") {
            $ip = $Matches[1]
        }
        if (-not $ip) {
            $ipAddr = (adb shell ip addr show wlan0 2>$null)
            if ($ipAddr -match "inet\s+(\d+\.\d+\.\d+\.\d+)") {
                $ip = $Matches[1]
            }
        }
        if (-not $ip) {
            $ipProp = (adb shell getprop dhcp.wlan0.ipaddress 2>$null)
            if ($ipProp -match "\d+\.\d+\.\d+\.\d+") {
                $ip = $ipProp.Trim()
            }
        }

        if (-not $ip) {
            [System.Windows.Forms.MessageBox]::Show(
                "Nie udało się automatycznie wykryć adresu IP telefonu w sieci Wi-Fi.`n`nUpewnij się, że telefon jest połączony z tą samą siecią Wi-Fi co komputer.",
                "Brak IP Wi-Fi",
                [System.Windows.Forms.MessageBoxButtons]::OK,
                [System.Windows.Forms.MessageBoxIcon]::Warning
            )
            return
        }

        # Uruchomienie trybu TCP/IP
        adb tcpip 5555 2>$null | Out-Null
        Start-Sleep -Milliseconds 500
        $connectRes = (adb connect "$($ip):5555" 2>$null)

        if ($connectRes -match "connected") {
            $script:isWifiConnected = $true
            [System.Windows.Forms.MessageBox]::Show(
                "Połączono bezprzewodowo z telefonem:`n$($ip):5555`n`nMożesz teraz odłączyć kabel USB!",
                "Połączenie Wi-Fi aktywne",
                [System.Windows.Forms.MessageBoxButtons]::OK,
                [System.Windows.Forms.MessageBoxIcon]::Information
            )
        }
        else {
            [System.Windows.Forms.MessageBox]::Show(
                "Próba połączenia z $($ip):5555 zwróciła:`n$connectRes",
                "Informacja o połączeniu",
                [System.Windows.Forms.MessageBoxButtons]::OK,
                [System.Windows.Forms.MessageBoxIcon]::Warning
            )
        }
    }

    function Start-ScrcpyApp {
        param(
            [Parameter(Mandatory = $true)][string]$PackageName,
            [string]$WindowTitle = "",
            [string]$DisplaySize = $script:appDisplaySize,
            [switch]$UseUhidKeyboard,
            [switch]$ForwardAllClicks
        )

        if (-not (Test-AdbDevice)) { return }

        Enable-ScreenLockPrevention

        if ($PackageName -eq "com.microsoft.rdc.androidx") {
            Optimize-RdcClipboard
        }

        $title = if ($WindowTitle) { $WindowTitle } else { $PackageName }

        $argListItems = @(
            "--new-display=$DisplaySize",
            "--start-app=$PackageName",
            "--window-title=`"$title`"",
            "--no-vd-system-decorations",
            "-w",
            "-K"
        )

        # Dźwięk: jeśli przełącznik audio jest odznaczony, wyciszamy
        if ($chkAudio -and -not $chkAudio.Checked) {
            $argListItems += "--no-audio"
        }

        # Dla aplikacji Windows App (RDP) NIE używamy flagi flex-display (-x), aby nie niszczyć wybranej rozdzielczości
        if ($PackageName -ne "com.microsoft.rdc.androidx") {
            $argListItems += "-x"
        }
        else {
            $argListItems += "-b"
            $argListItems += "16M"
        }

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
                "Nie udało się uruchomić scrcpy dla pakietu '$PackageName': $($_.Exception.Message)`n`nSprawdź, czy scrcpy.exe jest zainstalowany i dostępny w PATH.",
                "Błąd scrcpy",
                [System.Windows.Forms.MessageBoxButtons]::OK,
                [System.Windows.Forms.MessageBoxIcon]::Error
            )
        }
    }

    # --- WCZYTYWANIE KONFIGURACJI APLIKACJI (apps.json) ---

    $appsConfigFile = Join-Path $scriptDir "apps.json"
    $appButtons = @()

    if (Test-Path $appsConfigFile) {
        try {
            $rawJson = Get-Content $appsConfigFile -Raw -Encoding UTF8 -ErrorAction Stop
            $parsed = $rawJson | ConvertFrom-Json
            foreach ($item in $parsed) {
                $flagsArray = if ($item.flags) { @($item.flags) } else { @() }
                $appButtons += @{
                    Text    = [string]$item.name
                    Package = [string]$item.package
                    Flags   = $flagsArray
                }
            }
        }
        catch {
            # W razie błędu parsowania pliku JSON skorzystaj z listy domyślnej
        }
    }

    if ($appButtons.Count -eq 0) {
        $appButtons = @(
            @{ Text = "Claude";              Package = "com.anthropic.claude";                  Flags = @() },
            @{ Text = "ConneckBot";          Package = "org.connectbot";                        Flags = @("-UseUhidKeyboard") },
            @{ Text = "Gmail (Wszystkie)";   Package = "com.google.android.gm";                 Flags = @() },
            @{ Text = "Messenger";           Package = "com.facebook.orca";                     Flags = @() },
            @{ Text = "TurboTel";            Package = "ellipi.messenger";                      Flags = @() },
            @{ Text = "Ustawienia";          Package = "com.android.settings";                  Flags = @() },
            @{ Text = "Vivaldi";             Package = "com.vivaldi.browser";                   Flags = @("-ForwardAllClicks") },
            @{ Text = "WhatsApp";            Package = "com.whatsapp";                          Flags = @() },
            @{ Text = "Wiadomości (Google)"; Package = "com.google.android.apps.messaging";     Flags = @() },
            @{ Text = "Windows App";         Package = "com.microsoft.rdc.androidx";           Flags = @("-UseUhidKeyboard", "-ForwardAllClicks") }
        )
    }

    # Sortowanie alfabetyczne
    $appButtons = @($appButtons | Sort-Object { $_.Text })

    # --- KOLORYSTYKA I MOTYW (MODERN DARK THEME) ---

    $cBg         = [System.Drawing.Color]::FromArgb(26, 27, 34)      # Ciemny grafit
    $cCard       = [System.Drawing.Color]::FromArgb(35, 37, 46)      # Karty / panele
    $cCardBorder = [System.Drawing.Color]::FromArgb(55, 58, 72)      # Ramki
    $cText       = [System.Drawing.Color]::FromArgb(240, 242, 248)    # Główny tekst
    $cTextMuted  = [System.Drawing.Color]::FromArgb(150, 155, 175)    # Tekst pomocniczy
    $cGreen      = [System.Drawing.Color]::FromArgb(40, 167, 69)      # Zielony akcent (Uruchom)
    $cBlue       = [System.Drawing.Color]::FromArgb(33, 115, 205)     # Niebieski akcent (Narzędzia)
    $cPurple     = [System.Drawing.Color]::FromArgb(110, 75, 180)     # Fioletowy akcent (Wi-Fi)
    $cBtnApp     = [System.Drawing.Color]::FromArgb(48, 52, 65)      # Przyciski aplikacji
    $cRed        = [System.Drawing.Color]::FromArgb(180, 50, 50)      # Czerwony akcent (Reset)

    $fontRegular = New-Object System.Drawing.Font("Segoe UI", [float]9, [System.Drawing.FontStyle]::Regular)
    $fontBold    = New-Object System.Drawing.Font("Segoe UI", [float]9, [System.Drawing.FontStyle]::Bold)
    $fontTitle   = New-Object System.Drawing.Font("Segoe UI", [float]9.5, [System.Drawing.FontStyle]::Bold)
    $fontSmall   = New-Object System.Drawing.Font("Segoe UI", [float]8, [System.Drawing.FontStyle]::Regular)

    # --- GŁÓWNE OKNO FORMULARZA ---

    $form = New-Object System.Windows.Forms.Form
    $form.Text = "Scrcpy Manager"
    $form.Size = New-Object System.Drawing.Size(385, 765)
    $form.StartPosition = "CenterScreen"
    $form.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::FixedDialog
    $form.MaximizeBox = $false
    $form.BackColor = $cBg
    $form.ForeColor = $cText

    # Etykieta nagłówkowa urządzenia
    $lblHeader = New-Object System.Windows.Forms.Label
    $lblHeader.Text = "Zarządzanie urządzeniem:"
    $lblHeader.Location = New-Object System.Drawing.Point(18, 12)
    $lblHeader.Size = New-Object System.Drawing.Size(340, 20)
    $lblHeader.Font = $fontTitle
    $lblHeader.ForeColor = $cText
    $form.Controls.Add($lblHeader)

    # Przycisk 1: Tryb Desktop (Małe DPI)
    $btnDesktop = New-Object System.Windows.Forms.Button
    $btnDesktop.Location = New-Object System.Drawing.Point(18, 36)
    $btnDesktop.Size = New-Object System.Drawing.Size(335, 34)
    $btnDesktop.Text = "1. Włącz Tryb Desktop (Małe DPI)"
    $btnDesktop.BackColor = $cCard
    $btnDesktop.ForeColor = $cText
    $btnDesktop.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $btnDesktop.FlatAppearance.BorderColor = $cCardBorder
    $btnDesktop.Font = $fontBold
    $btnDesktop.Add_Click({
        if (-not (Test-AdbDevice)) { return }

        adb shell wm density 250 2>$null
        adb shell settings put global window_animation_scale 0.5 2>$null
        adb shell settings put global transition_animation_scale 0.5 2>$null
        adb shell settings put global animator_duration_scale 0.5 2>$null

        adb shell settings put global enable_freeform_support 1 2>$null
        adb shell settings put secure force_resizable_activities 1 2>$null

        Start-Taskbar

        [System.Windows.Forms.MessageBox]::Show(
            "Zastosowano małe DPI, przyspieszono animacje i włączono tryb okienkowy na telefonie.",
            "Tryb Desktop włączony",
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Information
        )
    })
    $form.Controls.Add($btnDesktop)

    # Przycisk 2: Główny przycisk URUCHOM SCRCPY
    $btnScrcpy = New-Object System.Windows.Forms.Button
    $btnScrcpy.Location = New-Object System.Drawing.Point(18, 76)
    $btnScrcpy.Size = New-Object System.Drawing.Size(335, 44)
    $btnScrcpy.Text = "2. URUCHOM SCRCPY (Cały Ekran)"
    $btnScrcpy.BackColor = $cGreen
    $btnScrcpy.ForeColor = [System.Drawing.Color]::White
    $btnScrcpy.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $btnScrcpy.FlatAppearance.BorderSize = 0
    $btnScrcpy.Font = New-Object System.Drawing.Font("Segoe UI", [float]10, [System.Drawing.FontStyle]::Bold)
    $btnScrcpy.Add_Click({
        if (-not (Test-AdbDevice)) { return }

        Enable-ScreenLockPrevention

        if ($chkAutoTaskbar.Checked) {
            Start-Taskbar
        }

        # Wymuszenie orientacji poziomej dla pełnego scrcpy
        adb shell settings put system accelerometer_rotation 0 2>$null
        adb shell settings put system user_rotation 1 2>$null

        Invoke-AdbUnlock

        $title = if ($script:deviceModel) { "$($script:deviceModel) (Scrcpy)" } else { "Telefon Android (Scrcpy)" }
        $audioArg = if ($chkAudio.Checked) { "" } else { "--no-audio" }

        try {
            $proc = Start-Process "scrcpy" -ArgumentList "-S -w -K -M --window-title=`"$title`" $audioArg" -PassThru -ErrorAction Stop
            Register-ScrcpyProcess -Process $proc
        }
        catch {
            [System.Windows.Forms.MessageBox]::Show(
                "Nie udało się uruchomić scrcpy: $($_.Exception.Message)`n`nUpewnij się, że scrcpy.exe jest zainstalowany i dostępny w PATH.",
                "Błąd scrcpy",
                [System.Windows.Forms.MessageBoxButtons]::OK,
                [System.Windows.Forms.MessageBoxIcon]::Error
            )
        }
    })
    $form.Controls.Add($btnScrcpy)

    # Przycisk 3: Tryb Normalny (Reset)
    $btnNormal = New-Object System.Windows.Forms.Button
    $btnNormal.Location = New-Object System.Drawing.Point(18, 126)
    $btnNormal.Size = New-Object System.Drawing.Size(335, 32)
    $btnNormal.Text = "3. Przywróć Tryb Normalny (Reset)"
    $btnNormal.BackColor = $cCard
    $btnNormal.ForeColor = [System.Drawing.Color]::FromArgb(255, 120, 120)
    $btnNormal.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $btnNormal.FlatAppearance.BorderColor = $cCardBorder
    $btnNormal.Font = $fontBold
    $btnNormal.Add_Click({
        if (-not (Test-AdbDevice)) { return }

        adb shell wm density reset 2>$null
        adb shell settings put global window_animation_scale 1.0 2>$null
        adb shell settings put global transition_animation_scale 1.0 2>$null
        adb shell settings put global animator_duration_scale 1.0 2>$null

        adb shell settings put system user_rotation 0 2>$null
        adb shell settings put system accelerometer_rotation 1 2>$null

        Restore-ScreenLockSettings
        Stop-Taskbar

        [System.Windows.Forms.MessageBox]::Show(
            "Przywrócono domyślne ustawienia telefonu, zresetowano DPI i zamknięto Taskbar.",
            "Przywrócono stan domyślny",
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Information
        )
    })
    $form.Controls.Add($btnNormal)

    # Rząd narzędzi: Schowek, Klawiatura, Wi-Fi
    $btnClipFix = New-Object System.Windows.Forms.Button
    $btnClipFix.Location = New-Object System.Drawing.Point(18, 164)
    $btnClipFix.Size = New-Object System.Drawing.Size(106, 30)
    $btnClipFix.Text = "Napraw schowek"
    $btnClipFix.BackColor = $cBlue
    $btnClipFix.ForeColor = [System.Drawing.Color]::White
    $btnClipFix.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $btnClipFix.FlatAppearance.BorderSize = 0
    $btnClipFix.Font = $fontSmall
    $btnClipFix.Add_Click({
        if (Test-AdbDeviceSilent) { Optimize-RdcClipboard }
        $remoteFixCmd = "taskkill /f /im rdpclip.exe & start rdpclip.exe"
        try { [System.Windows.Forms.Clipboard]::SetText($remoteFixCmd) } catch {}
        [System.Windows.Forms.MessageBox]::Show(
            "Zastosowano optymalizacje schowka dla telefonu.`n`nPolecenie naprawcze dla zdalnego PC skopiowano do schowka:`n$remoteFixCmd`n`nSkróty w oknie:`n• Alt + V : Wklej schowek PC do sesji`n• Alt + C : Pobierz schowek sesji do PC",
            "Schowek",
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Information
        )
    })
    $form.Controls.Add($btnClipFix)

    $btnKeyFix = New-Object System.Windows.Forms.Button
    $btnKeyFix.Location = New-Object System.Drawing.Point(130, 164)
    $btnKeyFix.Size = New-Object System.Drawing.Size(112, 30)
    $btnKeyFix.Text = "Klawiatura (Polski)"
    $btnKeyFix.BackColor = $cBlue
    $btnKeyFix.ForeColor = [System.Drawing.Color]::White
    $btnKeyFix.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $btnKeyFix.FlatAppearance.BorderSize = 0
    $btnKeyFix.Font = $fontSmall
    $btnKeyFix.Add_Click({
        if (Test-AdbDeviceSilent) {
            adb shell am start -a android.settings.HARD_KEYBOARD_SETTINGS 2>$null | Out-Null
        }
        [System.Windows.Forms.MessageBox]::Show(
            "Otwarto ustawienia klawiatury fizycznej w telefonie.`n`nUpewnij się, że układ klawiatury fizycznej 'scrcpy' ma zaznaczone 'Polski (programisty)'.",
            "Klawiatura fizyczna",
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Information
        )
    })
    $form.Controls.Add($btnKeyFix)

    $btnWifi = New-Object System.Windows.Forms.Button
    $btnWifi.Location = New-Object System.Drawing.Point(248, 164)
    $btnWifi.Size = New-Object System.Drawing.Size(105, 30)
    $btnWifi.Text = "Wi-Fi (Bez kabla)"
    $btnWifi.BackColor = $cPurple
    $btnWifi.ForeColor = [System.Drawing.Color]::White
    $btnWifi.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $btnWifi.FlatAppearance.BorderSize = 0
    $btnWifi.Font = $fontSmall
    $btnWifi.Add_Click({ Switch-ToWirelessAdb })
    $form.Controls.Add($btnWifi)

    # Przełączniki opcji: Taskbar i Audio
    $chkAutoTaskbar = New-Object System.Windows.Forms.CheckBox
    $chkAutoTaskbar.Text = "Automatycznie włączaj Taskbar na telefonie"
    $chkAutoTaskbar.Checked = $true
    $chkAutoTaskbar.AutoSize = $true
    $chkAutoTaskbar.ForeColor = $cText
    $chkAutoTaskbar.Font = $fontRegular
    $chkAutoTaskbar.Location = New-Object System.Drawing.Point(20, 202)
    $form.Controls.Add($chkAutoTaskbar)

    $chkAudio = New-Object System.Windows.Forms.CheckBox
    $chkAudio.Text = "Przesyłaj dźwięk z telefonu do PC"
    $chkAudio.Checked = $true
    $chkAudio.AutoSize = $true
    $chkAudio.ForeColor = $cText
    $chkAudio.Font = $fontRegular
    $chkAudio.Location = New-Object System.Drawing.Point(20, 224)
    $form.Controls.Add($chkAudio)

    # Profil rozdzielczości dla Windows App (RDP)
    $resProfiles = @{
        "Full HD Natywna (1920x1080 / DPI 160) - Żyleta 1:1 monitora" = "1920x1080/160"
        "2K QHD (2560x1440 / DPI 160) - Duża przestrzeń robocza"     = "2560x1440/160"
        "2K QHD Kompakt (2560x1440 / DPI 140)"                       = "2560x1440/140"
        "4K UHD (3840x2160 / DPI 200)"                               = "3840x2160/200"
        "Domyślna telefonu (Natywna)"                                 = "AUTO"
    }

    $lblRes = New-Object System.Windows.Forms.Label
    $lblRes.Text = "Rozdzielczość dla Windows App (RDP):"
    $lblRes.Location = New-Object System.Drawing.Point(18, 248)
    $lblRes.AutoSize = $true
    $lblRes.ForeColor = $cTextMuted
    $lblRes.Font = $fontRegular
    $form.Controls.Add($lblRes)

    $cmbRes = New-Object System.Windows.Forms.ComboBox
    $cmbRes.DropDownStyle = [System.Windows.Forms.ComboBoxStyle]::DropDownList
    $cmbRes.Location = New-Object System.Drawing.Point(18, 268)
    $cmbRes.Size = New-Object System.Drawing.Size(335, 24)
    $cmbRes.BackColor = $cCard
    $cmbRes.ForeColor = $cText
    $cmbRes.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $cmbRes.Font = $fontRegular
    $cmbRes.Items.Add("Full HD Natywna (1920x1080 / DPI 160) - Żyleta 1:1 monitora") | Out-Null
    $cmbRes.Items.Add("2K QHD (2560x1440 / DPI 160) - Duża przestrzeń robocza") | Out-Null
    $cmbRes.Items.Add("2K QHD Kompakt (2560x1440 / DPI 140)") | Out-Null
    $cmbRes.Items.Add("4K UHD (3840x2160 / DPI 200)") | Out-Null
    $cmbRes.Items.Add("Domyślna telefonu (Natywna)") | Out-Null
    $cmbRes.SelectedIndex = 0
    $form.Controls.Add($cmbRes)

    # --- SEKCJA APLIKACJI (2 KOLUMNY, ALFABETYCZNIE) ---

    $groupApps = New-Object System.Windows.Forms.GroupBox
    $groupApps.Text = "Uruchom aplikację w osobnym oknie"
    $groupApps.Location = New-Object System.Drawing.Point(18, 302)
    $groupApps.Size = New-Object System.Drawing.Size(335, 355)
    $groupApps.ForeColor = $cText
    $groupApps.BackColor = $cCard
    $groupApps.Font = $fontBold
    $form.Controls.Add($groupApps)

    $colWidth = 146
    $btnHeight = 34
    $rowPitch = 40
    $colPitch = 156
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
        $btn.Font = $fontRegular
        $btn.Location = New-Object System.Drawing.Point($posX, $posY)
        $btn.Size = New-Object System.Drawing.Size($colWidth, $btnHeight)
        $btn.BackColor = $cBtnApp
        $btn.ForeColor = $cText
        $btn.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
        $btn.FlatAppearance.BorderColor = $cCardBorder

        $pkg = $app.Package
        $appName = $app.Text
        $useUhid = $app.Flags -contains "-UseUhidKeyboard"
        $forwardClicks = $app.Flags -contains "-ForwardAllClicks"

        $btn.Add_Click({
            if ($chkAutoTaskbar.Checked) { Start-Taskbar }
            $disp = $script:appDisplaySize

            if ($pkg -eq "com.microsoft.rdc.androidx") {
                $sel = $cmbRes.SelectedItem.ToString()
                if ($resProfiles.ContainsKey($sel)) {
                    $profileVal = $resProfiles[$sel]
                    if ($profileVal -eq "AUTO") {
                        $disp = $script:appDisplaySize
                    }
                    else {
                        $disp = $profileVal
                    }
                }
                else {
                    $disp = "1920x1080/160"
                }
            }

            Start-ScrcpyApp -PackageName $pkg -WindowTitle $appName -UseUhidKeyboard:$useUhid -ForwardAllClicks:$forwardClicks -DisplaySize $disp
        }.GetNewClosure())

        $groupApps.Controls.Add($btn)
    }

    # Inny pakiet z pola tekstowego
    $lblCustom = New-Object System.Windows.Forms.Label
    $lblCustom.Text = "Inny pakiet (np. com.spotify.music):"
    $lblCustom.Location = New-Object System.Drawing.Point(15, 255)
    $lblCustom.AutoSize = $true
    $lblCustom.ForeColor = $cTextMuted
    $lblCustom.Font = $fontSmall
    $groupApps.Controls.Add($lblCustom)

    $txtCustom = New-Object System.Windows.Forms.TextBox
    $txtCustom.Location = New-Object System.Drawing.Point(15, 275)
    $txtCustom.Size = New-Object System.Drawing.Size(305, 23)
    $txtCustom.BackColor = $cBg
    $txtCustom.ForeColor = $cText
    $txtCustom.BorderStyle = [System.Windows.Forms.BorderStyle]::FixedSingle
    $txtCustom.Font = $fontRegular
    $groupApps.Controls.Add($txtCustom)

    $btnCustom = New-Object System.Windows.Forms.Button
    $btnCustom.Location = New-Object System.Drawing.Point(15, 305)
    $btnCustom.Size = New-Object System.Drawing.Size(305, 32)
    $btnCustom.Text = "Uruchom wpisany pakiet"
    $btnCustom.BackColor = $cBtnApp
    $btnCustom.ForeColor = $cText
    $btnCustom.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $btnCustom.FlatAppearance.BorderColor = $cCardBorder
    $btnCustom.Font = $fontBold
    $btnCustom.Add_Click({
        $pkg = $txtCustom.Text.Trim()
        if ([string]::IsNullOrWhiteSpace($pkg)) {
            [System.Windows.Forms.MessageBox]::Show("Wpisz poprawną nazwę pakietu Androida.", "Brak pakietu")
            return
        }
        if ($chkAutoTaskbar.Checked) { Start-Taskbar }
        Start-ScrcpyApp -PackageName $pkg -WindowTitle $pkg -DisplaySize $script:appDisplaySize
    })
    $groupApps.Controls.Add($btnCustom)

    # Pasek stanu na dole okna (Bateria, Model, Blokada)
    $lblStatus = New-Object System.Windows.Forms.Label
    $lblStatus.Text = "Inicjalizacja..."
    $lblStatus.Location = New-Object System.Drawing.Point(18, 665)
    $lblStatus.Size = New-Object System.Drawing.Size(335, 38)
    $lblStatus.ForeColor = $cTextMuted
    $lblStatus.Font = $fontSmall
    $form.Controls.Add($lblStatus)

    # Timer czuwania oraz odświeżania paska stanu
    $keepAwakeTimer = New-Object System.Windows.Forms.Timer
    $keepAwakeTimer.Interval = 8000
    $keepAwakeTimer.Add_Tick({
        Clean-ExitedProcesses

        if (Test-AdbDeviceSilent) {
            adb shell settings put system screen_off_timeout 2147483647 2>$null
            adb shell svc power stayon true 2>$null

            $isLocked = [bool](adb shell dumpsys window 2>$null | Select-String "isKeyguardShowing=true|mShowing=true")
            if ($isLocked) {
                Invoke-AdbUnlock
            }

            $bat = Get-DeviceBatteryStatus
            $modeStr = if ($script:isWifiConnected) { "Wi-Fi" } else { "USB" }
            $lblStatus.Text = "Urządzenie: $($script:deviceModel) ($modeStr)`nBateria: $bat | Czuwanie aktywne"
        }
        else {
            $lblStatus.Text = "Brak połączenia z telefonem (sprawdź kabel/Wi-Fi)"
        }
    })

    # Bezpieczne zdarzenie po wyświetleniu okna
    $form.Add_Shown({
        try {
            Update-DeviceInfo
            if ($script:deviceModel -ne "Brak urządzenia") {
                $lblHeader.Text = "Zarządzanie: $($script:deviceModel)"
            }
            Initialize-ScreenTimeoutSettings
            Enable-ScreenLockPrevention
            Optimize-RdcClipboard
            if ($chkAutoTaskbar.Checked) {
                Start-Taskbar
            }

            $bat = Get-DeviceBatteryStatus
            $modeStr = if ($script:isWifiConnected) { "Wi-Fi" } else { "USB" }
            $lblStatus.Text = "Urządzenie: $($script:deviceModel) ($modeStr)`nBateria: $bat | Czuwanie aktywne"

            $keepAwakeTimer.Start()
        }
        catch {
            # Błąd poboczny w tle nie zamyka okna
        }
    })

    # Zamknięcie formularza
    $form.Add_FormClosing({
        $keepAwakeTimer.Stop()
        Clean-ExitedProcesses

        $runningPids = @($script:launchedProcesses | Where-Object { -not $_.HasExited } | Select-Object -ExpandProperty Id)

        if ($runningPids.Count -eq 0) {
            Restore-ScreenLockSettings
        }
        else {
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

    $form.ShowDialog() | Out-Null
}
catch {
    $errLog = Join-Path $env:TEMP "scrcpy_manager_error.log"
    $errMsg = "Wystąpił błąd podczas uruchamiania Scrcpy Manager:`n`n$($_.Exception.ToString())"
    Set-Content -Path $errLog -Value $errMsg -Encoding UTF8 -ErrorAction SilentlyContinue

    [System.Windows.Forms.MessageBox]::Show(
        $errMsg,
        "Błąd Scrcpy Manager",
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Error
    )
}