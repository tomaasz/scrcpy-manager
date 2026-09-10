# scrcpy Manager 📱🖥️

> **Universal Windows desktop companion for `scrcpy` and Android power users.**  
> Launch Android apps in dedicated floating windows, manage virtual displays, switch seamlessly to Wireless ADB (Wi‑Fi), prevent screen sleep, and streamline Remote Desktop workflows.

<p align="center">
  <img src="docs/screenshot.png" alt="scrcpy Manager Screenshot" width="360" />
</p>

<p align="center">
  <a href="https://microsoft.com/PowerShell"><img src="https://img.shields.io/badge/PowerShell-5.1%20%7C%207%2B-blue.svg" alt="PowerShell" /></a>
  <a href="https://github.com/Genymobile/scrcpy"><img src="https://img.shields.io/badge/scrcpy-v4.1%2B-brightgreen.svg" alt="scrcpy" /></a>
  <a href="https://github.com/tomaasz/scrcpy-manager/releases"><img src="https://img.shields.io/badge/Release-Portable%20Edition-orange.svg" alt="Portable Release" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-yellow.svg" alt="License: MIT" /></a>
  <img src="https://img.shields.io/badge/Languages-PL%20%7C%20EN%20%7C%20DE%20%7C%20ES-lightgrey.svg" alt="Languages" />
</p>

---

## ⚡ Instant Download (Portable Edition)

👉 **[Download ScrcpyManager-Portable.exe (Latest Release)](https://github.com/tomaasz/scrcpy-manager/releases)**

* **Zero setup & zero dependencies**: Bundles `scrcpy v4.1`, Android Debug Bridge (`adb`), and all required libraries inside a single standalone executable.
* **No installation**: Runs immediately on any Windows 10/11 computer without installing `scrcpy` or modifying system `PATH`.
* **Zero console window**: Clean native desktop GUI experience with no flashing command prompts.

---

## 🌟 Key Features

* 🚀 **Multi-Window Floating Apps**: Launch any Android app in an independent virtual display window (`--new-display`).
* 🎨 **Native App Icons**: Extracts official application icons directly from your phone and Google Play, displaying crisp icons on your custom launch tiles.
* 🔎 **Real-Time Package Search**: Instant search and filtering when picking apps from your connected phone.
* 📶 **One-Click Wireless ADB (Wi‑Fi)**: Automatically detect your phone's Wi‑Fi IP and switch from USB cable to wireless debugging in one click.
* 🔊 **Audio Passthrough Control**: Toggle real-time audio forwarding from your Android device to PC speakers/headphones.
* ⌨️ **Hardware Keyboard (UHID) & Diacritics**: Full native support for international characters and AltGr shortcuts with direct hardware keyboard simulation (`-K`).
* 🖥️ **High-Resolution RDP Presets**:
  * `Full HD 1080p (Native 1:1)` – Pixel-sharp 1:1 scaling.
  * `2K QHD (2560x1440)` – +77% expanded desktop workspace.
  * `4K UHD (3840x2160)` – Maximum productivity view.
  * *High bitrate (16 Mbps)* enabled for razor-sharp text and fonts.
* 📐 **Flexible Tile Layouts**: Switch between 1-column list, 2-column standard, and 3-column compact view with persistent user preferences.
* 🌓 **Dark & Light Mode**: 1-click theme switcher (`☀️ Light` / `🌙 Dark`) with smooth custom controls.
* 🌐 **Multilingual (4 Languages)**: Instant on-the-fly toggling between 🇵🇱 Polish, 🇬🇧 English, 🇩🇪 German, and 🇪🇸 Spanish.
* 🔋 **Real-Time Status Card**: Connection status dot, battery level with charging state, and keep-awake monitor.

---

## 🚀 Quick Start (Running from Source)

If you prefer running the script directly instead of downloading the portable `.exe`:

### Requirements
1. **Windows 10 / 11** (PowerShell 5.1 or PowerShell 7+)
2. **[scrcpy](https://github.com/Genymobile/scrcpy)** & **adb** available in system `PATH`
3. Android device with **USB Debugging** enabled

### Instructions
1. Clone the repository:
   ```bash
   git clone https://github.com/tomaasz/scrcpy-manager.git
   cd scrcpy-manager
   ```
2. Double-click `ScrcpyApp.cmd` or launch in PowerShell:
   ```powershell
   & .\ScrcpyApp.ps1
   ```

---

## ⚙️ Customizing App Buttons (`apps.json`)

`apps.json` defines the bundled default button layout. You can also use the in-app **Edit** button to load applications from your connected phone, add, remove, rename, and rearrange buttons. User customizations are safely preserved in `%APPDATA%\scrcpy-manager\apps.json`.

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
    "name": "Windows App (RDP)",
    "package": "com.microsoft.rdc.androidx",
    "flags": ["-UseUhidKeyboard", "-ForwardAllClicks"]
  }
]
```

---

<details>
<summary><b>🇵🇱 Dokumentacja po polsku (Kliknij tutaj, aby rozwinąć)</b></summary>

<br>

### Najważniejsze funkcje
* **Aplikacje w osobnych oknach**: Uruchamianie aplikacji Androida w niezależnych, pływających oknach (`--new-display`).
* **Wersja przenośna (Portable)**: Gotowy, samodzielny plik `ScrcpyManager-Portable.exe` w zakładce [Releases](https://github.com/tomaasz/scrcpy-manager/releases) ze zintegrowanym `scrcpy v4.1` i `adb` (działa od razu bez instalacji czegokolwiek).
* **Ikony aplikacji na kafelkach**: Błyskawiczne pobieranie oryginalnych ikon aplikacji bezpośrednio z telefonu oraz Google Play.
* **Wyszukiwarka pakietów na żywo**: Błyskawiczne filtrowanie aplikacji zainstalowanych na telefonie w oknie edycji.
* **Bezprzewodowe ADB jednym kliknięciem**: Automatyczne wykrycie IP telefonu w sieci domowej/biurowej i przełączenie na tryb Wi‑Fi (`adb connect <IP>:5555`).
* **Przełącznik przesyłania dźwięku**: Opcja włączenia lub wyciszenia dźwięku (`--no-audio`) z poziomu panelu.
* **Pełna obsługa polskich znaków i klawiatury fizycznej**: Wszystkie aplikacje uruchamiają się z obsługą sprzętowej klawiatury UHID (`-K`), dzięki czemu prawy Alt (`AltGr`) działa natywnie.
* **Optymalizacje dla Windows App (RDP)**:
  * Gotowe profile rozdzielczości (Full HD 1:1, 2K QHD, 4K UHD).
  * Podniesiony bitrate do 16 Mbps dla ostrych czcionek na zdalnym pulpicie.
  * Przycisk szybkiej naprawy schowka między 3 maszynami (PC lokalny $\leftrightarrow$ Telefon $\leftrightarrow$ PC zdalny).
* **Automatyczne czuwanie i odblokowywanie**: Telefon nie blokuje się podczas pracy z panelem lub oknami scrcpy, a po zakończeniu przywracany jest pierwotny limit wygaszania.
* **Tryb Ciemny i Jasny**: Przełączanie motywu jednym kliknięciem.
* **Obsługa 4 języków**: Szybkie przełączanie między polskim, angielskim, niemieckim i hiszpańskim.
* **Dostosowanie układu kafelków**: Wybór między listą (1 kolumna), siatką 2-kolumnową i zwartą 3-kolumnową z zapamiętywaniem w preferencjach.

### Bezpieczeństwo i prywatność
* Skrypt **nie przechowuje ani nie wysyła** żadnych danych na zewnętrzne serwery.
* Kod PIN telefonu podawany jest wyłącznie jako lokalna zmienna środowiskowa (`$env:SCRCPY_ADB_PIN`) i nie jest zapisywany w plikach repozytorium.
* Pliki tymczasowe stanu są bezpiecznie przechowywane w folderze tymczasowym systemu Windows (`$env:TEMP`).

</details>

---

## 📜 License

This project is licensed under the **MIT License**. See [LICENSE](LICENSE) for details.