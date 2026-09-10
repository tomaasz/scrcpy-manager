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
$outputExe = Join-Path $repoDir "ScrcpyManager-Portable.exe"

if (Test-Path $tempStage) { Remove-Item $tempStage -Recurse -Force }
if (Test-Path $bundleZip) { Remove-Item $bundleZip -Force }

New-Item -ItemType Directory -Path $tempStage -Force | Out-Null

try {
    # 3. Skopiuj pliki scrcpy
    Get-ChildItem -Path $scrcpyDir -File | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $tempStage -Force
    }

    # 4. Skopiuj pliki aplikacji
    Copy-Item -LiteralPath (Join-Path $repoDir "ScrcpyApp.ps1") -Destination $tempStage -Force
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

    # 6. Kompilacja C#
    $csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
    $launcherCs = Join-Path $repoDir "src\LauncherPortable.cs"

    $refs = "System.dll,System.Windows.Forms.dll,System.IO.Compression.dll,System.IO.Compression.FileSystem.dll,Microsoft.CSharp.dll"

    Write-Host "Kompilacja przez csc.exe..." -ForegroundColor Cyan

    $iconArg = if (Test-Path $appIco) { "/win32icon:$appIco" } else { "" }
    & $csc /nologo /target:winexe /optimize+ /platform:x64 $iconArg "/out:$outputExe" "/resource:$bundleZip,bundle.zip" "/r:$refs" "$launcherCs"

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Błąd kompilacji csc.exe! Kod wyjścia: $LASTEXITCODE"
        exit 1
    }

    $exeSizeMb = (Get-Item $outputExe).Length / 1MB
    Write-Host ("
SUKCES! Utworzono: {0} ({1:N2} MB)" -f $outputExe, $exeSizeMb) -ForegroundColor Green
}
finally {
    # 7. Czyszczenie plików tymczasowych
    if (Test-Path $tempStage) { Remove-Item $tempStage -Recurse -Force -ErrorAction SilentlyContinue }
    if (Test-Path $bundleZip) { Remove-Item $bundleZip -Force -ErrorAction SilentlyContinue }
}