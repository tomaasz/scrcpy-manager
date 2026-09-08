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
            StatusConnectedUsb  = "połączono przez USB"
            StatusConnectedWifi = "połączono przez Wi‑Fi"
            StatusNoPhone       = "Brak połączenia z telefonem"
            StatusCheckConn     = "Podłącz kabel USB lub sprawdź sieć Wi‑Fi"
            BatteryLabel        = "Bateria:"
            ChargingStr         = " (ładowanie)"
            StatusActive        = "Czuwanie aktywne"
            ThemeDark           = "☀ Jasny"
            ThemeLight          = "☾ Ciemny"
            LangSwitch          = "EN"

            LaunchHero          = "▶  Uruchom scrcpy"
            FullScreenOpt       = "Uruchamiaj w trybie pełnoekranowym (-f)"

            SectionOptions      = "OBRAZ, DŹWIĘK I STEROWANIE"
            ResLabel            = "Rozdzielczość wirtualnego ekranu (RDP):"
            AudioPass           = "Przesyłaj dźwięk do PC"
            AutoTaskbar         = "Uruchamiaj Taskbar"
            WifiBtn             = "Połącz przez Wi‑Fi"
            KeyBtn              = "Klawiatura fizyczna"
            ClipBtn             = "Naprawa schowka"

            SectionApps         = "APLIKACJE W OKNACH"
            AppsSubtitle        = "Uruchom wybraną aplikację w oddzielnym oknie scrcpy"
            CustomLabel         = "Inny pakiet Androida (np. com.spotify.music):"
            CustomBtn           = "Uruchom"

            SectionDevice       = "OPERACJE NA URZĄDZENIU"
            DesktopMode         = "Włącz tryb pulpitu"
            RestoreDefault      = "Przywróć standardowy widok"
            RebootBtn           = "⚠  Uruchom telefon ponownie"

            TitleRestartConfirm = "Potwierdzenie restartu"
            MsgRestartConfirm   = "Czy na pewno chcesz uruchomić telefon ponownie?"
            MsgRestartSent      = "Polecenie restartu zostało wysłane do telefonu."
            MsgNoDevice         = "Nie wykryto podłączonego telefonu.`nSprawdź kabel USB, debugowanie USB w telefonie lub połączenie Wi‑Fi."
            MsgDesktopOn        = "Zastosowano niski DPI (250), przyspieszono animacje i włączono tryb okienkowy na telefonie."
            MsgResetDone        = "Przywrócono domyślne DPI telefonu, zresetowano animacje i zamknięto Taskbar."
            MsgClipDone         = "Zastosowano optymalizacje schowka dla telefonu.`nPolecenie naprawcze dla zdalnego PC skopiowano do schowka:`n{0}`n`nSkróty w oknie:`n• Alt + V : Wklej schowek PC do sesji`n• Alt + C : Pobierz schowek sesji do PC"
            MsgKeyDone          = "Otwarto ustawienia klawiatury fizycznej w telefonie.`nUpewnij się, że układ klawiatury fizycznej 'scrcpy' ma zaznaczone 'Polski (programisty)'."
            MsgWifiNoIp         = "Nie udało się automatycznie wykryć adresu IP telefonu w sieci Wi‑Fi.`nUpewnij się, że telefon jest połączony z tą samą siecią Wi‑Fi co komputer."
            MsgWifiDone         = "Połączono bezprzewodowo z telefonem:`n{0}:5555`n`nMożesz teraz odłączyć kabel USB!"
            MsgPkgEmpty         = "Wpisz poprawną nazwę pakietu Androida."
            ResNames            = @(
                "Full HD 1080p (Natywna 1:1)",
                "2K QHD (2560x1440)",
                "2K QHD Kompakt (DPI 140)",
                "4K UHD (3840x2160)",
                "Domyślna telefonu"
            )
        }
        EN = @{
            StatusConnectedUsb  = "connected via USB"
            StatusConnectedWifi = "connected via Wi‑Fi"
            StatusNoPhone       = "No phone connected"
            StatusCheckConn     = "Connect USB cable or check Wi‑Fi network"
            BatteryLabel        = "Battery:"
            ChargingStr         = " (charging)"
            StatusActive        = "Keep-awake active"
            ThemeDark           = "☀ Light"
            ThemeLight          = "☾ Dark"
            LangSwitch          = "PL"

            LaunchHero          = "▶  Launch scrcpy"
            FullScreenOpt       = "Launch in full screen mode (-f)"

            SectionOptions      = "DISPLAY, AUDIO & CONTROL"
            ResLabel            = "Virtual display resolution (RDP):"
            AudioPass           = "Forward audio to PC"
            AutoTaskbar         = "Start Taskbar"
            WifiBtn             = "Connect via Wi‑Fi"
            KeyBtn              = "Hardware keyboard"
            ClipBtn             = "Fix clipboard"

            SectionApps         = "WINDOWED APPLICATIONS"
            AppsSubtitle        = "Launch selected app in a dedicated scrcpy window"
            CustomLabel         = "Custom Android package (e.g. com.spotify.music):"
            CustomBtn           = "Launch"

            SectionDevice       = "DEVICE OPERATIONS"
            DesktopMode         = "Enable desktop mode"
            RestoreDefault      = "Restore default view"
            RebootBtn           = "⚠  Restart phone"

            TitleRestartConfirm = "Confirm Restart"
            MsgRestartConfirm   = "Are you sure you want to restart the phone?"
            MsgRestartSent      = "Restart command has been sent to the phone."
            MsgNoDevice         = "No Android device detected.`nPlease check USB cable, USB debugging, or Wi‑Fi connection."
            MsgDesktopOn        = "Low DPI (250) applied, animations accelerated, and freeform mode enabled on phone."
            MsgResetDone        = "Default DPI restored, animations reset, and Taskbar closed."
            MsgClipDone         = "Clipboard optimizations applied.`nRemote PC repair command copied to clipboard:`n{0}`n`nWindow Shortcuts:`n• Alt + V : Paste PC clipboard to session`n• Alt + C : Copy session clipboard to PC"
            MsgKeyDone          = "Physical keyboard settings opened on phone.`nEnsure 'scrcpy' hardware keyboard layout is set to your preferred layout."
            MsgWifiNoIp         = "Could not automatically detect phone IP on Wi‑Fi.`nEnsure your phone is connected to the same Wi‑Fi network as this PC."
            MsgWifiDone         = "Connected wirelessly to phone:`n{0}:5555`n`nYou can now disconnect the USB cable!"
            MsgPkgEmpty         = "Please enter a valid Android package name."
            ResNames            = @(
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
                $t.StatusNoPhone,
                [System.Windows.Forms.MessageBoxButtons]::OK,
                [System.Windows.Forms.MessageBoxIcon]::Warning
            )
            return $false
        }
        return $true
    }

    function Update-DeviceInfo {
        if (-not (Test-AdbDeviceSilent)) {
            $script:deviceModel = ""
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
                "Wi‑Fi",
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
                "Wi‑Fi",
                [System.Windows.Forms.MessageBoxButtons]::OK,
                [System.Windows.Forms.MessageBoxIcon]::Information
            )
        }
        else {
            [System.Windows.Forms.MessageBox]::Show(
                "Result: $connectRes",
                "Wi‑Fi",
                [System.Windows.Forms.MessageBoxButtons]::OK,
                [System.Windows.Forms.MessageBoxIcon]::Warning
            )
        }
    }

    function Restart-DeviceWithConfirmation {
        if (-not (Test-AdbDevice)) { return }
        $t = $i18n[$script:currentLang]
        $res = [System.Windows.Forms.MessageBox]::Show(
            $t.MsgRestartConfirm,
            $t.TitleRestartConfirm,
            [System.Windows.Forms.MessageBoxButtons]::YesNo,
            [System.Windows.Forms.MessageBoxIcon]::Warning
        )
        if ($res -eq [System.Windows.Forms.DialogResult]::Yes) {
            adb reboot 2>$null | Out-Null
            [System.Windows.Forms.MessageBox]::Show(
                $t.MsgRestartSent,
                $t.TitleRestartConfirm,
                [System.Windows.Forms.MessageBoxButtons]::OK,
                [System.Windows.Forms.MessageBoxIcon]::Information
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
            @{ Text = "Claude";                  Package = "com.anthropic.claude";                  Flags = @() },
            @{ Text = "ConneckBot";              Package = "org.connectbot";                        Flags = @("-UseUhidKeyboard") },
            @{ Text = "Gmail (Wszystkie)";       Package = "com.google.android.gm";                 Flags = @() },
            @{ Text = "Messenger";               Package = "com.facebook.orca";                     Flags = @() },
            @{ Text = "TurboTel";                Package = "ellipi.messenger";                      Flags = @() },
            @{ Text = "Ustawienia";              Package = "com.android.settings";                  Flags = @() },
            @{ Text = "Vivaldi";                 Package = "com.vivaldi.browser";                   Flags = @("-ForwardAllClicks") },
            @{ Text = "WhatsApp";                Package = "com.whatsapp";                          Flags = @() },
            @{ Text = "Wiadomości (Google)";     Package = "com.google.android.apps.messaging";     Flags = @() },
            @{ Text = "Windows App (RDP)";       Package = "com.microsoft.rdc.androidx";           Flags = @("-UseUhidKeyboard", "-ForwardAllClicks") }
        )
    }

    $appButtons = @($appButtons | Sort-Object { $_.Text })

    # --- PALETA KOLORÓW DLA MOTYWÓW ---

    $themeColors = @{
        Dark = @{
            Bg               = [System.Drawing.Color]::FromArgb(24, 25, 32)
            Card             = [System.Drawing.Color]::FromArgb(33, 35, 45)
            CardBorder       = [System.Drawing.Color]::FromArgb(52, 56, 70)
            Text             = [System.Drawing.Color]::FromArgb(240, 242, 248)
            TextMuted        = [System.Drawing.Color]::FromArgb(150, 155, 175)
            StatusDotOnline  = [System.Drawing.Color]::FromArgb(46, 204, 113)
            StatusDotOffline = [System.Drawing.Color]::FromArgb(127, 140, 141)
            BtnHero          = [System.Drawing.Color]::FromArgb(35, 145, 75)
            BtnHeroText      = [System.Drawing.Color]::White
            BtnMode          = [System.Drawing.Color]::FromArgb(44, 48, 62)
            BtnModeText      = [System.Drawing.Color]::FromArgb(230, 235, 245)
            BtnModeBorder    = [System.Drawing.Color]::FromArgb(66, 71, 92)
            BtnReboot        = [System.Drawing.Color]::FromArgb(62, 35, 40)
            BtnRebootText    = [System.Drawing.Color]::FromArgb(255, 138, 138)
            BtnRebootBorder  = [System.Drawing.Color]::FromArgb(110, 46, 53)
            BtnTool          = [System.Drawing.Color]::FromArgb(39, 58, 94)
            BtnToolText      = [System.Drawing.Color]::FromArgb(214, 228, 255)
            BtnToolBorder    = [System.Drawing.Color]::FromArgb(58, 80, 126)
            BtnApp           = [System.Drawing.Color]::FromArgb(37, 40, 52)
            BtnAppText       = [System.Drawing.Color]::FromArgb(235, 240, 250)
            BtnAppBorder     = [System.Drawing.Color]::FromArgb(56, 61, 80)
            InputBg          = [System.Drawing.Color]::FromArgb(28, 30, 38)
            InputText        = [System.Drawing.Color]::FromArgb(240, 242, 248)
            ToggleBg         = [System.Drawing.Color]::FromArgb(44, 48, 62)
            ToggleText       = [System.Drawing.Color]::FromArgb(230, 235, 245)
        }
        Light = @{
            Bg               = [System.Drawing.Color]::FromArgb(245, 246, 250)
            Card             = [System.Drawing.Color]::FromArgb(255, 255, 255)
            CardBorder       = [System.Drawing.Color]::FromArgb(216, 220, 230)
            Text             = [System.Drawing.Color]::FromArgb(25, 28, 36)
            TextMuted        = [System.Drawing.Color]::FromArgb(100, 105, 121)
            StatusDotOnline  = [System.Drawing.Color]::FromArgb(39, 174, 96)
            StatusDotOffline = [System.Drawing.Color]::FromArgb(189, 195, 199)
            BtnHero          = [System.Drawing.Color]::FromArgb(27, 138, 70)
            BtnHeroText      = [System.Drawing.Color]::White
            BtnMode          = [System.Drawing.Color]::FromArgb(240, 242, 248)
            BtnModeText      = [System.Drawing.Color]::FromArgb(36, 40, 56)
            BtnModeBorder    = [System.Drawing.Color]::FromArgb(212, 216, 230)
            BtnReboot        = [System.Drawing.Color]::FromArgb(253, 238, 239)
            BtnRebootText    = [System.Drawing.Color]::FromArgb(192, 34, 47)
            BtnRebootBorder  = [System.Drawing.Color]::FromArgb(246, 193, 197)
            BtnTool          = [System.Drawing.Color]::FromArgb(238, 243, 252)
            BtnToolText      = [System.Drawing.Color]::FromArgb(27, 79, 155)
            BtnToolBorder    = [System.Drawing.Color]::FromArgb(202, 217, 244)
            BtnApp           = [System.Drawing.Color]::FromArgb(255, 255, 255)
            BtnAppText       = [System.Drawing.Color]::FromArgb(33, 36, 48)
            BtnAppBorder     = [System.Drawing.Color]::FromArgb(216, 220, 230)
            InputBg          = [System.Drawing.Color]::White
            InputText        = [System.Drawing.Color]::FromArgb(25, 28, 36)
            ToggleBg         = [System.Drawing.Color]::FromArgb(232, 235, 245)
            ToggleText       = [System.Drawing.Color]::FromArgb(35, 40, 55)
        }
    }

    # Fonty
    $fontRegular   = New-Object System.Drawing.Font("Segoe UI", [float]9, [System.Drawing.FontStyle]::Regular)
    $fontBold      = New-Object System.Drawing.Font("Segoe UI", [float]9, [System.Drawing.FontStyle]::Bold)
    $fontHero      = New-Object System.Drawing.Font("Segoe UI", [float]10.5, [System.Drawing.FontStyle]::Bold)
    $fontTitle     = New-Object System.Drawing.Font("Segoe UI", [float]9.5, [System.Drawing.FontStyle]::Bold)
    $fontSection   = New-Object System.Drawing.Font("Segoe UI", [float]8.2, [System.Drawing.FontStyle]::Bold)
    $fontSmall     = New-Object System.Drawing.Font("Segoe UI", [float]8.2, [System.Drawing.FontStyle]::Regular)
    $fontDot       = New-Object System.Drawing.Font("Segoe UI", [float]11, [System.Drawing.FontStyle]::Bold)

    # --- OKNO FORMULARZA ---

    $form = New-Object System.Windows.Forms.Form
    $form.Text = "scrcpy Manager"
    $form.ClientSize = New-Object System.Drawing.Size(404, 668)
    $form.StartPosition = "CenterScreen"
    $form.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::FixedDialog
    $form.MaximizeBox = $false

    # 1. KARTA STATUSU I URZĄDZENIA (NA GÓRZE)
    $pnlStatus = New-Object System.Windows.Forms.Panel
    $pnlStatus.Location = New-Object System.Drawing.Point(16, 12)
    $pnlStatus.Size = New-Object System.Drawing.Size(372, 60)
    $form.Controls.Add($pnlStatus)

    $pnlStatus.Add_Paint({
        param($s, $e)
        $c = if ($script:isDarkMode) { $themeColors.Dark } else { $themeColors.Light }
        $pen = New-Object System.Drawing.Pen($c.CardBorder, 1)
        $e.Graphics.DrawRectangle($pen, 0, 0, $s.Width - 1, $s.Height - 1)
        $pen.Dispose()
    })

    $lblStatusDot = New-Object System.Windows.Forms.Label
    $lblStatusDot.Text = "●"
    $lblStatusDot.Font = $fontDot
    $lblStatusDot.Location = New-Object System.Drawing.Point(10, 8)
    $lblStatusDot.Size = New-Object System.Drawing.Size(18, 22)
    $pnlStatus.Controls.Add($lblStatusDot)

    $lblDeviceTitle = New-Object System.Windows.Forms.Label
    $lblDeviceTitle.Text = "Wyszukiwanie urządzenia..."
    $lblDeviceTitle.Font = $fontTitle
    $lblDeviceTitle.Location = New-Object System.Drawing.Point(28, 9)
    $lblDeviceTitle.Size = New-Object System.Drawing.Size(225, 20)
    $pnlStatus.Controls.Add($lblDeviceTitle)

    $lblStatusDetail = New-Object System.Windows.Forms.Label
    $lblStatusDetail.Text = "Inicjalizacja..."
    $lblStatusDetail.Font = $fontSmall
    $lblStatusDetail.Location = New-Object System.Drawing.Point(28, 32)
    $lblStatusDetail.Size = New-Object System.Drawing.Size(225, 20)
    $pnlStatus.Controls.Add($lblStatusDetail)

    # Przełączniki motywu i języka w karcie nagłówka
    $btnTheme = New-Object System.Windows.Forms.Button
    $btnTheme.Location = New-Object System.Drawing.Point(260, 8)
    $btnTheme.Size = New-Object System.Drawing.Size(62, 24)
    $btnTheme.Font = $fontSmall
    $btnTheme.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $btnTheme.FlatAppearance.BorderSize = 1
    $pnlStatus.Controls.Add($btnTheme)

    $btnLang = New-Object System.Windows.Forms.Button
    $btnLang.Location = New-Object System.Drawing.Point(326, 8)
    $btnLang.Size = New-Object System.Drawing.Size(38, 24)
    $btnLang.Font = $fontSmall
    $btnLang.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $btnLang.FlatAppearance.BorderSize = 1
    $pnlStatus.Controls.Add($btnLang)

    # 2. GŁÓWNA AKCJA: URUCHOM SCRCPY
    $btnScrcpy = New-Object System.Windows.Forms.Button
    $btnScrcpy.Location = New-Object System.Drawing.Point(16, 82)
    $btnScrcpy.Size = New-Object System.Drawing.Size(372, 40)
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

        $title = if ($script:deviceModel) { "$($script:deviceModel) (scrcpy)" } else { "Android (scrcpy)" }
        $audioArg = if ($chkAudio.Checked) { "" } else { "--no-audio" }
        $fsArg = if ($chkFullScreen.Checked) { "-f" } else { "" }

        $argsToRun = "-S -w -K -M"
        if ($fsArg) { $argsToRun += " $fsArg" }
        if ($audioArg) { $argsToRun += " $audioArg" }
        $argsToRun += " --window-title=`"$title`""

        try {
            $proc = Start-Process "scrcpy" -ArgumentList $argsToRun -PassThru -ErrorAction Stop
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

    $chkFullScreen = New-Object System.Windows.Forms.CheckBox
    $chkFullScreen.Checked = $false
    $chkFullScreen.AutoSize = $true
    $chkFullScreen.Font = $fontSmall
    $chkFullScreen.Location = New-Object System.Drawing.Point(18, 126)
    $form.Controls.Add($chkFullScreen)

    # 3. OBRAZ, DŹWIĘK I STEROWANIE
    $lblSectionOptions = New-Object System.Windows.Forms.Label
    $lblSectionOptions.Location = New-Object System.Drawing.Point(16, 150)
    $lblSectionOptions.Size = New-Object System.Drawing.Size(372, 16)
    $lblSectionOptions.Font = $fontSection
    $form.Controls.Add($lblSectionOptions)

    $lblRes = New-Object System.Windows.Forms.Label
    $lblRes.Location = New-Object System.Drawing.Point(16, 170)
    $lblRes.AutoSize = $true
    $lblRes.Font = $fontSmall
    $form.Controls.Add($lblRes)

    $cmbRes = New-Object System.Windows.Forms.ComboBox
    $cmbRes.DropDownStyle = [System.Windows.Forms.ComboBoxStyle]::DropDownList
    $cmbRes.DrawMode = [System.Windows.Forms.DrawMode]::OwnerDrawFixed
    $cmbRes.ItemHeight = 22
    $cmbRes.Location = New-Object System.Drawing.Point(16, 188)
    $cmbRes.Size = New-Object System.Drawing.Size(372, 26)
    $cmbRes.Font = $fontRegular

    $initialNames = $i18n[$script:currentLang].ResNames
    foreach ($rn in $initialNames) { $cmbRes.Items.Add($rn) | Out-Null }
    $cmbRes.SelectedIndex = 0

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

    $chkAudio = New-Object System.Windows.Forms.CheckBox
    $chkAudio.Checked = $true
    $chkAudio.AutoSize = $true
    $chkAudio.Font = $fontSmall
    $chkAudio.Location = New-Object System.Drawing.Point(18, 220)
    $form.Controls.Add($chkAudio)

    $chkAutoTaskbar = New-Object System.Windows.Forms.CheckBox
    $chkAutoTaskbar.Checked = $true
    $chkAutoTaskbar.AutoSize = $true
    $chkAutoTaskbar.Font = $fontSmall
    $chkAutoTaskbar.Location = New-Object System.Drawing.Point(205, 220)
    $form.Controls.Add($chkAutoTaskbar)

    # 3 pigułki narzędziowe
    $btnWifi = New-Object System.Windows.Forms.Button
    $btnWifi.Location = New-Object System.Drawing.Point(16, 244)
    $btnWifi.Size = New-Object System.Drawing.Size(118, 28)
    $btnWifi.Font = $fontSmall
    $btnWifi.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $btnWifi.FlatAppearance.BorderSize = 1
    $btnWifi.Add_Click({ Switch-ToWirelessAdb })
    $form.Controls.Add($btnWifi)

    $btnKeyFix = New-Object System.Windows.Forms.Button
    $btnKeyFix.Location = New-Object System.Drawing.Point(143, 244)
    $btnKeyFix.Size = New-Object System.Drawing.Size(118, 28)
    $btnKeyFix.Font = $fontSmall
    $btnKeyFix.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $btnKeyFix.FlatAppearance.BorderSize = 1
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

    $btnClipFix = New-Object System.Windows.Forms.Button
    $btnClipFix.Location = New-Object System.Drawing.Point(270, 244)
    $btnClipFix.Size = New-Object System.Drawing.Size(118, 28)
    $btnClipFix.Font = $fontSmall
    $btnClipFix.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $btnClipFix.FlatAppearance.BorderSize = 1
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

    # 4. URUCHAMIANIE APLIKACJI W OKNACH
    $lblSectionApps = New-Object System.Windows.Forms.Label
    $lblSectionApps.Location = New-Object System.Drawing.Point(16, 282)
    $lblSectionApps.Size = New-Object System.Drawing.Size(372, 16)
    $lblSectionApps.Font = $fontSection
    $form.Controls.Add($lblSectionApps)

    $lblAppsSubtitle = New-Object System.Windows.Forms.Label
    $lblAppsSubtitle.Location = New-Object System.Drawing.Point(16, 300)
    $lblAppsSubtitle.Size = New-Object System.Drawing.Size(372, 16)
    $lblAppsSubtitle.Font = $fontSmall
    $form.Controls.Add($lblAppsSubtitle)

    $colWidth = 180
    $btnHeight = 30
    $rowPitch = 36
    $colPitch = 192
    $startX = 16
    $startY = 320

    $createdAppButtons = New-Object 'System.Collections.Generic.List[System.Windows.Forms.Button]'

    for ($i = 0; $i -lt $appButtons.Count; $i++) {
        $app = $appButtons[$i]
        $row = [int][math]::Floor($i / 2)
        $col = [int]($i % 2)

        $posX = [int]($startX + $col * $colPitch)
        $posY = [int]($startY + $row * $rowPitch)

        $btn = New-Object System.Windows.Forms.Button
        $btn.Text = $app.Text
        $btn.Font = $fontSmall
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

        $form.Controls.Add($btn)
        $createdAppButtons.Add($btn)
    }

    # Własny pakiet (bezpośrednio pod siatką aplikacji)
    $lblCustom = New-Object System.Windows.Forms.Label
    $lblCustom.Location = New-Object System.Drawing.Point(16, 502)
    $lblCustom.Size = New-Object System.Drawing.Size(372, 16)
    $lblCustom.Font = $fontSmall
    $form.Controls.Add($lblCustom)

    $txtCustom = New-Object System.Windows.Forms.TextBox
    $txtCustom.Location = New-Object System.Drawing.Point(16, 520)
    $txtCustom.Size = New-Object System.Drawing.Size(284, 25)
    $txtCustom.BorderStyle = [System.Windows.Forms.BorderStyle]::FixedSingle
    $txtCustom.Font = $fontRegular
    $form.Controls.Add($txtCustom)

    $btnCustom = New-Object System.Windows.Forms.Button
    $btnCustom.Location = New-Object System.Drawing.Point(306, 519)
    $btnCustom.Size = New-Object System.Drawing.Size(82, 26)
    $btnCustom.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $btnCustom.FlatAppearance.BorderSize = 1
    $btnCustom.Font = $fontSection
    $btnCustom.Add_Click({
        $pkg = $txtCustom.Text.Trim()
        $t = $i18n[$script:currentLang]
        if ([string]::IsNullOrWhiteSpace($pkg)) {
            [System.Windows.Forms.MessageBox]::Show($t.MsgPkgEmpty, "scrcpy", [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Warning)
            return
        }
        if ($chkAutoTaskbar.Checked) { Start-Taskbar }
        Start-ScrcpyApp -PackageName $pkg -WindowTitle $pkg -DisplaySize $script:appDisplaySize
    })
    $form.Controls.Add($btnCustom)

    # 5. OPERACJE NA URZĄDZENIU
    $lblSectionDevice = New-Object System.Windows.Forms.Label
    $lblSectionDevice.Location = New-Object System.Drawing.Point(16, 556)
    $lblSectionDevice.Size = New-Object System.Drawing.Size(372, 16)
    $lblSectionDevice.Font = $fontSection
    $form.Controls.Add($lblSectionDevice)

    $btnDesktop = New-Object System.Windows.Forms.Button
    $btnDesktop.Location = New-Object System.Drawing.Point(16, 576)
    $btnDesktop.Size = New-Object System.Drawing.Size(180, 32)
    $btnDesktop.Font = $fontSection
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
    $btnNormal.Location = New-Object System.Drawing.Point(208, 576)
    $btnNormal.Size = New-Object System.Drawing.Size(180, 32)
    $btnNormal.Font = $fontSection
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
            $t.RestoreDefault,
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Information
        )
    })
    $form.Controls.Add($btnNormal)

    # Przycisk restartu telefonu (zabezpieczony oknem potwierdzenia)
    $btnReboot = New-Object System.Windows.Forms.Button
    $btnReboot.Location = New-Object System.Drawing.Point(16, 616)
    $btnReboot.Size = New-Object System.Drawing.Size(372, 30)
    $btnReboot.Font = $fontSection
    $btnReboot.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $btnReboot.FlatAppearance.BorderSize = 1
    $btnReboot.Add_Click({ Restart-DeviceWithConfirmation })
    $form.Controls.Add($btnReboot)

    # --- FUNKCJE STYLIZACJI MOTYWU I JĘZYKA ---

    function Apply-Theme {
        $c = if ($script:isDarkMode) { $themeColors.Dark } else { $themeColors.Light }

        $form.BackColor = $c.Bg
        $form.ForeColor = $c.Text

        $pnlStatus.BackColor = $c.Card
        $lblDeviceTitle.ForeColor = $c.Text
        $lblStatusDetail.ForeColor = $c.TextMuted

        $btnTheme.BackColor = $c.ToggleBg
        $btnTheme.ForeColor = $c.ToggleText
        $btnTheme.FlatAppearance.BorderColor = $c.CardBorder

        $btnLang.BackColor = $c.ToggleBg
        $btnLang.ForeColor = $c.ToggleText
        $btnLang.FlatAppearance.BorderColor = $c.CardBorder

        $btnScrcpy.BackColor = $c.BtnHero
        $btnScrcpy.ForeColor = $c.BtnHeroText

        $chkFullScreen.ForeColor = $c.TextMuted
        $lblSectionOptions.ForeColor = $c.TextMuted
        $lblRes.ForeColor = $c.TextMuted
        $chkAudio.ForeColor = $c.Text
        $chkAutoTaskbar.ForeColor = $c.Text

        $cmbRes.BackColor = $c.Card
        $cmbRes.ForeColor = $c.Text

        $btnWifi.BackColor = $c.BtnTool
        $btnWifi.ForeColor = $c.BtnToolText
        $btnWifi.FlatAppearance.BorderColor = $c.BtnToolBorder

        $btnKeyFix.BackColor = $c.BtnTool
        $btnKeyFix.ForeColor = $c.BtnToolText
        $btnKeyFix.FlatAppearance.BorderColor = $c.BtnToolBorder

        $btnClipFix.BackColor = $c.BtnTool
        $btnClipFix.ForeColor = $c.BtnToolText
        $btnClipFix.FlatAppearance.BorderColor = $c.BtnToolBorder

        $lblSectionApps.ForeColor = $c.TextMuted
        $lblAppsSubtitle.ForeColor = $c.TextMuted

        foreach ($btn in $createdAppButtons) {
            $btn.BackColor = $c.BtnApp
            $btn.ForeColor = $c.BtnAppText
            $btn.FlatAppearance.BorderColor = $c.BtnAppBorder
        }

        $lblCustom.ForeColor = $c.TextMuted
        $txtCustom.BackColor = $c.InputBg
        $txtCustom.ForeColor = $c.InputText

        $btnCustom.BackColor = $c.BtnApp
        $btnCustom.ForeColor = $c.BtnAppText
        $btnCustom.FlatAppearance.BorderColor = $c.BtnAppBorder

        $lblSectionDevice.ForeColor = $c.TextMuted

        $btnDesktop.BackColor = $c.BtnMode
        $btnDesktop.ForeColor = $c.BtnModeText
        $btnDesktop.FlatAppearance.BorderColor = $c.BtnModeBorder

        $btnNormal.BackColor = $c.BtnMode
        $btnNormal.ForeColor = $c.BtnModeText
        $btnNormal.FlatAppearance.BorderColor = $c.BtnModeBorder

        $btnReboot.BackColor = $c.BtnReboot
        $btnReboot.ForeColor = $c.BtnRebootText
        $btnReboot.FlatAppearance.BorderColor = $c.BtnRebootBorder

        Update-StatusDisplay
        $pnlStatus.Invalidate()
        $cmbRes.Invalidate()
        $form.Invalidate($true)
    }

    function Apply-Language {
        $t = $i18n[$script:currentLang]

        $btnTheme.Text = if ($script:isDarkMode) { $t.ThemeDark } else { $t.ThemeLight }
        $btnLang.Text = $t.LangSwitch

        $btnScrcpy.Text = $t.LaunchHero
        $chkFullScreen.Text = $t.FullScreenOpt

        $lblSectionOptions.Text = $t.SectionOptions
        $lblRes.Text = $t.ResLabel
        $chkAudio.Text = $t.AudioPass
        $chkAutoTaskbar.Text = $t.AutoTaskbar

        $btnWifi.Text = $t.WifiBtn
        $btnKeyFix.Text = $t.KeyBtn
        $btnClipFix.Text = $t.ClipBtn

        $lblSectionApps.Text = $t.SectionApps
        $lblAppsSubtitle.Text = $t.AppsSubtitle

        $lblCustom.Text = $t.CustomLabel
        $btnCustom.Text = $t.CustomBtn

        $lblSectionDevice.Text = $t.SectionDevice
        $btnDesktop.Text = $t.DesktopMode
        $btnNormal.Text = $t.RestoreDefault
        $btnReboot.Text = $t.RebootBtn

        # Odświeżenie elementów w combobox z zachowaniem wybranego indeksu
        $currIdx = $cmbRes.SelectedIndex
        if ($currIdx -lt 0) { $currIdx = 0 }
        $cmbRes.Items.Clear()
        foreach ($rn in $t.ResNames) {
            $cmbRes.Items.Add($rn) | Out-Null
        }
        $cmbRes.SelectedIndex = [math]::Min($currIdx, $cmbRes.Items.Count - 1)

        Update-StatusDisplay
    }

    function Update-StatusDisplay {
        $t = $i18n[$script:currentLang]
        $c = if ($script:isDarkMode) { $themeColors.Dark } else { $themeColors.Light }
        if (Test-AdbDeviceSilent) {
            $lblStatusDot.ForeColor = $c.StatusDotOnline
            $connType = if ($script:isWifiConnected) { $t.StatusConnectedWifi } else { $t.StatusConnectedUsb }
            $devName = if ($script:deviceModel) { $script:deviceModel } else { "Android" }
            $lblDeviceTitle.Text = "$devName — $connType"
            $bat = Get-DeviceBatteryStatus
            $lblStatusDetail.Text = "$($t.BatteryLabel) $bat  |  $($t.StatusActive)"
        }
        else {
            $lblStatusDot.ForeColor = $c.StatusDotOffline
            $lblDeviceTitle.Text = $t.StatusNoPhone
            $lblStatusDetail.Text = $t.StatusCheckConn
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
            Update-StatusDisplay
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
    $errMsg = "Wystąpił błąd podczas uruchamiania scrcpy Manager:`n`n$($_.Exception.ToString())"
    Set-Content -Path $errLog -Value $errMsg -Encoding UTF8 -ErrorAction SilentlyContinue

    [System.Windows.Forms.MessageBox]::Show(
        $errMsg,
        "Błąd scrcpy Manager",
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Error
    )
}