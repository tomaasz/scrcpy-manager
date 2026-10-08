# ============================================================
# Build Script: ScrcpyManager-Portable.exe
# ============================================================

$ErrorActionPreference = "Stop"

$repoDir = $PSScriptRoot
if (-not $repoDir) { $repoDir = (Get-Location).Path }

Write-Host "Rozpoczynanie budowy ScrcpyManager-Portable.exe..." -ForegroundColor Cyan

# 1. Znajdź pliki scrcpy i adb
$scrcpyDir = $null

if ($env:SCRCPY_DIR -and (Test-Path (Join-Path $env:SCRCPY_DIR "scrcpy.exe"))) {
    $scrcpyDir = $env:SCRCPY_DIR
} else {
    $scrcpyCmd = Get-Command scrcpy.exe -ErrorAction SilentlyContinue
    if ($scrcpyCmd) {
        $scrcpyDir = Split-Path -Parent $scrcpyCmd.Source
    } else {
        $wingetPath = "$env:LOCALAPPDATA\Microsoft\WinGet\Packages\Genymobile.scrcpy_Microsoft.Winget.Source_8wekyb3d8bbwe"
        if (Test-Path $wingetPath) {
            $sub = Get-ChildItem $wingetPath -Directory | Where-Object { $_.Name -like "scrcpy*" } | Select-Object -First 1
            if ($sub) { $scrcpyDir = $sub.FullName }
        }
        if (-not $scrcpyDir) {
            $runtimePath = Join-Path $env:LOCALAPPDATA "scrcpy-manager\runtime"
            if (Test-Path (Join-Path $runtimePath "scrcpy.exe")) {
                $scrcpyDir = $runtimePath
            }
        }
    }
}

if (-not $scrcpyDir -or -not (Test-Path (Join-Path $scrcpyDir "scrcpy.exe"))) {
    Write-Error "Nie znaleziono instalacji scrcpy! Upewnij się, że scrcpy znajduje się w PATH."
    exit 1
}

Write-Host "Katalog źródłowy scrcpy: $scrcpyDir" -ForegroundColor Green

# 2. Przygotuj staging
$tempStage = Join-Path $repoDir "temp_portable_stage"
$bundleZip = Join-Path $repoDir "bundle.zip"
$outputExe = if ($env:OUTPUT_EXE) { $env:OUTPUT_EXE } else { Join-Path $repoDir "ScrcpyManager-Portable.exe" }

if (Test-Path $tempStage) { Remove-Item $tempStage -Recurse -Force }
if (Test-Path $bundleZip) { Remove-Item $bundleZip -Force }

New-Item -ItemType Directory -Path $tempStage -Force | Out-Null

try {
    # 3. Skopiuj wyłącznie pliki wymagane przez scrcpy/ADB. Jawna lista
    # zapobiega przypadkowemu dołączeniu prywatnych JSON-ów, logów i skryptów.
    $requiredRuntimeFiles = @(
        "scrcpy.exe",
        "scrcpy-server",
        "adb.exe",
        "AdbWinApi.dll",
        "AdbWinUsbApi.dll",
        "SDL3.dll",
        "libusb-1.0.dll"
    )
    foreach ($name in $requiredRuntimeFiles) {
        $source = Join-Path $scrcpyDir $name
        if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
            throw "Brak wymaganego pliku runtime: $name"
        }
        Copy-Item -LiteralPath $source -Destination $tempStage -Force
    }

    $codecPatterns = @("avcodec-*.dll", "avformat-*.dll", "avutil-*.dll", "swresample-*.dll")
    foreach ($pattern in $codecPatterns) {
        $matches = @(Get-ChildItem -LiteralPath $scrcpyDir -Filter $pattern -File)
        if ($matches.Count -eq 0) {
            throw "Brak wymaganej biblioteki runtime pasującej do: $pattern"
        }
        foreach ($match in $matches) {
            Copy-Item -LiteralPath $match.FullName -Destination $tempStage -Force
        }
    }

    $licenseFile = Join-Path $scrcpyDir "LICENSE.txt"
    if (Test-Path -LiteralPath $licenseFile -PathType Leaf) {
        Copy-Item -LiteralPath $licenseFile -Destination $tempStage -Force
    }

    # 4. Skopiuj pliki aplikacji
    Copy-Item -LiteralPath (Join-Path $repoDir "apps.json") -Destination $tempStage -Force
    $appIco = Join-Path $repoDir "app.ico"
    if (Test-Path $appIco) {
        Copy-Item -LiteralPath $appIco -Destination $tempStage -Force
    }
    $iconsDir = Join-Path $repoDir "icons"
    if (Test-Path $iconsDir) {
        Copy-Item -LiteralPath $iconsDir -Destination (Join-Path $tempStage "icons") -Recurse -Force
    }

    Write-Host "Spakowano pliki do stagingu: $( (Get-ChildItem $tempStage).Count ) plików" -ForegroundColor Green

    # 5. Utwórz archiwum ZIP
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory($tempStage, $bundleZip, [System.IO.Compression.CompressionLevel]::Optimal, $false)

    $zipSizeMb = (Get-Item $bundleZip).Length / 1MB
    Write-Host ("Rozmiar archiwum bundle.zip: {0:N2} MB" -f $zipSizeMb) -ForegroundColor Green

    # 6. Kompilacja i publikacja (.NET 10, self-contained, pojedynczy plik)
    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnet) {
        Write-Error "Nie znaleziono polecenia dotnet! Zainstaluj .NET 10 SDK."
        exit 1
    }

    $publishDir = Join-Path $repoDir "temp_portable_publish"
    if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

    Write-Host "Publikacja aplikacji (.NET 10 self-contained, single-file)..." -ForegroundColor Cyan
    & dotnet publish (Join-Path $repoDir "ScrcpyManager.csproj") -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $publishDir --nologo -v:q

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Błąd publikacji dotnet! Kod wyjścia: $LASTEXITCODE"
        exit 1
    }

    $publishedExe = Join-Path $publishDir "ScrcpyManager.exe"
    if (-not (Test-Path $publishedExe)) {
        Write-Error "Nie znaleziono opublikowanego pliku ScrcpyManager.exe."
        exit 1
    }
    Copy-Item -LiteralPath $publishedExe -Destination $outputExe -Force

    $exeSizeMb = (Get-Item $outputExe).Length / 1MB
    Write-Host ("
SUKCES! Utworzono: {0} ({1:N2} MB)" -f $outputExe, $exeSizeMb) -ForegroundColor Green
}
finally {
    # 7. Czyszczenie plików tymczasowych
    if (Test-Path $tempStage) { Remove-Item $tempStage -Recurse -Force -ErrorAction SilentlyContinue }
    if (Test-Path $bundleZip) { Remove-Item $bundleZip -Force -ErrorAction SilentlyContinue }
    if ($publishDir -and (Test-Path $publishDir)) { Remove-Item $publishDir -Recurse -Force -ErrorAction SilentlyContinue }
}
