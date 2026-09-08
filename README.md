# Scrcpy Manager 📱🖥️

> **Universal PowerShell desktop companion for `scrcpy` and Android power users.**  
> Launch Android apps in dedicated floating windows, manage virtual displays, switch seamlessly to Wireless ADB, prevent screen sleep, and streamline Remote Desktop workflows.

[![PowerShell](https://img.shields.io/badge/PowerShell-5.1%20%7C%207%2B-blue.svg)](https://microsoft.com/PowerShell)
[![scrcpy](https://img.shields.io/badge/scrcpy-2.0%2B-brightgreen.svg)](https://github.com/Genymobile/scrcpy)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Platform: Android](https://img.shields.io/badge/Android-Universal-green.svg)](https://android.com)

---

## English Documentation

### Key Features

* 🚀 **Multi-Window App Launcher**: Launch any installed Android app in an independent virtual display window (`--new-display`).
* 📶 **One-Click Wireless ADB (Wi-Fi)**: Automatically detect your phone's Wi-Fi IP and switch from USB cable to wireless debugging in one click.
* 🔊 **Audio Passthrough Control**: Toggle real-time audio forwarding from your Android device to PC speakers/headphones.
* ⌨️ **Hardware Keyboard (UHID) & Diacritics**: Full native support for special characters and diacritics (e.g., Polish `AltGr + a/e/c/s/l/z/x/o/n`) with direct hardware keyboard simulation (`-K`).
* 🖥️ **High-Resolution RDP Profiles (Windows App)**: Optimized presets for Microsoft Remote Desktop / Windows App:
  * `Full HD Native (1920x1080 / DPI 160)` – 1:1 pixel sharpness on 1080p monitors.
  * `2K QHD (2560x1440 / DPI 160)` – +77% expanded desktop workspace.
  * `4K UHD (3840x2160 / DPI 200)` – Ultra-high workspace.
  * *High bitrate (16 Mbps)* enabled for razor-sharp text and fonts.
* 🌓 **Dark & Light Mode**: Seamless 1-click theme switcher (`☀️ Light` / `🌙 Dark`) with custom OwnerDraw dropdowns.
* 🌐 **Bilingual Interface**: Instant on-the-fly toggling between Polish (PL) and English (EN) with preference persistence (`preferences.json`).
* 🔋 **Battery & Device Monitor**: Real-time battery percentage, charging state, device model, and connection mode.
* 🎨 **Refined Action Layout**: Hero launch button, side-by-side mode controls, and 3-column utility pills.
* ⚙️ **Configurable App Grid (`apps.json`)**: Add, remove, or customize your favorite apps simply by editing a JSON file.
* 🔒 **Zero Hardcoded Secrets**: Secure PIN handling via environment variables; no credentials or sensitive tokens stored in git.

---

### Requirements

1. **Windows 10 / 11** (PowerShell 5.1 or PowerShell 7+)
2. **[scrcpy](https://github.com/Genymobile/scrcpy)** (v2.0 or newer recommended, available in `PATH`)
3. **Android Platform Tools (`adb`)** (available in `PATH`)
4. **Android Device** (Google Pixel, Samsung Galaxy, Xiaomi, Motorola, OnePlus, etc.) with:
   * **USB Debugging** enabled in *Developer Options*.
   * (Optional) **Taskbar** app (by farmerbb) for freeform window management.

---

### Quick Start

1. Clone the repository:
   ```bash
   git clone https://github.com/tomaasz/scrcpy-manager.git
   cd scrcpy-manager
   ```

2. (Optional) Set your phone's unlock PIN for the current session:
   ```powershell
   $env:SCRCPY_ADB_PIN = "1234"
   ```

3. Launch the manager:
   ```powershell
   & .\ScrcpyApp.ps1
   ```

---

### Customizing Apps (`apps.json`)

You can edit `apps.json` to configure the buttons shown in the manager:

```json
[
  {
    "name": "Messenger",
    "package": "com.facebook.orca",
    "flags": []
  },
  {
    "name": "Spotify",
    "package": "com.spotify.music",
    "flags": []
  },
  {
    "name": "Windows App",
    "package": "com.microsoft.rdc.androidx",
    "flags": ["-UseUhidKeyboard", "-ForwardAllClicks"]
  }
]
```

---

## Dokumentacja po polsku (Polish)

### Najważniejsze funkcje

* **Aplikacje w osobnych oknach**: Uruchamianie aplikacji Androida w niezależnych, pływających oknach (`--new-display`).
* **Bezprzewodowe ADB jednym kliknięciem**: Automatyczne wykrycie IP telefonu w sieci domowej/biurowej i przełączenie na tryb Wi-Fi (`adb connect <IP>:5555`).
* **Przełącznik przesyłania dźwięku**: Opcja włączenia lub wyciszenia dźwięku (`--no-audio`) z poziomu panelu.
* **Pełna obsługa polskich znaków i klawiatury fizycznej**: Wszystkie aplikacje uruchamiają się z obsługą sprzętowej klawiatury UHID (`-K`), dzięki czemu prawy Alt (`AltGr + a, e, c, s, l, z, x, o, n`) działa natywnie.
* **Optymalizacje dla Windows App (RDP)**:
  * Gotowe profile rozdzielczości (Full HD 1:1, 2K QHD, 4K UHD).
  * Wyłączone wymuszone skalowanie w dół i podniesiony bitrate do 16 Mbps dla ostrych czcionek.
  * Przycisk szybkiej naprawy schowka między 3 maszynami (PC lokalny $\leftrightarrow$ Telefon $\leftrightarrow$ PC zdalny).
* **Automatyczne czuwanie i odblokowywanie**: Telefon nie blokuje się podczas pracy z panelem lub oknami scrcpy, a po zakończeniu przywracany jest pierwotny limit wygaszania.
* **Tryb Ciemny i Jasny (Dark / Light Mode)**: Przełączanie motywu jednym kliknięciem z dedykowaną obsługą rysowania list rozwijanych (brak białych pól w trybie ciemnym).
* **Pełna dwujęzyczność (PL / EN)**: Błyskawiczna zmiana języka interfejsu w locie z zapamiętywaniem w `preferences.json`.
* **Przejrzysty układ przycisków**: Przycisk akcji głównej (Hero), czytelne przyciski trybów obok siebie oraz 3-kolumnowe pigułki narzędziowe (Schowek, Klawiatura, Wi-Fi).
* **Konfiguracja przez `apps.json`**: Łatwe dodawanie i usuwanie programów bez modyfikacji kodu skryptu.

---

### Bezpieczeństwo i prywatność

* Skrypt **nie przechowuje ani nie wysyła** żadnych danych na zewnętrzne serwery.
* Kod PIN telefonu podawany jest wyłącznie jako lokalna zmienna środowiskowa (`$env:SCRCPY_ADB_PIN`) i nie jest zapisywany w żadnych plikach repozytorium.
* Pliki tymczasowe stanu są bezpiecznie przechowywane w folderze tymczasowym systemu Windows (`$env:TEMP`).

---

### Licencja

Projekt udostępniany jest na warunkach licencji **MIT**. Szczegóły w pliku [LICENSE](LICENSE).