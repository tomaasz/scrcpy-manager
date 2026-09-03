# Scrcpy Manager

Panel PowerShell do uruchamiania `scrcpy` i aplikacji Android w osobnych oknach.

## Wymagania

- PowerShell 7+
- `adb` i `scrcpy` dostępne w `PATH`
- telefon z włączonym debugowaniem USB

## Uruchomienie

Ustaw PIN odblokowania tylko lokalnie w bieżącej sesji PowerShell:

```powershell
$env:SCRCPY_ADB_PIN = "1234"
& .\ScrcpyApp.ps1
```

Przycisk ConneckBot uruchamia pakiet `org.connectbot`.