try {
    Add-Type -AssemblyName System.Windows.Forms
    Add-Type -AssemblyName System.Drawing

    if (-not ("WinFormsCueBanner" -as [type])) {
        Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;

public static class WinFormsCueBanner {
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);
}
"@
    }

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
            AppsSubtitle        = "Uruchom w oknie lub kliknij [Edytuj], by dostosować listę"
            AppsEdit            = "Edytuj"
            AppsEditTooltip     = "Dostosuj listę: dodawaj aplikacje z telefonu, zmieniaj kolejność i wczytuj popularne szablony"
            AppsAddPrompt       = "+ Dodaj aplikacje z telefonu (kliknij tutaj lub [Edytuj])"
            CustomLabel         = "Inny pakiet Androida (np. com.spotify.music):"
            CustomPlaceholder   = "Szukaj pakietu (np. spotify, maps)..."
            CustomBtn           = "Uruchom"
            CustomAddBtn        = "+ Dodaj"
            CustomAddTooltip    = "Dodaj wpisany/wybrany pakiet jako stały kafelek na liście aplikacji"
            AppsRemoveTooltip   = "Usuń kafelek '{0}' z listy"
            MsgConfirmRemoveApp = "Czy na pewno chcesz usunąć kafelek '{0}' z listy aplikacji?"
            AddAppDialogTitle   = "Dodaj kafelek aplikacji"
            AddAppDialogLabel   = "Podaj nazwę dla nowego kafelka:"
            AddAppDialogAdd     = "+ Dodaj kafelek"
            MsgAppAlreadyExists = "Pakiet '{0}' znajduje się już na liście pod nazwą '{1}'!"

            AppsEditorTitle               = "Edytuj aplikacje w oknach"
            AppsEditorIntro               = "Twój układ przycisków jest zapisywany osobno dla użytkownika Windows."
            AppsEditorPhoneList           = "Wybierz aplikację pobraną z telefonu"
            AppsEditorSearchPlaceholder   = "Szukaj pakietu (np. spotify, vivaldi, messenger)..."
            AppsEditorNoMatches           = "Brak pasujących pakietów"
            AppsEditorRefresh             = "Odśwież"
            AppsEditorName                = "Nazwa"
            AppsEditorPackage             = "Pakiet Androida"
            AppsEditorUhid                = "Klawiatura"
            AppsEditorClicks              = "Wszystkie kliknięcia"
            AppsEditorAdd                 = "+ Dodaj"
            AppsEditorRemove              = "Usuń"
            AppsEditorUp                  = "W górę"
            AppsEditorDown                = "W dół"
            AppsEditorPopular             = "★ Popularne aplikacje"
            AppsEditorSave                = "Zapisz"
            AppsEditorCancel              = "Anuluj"
            MsgAppsInvalid                = "Każdy wpis musi mieć nazwę i poprawny pakiet Androida (np. com.spotify.music)."
            MsgAppsEmpty                  = "Lista musi zawierać co najmniej jedną aplikację."
            MsgAppsSaveError              = "Nie udało się zapisać układu użytkownika:`n{0}"
            MsgAppsPhoneError             = "Nie udało się pobrać aplikacji z telefonu. Sprawdź połączenie ADB."
            MsgLoadPopular                = "Czy chcesz dodać zestaw 10 popularnych aplikacji (YouTube, Spotify, Chrome, WhatsApp, Messenger, Mapy itp.) do swojej listy?"
            MsgEmptyAppsPrompt            = "Twoja lista aplikacji jest pusta. Czy chcesz wczytać zestaw 10 popularnych aplikacji (YouTube, Spotify, Chrome itp.)?"

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
            AppsSubtitle        = "Run in window or click [Edit] to customize your list"
            AppsEdit            = "Edit"
            AppsEditTooltip     = "Customize list: add apps from phone, reorder, or load popular presets"
            AppsAddPrompt       = "+ Add apps from phone (click here or [Edit])"
            CustomLabel         = "Custom Android package (e.g. com.spotify.music):"
            CustomPlaceholder   = "Search package (e.g. spotify, maps)..."
            CustomBtn           = "Launch"
            CustomAddBtn        = "+ Add"
            CustomAddTooltip    = "Add the typed/selected package as a permanent tile in the app list"
            AppsRemoveTooltip   = "Remove '{0}' tile from the list"
            MsgConfirmRemoveApp = "Are you sure you want to remove the '{0}' tile from the application list?"
            AddAppDialogTitle   = "Add Application Tile"
            AddAppDialogLabel   = "Enter a name for the new tile:"
            AddAppDialogAdd     = "+ Add Tile"
            MsgAppAlreadyExists = "Package '{0}' is already in the list as '{1}'!"

            AppsEditorTitle               = "Edit windowed applications"
            AppsEditorIntro               = "Your button layout is stored separately for each Windows user."
            AppsEditorPhoneList           = "Select an application loaded from the phone"
            AppsEditorSearchPlaceholder   = "Search package (e.g. spotify, vivaldi, messenger)..."
            AppsEditorNoMatches           = "No matching packages"
            AppsEditorRefresh             = "Refresh"
            AppsEditorName                = "Name"
            AppsEditorPackage             = "Android package"
            AppsEditorUhid                = "Keyboard"
            AppsEditorClicks              = "All clicks"
            AppsEditorAdd                 = "+ Add"
            AppsEditorRemove              = "Remove"
            AppsEditorUp                  = "Move up"
            AppsEditorDown                = "Move down"
            AppsEditorPopular             = "★ Popular Apps"
            AppsEditorSave                = "Save"
            AppsEditorCancel              = "Cancel"
            MsgAppsInvalid                = "Every entry needs a name and a valid Android package (e.g. com.spotify.music)."
            MsgAppsEmpty                  = "The list must contain at least one application."
            MsgAppsSaveError              = "Could not save the user layout:`n{0}"
            MsgAppsPhoneError             = "Could not load applications from the phone. Check the ADB connection."
            MsgLoadPopular                = "Do you want to add a set of 10 popular apps (YouTube, Spotify, Chrome, WhatsApp, Messenger, Maps, etc.) to your list?"
            MsgEmptyAppsPrompt            = "Your app list is empty. Would you like to load a set of 10 popular apps (YouTube, Spotify, Chrome, etc.)?"

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
                "Phone Default"
            )
        }
    }

    # Wartości techniczne rozdzielczości sesji RDP
    $resValues = @(
        "1920x1080/160",
        "2560x1440/160",
        "2560x1440/140",
        "3840x2160/240",
        "AUTO"
    )

    # --- FUNKCJE POMOCNICZE ADB ---

    function Test-AdbDeviceSilent {
        $adbOut = adb devices 2>$null
        return [bool]($adbOut | Select-String -Pattern "\bdevice\b(?!\s*unauthorized)")
    }

    function Test-AdbDevice {
        $t = $i18n[$script:currentLang]
        if (-not (Test-AdbDeviceSilent)) {
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
            $script:deviceModel = "Android"
            $script:deviceManufacturer = ""
            $script:isWifiConnected = $false
            return
        }

        try {
            $model = (adb shell getprop ro.product.model 2>$null).Trim()
            $mfg = (adb shell getprop ro.product.manufacturer 2>$null).Trim()
            if ($model) { $script:deviceModel = $model }
            if ($mfg) { $script:deviceManufacturer = $mfg }

            $devs = adb devices 2>$null
            $script:isWifiConnected = [bool]($devs | Select-String -Pattern "\b\d{1,3}(\.\d{1,3}){3}:\d+\b\s+device")
        }
        catch {}
    }

    function Get-DeviceBatteryStatus {
        if (-not (Test-AdbDeviceSilent)) { return "---" }
        try {
            $dump = adb shell dumpsys battery 2>$null
            $level = ($dump | Select-String "level:\s*(\d+)" | ForEach-Object { $_.Matches.Groups[1].Value })
            $status = ($dump | Select-String "status:\s*(\d+)" | ForEach-Object { $_.Matches.Groups[1].Value })

            $t = $i18n[$script:currentLang]
            $isCharging = ($status -eq "2" -or $status -eq "5")
            $chg = if ($isCharging) { $t.ChargingStr } else { "" }

            if ($level) { return "$level%$chg" }
            return "---"
        }
        catch {
            return "---"
        }
    }

    function Initialize-ScreenTimeoutSettings {
        if (-not (Test-AdbDeviceSilent)) { return }
        try {
            $currentTimeout = (adb shell settings get system screen_off_timeout 2>$null).Trim()
            if ($currentTimeout -match "^\d+$") {
                $val = [int]$currentTimeout
                if ($val -ne 2147483647 -and $val -gt 0) {
                    $script:originalTimeout = $val
                    Set-Content -Path $stateFile -Value $val -Encoding UTF8 -ErrorAction SilentlyContinue
                }
            }
        }
        catch {}
    }

    function Invoke-AdbUnlock {
        try {
            adb shell input keyevent 224 2>$null
            Start-Sleep -Milliseconds 150
            adb shell wm dismiss-keyguard 2>$null

            if ($adbPin) {
                Start-Sleep -Milliseconds 200
                adb shell input text "$adbPin" 2>$null
                Start-Sleep -Milliseconds 100
                adb shell input keyevent 66 2>$null
            }
        }
        catch {}
    }

    function Enable-ScreenLockPrevention {
        if (-not (Test-AdbDeviceSilent)) { return }
        adb shell settings put system screen_off_timeout 2147483647 2>$null
        adb shell svc power stayon true 2>$null
        Invoke-AdbUnlock
    }

    function Restore-ScreenLockSettings {
        if (-not (Test-AdbDeviceSilent)) { return }
        $timeoutVal = if ($script:originalTimeout -gt 0) { $script:originalTimeout } else { 30000 }
        adb shell settings put system screen_off_timeout $timeoutVal 2>$null
        adb shell svc power stayon false 2>$null
        if (Test-Path $stateFile) {
            Remove-Item $stateFile -Force -ErrorAction SilentlyContinue
        }
    }

    function Start-Taskbar {
        if (-not (Test-AdbDeviceSilent)) { return }
        adb shell am start -n com.farmerbb.taskbar/.activity.MainActivity 2>$null | Out-Null
    }

    function Stop-Taskbar {
        if (-not (Test-AdbDeviceSilent)) { return }
        adb shell am force-stop com.farmerbb.taskbar 2>$null | Out-Null
    }

    function Optimize-RdcClipboard {
        if (-not (Test-AdbDeviceSilent)) { return }
        adb shell settings put system clipboard_format_priority 1 2>$null
    }

    function Register-ScrcpyProcess {
        param($Process)
        if ($Process -and -not $Process.HasExited) {
            $script:launchedProcesses.Add($Process)
        }
    }

    function Clean-ExitedProcesses {
        $active = New-Object 'System.Collections.Generic.List[System.Diagnostics.Process]'
        foreach ($p in $script:launchedProcesses) {
            if ($p -and -not $p.HasExited) {
                $active.Add($p)
            }
        }
        $script:launchedProcesses = $active
    }

    function Switch-ToWirelessAdb {
        $t = $i18n[$script:currentLang]
        if (-not (Test-AdbDeviceSilent)) {
            [System.Windows.Forms.MessageBox]::Show(
                $t.MsgNoDevice,
                $t.WifiBtn,
                [System.Windows.Forms.MessageBoxButtons]::OK,
                [System.Windows.Forms.MessageBoxIcon]::Warning
            )
            return
        }

        $ip = $null
        try {
            $ipRaw = adb shell ip route 2>$null | Select-String -Pattern "src\s+(\d{1,3}(\.\d{1,3}){3})"
            if ($ipRaw -match "src\s+(\d{1,3}(\.\d{1,3}){3})") {
                $ip = $Matches[1]
            }
            if (-not $ip) {
                $wlanRaw = adb shell ifconfig wlan0 2>$null | Select-String -Pattern "inet\s+addr:(\d{1,3}(\.\d{1,3}){3})"
                if ($wlanRaw -match "inet\s+addr:(\d{1,3}(\.\d{1,3}){3})") {
                    $ip = $Matches[1]
                }
            }
        }
        catch {}

        if (-not $ip -or $ip -eq "127.0.0.1") {
            [System.Windows.Forms.MessageBox]::Show(
                $t.MsgWifiNoIp,
                $t.WifiBtn,
                [System.Windows.Forms.MessageBoxButtons]::OK,
                [System.Windows.Forms.MessageBoxIcon]::Warning
            )
            return
        }

        adb tcpip 5555 2>$null | Out-Null
        Start-Sleep -Seconds 1
        $connectOut = adb connect "$($ip):5555" 2>$null

        if ($connectOut -match "connected to") {
            $script:isWifiConnected = $true
            Update-DeviceInfo
            Update-StatusDisplay
            [System.Windows.Forms.MessageBox]::Show(
                ($t.MsgWifiDone -f $ip),
                $t.WifiBtn,
                [System.Windows.Forms.MessageBoxButtons]::OK,
                [System.Windows.Forms.MessageBoxIcon]::Information
            )
        }
        else {
            [System.Windows.Forms.MessageBox]::Show(
                "ADB connection to $ip:5555 failed.`n$connectOut",
                $t.WifiBtn,
                [System.Windows.Forms.MessageBoxButtons]::OK,
                [System.Windows.Forms.MessageBoxIcon]::Error
            )
        }
    }

    function Restart-DeviceWithConfirmation {
        $t = $i18n[$script:currentLang]
        if (-not (Test-AdbDevice)) { return }

        $res = [System.Windows.Forms.MessageBox]::Show(
            $t.MsgRestartConfirm,
            $t.TitleRestartConfirm,
            [System.Windows.Forms.MessageBoxButtons]::YesNo,
            [System.Windows.Forms.MessageBoxIcon]::Question
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
            [string]$PackageName,
            [string]$WindowTitle = "",
            [string]$DisplaySize = "1920x1080/160",
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

    $userConfigRoot = Join-Path $env:APPDATA "scrcpy-manager"
    $defaultAppsConfigFile = Join-Path $scriptDir "apps.json"
    $appsConfigFile = Join-Path $userConfigRoot "apps.json"
    $appsSourceFile = if (Test-Path $appsConfigFile) { $appsConfigFile } else { $defaultAppsConfigFile }
    $appButtons = @()

    if (Test-Path $appsSourceFile) {
        try {
            $rawJson = Get-Content $appsSourceFile -Raw -Encoding UTF8 -ErrorAction Stop
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

    $script:defaultPopularApps = @(
        @{ Text = "YouTube";                 Package = "com.google.android.youtube";            Flags = @() },
        @{ Text = "Spotify";                 Package = "com.spotify.music";                     Flags = @() },
        @{ Text = "Chrome";                  Package = "com.android.chrome";                    Flags = @("-ForwardAllClicks") },
        @{ Text = "WhatsApp";                Package = "com.whatsapp";                          Flags = @() },
        @{ Text = "Messenger";               Package = "com.facebook.orca";                     Flags = @() },
        @{ Text = "Gmail";                   Package = "com.google.android.gm";                 Flags = @() },
        @{ Text = "Mapy Google";             Package = "com.google.android.apps.maps";          Flags = @() },
        @{ Text = "Wiadomości";              Package = "com.google.android.apps.messaging";     Flags = @() },
        @{ Text = "Ustawienia";              Package = "com.android.settings";                  Flags = @() },
        @{ Text = "Claude";                  Package = "com.anthropic.claude";                  Flags = @() }
    )

    if ($appButtons.Count -eq 0) {
        $appButtons = @($script:defaultPopularApps)
    }

    $loadedAppButtons = @($appButtons)
    $appButtons = New-Object System.Collections.ArrayList
    foreach ($app in $loadedAppButtons) { [void]$appButtons.Add($app) }

    $script:cachedInstalledPackages = New-Object 'System.Collections.Generic.List[string]'

    function Get-InstalledAndroidPackages {
        if (-not (Test-AdbDeviceSilent)) { return @() }

        $packages = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
        try {
            $launcherDump = adb shell cmd package query-activities --brief -a android.intent.action.MAIN -c android.intent.category.LAUNCHER 2>$null
            foreach ($line in $launcherDump) {
                if ([string]$line -match '^\s*([A-Za-z0-9_]+(?:\.[A-Za-z0-9_]+)+)/') {
                    [void]$packages.Add($Matches[1])
                }
            }

            if ($packages.Count -eq 0) {
                foreach ($line in (adb shell pm list packages -3 2>$null)) {
                    if ([string]$line -match '^package:(.+)$') { [void]$packages.Add($Matches[1].Trim()) }
                }
            }
        }
        catch {}

        $sorted = @($packages | Sort-Object)
        if ($sorted.Count -gt 0) {
            $script:cachedInstalledPackages.Clear()
            foreach ($pkg in $sorted) {
                [void]$script:cachedInstalledPackages.Add($pkg)
            }
        }
        return $sorted
    }

    function Ensure-InstalledAndroidPackages {
        if ($script:cachedInstalledPackages.Count -eq 0 -and (Test-AdbDeviceSilent)) {
            [void](Get-InstalledAndroidPackages)
        }
        return $script:cachedInstalledPackages
    }

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
            ToggleChecked    = [System.Drawing.Color]::FromArgb(48, 70, 104)
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
            ToggleChecked    = [System.Drawing.Color]::FromArgb(214, 225, 246)
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

    # --- EDYTOR APLIKACJI W OKNACH ---

    function Show-AppsEditor {
        $t = $i18n[$script:currentLang]
        $c = if ($script:isDarkMode) { $themeColors.Dark } else { $themeColors.Light }

        $editor = New-Object System.Windows.Forms.Form
        $editor.Text = $t.AppsEditorTitle
        $editor.ClientSize = New-Object System.Drawing.Size(760, 506)
        $editor.StartPosition = [System.Windows.Forms.FormStartPosition]::CenterParent
        $editor.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::FixedDialog
        $editor.MaximizeBox = $false
        $editor.MinimizeBox = $false
        $editor.ShowInTaskbar = $false
        if ($form -and $form.Icon) {
            $editor.Icon = $form.Icon
        }
        $editor.Font = $fontRegular
        $editor.BackColor = $c.Bg
        $editor.ForeColor = $c.Text

        $lblIntro = New-Object System.Windows.Forms.Label
        $lblIntro.Text = $t.AppsEditorIntro
        $lblIntro.Location = New-Object System.Drawing.Point(16, 12)
        $lblIntro.Size = New-Object System.Drawing.Size(728, 20)
        $lblIntro.ForeColor = $c.TextMuted
        $editor.Controls.Add($lblIntro)

        $txtSearch = New-Object System.Windows.Forms.TextBox
        $txtSearch.Location = New-Object System.Drawing.Point(16, 36)
        $txtSearch.Size = New-Object System.Drawing.Size(692, 26)
        $txtSearch.Font = $fontRegular
        $txtSearch.BorderStyle = [System.Windows.Forms.BorderStyle]::FixedSingle
        $txtSearch.BackColor = $c.InputBg
        $txtSearch.ForeColor = $c.InputText
        [WinFormsCueBanner]::SendMessage($txtSearch.Handle, 0x1501, [IntPtr]::Zero, $t.AppsEditorSearchPlaceholder) | Out-Null
        $editor.Controls.Add($txtSearch)

        $btnClearSearch = New-Object System.Windows.Forms.Button
        $btnClearSearch.Text = "✕"
        $btnClearSearch.Location = New-Object System.Drawing.Point(714, 35)
        $btnClearSearch.Size = New-Object System.Drawing.Size(30, 28)
        $btnClearSearch.Font = $fontSmall
        $btnClearSearch.Add_Click({
            $txtSearch.Text = ""
            $txtSearch.Focus()
        })
        $editor.Controls.Add($btnClearSearch)

        $allPhonePackages = New-Object 'System.Collections.Generic.List[string]'

        $cmbPhoneApps = New-Object System.Windows.Forms.ComboBox
        $cmbPhoneApps.DropDownStyle = [System.Windows.Forms.ComboBoxStyle]::DropDownList
        $cmbPhoneApps.Location = New-Object System.Drawing.Point(16, 70)
        $cmbPhoneApps.Size = New-Object System.Drawing.Size(500, 28)
        $cmbPhoneApps.DropDownWidth = 520
        $cmbPhoneApps.Font = $fontRegular
        $cmbPhoneApps.BackColor = $c.InputBg
        $cmbPhoneApps.ForeColor = $c.InputText
        [void]$cmbPhoneApps.Items.Add($t.AppsEditorPhoneList)
        $cmbPhoneApps.SelectedIndex = 0
        $editor.Controls.Add($cmbPhoneApps)

        $applyFilter = {
            $query = $txtSearch.Text.Trim()
            $cmbPhoneApps.BeginUpdate()
            try {
                $cmbPhoneApps.Items.Clear()
                if ($allPhonePackages.Count -eq 0) {
                    [void]$cmbPhoneApps.Items.Add($t.AppsEditorPhoneList)
                    $cmbPhoneApps.SelectedIndex = 0
                    return
                }

                $tokens = @($query -split '\s+' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
                $filtered = if ($tokens.Count -eq 0) {
                    @($allPhonePackages)
                } else {
                    @($allPhonePackages | Where-Object {
                        $pkg = $_
                        $allMatch = $true
                        foreach ($tok in $tokens) {
                            if ($pkg -notlike "*$tok*") {
                                $allMatch = $false
                                break
                            }
                        }
                        $allMatch
                    })
                }

                if ($filtered.Count -gt 0) {
                    $headerText = if ($tokens.Count -eq 0) {
                        $t.AppsEditorPhoneList
                    } else {
                        "$($t.AppsEditorPhoneList) ($($filtered.Count))"
                    }
                    [void]$cmbPhoneApps.Items.Add($headerText)
                    foreach ($pkg in $filtered) {
                        [void]$cmbPhoneApps.Items.Add($pkg)
                    }

                    if ($tokens.Count -gt 0) {
                        $cmbPhoneApps.SelectedIndex = 1
                    } else {
                        $cmbPhoneApps.SelectedIndex = 0
                    }
                } else {
                    [void]$cmbPhoneApps.Items.Add($t.AppsEditorNoMatches)
                    $cmbPhoneApps.SelectedIndex = 0
                }
            }
            finally {
                $cmbPhoneApps.EndUpdate()
            }
        }

        $txtSearch.Add_TextChanged({ & $applyFilter })

        $loadPhoneApps = {
            $editor.UseWaitCursor = $true
            try {
                $allPhonePackages.Clear()
                foreach ($package in @(Get-InstalledAndroidPackages)) {
                    [void]$allPhonePackages.Add($package)
                }
                & $applyFilter
                if ($allPhonePackages.Count -eq 0) {
                    [System.Windows.Forms.MessageBox]::Show($t.MsgAppsPhoneError, $t.AppsEditorTitle, [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Warning)
                }
            }
            finally {
                $editor.UseWaitCursor = $false
            }
        }

        $btnRefreshApps = New-Object System.Windows.Forms.Button
        $btnRefreshApps.Text = $t.AppsEditorRefresh
        $btnRefreshApps.Location = New-Object System.Drawing.Point(522, 69)
        $btnRefreshApps.Size = New-Object System.Drawing.Size(104, 30)
        $btnRefreshApps.Add_Click({ & $loadPhoneApps })
        $editor.Controls.Add($btnRefreshApps)

        $grid = New-Object System.Windows.Forms.DataGridView
        $grid.Location = New-Object System.Drawing.Point(16, 108)
        $grid.Size = New-Object System.Drawing.Size(728, 334)
        $grid.AllowUserToAddRows = $false
        $grid.AllowUserToDeleteRows = $false
        $grid.AllowUserToResizeRows = $false
        $grid.RowHeadersVisible = $false
        $grid.MultiSelect = $false
        $grid.SelectionMode = [System.Windows.Forms.DataGridViewSelectionMode]::FullRowSelect
        $grid.AutoSizeRowsMode = [System.Windows.Forms.DataGridViewAutoSizeRowsMode]::None
        $grid.RowTemplate.Height = 28
        $grid.BorderStyle = [System.Windows.Forms.BorderStyle]::FixedSingle
        $grid.BackgroundColor = $c.Card
        $grid.GridColor = $c.CardBorder
        $grid.EnableHeadersVisualStyles = $false
        $grid.ColumnHeadersDefaultCellStyle.BackColor = $c.ToggleBg
        $grid.ColumnHeadersDefaultCellStyle.ForeColor = $c.Text
        $grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = if ($c.ToggleBg) { $c.ToggleBg } else { $grid.ColumnHeadersDefaultCellStyle.BackColor }
        $grid.DefaultCellStyle.BackColor = $c.InputBg
        $grid.DefaultCellStyle.ForeColor = $c.InputText
        $grid.DefaultCellStyle.SelectionBackColor = if ($c.ToggleChecked) { $c.ToggleChecked } else { $c.ToggleBg }
        $grid.DefaultCellStyle.SelectionForeColor = $c.InputText

        $colName = New-Object System.Windows.Forms.DataGridViewTextBoxColumn
        $colName.Name = "AppName"
        $colName.HeaderText = $t.AppsEditorName
        $colName.Width = 180
        [void]$grid.Columns.Add($colName)

        $colPackage = New-Object System.Windows.Forms.DataGridViewTextBoxColumn
        $colPackage.Name = "Package"
        $colPackage.HeaderText = $t.AppsEditorPackage
        $colPackage.Width = 280
        [void]$grid.Columns.Add($colPackage)

        $colUhid = New-Object System.Windows.Forms.DataGridViewCheckBoxColumn
        $colUhid.Name = "UseUhid"
        $colUhid.HeaderText = $t.AppsEditorUhid
        $colUhid.Width = 105
        $colUhid.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
        [void]$grid.Columns.Add($colUhid)

        $colClicks = New-Object System.Windows.Forms.DataGridViewCheckBoxColumn
        $colClicks.Name = "ForwardClicks"
        $colClicks.HeaderText = $t.AppsEditorClicks
        $colClicks.AutoSizeMode = [System.Windows.Forms.DataGridViewAutoSizeColumnMode]::Fill
        $colClicks.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
        [void]$grid.Columns.Add($colClicks)

        foreach ($app in $appButtons) {
            $rowIndex = $grid.Rows.Add(
                $app.Text,
                $app.Package,
                ($app.Flags -contains "-UseUhidKeyboard"),
                ($app.Flags -contains "-ForwardAllClicks")
            )
            $grid.Rows[$rowIndex].Tag = @($app.Flags | Where-Object { $_ -notin @("-UseUhidKeyboard", "-ForwardAllClicks") })
        }
        $editor.Controls.Add($grid)

        $btnAddApp = New-Object System.Windows.Forms.Button
        $btnAddApp.Text = $t.AppsEditorAdd
        $btnAddApp.Location = New-Object System.Drawing.Point(634, 69)
        $btnAddApp.Size = New-Object System.Drawing.Size(110, 30)

        $addSelectedApp = {
            $package = ""
            if ($cmbPhoneApps.SelectedIndex -gt 0) {
                $package = [string]$cmbPhoneApps.SelectedItem
            }
            elseif (-not [string]::IsNullOrWhiteSpace($txtSearch.Text)) {
                $candidate = $txtSearch.Text.Trim()
                if ($candidate -match '^[A-Za-z0-9_]+(\.[A-Za-z0-9_]+)+$') {
                    $package = $candidate
                }
            }

            if ([string]::IsNullOrWhiteSpace($package)) { return }

            $defaultName = ($package -split '\.')[-1]
            $rowIndex = $grid.Rows.Add($defaultName, $package, $false, $false)
            $grid.Rows[$rowIndex].Tag = @()
            $grid.CurrentCell = $grid.Rows[$rowIndex].Cells[0]
            $grid.BeginEdit($true)
        }

        $btnAddApp.Add_Click({ & $addSelectedApp })
        $editor.Controls.Add($btnAddApp)

        $txtSearch.Add_KeyDown({
            param($s, $e)
            if ($e.KeyCode -eq [System.Windows.Forms.Keys]::Enter) {
                $e.SuppressKeyPress = $true
                & $addSelectedApp
            }
            elseif ($e.KeyCode -eq [System.Windows.Forms.Keys]::Down) {
                $e.SuppressKeyPress = $true
                $cmbPhoneApps.Focus()
                if ($cmbPhoneApps.Items.Count -gt 1) {
                    if ($cmbPhoneApps.SelectedIndex -le 0) {
                        $cmbPhoneApps.SelectedIndex = 1
                    }
                    $cmbPhoneApps.DroppedDown = $true
                }
            }
            elseif ($e.KeyCode -eq [System.Windows.Forms.Keys]::Escape) {
                if (-not [string]::IsNullOrWhiteSpace($txtSearch.Text)) {
                    $txtSearch.Text = ""
                    $e.SuppressKeyPress = $true
                }
            }
        })

        $cmbPhoneApps.Add_KeyDown({
            param($s, $e)
            if ($e.KeyCode -eq [System.Windows.Forms.Keys]::Enter) {
                $e.SuppressKeyPress = $true
                & $addSelectedApp
            }
            elseif ($e.KeyCode -eq [System.Windows.Forms.Keys]::Up -and $cmbPhoneApps.SelectedIndex -le 1) {
                $txtSearch.Focus()
            }
        })

        $btnRemoveApp = New-Object System.Windows.Forms.Button
        $btnRemoveApp.Text = $t.AppsEditorRemove
        $btnRemoveApp.Location = New-Object System.Drawing.Point(16, 458)
        $btnRemoveApp.Size = New-Object System.Drawing.Size(90, 32)
        $btnRemoveApp.Add_Click({
            if ($null -ne $grid.CurrentRow) {
                $grid.Rows.RemoveAt($grid.CurrentRow.Index)
            }
        })
        $editor.Controls.Add($btnRemoveApp)

        $moveSelectedRow = {
            param([int]$Offset)
            if ($null -eq $grid.CurrentRow) { return }
            [void]$grid.EndEdit()
            $sourceIndex = $grid.CurrentRow.Index
            $targetIndex = $sourceIndex + $Offset
            if ($targetIndex -lt 0 -or $targetIndex -ge $grid.Rows.Count) { return }

            $sourceValues = @($grid.Rows[$sourceIndex].Cells | ForEach-Object { $_.Value })
            $sourceTag = @($grid.Rows[$sourceIndex].Tag)
            for ($columnIndex = 0; $columnIndex -lt $grid.Columns.Count; $columnIndex++) {
                $grid.Rows[$sourceIndex].Cells[$columnIndex].Value = $grid.Rows[$targetIndex].Cells[$columnIndex].Value
                $grid.Rows[$targetIndex].Cells[$columnIndex].Value = $sourceValues[$columnIndex]
            }
            $grid.Rows[$sourceIndex].Tag = @($grid.Rows[$targetIndex].Tag)
            $grid.Rows[$targetIndex].Tag = $sourceTag
            $grid.CurrentCell = $grid.Rows[$targetIndex].Cells[0]
        }

        $btnMoveUp = New-Object System.Windows.Forms.Button
        $btnMoveUp.Text = $t.AppsEditorUp
        $btnMoveUp.Location = New-Object System.Drawing.Point(112, 458)
        $btnMoveUp.Size = New-Object System.Drawing.Size(90, 32)
        $btnMoveUp.Add_Click({ & $moveSelectedRow -Offset -1 })
        $editor.Controls.Add($btnMoveUp)

        $btnMoveDown = New-Object System.Windows.Forms.Button
        $btnMoveDown.Text = $t.AppsEditorDown
        $btnMoveDown.Location = New-Object System.Drawing.Point(208, 458)
        $btnMoveDown.Size = New-Object System.Drawing.Size(90, 32)
        $btnMoveDown.Add_Click({ & $moveSelectedRow -Offset 1 })
        $editor.Controls.Add($btnMoveDown)

        $btnPopularApps = New-Object System.Windows.Forms.Button
        $btnPopularApps.Text = $t.AppsEditorPopular
        $btnPopularApps.Location = New-Object System.Drawing.Point(308, 458)
        $btnPopularApps.Size = New-Object System.Drawing.Size(150, 32)
        $btnPopularApps.Add_Click({
            $existingPkgs = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
            foreach ($row in $grid.Rows) {
                $p = [string]$row.Cells["Package"].Value
                if (-not [string]::IsNullOrWhiteSpace($p)) { [void]$existingPkgs.Add($p.Trim()) }
            }

            if ($grid.Rows.Count -gt 0) {
                $res = [System.Windows.Forms.MessageBox]::Show(
                    $t.MsgLoadPopular,
                    $t.AppsEditorPopular,
                    [System.Windows.Forms.MessageBoxButtons]::YesNo,
                    [System.Windows.Forms.MessageBoxIcon]::Question
                )
                if ($res -ne [System.Windows.Forms.DialogResult]::Yes) { return }
            }

            $addedCount = 0
            foreach ($app in $script:defaultPopularApps) {
                if (-not $existingPkgs.Contains($app.Package)) {
                    $uhid = ($app.Flags -contains "-UseUhidKeyboard")
                    $clicks = ($app.Flags -contains "-ForwardAllClicks")
                    $ri = $grid.Rows.Add($app.Text, $app.Package, $uhid, $clicks)
                    $grid.Rows[$ri].Tag = @($app.Flags | Where-Object { $_ -notin @("-UseUhidKeyboard", "-ForwardAllClicks") })
                    $addedCount++
                }
            }
            if ($grid.Rows.Count -gt 0) {
                $grid.CurrentCell = $grid.Rows[$grid.Rows.Count - 1].Cells[0]
            }
        })
        $editor.Controls.Add($btnPopularApps)

        $btnCancelEditor = New-Object System.Windows.Forms.Button
        $btnCancelEditor.Text = $t.AppsEditorCancel
        $btnCancelEditor.Location = New-Object System.Drawing.Point(550, 458)
        $btnCancelEditor.Size = New-Object System.Drawing.Size(92, 32)
        $btnCancelEditor.DialogResult = [System.Windows.Forms.DialogResult]::Cancel
        $editor.CancelButton = $btnCancelEditor
        $editor.Controls.Add($btnCancelEditor)

        $btnSaveApps = New-Object System.Windows.Forms.Button
        $btnSaveApps.Text = $t.AppsEditorSave
        $btnSaveApps.Location = New-Object System.Drawing.Point(652, 458)
        $btnSaveApps.Size = New-Object System.Drawing.Size(92, 32)
        $btnSaveApps.Font = $fontBold
        $btnSaveApps.Add_Click({
            [void]$grid.EndEdit()
            if ($grid.Rows.Count -eq 0) {
                [System.Windows.Forms.MessageBox]::Show($t.MsgAppsEmpty, $t.AppsEditorTitle, [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Warning)
                return
            }

            $updatedApps = @()
            foreach ($row in $grid.Rows) {
                $name = ([string]$row.Cells["AppName"].Value).Trim()
                $package = ([string]$row.Cells["Package"].Value).Trim()
                if ([string]::IsNullOrWhiteSpace($name) -or $package -notmatch '^[A-Za-z0-9_]+(\.[A-Za-z0-9_]+)+$') {
                    $grid.CurrentCell = $row.Cells[0]
                    [System.Windows.Forms.MessageBox]::Show($t.MsgAppsInvalid, $t.AppsEditorTitle, [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Warning)
                    return
                }

                $flags = @($row.Tag)
                if ([bool]$row.Cells["UseUhid"].Value) { $flags += "-UseUhidKeyboard" }
                if ([bool]$row.Cells["ForwardClicks"].Value) { $flags += "-ForwardAllClicks" }
                $updatedApps += @{
                    Text = $name
                    Package = $package
                    Flags = @($flags)
                }
            }

            try {
                $jsonApps = @(
                    foreach ($app in $updatedApps) {
                        [pscustomobject]@{
                            name = $app.Text
                            package = $app.Package
                            flags = @($app.Flags)
                        }
                    }
                )
                $json = ConvertTo-Json -InputObject $jsonApps -Depth 4
                if (-not (Test-Path $userConfigRoot)) { [void](New-Item -ItemType Directory -Path $userConfigRoot -Force -ErrorAction Stop) }
                Set-Content -LiteralPath $appsConfigFile -Value $json -Encoding UTF8 -ErrorAction Stop

                $appButtons.Clear()
                foreach ($app in $updatedApps) {
                    [void]$appButtons.Add($app)
                }
                Update-AppButtonGrid
                $editor.DialogResult = [System.Windows.Forms.DialogResult]::OK
                $editor.Close()
            }
            catch {
                [System.Windows.Forms.MessageBox]::Show(($t.MsgAppsSaveError -f $_.Exception.Message), $t.AppsEditorTitle, [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Error)
            }
        })
        $editor.Controls.Add($btnSaveApps)

        foreach ($button in @($btnAddApp, $btnRefreshApps, $btnClearSearch, $btnRemoveApp, $btnMoveUp, $btnMoveDown, $btnPopularApps, $btnCancelEditor, $btnSaveApps)) {
            $button.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
            $button.FlatAppearance.BorderSize = 1
            $button.FlatAppearance.BorderColor = $c.BtnAppBorder
            $button.BackColor = $c.BtnApp
            $button.ForeColor = $c.BtnAppText
        }
        $btnSaveApps.BackColor = $c.BtnHero
        $btnSaveApps.ForeColor = $c.BtnHeroText
        $btnSaveApps.FlatAppearance.BorderColor = $c.BtnHero

        $editor.Add_Shown({
            & $loadPhoneApps
            $txtSearch.Focus()

            if ($grid.Rows.Count -eq 0) {
                $res = [System.Windows.Forms.MessageBox]::Show(
                    $t.MsgEmptyAppsPrompt,
                    $t.AppsEditorTitle,
                    [System.Windows.Forms.MessageBoxButtons]::YesNo,
                    [System.Windows.Forms.MessageBoxIcon]::Question
                )
                if ($res -eq [System.Windows.Forms.DialogResult]::Yes) {
                    $btnPopularApps.PerformClick()
                }
            }
        })
        [void]$editor.ShowDialog($form)
        $editor.Dispose()
    }

    # --- OKNO FORMULARZA ---

    $form = New-Object System.Windows.Forms.Form
    $form.Text = "scrcpy Manager"
    $form.ClientSize = New-Object System.Drawing.Size(404, 668)
    $form.StartPosition = "CenterScreen"
    $form.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::FixedDialog
    $form.MaximizeBox = $false

    $appIconPath = Join-Path $scriptDir "app.ico"
    if (Test-Path $appIconPath) {
        try {
            $form.Icon = New-Object System.Drawing.Icon($appIconPath)
        } catch {}
    }

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
    $lblAppsSubtitle.Location = New-Object System.Drawing.Point(16, 302)
    $lblAppsSubtitle.Size = New-Object System.Drawing.Size(292, 18)
    $lblAppsSubtitle.Font = $fontSmall
    $form.Controls.Add($lblAppsSubtitle)

    $btnEditApps = New-Object System.Windows.Forms.Button
    $btnEditApps.Location = New-Object System.Drawing.Point(312, 298)
    $btnEditApps.Size = New-Object System.Drawing.Size(76, 24)
    $btnEditApps.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $btnEditApps.FlatAppearance.BorderSize = 1
    $btnEditApps.Font = $fontSection
    $btnEditApps.Add_Click({ Show-AppsEditor })
    $form.Controls.Add($btnEditApps)

    $tipEdit = New-Object System.Windows.Forms.ToolTip
    $tipEdit.SetToolTip($btnEditApps, $i18n[$script:currentLang].AppsEditTooltip)

    $flowAppButtons = New-Object System.Windows.Forms.FlowLayoutPanel
    $flowAppButtons.Location = New-Object System.Drawing.Point(14, 326)
    $flowAppButtons.Size = New-Object System.Drawing.Size(376, 186)
    $flowAppButtons.FlowDirection = [System.Windows.Forms.FlowDirection]::LeftToRight
    $flowAppButtons.WrapContents = $true
    $flowAppButtons.AutoScroll = $false
    $flowAppButtons.BackColor = [System.Drawing.Color]::Transparent
    $form.Controls.Add($flowAppButtons)

    function Show-AddCustomAppDialog {
        param([string]$Package)
        $t = $i18n[$script:currentLang]
        $c = if ($script:isDarkMode) { $themeColors.Dark } else { $themeColors.Light }

        $generic = @("com", "android", "app", "apps", "mobile", "client", "androidx", "google", "pl", "de", "at", "org", "net")
        $segments = @($Package -split '\.' | Where-Object { $_ -notin $generic })
        $rawName = if ($segments.Count -gt 0) { $segments[0] } else { ($Package -split '\.')[-1] }
        $defName = (Get-Culture).TextInfo.ToTitleCase($rawName)

        $dlg = New-Object System.Windows.Forms.Form
        $dlg.Text = $t.AddAppDialogTitle
        $dlg.ClientSize = New-Object System.Drawing.Size(360, 156)
        $dlg.StartPosition = [System.Windows.Forms.FormStartPosition]::CenterParent
        $dlg.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::FixedDialog
        $dlg.MaximizeBox = $false
        $dlg.MinimizeBox = $false
        $dlg.ShowInTaskbar = $false
        if ($form -and $form.Icon) { $dlg.Icon = $form.Icon }
        $dlg.Font = $fontRegular
        $dlg.BackColor = $c.Bg
        $dlg.ForeColor = $c.Text

        $lblPrompt = New-Object System.Windows.Forms.Label
        $lblPrompt.Location = New-Object System.Drawing.Point(16, 14)
        $lblPrompt.Size = New-Object System.Drawing.Size(328, 18)
        $lblPrompt.Text = $t.AddAppDialogLabel
        $lblPrompt.Font = $fontSection
        $dlg.Controls.Add($lblPrompt)

        $txtName = New-Object System.Windows.Forms.TextBox
        $txtName.Location = New-Object System.Drawing.Point(16, 36)
        $txtName.Size = New-Object System.Drawing.Size(328, 26)
        $txtName.Text = $defName
        $txtName.Font = $fontRegular
        $txtName.BorderStyle = [System.Windows.Forms.BorderStyle]::FixedSingle
        $txtName.BackColor = $c.InputBg
        $txtName.ForeColor = $c.InputText
        $dlg.Controls.Add($txtName)

        $lblPkg = New-Object System.Windows.Forms.Label
        $lblPkg.Location = New-Object System.Drawing.Point(16, 68)
        $lblPkg.Size = New-Object System.Drawing.Size(328, 16)
        $lblPkg.Text = "Pakiet: $Package"
        $lblPkg.Font = $fontSmall
        $lblPkg.ForeColor = $c.TextMuted
        $dlg.Controls.Add($lblPkg)

        $btnOk = New-Object System.Windows.Forms.Button
        $btnOk.Text = $t.AddAppDialogAdd
        $btnOk.Location = New-Object System.Drawing.Point(140, 106)
        $btnOk.Size = New-Object System.Drawing.Size(110, 30)
        $btnOk.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
        $btnOk.FlatAppearance.BorderSize = 1
        $btnOk.FlatAppearance.BorderColor = $c.BtnHero
        $btnOk.BackColor = $c.BtnHero
        $btnOk.ForeColor = $c.BtnHeroText
        $btnOk.Font = $fontSection
        $btnOk.DialogResult = [System.Windows.Forms.DialogResult]::OK
        $dlg.AcceptButton = $btnOk
        $dlg.Controls.Add($btnOk)

        $btnCancel = New-Object System.Windows.Forms.Button
        $btnCancel.Text = $t.AppsEditorCancel
        $btnCancel.Location = New-Object System.Drawing.Point(258, 106)
        $btnCancel.Size = New-Object System.Drawing.Size(86, 30)
        $btnCancel.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
        $btnCancel.FlatAppearance.BorderSize = 1
        $btnCancel.FlatAppearance.BorderColor = $c.BtnAppBorder
        $btnCancel.BackColor = $c.BtnApp
        $btnCancel.ForeColor = $c.BtnAppText
        $btnCancel.Font = $fontSection
        $btnCancel.DialogResult = [System.Windows.Forms.DialogResult]::Cancel
        $dlg.CancelButton = $btnCancel
        $dlg.Controls.Add($btnCancel)

        $dlg.Add_Shown({
            $txtName.Focus()
            $txtName.SelectAll()
        })

        if ($dlg.ShowDialog($form) -eq [System.Windows.Forms.DialogResult]::OK) {
            $chosenName = $txtName.Text.Trim()
            if (-not [string]::IsNullOrWhiteSpace($chosenName)) {
                $dlg.Dispose()
                return $chosenName
            }
        }
        $dlg.Dispose()
        return $null
    }

    $createdAppButtons = New-Object 'System.Collections.Generic.List[System.Windows.Forms.Button]'
    $createdDelButtons = New-Object 'System.Collections.Generic.List[System.Windows.Forms.Button]'

    function Update-AppButtonGrid {
        $flowAppButtons.SuspendLayout()
        try {
            while ($flowAppButtons.Controls.Count -gt 0) {
                $ctrl = $flowAppButtons.Controls[0]
                $flowAppButtons.Controls.RemoveAt(0)
                $ctrl.Dispose()
            }
            $createdAppButtons.Clear()
            $createdDelButtons.Clear()
            $c = if ($script:isDarkMode) { $themeColors.Dark } else { $themeColors.Light }
            $t = $i18n[$script:currentLang]

            if ($appButtons.Count -eq 0) {
                $btnEmpty = New-Object System.Windows.Forms.Button
                $btnEmpty.Text = if ($t.AppsAddPrompt) { $t.AppsAddPrompt } else { "+ Dodaj aplikacje z telefonu (kliknij tutaj)" }
                $btnEmpty.Size = New-Object System.Drawing.Size(350, 40)
                $btnEmpty.Margin = New-Object System.Windows.Forms.Padding(12, 3, 3, 3)
                $btnEmpty.Font = $fontSection
                $btnEmpty.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
                $btnEmpty.FlatAppearance.BorderSize = 1
                $btnEmpty.FlatAppearance.BorderColor = $c.CardBorder
                $btnEmpty.BackColor = $c.Card
                $btnEmpty.ForeColor = $c.TextMuted
                $btnEmpty.Cursor = [System.Windows.Forms.Cursors]::Hand
                $btnEmpty.Add_Click({ Show-AppsEditor })
                [void]$flowAppButtons.Controls.Add($btnEmpty)
                $createdAppButtons.Add($btnEmpty)
            }
            else {
                foreach ($app in $appButtons) {
                    $selectedApp = $app

                    $pnlTile = New-Object System.Windows.Forms.Panel
                    $pnlTile.Size = New-Object System.Drawing.Size(172, 30)
                    $pnlTile.Margin = New-Object System.Windows.Forms.Padding(3)
                    $pnlTile.BackColor = [System.Drawing.Color]::Transparent

                    $btn = New-Object System.Windows.Forms.Button
                    $btn.Text = $app.Text
                    $btn.Location = New-Object System.Drawing.Point(0, 0)
                    $btn.Size = New-Object System.Drawing.Size(148, 30)
                    $btn.Font = $fontSmall
                    $btn.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
                    $btn.FlatAppearance.BorderSize = 1
                    $btn.FlatAppearance.BorderColor = $c.BtnAppBorder
                    $btn.BackColor = $c.BtnApp
                    $btn.ForeColor = $c.BtnAppText
                    $btn.Cursor = [System.Windows.Forms.Cursors]::Hand

                    $btn.Add_Click({
                        if ($chkAutoTaskbar.Checked) { Start-Taskbar }
                        $disp = $script:appDisplaySize

                        if ($selectedApp.Package -eq "com.microsoft.rdc.androidx") {
                            $idx = $cmbRes.SelectedIndex
                            if ($idx -ge 0 -and $idx -lt $resValues.Count) {
                                $val = $resValues[$idx]
                                $disp = if ($val -eq "AUTO") { $script:appDisplaySize } else { $val }
                            }
                            else {
                                $disp = "1920x1080/160"
                            }
                        }

                        $useUhid = $selectedApp.Flags -contains "-UseUhidKeyboard"
                        $forwardClicks = $selectedApp.Flags -contains "-ForwardAllClicks"
                        Start-ScrcpyApp -PackageName $selectedApp.Package -WindowTitle $selectedApp.Text -UseUhidKeyboard:$useUhid -ForwardAllClicks:$forwardClicks -DisplaySize $disp
                    }.GetNewClosure())

                    # Mały przycisk usuwania kafelka (✕)
                    $btnDel = New-Object System.Windows.Forms.Button
                    $btnDel.Text = "✕"
                    $btnDel.Location = New-Object System.Drawing.Point(147, 0)
                    $btnDel.Size = New-Object System.Drawing.Size(25, 30)
                    $btnDel.Font = $fontSmall
                    $btnDel.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
                    $btnDel.FlatAppearance.BorderSize = 1
                    $btnDel.FlatAppearance.BorderColor = $c.BtnAppBorder
                    $btnDel.BackColor = $c.BtnApp
                    $btnDel.ForeColor = $c.TextMuted
                    $btnDel.Cursor = [System.Windows.Forms.Cursors]::Hand

                    $delTip = New-Object System.Windows.Forms.ToolTip
                    $delTip.SetToolTip($btnDel, ($t.AppsRemoveTooltip -f $selectedApp.Text))

                    $btnDel.Add_MouseEnter({
                        param($s, $e)
                        $s.ForeColor = [System.Drawing.Color]::FromArgb(255, 90, 90)
                    })
                    $btnDel.Add_MouseLeave({
                        param($s, $e)
                        $col = if ($script:isDarkMode) { $themeColors.Dark.TextMuted } else { $themeColors.Light.TextMuted }
                        $s.ForeColor = $col
                    })

                    $btnDel.Add_Click({
                        $appToRemove = $selectedApp
                        $confirmMsg = $t.MsgConfirmRemoveApp -f $appToRemove.Text
                        $dialogRes = [System.Windows.Forms.MessageBox]::Show(
                            $confirmMsg,
                            $t.AppsEditorTitle,
                            [System.Windows.Forms.MessageBoxButtons]::YesNo,
                            [System.Windows.Forms.MessageBoxIcon]::Question
                        )
                        if ($dialogRes -eq [System.Windows.Forms.DialogResult]::Yes) {
                            $match = @($appButtons | Where-Object { $_.Package -eq $appToRemove.Package -and $_.Text -eq $appToRemove.Text })
                            if ($match.Count -gt 0) { [void]$appButtons.Remove($match[0]) }

                            try {
                                $jsonApps = @(
                                    foreach ($a in $appButtons) {
                                        [pscustomobject]@{
                                            name = $a.Text
                                            package = $a.Package
                                            flags = @($a.Flags)
                                        }
                                    }
                                )
                                $json = ConvertTo-Json -InputObject $jsonApps -Depth 4
                                if (-not (Test-Path $userConfigRoot)) { [void](New-Item -ItemType Directory -Path $userConfigRoot -Force -ErrorAction Stop) }
                                Set-Content -LiteralPath $appsConfigFile -Value $json -Encoding UTF8 -ErrorAction Stop
                            }
                            catch {}

                            Update-AppButtonGrid
                        }
                    }.GetNewClosure())

                    [void]$pnlTile.Controls.Add($btn)
                    [void]$pnlTile.Controls.Add($btnDel)
                    [void]$flowAppButtons.Controls.Add($pnlTile)

                    $createdAppButtons.Add($btn)
                    $createdDelButtons.Add($btnDel)
                }
            }
        }
        finally {
            $flowAppButtons.ResumeLayout()
        }

        # Dynamiczne dostosowanie wysokości sekcji Aplikacje w oknach i płynne przesunięcie kontrolek poniżej
        if ($lblCustom -and $txtCustom -and $btnCustom -and $btnAddCustom -and $lblSectionDevice -and $btnDesktop -and $btnNormal -and $btnReboot) {
            $rowCount = if ($appButtons.Count -eq 0) { 1 } else { [Math]::Ceiling($appButtons.Count / 2.0) }
            $neededFlowH = ($rowCount * 36) + 6

            $screenH = 900
            try {
                $screenH = [System.Windows.Forms.Screen]::FromControl($form).WorkingArea.Height
            } catch {}
            $maxFlowH = [Math]::Max(186, $screenH - 520)

            if ($neededFlowH -gt $maxFlowH) {
                $flowAppButtons.Height = $maxFlowH
                $flowAppButtons.AutoScroll = $true
            }
            else {
                $flowAppButtons.Height = $neededFlowH
                $flowAppButtons.AutoScroll = $false
            }

            $curY = $flowAppButtons.Bottom + 10
            $lblCustom.Location = New-Object System.Drawing.Point(16, $curY)

            $curY = $lblCustom.Bottom + 4
            $txtCustom.Location = New-Object System.Drawing.Point(16, $curY)
            $btnCustom.Location = New-Object System.Drawing.Point(250, ($curY - 1))
            $btnAddCustom.Location = New-Object System.Drawing.Point(326, ($curY - 1))

            if ($lstCustomSuggestions) {
                $lstCustomSuggestions.Location = New-Object System.Drawing.Point($txtCustom.Left, ($txtCustom.Bottom + 1))
            }

            $curY = $txtCustom.Bottom + 12
            $lblSectionDevice.Location = New-Object System.Drawing.Point(16, $curY)

            $curY = $lblSectionDevice.Bottom + 6
            $btnDesktop.Location = New-Object System.Drawing.Point(16, $curY)
            $btnNormal.Location = New-Object System.Drawing.Point(208, $curY)

            $curY = $btnDesktop.Bottom + 8
            $btnReboot.Location = New-Object System.Drawing.Point(16, $curY)

            $curY = $btnReboot.Bottom + 18
            $form.ClientSize = New-Object System.Drawing.Size(404, $curY)
        }
    }

    # Własny pakiet (bezpośrednio pod siatką aplikacji)
    $lblCustom = New-Object System.Windows.Forms.Label
    $lblCustom.Location = New-Object System.Drawing.Point(16, 502)
    $lblCustom.Size = New-Object System.Drawing.Size(372, 16)
    $lblCustom.Font = $fontSmall
    $form.Controls.Add($lblCustom)

    $txtCustom = New-Object System.Windows.Forms.TextBox
    $txtCustom.Location = New-Object System.Drawing.Point(16, 520)
    $txtCustom.Size = New-Object System.Drawing.Size(228, 25)
    $txtCustom.BorderStyle = [System.Windows.Forms.BorderStyle]::FixedSingle
    $txtCustom.Font = $fontRegular
    $form.Controls.Add($txtCustom)

    # Dynamiczna wyszukiwarka / podpowiedzi pakietów (nakładana lista bez utraty fokusu)
    $lstCustomSuggestions = New-Object System.Windows.Forms.ListBox
    $lstCustomSuggestions.BorderStyle = [System.Windows.Forms.BorderStyle]::FixedSingle
    $lstCustomSuggestions.Font = $fontRegular
    $lstCustomSuggestions.IntegralHeight = $false
    $lstCustomSuggestions.Location = New-Object System.Drawing.Point($txtCustom.Left, ($txtCustom.Bottom + 1))
    $lstCustomSuggestions.Width = 284
    $lstCustomSuggestions.Height = 118
    $lstCustomSuggestions.Visible = $false
    $lstCustomSuggestions.Cursor = [System.Windows.Forms.Cursors]::Hand
    $form.Controls.Add($lstCustomSuggestions)

    $applyCustomFilter = {
        Ensure-InstalledAndroidPackages | Out-Null
        $query = $txtCustom.Text.Trim()
        if ($query.Length -lt 1) {
            if ($lstCustomSuggestions.Visible) { $lstCustomSuggestions.Visible = $false }
            return
        }

        # Pula pakietów: skonfigurowane kafelki + pakiety pobrane z podłączonego telefonu
        $pool = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
        foreach ($app in $appButtons) {
            if ($app -and -not [string]::IsNullOrWhiteSpace($app.Package)) {
                [void]$pool.Add($app.Package.Trim())
            }
        }
        foreach ($pkg in $script:cachedInstalledPackages) {
            if (-not [string]::IsNullOrWhiteSpace($pkg)) {
                [void]$pool.Add($pkg.Trim())
            }
        }

        if ($pool.Count -eq 0) {
            if ($lstCustomSuggestions.Visible) { $lstCustomSuggestions.Visible = $false }
            return
        }

        $tokens = @($query -split '\s+' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        $matches = @($pool | Where-Object {
            $pkg = $_
            $allMatch = $true
            foreach ($tok in $tokens) {
                if ($pkg.IndexOf($tok, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
                    $allMatch = $false
                    break
                }
            }
            $allMatch
        } | Sort-Object)

        if ($matches.Count -eq 0) {
            if ($lstCustomSuggestions.Visible) { $lstCustomSuggestions.Visible = $false }
            return
        }

        $lstCustomSuggestions.BeginUpdate()
        try {
            $lstCustomSuggestions.Items.Clear()
            $maxCount = [Math]::Min($matches.Count, 30)
            for ($i = 0; $i -lt $maxCount; $i++) {
                [void]$lstCustomSuggestions.Items.Add($matches[$i])
            }
            $lstCustomSuggestions.SelectedIndex = -1
        }
        finally {
            $lstCustomSuggestions.EndUpdate()
        }

        $itemH = [Math]::Max($lstCustomSuggestions.ItemHeight, 20)
        $visibleCount = [Math]::Min($matches.Count, 6)
        $h = ($visibleCount * $itemH) + 4
        $lstCustomSuggestions.Height = [Math]::Min($h, 118)
        $lstCustomSuggestions.BringToFront()
        if (-not $lstCustomSuggestions.Visible) {
            $lstCustomSuggestions.Visible = $true
        }
    }

    $txtCustom.Add_TextChanged({ & $applyCustomFilter })

    $txtCustom.Add_KeyDown({
        param($s, $e)
        if ($e.KeyCode -eq [System.Windows.Forms.Keys]::Down -and $lstCustomSuggestions.Visible) {
            if ($lstCustomSuggestions.Items.Count -gt 0) {
                if ($lstCustomSuggestions.SelectedIndex -lt ($lstCustomSuggestions.Items.Count - 1)) {
                    $lstCustomSuggestions.SelectedIndex++
                } else {
                    $lstCustomSuggestions.SelectedIndex = 0
                }
            }
            $e.Handled = $true
        }
        elseif ($e.KeyCode -eq [System.Windows.Forms.Keys]::Up -and $lstCustomSuggestions.Visible) {
            if ($lstCustomSuggestions.Items.Count -gt 0) {
                if ($lstCustomSuggestions.SelectedIndex -gt 0) {
                    $lstCustomSuggestions.SelectedIndex--
                } else {
                    $lstCustomSuggestions.SelectedIndex = $lstCustomSuggestions.Items.Count - 1
                }
            }
            $e.Handled = $true
        }
        elseif ($e.KeyCode -eq [System.Windows.Forms.Keys]::Enter) {
            if ($lstCustomSuggestions.Visible -and $lstCustomSuggestions.SelectedIndex -ge 0) {
                $txtCustom.Text = [string]$lstCustomSuggestions.SelectedItem
                $txtCustom.SelectionStart = $txtCustom.Text.Length
                $lstCustomSuggestions.Visible = $false
                $e.Handled = $true
                $e.SuppressKeyPress = $true
            }
            else {
                $lstCustomSuggestions.Visible = $false
                $btnCustom.PerformClick()
                $e.Handled = $true
                $e.SuppressKeyPress = $true
            }
        }
        elseif ($e.KeyCode -eq [System.Windows.Forms.Keys]::Escape) {
            if ($lstCustomSuggestions.Visible) {
                $lstCustomSuggestions.Visible = $false
                $e.Handled = $true
            }
        }
    })

    $txtCustom.Add_LostFocus({
        $pt = $lstCustomSuggestions.PointToClient([System.Windows.Forms.Cursor]::Position)
        if (-not $lstCustomSuggestions.ClientRectangle.Contains($pt)) {
            $lstCustomSuggestions.Visible = $false
        }
    })

    $lstCustomSuggestions.Add_Click({
        if ($lstCustomSuggestions.SelectedItem) {
            $txtCustom.Text = [string]$lstCustomSuggestions.SelectedItem
            $txtCustom.SelectionStart = $txtCustom.Text.Length
            $lstCustomSuggestions.Visible = $false
            $txtCustom.Focus()
        }
    })

    $lstCustomSuggestions.Add_DoubleClick({
        if ($lstCustomSuggestions.SelectedItem) {
            $txtCustom.Text = [string]$lstCustomSuggestions.SelectedItem
            $lstCustomSuggestions.Visible = $false
            $btnCustom.PerformClick()
        }
    })

    $lstCustomSuggestions.Add_LostFocus({
        $lstCustomSuggestions.Visible = $false
    })

    $form.Add_Click({
        if ($lstCustomSuggestions.Visible) {
            $lstCustomSuggestions.Visible = $false
        }
    })

    $btnCustom = New-Object System.Windows.Forms.Button
    $btnCustom.Location = New-Object System.Drawing.Point(250, 519)
    $btnCustom.Size = New-Object System.Drawing.Size(72, 26)
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

    $btnAddCustom = New-Object System.Windows.Forms.Button
    $btnAddCustom.Location = New-Object System.Drawing.Point(326, 519)
    $btnAddCustom.Size = New-Object System.Drawing.Size(62, 26)
    $btnAddCustom.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $btnAddCustom.FlatAppearance.BorderSize = 1
    $btnAddCustom.Font = $fontSection
    $btnAddCustom.Text = "+ Dodaj"
    $btnAddCustom.Cursor = [System.Windows.Forms.Cursors]::Hand
    $tipAddCustom = New-Object System.Windows.Forms.ToolTip
    $tipAddCustom.SetToolTip($btnAddCustom, $i18n[$script:currentLang].CustomAddTooltip)
    $btnAddCustom.Add_Click({
        $pkg = $txtCustom.Text.Trim()
        $t = $i18n[$script:currentLang]
        if ([string]::IsNullOrWhiteSpace($pkg)) {
            [System.Windows.Forms.MessageBox]::Show($t.MsgPkgEmpty, "scrcpy", [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Warning)
            return
        }
        $existing = @($appButtons | Where-Object { $_.Package -eq $pkg })
        if ($existing.Count -gt 0) {
            [System.Windows.Forms.MessageBox]::Show(($t.MsgAppAlreadyExists -f $pkg, $existing[0].Text), "scrcpy", [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Information)
            return
        }
        $chosenName = Show-AddCustomAppDialog -Package $pkg
        if (-not [string]::IsNullOrWhiteSpace($chosenName)) {
            $newApp = @{
                Text = $chosenName
                Package = $pkg
                Flags = @()
            }
            [void]$appButtons.Add($newApp)
            try {
                $jsonApps = @(
                    foreach ($a in $appButtons) {
                        [pscustomobject]@{
                            name = $a.Text
                            package = $a.Package
                            flags = @($a.Flags)
                        }
                    }
                )
                $json = ConvertTo-Json -InputObject $jsonApps -Depth 4
                if (-not (Test-Path $userConfigRoot)) { [void](New-Item -ItemType Directory -Path $userConfigRoot -Force -ErrorAction Stop) }
                Set-Content -LiteralPath $appsConfigFile -Value $json -Encoding UTF8 -ErrorAction Stop
            }
            catch {}
            if ($lstCustomSuggestions) { $lstCustomSuggestions.Visible = $false }
            $txtCustom.Text = ""
            Update-AppButtonGrid
        }
    })
    $form.Controls.Add($btnAddCustom)

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

    Update-AppButtonGrid

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

        $btnEditApps.BackColor = $c.BtnApp
        $btnEditApps.ForeColor = $c.BtnAppText
        $btnEditApps.FlatAppearance.BorderColor = $c.BtnAppBorder

        foreach ($btn in $createdAppButtons) {
            if ($appButtons.Count -eq 0) {
                $btn.BackColor = $c.Card
                $btn.ForeColor = $c.TextMuted
                $btn.FlatAppearance.BorderColor = $c.CardBorder
            } else {
                $btn.BackColor = $c.BtnApp
                $btn.ForeColor = $c.BtnAppText
                $btn.FlatAppearance.BorderColor = $c.BtnAppBorder
            }
        }

        foreach ($btnDel in $createdDelButtons) {
            $btnDel.BackColor = $c.BtnApp
            $btnDel.ForeColor = $c.TextMuted
            $btnDel.FlatAppearance.BorderColor = $c.BtnAppBorder
        }

        $lblCustom.ForeColor = $c.TextMuted
        $txtCustom.BackColor = $c.InputBg
        $txtCustom.ForeColor = $c.InputText
        if ($lstCustomSuggestions) {
            $lstCustomSuggestions.BackColor = $c.InputBg
            $lstCustomSuggestions.ForeColor = $c.InputText
        }

        $btnCustom.BackColor = $c.BtnApp
        $btnCustom.ForeColor = $c.BtnAppText
        $btnCustom.FlatAppearance.BorderColor = $c.BtnAppBorder

        if ($btnAddCustom) {
            $btnAddCustom.BackColor = $c.BtnApp
            $btnAddCustom.ForeColor = $c.BtnAppText
            $btnAddCustom.FlatAppearance.BorderColor = $c.BtnAppBorder
        }

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
        $btnEditApps.Text = $t.AppsEdit
        if ($tipEdit) { $tipEdit.SetToolTip($btnEditApps, $t.AppsEditTooltip) }
        if ($appButtons.Count -eq 0 -and $createdAppButtons.Count -gt 0) {
            $createdAppButtons[0].Text = $t.AppsAddPrompt
        }
        $lblCustom.Text = $t.CustomLabel
        $btnCustom.Text = $t.CustomBtn
        if ($btnAddCustom) {
            $btnAddCustom.Text = $t.CustomAddBtn
            if ($tipAddCustom) { $tipAddCustom.SetToolTip($btnAddCustom, $t.CustomAddTooltip) }
        }
        [WinFormsCueBanner]::SendMessage($txtCustom.Handle, 0x1501, [IntPtr]::Zero, $t.CustomPlaceholder) | Out-Null

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
            $lblDeviceTitle.Text = "$devName - $connType"
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
