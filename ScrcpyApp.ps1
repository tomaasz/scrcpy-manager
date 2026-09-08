try {
    Add-Type -AssemblyName System.Windows.Forms
    Add-Type -AssemblyName System.Drawing

    $scriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
    if (-not $scriptDir) { $scriptDir = (Get-Location).Path }

    # --- BEZPIECZEŃSTWO I KONFIGURACJA ---
    # PIN jest pobierany wyłącznie ze zmiennej środowiskowej SCRCPY_ADB_PIN.
    $adbPin = $env:SCRCPY_ADB_PIN

    # Stan urządzenia i sesji
    $script:appDisplaySize = "1080x2400"
    $script:deviceModel = "Android"
    $script:deviceManufacturer = ""
    $script:isWifiConnected = $false

    # Preferencje użytkownika (Motyw, Język)
    $prefFile = Join-Path $scriptDir "preferences.json"
    $script:isDarkMode = $true
    $script:currentLang = "PL"

    if (Test-Path $prefFile) {
        try {
            $pref = Get-Content $prefFile -Raw -Encoding UTF8 | ConvertFrom-Json
            if ($pref.theme -eq "light") { $script:isDarkMode = $false }
            if ($pref.lang -eq "EN") { $script:currentLang = "EN" }
        }
        catch {}
    }

    function Save-Preferences {
        try {
            $prefObj = @{
                theme = if ($script:isDarkMode) { "dark" } else { "light" }
                lang  = $script:currentLang
            }
            $json = $prefObj | ConvertTo-Json
            Set-Content -Path $prefFile -Value $json -Encoding UTF8 -ErrorAction SilentlyContinue
        }
        catch {}
    }

    # Plik stanu do bezpiecznego zapamiętania pierwotnego limitu wygaszania ekranu
    $stateFile = Join-Path $env:TEMP "scrcpy_manager_original_timeout.txt"
    $script:launchedProcesses = New-Object 'System.Collections.Generic.List[System.Diagnostics.Process]'
    $script:originalTimeout = 30000

    # --- SŁOWNIK JĘZYKOWY (I18N) ---

    $i18n = @{
        PL = @{
            HeaderPrefix    = "Zarządzanie:"
            NoDevice        = "Brak urządzenia"
            LaunchHero      = "▶  URUCHOM SCRCPY (Pełny Ekran)"
            DesktopMode     = "🖥️ Tryb Desktop"
            ResetPhone      = "🔄 Reset telefonu"
            ClipBtn         = "📋 Schowek"
            KeyBtn          = "⌨️ Klawiatura"
            WifiBtn         = "📶 Wi-Fi"
            AutoTaskbar     = "Włączaj Taskbar na telefonie"
            AudioPass       = "Przesyłaj dźwięk do PC"
            ResLabel        = "Rozdzielczość Windows App (RDP):"
            AppsGroup       = "Aplikacje w osobnym oknie"
            CustomLabel     = "Inny pakiet (np. com.spotify.music):"
            CustomBtn       = "Uruchom wpisany pakiet"
            StatusDevice    = "Urządzenie:"
            StatusBattery   = "Bateria:"
            StatusActive    = "Czuwanie aktywne"
            StatusNoPhone   = "Brak połączenia z telefonem (sprawdź kabel/Wi-Fi)"
            ChargingStr     = " (Ładowanie)"
            ThemeDark       = "☀️ Jasny"
            ThemeLight      = "🌙 Ciemny"
            LangSwitch      = "EN"
            MsgNoDevice     = "Nie wykryto podłączonego telefonu.`nSprawdź kabel USB, debugowanie USB w telefonie lub połączenie Wi-Fi."
            MsgDesktopOn    = "Zastosowano małe DPI, przyspieszono animacje i włączono tryb okienkowy na telefonie."
            MsgResetDone    = "Przywrócono domyślne ustawienia telefonu, zresetowano DPI i zamknięto Taskbar."
            MsgClipDone     = "Zastosowano optymalizacje schowka dla telefonu.`nPolecenie naprawcze dla zdalnego PC skopiowano do Twojego schowka:`n{0}`n`nSkróty w oknie:`n• Alt + V : Wklej schowek PC do sesji`n• Alt + C : Pobierz schowek sesji do PC"
            MsgKeyDone      = "Otwarto ustawienia klawiatury fizycznej w telefonie.`nUpewnij się, że układ klawiatury fizycznej 'scrcpy' ma zaznaczone 'Polski (programisty)'."
            MsgWifiNoIp     = "Nie udało się automatycznie wykryć adresu IP telefonu w sieci Wi-Fi.`nUpewnij się, że telefon jest połączony z tą samą siecią Wi-Fi co komputer."
            MsgWifiDone     = "Połączono bezprzewodowo z telefonem:`n{0}:5555`n`nMożesz teraz odłączyć kabel USB!"
            MsgPkgEmpty     = "Wpisz poprawną nazwę pakietu Androida."
            ResNames        = @(
                "Full HD 1080p (Natywna 1:1)",
                "2K QHD (2560x1440)",
                "2K QHD Kompakt (DPI 140)",
                "4K UHD (3840x2160)",
                "Domyślna telefonu"
            )
        }
        EN = @{
            HeaderPrefix    = "Managing:"
            NoDevice        = "No device found"
            LaunchHero      = "▶  LAUNCH SCRCPY (Full Screen)"
            DesktopMode     = "🖥️ Desktop Mode"
            ResetPhone      = "🔄 Reset Phone"
            ClipBtn         = "📋 Clipboard"
            KeyBtn          = "⌨️ Keyboard"
            WifiBtn         = "📶 Wi-Fi"
            AutoTaskbar     = "Auto-start Taskbar on phone"
            AudioPass       = "Forward audio to PC"
            ResLabel        = "Windows App Resolution (RDP):"
            AppsGroup       = "Launch App in Window"
            CustomLabel     = "Custom package (e.g. com.spotify.music):"
            CustomBtn       = "Launch custom package"
            StatusDevice    = "Device:"
            StatusBattery   = "Battery:"
            StatusActive    = "Keep-awake active"
            StatusNoPhone   = "No phone connected (check USB/Wi-Fi)"
            ChargingStr     = " (Charging)"
            ThemeDark       = "☀️ Light"
            ThemeLight      = "🌙 Dark"
            LangSwitch      = "PL"
            MsgNoDevice     = "No Android device detected.`nPlease check USB cable, USB debugging, or Wi-Fi connection."
            MsgDesktopOn    = "Low DPI applied, animations accelerated, and freeform mode enabled on phone."
            MsgResetDone    = "Phone restored to default settings, DPI reset, and Taskbar closed."
            MsgClipDone     = "Clipboard optimizations applied.`nRemote PC repair command copied to your clipboard:`n{0}`n`nWindow Shortcuts:`n• Alt + V : Paste PC clipboard to session`n• Alt + C : Copy session clipboard to PC"
            MsgKeyDone      = "Physical keyboard settings opened on phone.`nEnsure 'scrcpy' hardware keyboard layout is set to your preferred layout."
            MsgWifiNoIp     = "Could not automatically detect phone IP on Wi-Fi.`nEnsure your phone is connected to the same Wi-Fi network as this PC."
            MsgWifiDone     = "Connected wirelessly to phone:`n{0}:5555`n`nYou can now disconnect the USB cable!"
            MsgPkgEmpty     = "Please enter a valid Android package name."
            ResNames        = @(
                "Full HD 1080p (Native 1:1)",
                "2K QHD (2560x1440)",
                "2K QHD Compact (DPI 140)",
                "4K UHD (3840x2160)",
                "Device Native"
            )
        }
    }

    # Wartości techniczne rozdzielczości dla scrcpy
    $resValues = @(
        "1920x1080/160",
        "2560x1440/160",
        "2560x1440/140",
        "3840x2160/200",
        "AUTO"
    )

    # --- FUNKCJE POMOCNICZE ADB ---

    function Test-AdbDeviceSilent {
        $devices = adb devices 2>$null | Select-String "device$"
        return [bool]$devices
    }

    function Test-AdbDevice {
        if (-not (Test-AdbDeviceSilent)) {
            $t = $i18n[$script:currentLang]
            [System.Windows.Forms.MessageBox]::Show(
                $t.MsgNoDevice,
                $t.NoDevice,
                [System.Windows.Forms.MessageBoxButtons]::OK,
                [System.Windows.Forms.MessageBoxIcon]::Warning
            )
            return $false
        }
        return $true
    }

    function Update-DeviceInfo {
        if (-not (Test-AdbDeviceSilent)) {
            $script:deviceModel = $i18n[$script:currentLang].NoDevice
            return
        }

        try {
            $model = (adb shell getprop ro.product.model 2>$null)
            $mfg = (adb shell getprop ro.product.manufacturer 2>$null)
            if ($model) { $script:deviceModel = $model.Trim() }
            if ($mfg) { $script:deviceManufacturer = $mfg.Trim() }

            $wmSize = ((adb shell wm size 2>$null) -join "`n")
            if ($wmSize -match "Physical size:\s*(\d+)x(\d+)") {
                $script:appDisplaySize = "$($Matches[1])x$($Matches[2])"
            }
        }
        catch {}
    }

    function Get-DeviceBatteryStatus {
        if (-not (Test-AdbDeviceSilent)) { return "--" }
        try {
            $dump = ((adb shell dumpsys battery 2>$null) -join "`n")
            $level = 0
            $charging = $false

            if ($dump -match "(?m)^\s*level:\s*(\d+)") { $level = [int]$Matches[1] }
            if ($dump -match "(?m)^\s*(USB|AC|Wireless) powered:\s*true") { $charging = $true }

            $t = $i18n[$script:currentLang]
            $chargeStr = if ($charging) { $t.ChargingStr } else { "" }
            return "$level%$chargeStr"
        }
        catch {
            return "--"
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

        adb shell input keyevent 224 2>$null

        $isLocked = [bool](adb shell dumpsys window 2>$null | Select-String "isKeyguardShowing=true|mShowing=true")
        if ($isLocked -and -not [string]::IsNullOrWhiteSpace($adbPin)) {
            Start-Sleep -Milliseconds 350
            adb shell input swipe 500 1500 500 300 200 2>$null
            Start-Sleep -Milliseconds 350
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
        $t = $i18n[$script:currentLang]

        $ip = $null
        $ipRoute = ((adb shell ip route 2>$null) -join "`n")
        if ($ipRoute -match "src\s+(\d+\.\d+\.\d+\.\d+)") {
            $ip = $Matches[1]
        }
        if (-not $ip) {
            $ipAddr = ((adb shell ip addr show wlan0 2>$null) -join "`n")
            if ($ipAddr -match "inet\s+(\d+\.\d+\.\d+\.\d+)") {
                $ip = $Matches[1]
            }
        }
        if (-not $ip) {
            $ipProp = ((adb shell getprop dhcp.wlan0.ipaddress 2>$null) -join "`n")
            if ($ipProp -match "\d+\.\d+\.\d+\.\d+") {
                $ip = $Matches[0].Trim()
            }
        }

        if (-not $ip) {
            [System.Windows.Forms.MessageBox]::Show(
                $t.MsgWifiNoIp,
                "Wi-Fi",
                [System.Windows.Forms.MessageBoxButtons]::OK,
                [System.Windows.Forms.MessageBoxIcon]::Warning
            )
            return
        }

        adb tcpip 5555 2>$null | Out-Null
        Start-Sleep -Milliseconds 500
        $connectRes = (adb connect "$($ip):5555" 2>$null)

        if ($connectRes -match "connected") {
            $script:isWifiConnected = $true
            [System.Windows.Forms.MessageBox]::Show(
                ($t.MsgWifiDone -f $ip),
                "Wi-Fi",
                [System.Windows.Forms.MessageBoxButtons]::OK,
                [System.Windows.Forms.MessageBoxIcon]::Information
            )
        }
        else {
            [System.Windows.Forms.MessageBox]::Show(
                "Result: $connectRes",
                "Wi-Fi",
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

        if ($chkAudio -and -not $chkAudio.Checked) {
            $argListItems += "--no-audio"
        }

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
                "Error starting scrcpy: $($_.Exception.Message)",
                "Error",
                [System.Windows.Forms.MessageBoxButtons]::OK,
                [System.Windows.Forms.MessageBoxIcon]::Error
            )
        }
    }

    # --- WCZYTYWANIE APLIKACJI (apps.json) ---

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
        catch {}
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

    $appButtons = @($appButtons | Sort-Object { $_.Text })

    # --- PALETA KOLORÓW DLA MOTYWÓW ---

    $themeColors = @{
        Dark = @{
            Bg          = [System.Drawing.Color]::FromArgb(24, 25, 32)
            Card        = [System.Drawing.Color]::FromArgb(33, 35, 45)
            CardBorder  = [System.Drawing.Color]::FromArgb(52, 56, 70)
            Text        = [System.Drawing.Color]::FromArgb(240, 242, 248)
            TextMuted   = [System.Drawing.Color]::FromArgb(150, 155, 175)
            BtnHero     = [System.Drawing.Color]::FromArgb(35, 145, 75)
            BtnHeroText = [System.Drawing.Color]::White
            BtnMode     = [System.Drawing.Color]::FromArgb(44, 48, 62)
            BtnModeText = [System.Drawing.Color]::FromArgb(230, 235, 245)
            BtnReset    = [System.Drawing.Color]::FromArgb(55, 35, 42)
            BtnResetText= [System.Drawing.Color]::FromArgb(255, 130, 130)
            BtnTool     = [System.Drawing.Color]::FromArgb(35, 75, 135)
            BtnToolText = [System.Drawing.Color]::White
            BtnWifi     = [System.Drawing.Color]::FromArgb(95, 60, 155)
            BtnApp      = [System.Drawing.Color]::FromArgb(45, 48, 60)
            BtnAppText  = [System.Drawing.Color]::FromArgb(235, 238, 248)
            InputBg     = [System.Drawing.Color]::FromArgb(28, 30, 38)
            InputText   = [System.Drawing.Color]::FromArgb(240, 242, 248)
            ToggleBg    = [System.Drawing.Color]::FromArgb(44, 48, 62)
            ToggleText  = [System.Drawing.Color]::FromArgb(230, 235, 245)
        }
        Light = @{
            Bg          = [System.Drawing.Color]::FromArgb(245, 246, 250)
            Card        = [System.Drawing.Color]::FromArgb(255, 255, 255)
            CardBorder  = [System.Drawing.Color]::FromArgb(215, 220, 232)
            Text        = [System.Drawing.Color]::FromArgb(25, 28, 36)
            TextMuted   = [System.Drawing.Color]::FromArgb(105, 110, 125)
            BtnHero     = [System.Drawing.Color]::FromArgb(28, 135, 68)
            BtnHeroText = [System.Drawing.Color]::White
            BtnMode     = [System.Drawing.Color]::FromArgb(238, 241, 248)
            BtnModeText = [System.Drawing.Color]::FromArgb(35, 40, 55)
            BtnReset    = [System.Drawing.Color]::FromArgb(254, 238, 238)
            BtnResetText= [System.Drawing.Color]::FromArgb(195, 40, 40)
            BtnTool     = [System.Drawing.Color]::FromArgb(38, 105, 195)
            BtnToolText = [System.Drawing.Color]::White
            BtnWifi     = [System.Drawing.Color]::FromArgb(120, 75, 190)
            BtnApp      = [System.Drawing.Color]::FromArgb(246, 248, 253)
            BtnAppText  = [System.Drawing.Color]::FromArgb(30, 35, 48)
            InputBg     = [System.Drawing.Color]::White
            InputText   = [System.Drawing.Color]::FromArgb(25, 28, 36)
            ToggleBg    = [System.Drawing.Color]::FromArgb(232, 235, 245)
            ToggleText  = [System.Drawing.Color]::FromArgb(35, 40, 55)
        }
    }

    # Fonty
    $fontRegular = New-Object System.Drawing.Font("Segoe UI", [float]9, [System.Drawing.FontStyle]::Regular)
    $fontBold    = New-Object System.Drawing.Font("Segoe UI", [float]9, [System.Drawing.FontStyle]::Bold)
    $fontHero    = New-Object System.Drawing.Font("Segoe UI", [float]10, [System.Drawing.FontStyle]::Bold)
    $fontTitle   = New-Object System.Drawing.Font("Segoe UI", [float]9.5, [System.Drawing.FontStyle]::Bold)
    $fontSmall   = New-Object System.Drawing.Font("Segoe UI", [float]8.2, [System.Drawing.FontStyle]::Regular)

    # --- OKNO FORMULARZA ---

    $form = New-Object System.Windows.Forms.Form
    $form.Text = "Scrcpy Manager"
    $form.Size = New-Object System.Drawing.Size(385, 755)
    $form.StartPosition = "CenterScreen"
    $form.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::FixedDialog
    $form.MaximizeBox = $false

    # Nagłówek: Nazwa urządzenia (po lewej)
    $lblHeader = New-Object System.Windows.Forms.Label
    $lblHeader.Text = "Zarządzanie: Inicjalizacja..."
    $lblHeader.Location = New-Object System.Drawing.Point(18, 13)
    $lblHeader.Size = New-Object System.Drawing.Size(205, 22)
    $lblHeader.Font = $fontTitle
    $form.Controls.Add($lblHeader)

    # Przycisk Przełączania Motywu (Jasny / Ciemny)
    $btnTheme = New-Object System.Windows.Forms.Button
    $btnTheme.Location = New-Object System.Drawing.Point(232, 10)
    $btnTheme.Size = New-Object System.Drawing.Size(74, 26)
    $btnTheme.Font = $fontSmall
    $btnTheme.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $btnTheme.FlatAppearance.BorderSize = 1
    $form.Controls.Add($btnTheme)

    # Przycisk Przełączania Języka (PL / EN)
    $btnLang = New-Object System.Windows.Forms.Button
    $btnLang.Location = New-Object System.Drawing.Point(310, 10)
    $btnLang.Size = New-Object System.Drawing.Size(43, 26)
    $btnLang.Font = $fontSmall
    $btnLang.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $btnLang.FlatAppearance.BorderSize = 1
    $form.Controls.Add($btnLang)

    # Główny przycisk HERO: Uruchom Scrcpy (Pełny ekran)
    $btnScrcpy = New-Object System.Windows.Forms.Button
    $btnScrcpy.Location = New-Object System.Drawing.Point(18, 44)
    $btnScrcpy.Size = New-Object System.Drawing.Size(335, 42)
    $btnScrcpy.Font = $fontHero
    $btnScrcpy.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $btnScrcpy.FlatAppearance.BorderSize = 0
    $btnScrcpy.Add_Click({
        if (-not (Test-AdbDevice)) { return }

        Enable-ScreenLockPrevention

        if ($chkAutoTaskbar.Checked) {
            Start-Taskbar
        }

        adb shell settings put system accelerometer_rotation 0 2>$null
        adb shell settings put system user_rotation 1 2>$null
        Invoke-AdbUnlock

        $title = if ($script:deviceModel) { "$($script:deviceModel) (Scrcpy)" } else { "Android (Scrcpy)" }
        $audioArg = if ($chkAudio.Checked) { "" } else { "--no-audio" }

        try {
            $proc = Start-Process "scrcpy" -ArgumentList "-S -w -K -M --window-title=`"$title`" $audioArg" -PassThru -ErrorAction Stop
            Register-ScrcpyProcess -Process $proc
        }
        catch {
            [System.Windows.Forms.MessageBox]::Show(
                "Error starting scrcpy: $($_.Exception.Message)",
                "Error",
                [System.Windows.Forms.MessageBoxButtons]::OK,
                [System.Windows.Forms.MessageBoxIcon]::Error
            )
        }
    })
    $form.Controls.Add($btnScrcpy)

    # Rząd trybów: Tryb Desktop (lewo) + Reset telefonu (prawo)
    $btnDesktop = New-Object System.Windows.Forms.Button
    $btnDesktop.Location = New-Object System.Drawing.Point(18, 92)
    $btnDesktop.Size = New-Object System.Drawing.Size(163, 34)
    $btnDesktop.Font = $fontBold
    $btnDesktop.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $btnDesktop.FlatAppearance.BorderSize = 1
    $btnDesktop.Add_Click({
        if (-not (Test-AdbDevice)) { return }

        adb shell wm density 250 2>$null
        adb shell settings put global window_animation_scale 0.5 2>$null
        adb shell settings put global transition_animation_scale 0.5 2>$null
        adb shell settings put global animator_duration_scale 0.5 2>$null
        adb shell settings put global enable_freeform_support 1 2>$null
        adb shell settings put secure force_resizable_activities 1 2>$null

        Start-Taskbar

        $t = $i18n[$script:currentLang]
        [System.Windows.Forms.MessageBox]::Show(
            $t.MsgDesktopOn,
            $t.DesktopMode,
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Information
        )
    })
    $form.Controls.Add($btnDesktop)

    $btnNormal = New-Object System.Windows.Forms.Button
    $btnNormal.Location = New-Object System.Drawing.Point(190, 92)
    $btnNormal.Size = New-Object System.Drawing.Size(163, 34)
    $btnNormal.Font = $fontBold
    $btnNormal.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $btnNormal.FlatAppearance.BorderSize = 1
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

        $t = $i18n[$script:currentLang]
        [System.Windows.Forms.MessageBox]::Show(
            $t.MsgResetDone,
            $t.ResetPhone,
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Information
        )
    })
    $form.Controls.Add($btnNormal)

    # Rząd narzędzi: Schowek, Klawiatura, Wi-Fi (3 równe kolumny)
    $btnClipFix = New-Object System.Windows.Forms.Button
    $btnClipFix.Location = New-Object System.Drawing.Point(18, 132)
    $btnClipFix.Size = New-Object System.Drawing.Size(106, 29)
    $btnClipFix.Font = $fontSmall
    $btnClipFix.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $btnClipFix.FlatAppearance.BorderSize = 0
    $btnClipFix.Add_Click({
        if (Test-AdbDeviceSilent) { Optimize-RdcClipboard }
        $remoteFixCmd = "taskkill /f /im rdpclip.exe & start rdpclip.exe"
        try { [System.Windows.Forms.Clipboard]::SetText($remoteFixCmd) } catch {}

        $t = $i18n[$script:currentLang]
        [System.Windows.Forms.MessageBox]::Show(
            ($t.MsgClipDone -f $remoteFixCmd),
            $t.ClipBtn,
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Information
        )
    })
    $form.Controls.Add($btnClipFix)

    $btnKeyFix = New-Object System.Windows.Forms.Button
    $btnKeyFix.Location = New-Object System.Drawing.Point(130, 132)
    $btnKeyFix.Size = New-Object System.Drawing.Size(112, 29)
    $btnKeyFix.Font = $fontSmall
    $btnKeyFix.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $btnKeyFix.FlatAppearance.BorderSize = 0
    $btnKeyFix.Add_Click({
        if (Test-AdbDeviceSilent) {
            adb shell am start -a android.settings.HARD_KEYBOARD_SETTINGS 2>$null | Out-Null
        }
        $t = $i18n[$script:currentLang]
        [System.Windows.Forms.MessageBox]::Show(
            $t.MsgKeyDone,
            $t.KeyBtn,
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Information
        )
    })
    $form.Controls.Add($btnKeyFix)

    $btnWifi = New-Object System.Windows.Forms.Button
    $btnWifi.Location = New-Object System.Drawing.Point(248, 132)
    $btnWifi.Size = New-Object System.Drawing.Size(105, 29)
    $btnWifi.Font = $fontSmall
    $btnWifi.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $btnWifi.FlatAppearance.BorderSize = 0
    $btnWifi.Add_Click({ Switch-ToWirelessAdb })
    $form.Controls.Add($btnWifi)

    # Opcje: Taskbar i Audio
    $chkAutoTaskbar = New-Object System.Windows.Forms.CheckBox
    $chkAutoTaskbar.Checked = $true
    $chkAutoTaskbar.AutoSize = $true
    $chkAutoTaskbar.Font = $fontRegular
    $chkAutoTaskbar.Location = New-Object System.Drawing.Point(20, 168)
    $form.Controls.Add($chkAutoTaskbar)

    $chkAudio = New-Object System.Windows.Forms.CheckBox
    $chkAudio.Checked = $true
    $chkAudio.AutoSize = $true
    $chkAudio.Font = $fontRegular
    $chkAudio.Location = New-Object System.Drawing.Point(20, 190)
    $form.Controls.Add($chkAudio)

    # Rozdzielczość dla Windows App (RDP)
    $lblRes = New-Object System.Windows.Forms.Label
    $lblRes.Location = New-Object System.Drawing.Point(18, 214)
    $lblRes.AutoSize = $true
    $lblRes.Font = $fontRegular
    $form.Controls.Add($lblRes)

    $cmbRes = New-Object System.Windows.Forms.ComboBox
    $cmbRes.DropDownStyle = [System.Windows.Forms.ComboBoxStyle]::DropDownList
    $cmbRes.DrawMode = [System.Windows.Forms.DrawMode]::OwnerDrawFixed
    $cmbRes.ItemHeight = 22
    $cmbRes.Location = New-Object System.Drawing.Point(18, 234)
    $cmbRes.Size = New-Object System.Drawing.Size(335, 26)
    $cmbRes.Font = $fontRegular

    # Wypełnienie początkowe
    $initialNames = $i18n[$script:currentLang].ResNames
    foreach ($rn in $initialNames) { $cmbRes.Items.Add($rn) | Out-Null }
    $cmbRes.SelectedIndex = 0

    # Własne rysowanie (OwnerDraw) — idealny dark/light mode bez białych plam
    $cmbRes.Add_DrawItem({
        param($s, $e)
        if ($e.Index -lt 0) { return }
        $isDark = $script:isDarkMode
        $isSelected = ($e.State -band [System.Windows.Forms.DrawItemState]::Selected) -ne 0

        $bgCol = if ($isSelected) {
            if ($isDark) { [System.Drawing.Color]::FromArgb(40, 95, 175) } else { [System.Drawing.Color]::FromArgb(60, 125, 220) }
        } elseif ($isDark) {
            [System.Drawing.Color]::FromArgb(33, 35, 45)
        } else {
            [System.Drawing.Color]::FromArgb(255, 255, 255)
        }

        $textCol = if ($isSelected -or $isDark) {
            [System.Drawing.Color]::FromArgb(240, 242, 248)
        } else {
            [System.Drawing.Color]::FromArgb(25, 28, 36)
        }

        $bgBrush = New-Object System.Drawing.SolidBrush($bgCol)
        $textBrush = New-Object System.Drawing.SolidBrush($textCol)

        $e.Graphics.FillRectangle($bgBrush, $e.Bounds)
        $itemText = $s.Items[$e.Index].ToString()
        $textY = $e.Bounds.Y + [math]::Max(0, [int](($e.Bounds.Height - $s.Font.Height) / 2))
        $e.Graphics.DrawString($itemText, $s.Font, $textBrush, [float]($e.Bounds.X + 6), [float]$textY)

        $bgBrush.Dispose()
        $textBrush.Dispose()
    })
    $form.Controls.Add($cmbRes)

    # --- GRUPA APLIKACJI (2 KOLUMNY, ALFABETYCZNIE) ---

    $groupApps = New-Object System.Windows.Forms.GroupBox
    $groupApps.Location = New-Object System.Drawing.Point(18, 268)
    $groupApps.Size = New-Object System.Drawing.Size(335, 395)
    $groupApps.Font = $fontBold
    $form.Controls.Add($groupApps)

    $colWidth = 146
    $btnHeight = 34
    $rowPitch = 40
    $colPitch = 156
    $startX = 15
    $startY = 24

    $createdAppButtons = New-Object 'System.Collections.Generic.List[System.Windows.Forms.Button]'

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
        $btn.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
        $btn.FlatAppearance.BorderSize = 1

        $pkg = $app.Package
        $appName = $app.Text
        $useUhid = $app.Flags -contains "-UseUhidKeyboard"
        $forwardClicks = $app.Flags -contains "-ForwardAllClicks"

        $btn.Add_Click({
            if ($chkAutoTaskbar.Checked) { Start-Taskbar }
            $disp = $script:appDisplaySize

            if ($pkg -eq "com.microsoft.rdc.androidx") {
                $idx = $cmbRes.SelectedIndex
                if ($idx -ge 0 -and $idx -lt $resValues.Count) {
                    $val = $resValues[$idx]
                    $disp = if ($val -eq "AUTO") { $script:appDisplaySize } else { $val }
                }
                else {
                    $disp = "1920x1080/160"
                }
            }

            Start-ScrcpyApp -PackageName $pkg -WindowTitle $appName -UseUhidKeyboard:$useUhid -ForwardAllClicks:$forwardClicks -DisplaySize $disp
        }.GetNewClosure())

        $groupApps.Controls.Add($btn)
        $createdAppButtons.Add($btn)
    }

    # Własny pakiet
    $lblCustom = New-Object System.Windows.Forms.Label
    $lblCustom.Location = New-Object System.Drawing.Point(15, 290)
    $lblCustom.AutoSize = $true
    $lblCustom.Font = $fontSmall
    $groupApps.Controls.Add($lblCustom)

    $txtCustom = New-Object System.Windows.Forms.TextBox
    $txtCustom.Location = New-Object System.Drawing.Point(15, 310)
    $txtCustom.Size = New-Object System.Drawing.Size(305, 23)
    $txtCustom.BorderStyle = [System.Windows.Forms.BorderStyle]::FixedSingle
    $txtCustom.Font = $fontRegular
    $groupApps.Controls.Add($txtCustom)

    $btnCustom = New-Object System.Windows.Forms.Button
    $btnCustom.Location = New-Object System.Drawing.Point(15, 342)
    $btnCustom.Size = New-Object System.Drawing.Size(305, 32)
    $btnCustom.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $btnCustom.FlatAppearance.BorderSize = 1
    $btnCustom.Font = $fontBold
    $btnCustom.Add_Click({
        $pkg = $txtCustom.Text.Trim()
        $t = $i18n[$script:currentLang]
        if ([string]::IsNullOrWhiteSpace($pkg)) {
            [System.Windows.Forms.MessageBox]::Show($t.MsgPkgEmpty, "Scrcpy", [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Warning)
            return
        }
        if ($chkAutoTaskbar.Checked) { Start-Taskbar }
        Start-ScrcpyApp -PackageName $pkg -WindowTitle $pkg -DisplaySize $script:appDisplaySize
    })
    $groupApps.Controls.Add($btnCustom)

    # Pasek stanu na dole
    $lblStatus = New-Object System.Windows.Forms.Label
    $lblStatus.Location = New-Object System.Drawing.Point(18, 672)
    $lblStatus.Size = New-Object System.Drawing.Size(335, 38)
    $lblStatus.Font = $fontSmall
    $form.Controls.Add($lblStatus)

    # --- FUNKCJE STYLIZACJI MOTYWU I JĘZYKA ---

    function Apply-Theme {
        $c = if ($script:isDarkMode) { $themeColors.Dark } else { $themeColors.Light }

        $form.BackColor = $c.Bg
        $form.ForeColor = $c.Text

        $lblHeader.ForeColor = $c.Text

        $btnTheme.BackColor = $c.ToggleBg
        $btnTheme.ForeColor = $c.ToggleText
        $btnTheme.FlatAppearance.BorderColor = $c.CardBorder

        $btnLang.BackColor = $c.ToggleBg
        $btnLang.ForeColor = $c.ToggleText
        $btnLang.FlatAppearance.BorderColor = $c.CardBorder

        $btnScrcpy.BackColor = $c.BtnHero
        $btnScrcpy.ForeColor = $c.BtnHeroText

        $btnDesktop.BackColor = $c.BtnMode
        $btnDesktop.ForeColor = $c.BtnModeText
        $btnDesktop.FlatAppearance.BorderColor = $c.CardBorder

        $btnNormal.BackColor = $c.BtnReset
        $btnNormal.ForeColor = $c.BtnResetText
        $btnNormal.FlatAppearance.BorderColor = $c.CardBorder

        $btnClipFix.BackColor = $c.BtnTool
        $btnClipFix.ForeColor = $c.BtnToolText

        $btnKeyFix.BackColor = $c.BtnTool
        $btnKeyFix.ForeColor = $c.BtnToolText

        $btnWifi.BackColor = $c.BtnWifi
        $btnWifi.ForeColor = [System.Drawing.Color]::White

        $chkAutoTaskbar.ForeColor = $c.Text
        $chkAudio.ForeColor = $c.Text

        $lblRes.ForeColor = $c.TextMuted
        $cmbRes.BackColor = $c.Card
        $cmbRes.ForeColor = $c.Text

        $groupApps.BackColor = $c.Card
        $groupApps.ForeColor = $c.Text

        foreach ($btn in $createdAppButtons) {
            $btn.BackColor = $c.BtnApp
            $btn.ForeColor = $c.BtnAppText
            $btn.FlatAppearance.BorderColor = $c.CardBorder
        }

        $lblCustom.ForeColor = $c.TextMuted
        $txtCustom.BackColor = $c.InputBg
        $txtCustom.ForeColor = $c.InputText

        $btnCustom.BackColor = $c.BtnApp
        $btnCustom.ForeColor = $c.BtnAppText
        $btnCustom.FlatAppearance.BorderColor = $c.CardBorder

        $lblStatus.ForeColor = $c.TextMuted

        $cmbRes.Invalidate()
        $form.Invalidate($true)
    }

    function Apply-Language {
        $t = $i18n[$script:currentLang]

        $dev = if ($script:deviceModel) { $script:deviceModel } else { $t.NoDevice }
        $lblHeader.Text = "$($t.HeaderPrefix) $dev"

        $btnTheme.Text = if ($script:isDarkMode) { $t.ThemeDark } else { $t.ThemeLight }
        $btnLang.Text = $t.LangSwitch

        $btnScrcpy.Text = $t.LaunchHero
        $btnDesktop.Text = $t.DesktopMode
        $btnNormal.Text = $t.ResetPhone
        $btnClipFix.Text = $t.ClipBtn
        $btnKeyFix.Text = $t.KeyBtn
        $btnWifi.Text = $t.WifiBtn

        $chkAutoTaskbar.Text = $t.AutoTaskbar
        $chkAudio.Text = $t.AudioPass
        $lblRes.Text = $t.ResLabel

        # Odświeżenie elementów w combobox z zachowaniem wybranego indeksu
        $currIdx = $cmbRes.SelectedIndex
        if ($currIdx -lt 0) { $currIdx = 0 }
        $cmbRes.Items.Clear()
        foreach ($rn in $t.ResNames) {
            $cmbRes.Items.Add($rn) | Out-Null
        }
        $cmbRes.SelectedIndex = [math]::Min($currIdx, $cmbRes.Items.Count - 1)

        $groupApps.Text = $t.AppsGroup
        $lblCustom.Text = $t.CustomLabel
        $btnCustom.Text = $t.CustomBtn

        # Pasek stanu
        Update-StatusDisplay
    }

    function Update-StatusDisplay {
        $t = $i18n[$script:currentLang]
        if (Test-AdbDeviceSilent) {
            $bat = Get-DeviceBatteryStatus
            $modeStr = if ($script:isWifiConnected) { "Wi-Fi" } else { "USB" }
            $lblStatus.Text = "$($t.StatusDevice) $($script:deviceModel) ($modeStr)`n$($t.StatusBattery) $bat | $($t.StatusActive)"
        }
        else {
            $lblStatus.Text = $t.StatusNoPhone
        }
    }

    # Obsługa przełączania motywu
    $btnTheme.Add_Click({
        $script:isDarkMode = -not $script:isDarkMode
        Save-Preferences
        Apply-Theme
        Apply-Language
    })

    # Obsługa przełączania języka
    $btnLang.Add_Click({
        $script:currentLang = if ($script:currentLang -eq "PL") { "EN" } else { "PL" }
        Save-Preferences
        Apply-Language
        Apply-Theme
    })

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

            Update-StatusDisplay
        }
        else {
            $t = $i18n[$script:currentLang]
            $lblStatus.Text = $t.StatusNoPhone
        }
    })

    # Zdarzenie po załadowaniu okna na ekranie
    $form.Add_Shown({
        try {
            Update-DeviceInfo
            Apply-Language
            Apply-Theme

            Initialize-ScreenTimeoutSettings
            Enable-ScreenLockPrevention
            Optimize-RdcClipboard
            if ($chkAutoTaskbar.Checked) {
                Start-Taskbar
            }

            Update-StatusDisplay
            $keepAwakeTimer.Start()
        }
        catch {}
    })

    # Zdarzenie zamknięcia okna
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