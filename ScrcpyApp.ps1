Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# --- KONFIGURACJA ---
# PIN jest pobierany ze zmiennej środowiskowej, aby nie trafiał do repozytorium.
$adbPin = $env:SCRCPY_ADB_PIN

# Domyślny rozmiar wirtualnego ekranu dla pojedynczych aplikacji (szer x wys)
$appDisplaySize = "1080x2200"

# --- FUNKCJE POMOCNICZE ---

function Test-AdbDevice {
    $devices = adb devices 2>$null | Select-String "device$"
    if (-not $devices) {
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

function Start-ScrcpyApp {
    param(
        [Parameter(Mandatory = $true)][string]$PackageName,
        [string]$DisplaySize = $appDisplaySize
    )

    if (-not (Test-AdbDevice)) { return }

    $argList = @(
        "--new-display=$DisplaySize",
        "--start-app=$PackageName",
        "--no-vd-system-decorations",
        "-x"
    ) -join " "

    try {
        Start-Process -NoNewWindow "scrcpy" -ArgumentList $argList -ErrorAction Stop
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

# Główne okno aplikacji
$form = New-Object System.Windows.Forms.Form
$form.Text = "Scrcpy Manager"
$form.Size = New-Object System.Drawing.Size(320, 620)
$form.StartPosition = "CenterScreen"
$form.FormBorderStyle = "FixedDialog"
$form.MaximizeBox = $false

# Etykieta
$label = New-Object System.Windows.Forms.Label
$label.Text = "Zarządzanie ekranem (Pixel):"
$label.Location = New-Object System.Drawing.Point(20, 15)
$label.AutoSize = $true
$form.Controls.Add($label)

# Przycisk 1: Tryb Desktop
$btnDesktop = New-Object System.Windows.Forms.Button
$btnDesktop.Location = New-Object System.Drawing.Point(20, 40)
$btnDesktop.Size = New-Object System.Drawing.Size(260, 40)
$btnDesktop.Text = "1. Włącz Tryb Desktop (Małe DPI)"
$btnDesktop.Add_Click({
    if (-not (Test-AdbDevice)) { return }

    adb shell wm density 250
    adb shell settings put global window_animation_scale 0.5
    adb shell settings put global transition_animation_scale 0.5
    adb shell settings put global animator_duration_scale 0.5

    # Ustawienia wymagane, żeby okna freeform w Taskbarze miały ramkę
    # i dało się je przeciągać/skalować (bez tego bywają "gołe").
    adb shell settings put global enable_freeform_support 1
    adb shell settings put secure force_resizable_activities 1

    # Automatyczne uruchomienie Taskbara
    adb shell monkey -p com.farmerbb.taskbar 1 | Out-Null

    [System.Windows.Forms.MessageBox]::Show(
        "Zastosowano małe DPI, przyspieszono animacje, włączono freeform i uruchomiono Taskbar.`n`nJeśli okna aplikacji w Taskbarze nadal nie mają ramki do przeciągania/zmiany rozmiaru - zrestartuj telefon.",
        "Gotowe"
    )
})
$form.Controls.Add($btnDesktop)

# Przycisk 2: Uruchom scrcpy (z automatycznym obracaniem i logowaniem)
$btnScrcpy = New-Object System.Windows.Forms.Button
$btnScrcpy.Location = New-Object System.Drawing.Point(20, 90)
$btnScrcpy.Size = New-Object System.Drawing.Size(260, 50)
$btnScrcpy.Text = "2. URUCHOM SCRCPY"
$btnScrcpy.BackColor = [System.Drawing.Color]::LightGreen
$btnScrcpy.Font = New-Object System.Drawing.Font("Arial", 9, [System.Drawing.FontStyle]::Bold)
$btnScrcpy.Add_Click({
    if (-not (Test-AdbDevice)) { return }

    # 1. Wymuszenie orientacji poziomej
    adb shell settings put system accelerometer_rotation 0
    adb shell settings put system user_rotation 1

    # 2. Wybudzenie ekranu
    adb shell input keyevent 224
    Start-Sleep -Milliseconds 500

    # 3. Symulacja pociągnięcia palcem do góry (aby odsłonić pole na PIN)
    adb shell input swipe 500 1500 500 300 200
    Start-Sleep -Milliseconds 500

    # 4. Wpisanie PINu i zatwierdzenie
    adb shell input text $adbPin
    adb shell input keyevent 66
    Start-Sleep -Milliseconds 300

    # 5. Uruchomienie scrcpy
    try {
        Start-Process -NoNewWindow "scrcpy" -ArgumentList "-S -w -K -M" -ErrorAction Stop
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
$btnNormal.Location = New-Object System.Drawing.Point(20, 150)
$btnNormal.Size = New-Object System.Drawing.Size(260, 40)
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

    # Wymuszone zamknięcie Taskbara i powrót na domyślny ekran główny
    adb shell am force-stop com.farmerbb.taskbar
    adb shell input keyevent 3

    [System.Windows.Forms.MessageBox]::Show("Przywrócono domyślne ustawienia telefonu i zamknięto Taskbar.", "Gotowe")
})
$form.Controls.Add($btnNormal)

# --- Sekcja: uruchamianie konkretnych aplikacji w osobnym oknie ---

$groupApps = New-Object System.Windows.Forms.GroupBox
$groupApps.Text = "Uruchom aplikację w osobnym oknie"
$groupApps.Location = New-Object System.Drawing.Point(20, 200)
$groupApps.Size = New-Object System.Drawing.Size(260, 370)
$form.Controls.Add($groupApps)

# Przycisk: WhatsApp
$btnWhatsApp = New-Object System.Windows.Forms.Button
$btnWhatsApp.Location = New-Object System.Drawing.Point(15, 25)
$btnWhatsApp.Size = New-Object System.Drawing.Size(230, 35)
$btnWhatsApp.Text = "WhatsApp"
$btnWhatsApp.Add_Click({ Start-ScrcpyApp -PackageName "com.whatsapp" })
$groupApps.Controls.Add($btnWhatsApp)

# Przycisk: Vivaldi
$btnVivaldi = New-Object System.Windows.Forms.Button
$btnVivaldi.Location = New-Object System.Drawing.Point(15, 65)
$btnVivaldi.Size = New-Object System.Drawing.Size(230, 35)
$btnVivaldi.Text = "Vivaldi"
$btnVivaldi.Add_Click({ Start-ScrcpyApp -PackageName "com.vivaldi.browser" })
$groupApps.Controls.Add($btnVivaldi)

# Przycisk: TurboTel (mod Telegrama)
$btnTurboTel = New-Object System.Windows.Forms.Button
$btnTurboTel.Location = New-Object System.Drawing.Point(15, 105)
$btnTurboTel.Size = New-Object System.Drawing.Size(230, 35)
$btnTurboTel.Text = "TurboTel"
$btnTurboTel.Add_Click({ Start-ScrcpyApp -PackageName "ellipi.messenger" })
$groupApps.Controls.Add($btnTurboTel)

# Przycisk: Wiadomości (Google Messages)
$btnMessages = New-Object System.Windows.Forms.Button
$btnMessages.Location = New-Object System.Drawing.Point(15, 145)
$btnMessages.Size = New-Object System.Drawing.Size(230, 35)
$btnMessages.Text = "Wiadomości (Google)"
$btnMessages.Add_Click({ Start-ScrcpyApp -PackageName "com.google.android.apps.messaging" })
$groupApps.Controls.Add($btnMessages)

# Przycisk: Windows App (Microsoft Remote Desktop)
$btnWindowsApp = New-Object System.Windows.Forms.Button
$btnWindowsApp.Location = New-Object System.Drawing.Point(15, 185)
$btnWindowsApp.Size = New-Object System.Drawing.Size(230, 35)
$btnWindowsApp.Text = "Windows App"
$btnWindowsApp.Add_Click({ Start-ScrcpyApp -PackageName "com.microsoft.rdc.androidx" })
$groupApps.Controls.Add($btnWindowsApp)

# Przycisk: ConneckBot (ConnectBot)
$btnConneckBot = New-Object System.Windows.Forms.Button
$btnConneckBot.Location = New-Object System.Drawing.Point(15, 225)
$btnConneckBot.Size = New-Object System.Drawing.Size(230, 35)
$btnConneckBot.Text = "ConneckBot"
$btnConneckBot.Add_Click({ Start-ScrcpyApp -PackageName "org.connectbot" })
$groupApps.Controls.Add($btnConneckBot)

# Dowolny inny pakiet - pole tekstowe + przycisk
$lblCustom = New-Object System.Windows.Forms.Label
$lblCustom.Text = "Inny pakiet (np. com.spotify.music):"
$lblCustom.Location = New-Object System.Drawing.Point(15, 270)
$lblCustom.AutoSize = $true
$groupApps.Controls.Add($lblCustom)

$txtCustom = New-Object System.Windows.Forms.TextBox
$txtCustom.Location = New-Object System.Drawing.Point(15, 290)
$txtCustom.Size = New-Object System.Drawing.Size(230, 20)
$groupApps.Controls.Add($txtCustom)

$btnCustom = New-Object System.Windows.Forms.Button
$btnCustom.Location = New-Object System.Drawing.Point(15, 315)
$btnCustom.Size = New-Object System.Drawing.Size(230, 35)
$btnCustom.Text = "Uruchom"
$btnCustom.Add_Click({
    $pkg = $txtCustom.Text.Trim()
    if ([string]::IsNullOrWhiteSpace($pkg)) {
        [System.Windows.Forms.MessageBox]::Show("Wpisz nazwę pakietu aplikacji.", "Brak pakietu")
        return
    }
    Start-ScrcpyApp -PackageName $pkg
})
$groupApps.Controls.Add($btnCustom)

# Uruchomienie okna
$form.ShowDialog()