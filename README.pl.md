# scrcpy Manager 📱🖥️

> **Uniwersalny pulpitowy menedżer dla `scrcpy` oraz zaawansowanych użytkowników Androida.**  
> Uruchamiaj aplikacje Androida w dedykowanych oknach, zarządzaj wirtualnymi ekranami, przełączaj się bezprzewodowo na Wi‑Fi (ADB), zapobiegaj usypianiu ekranu i optymalizuj pracę ze zdalnym pulpitem (RDP).

<p align="center">
  <b>🌐 Language / Język / Sprache / Idioma:</b><br>
  <a href="README.md">🇬🇧 English</a> &nbsp;|&nbsp;
  <b><a href="README.pl.md">🇵🇱 Polski</a></b> &nbsp;|&nbsp;
  <a href="README.de.md">🇩🇪 Deutsch</a> &nbsp;|&nbsp;
  <a href="README.es.md">🇪🇸 Español</a>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/C%23-Native%20WinForms-239120.svg" alt="C# Native" />
  <a href="https://github.com/Genymobile/scrcpy"><img src="https://img.shields.io/badge/scrcpy-v4.1%2B-brightgreen.svg" alt="scrcpy" /></a>
  <a href="https://github.com/tomaasz/scrcpy-manager/releases"><img src="https://img.shields.io/badge/Wydanie-Wersja%20Portable-orange.svg" alt="Wydanie Portable" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/Licencja-MIT-yellow.svg" alt="Licencja: MIT" /></a>
  <img src="https://img.shields.io/badge/Języki-PL%20%7C%20EN%20%7C%20DE%20%7C%20ES-lightgrey.svg" alt="Języki" />
</p>

<p align="center">
  <img src="docs/screenshot.pl.png" alt="Zrzut ekranu scrcpy Manager" width="360" />
</p>

---

## ⚡ Błyskawiczne pobieranie (Wersja Portable)

👉 **[Pobierz ScrcpyManager-Portable.exe (Najnowsze wydanie)](https://github.com/tomaasz/scrcpy-manager/releases)**

* **Zero instalacji i brak zależności**: Zawiera w jednym pliku `scrcpy v4.1`, Android Debug Bridge (`adb`) oraz wszystkie biblioteki DLL.
* **Działa od razu**: Uruchamia się na dowolnym komputerze z Windows 10/11 bez potrzeby wcześniejszej instalacji `scrcpy` czy konfiguracji zmiennej `PATH`.
* **Czysty interfejs**: Brak migających czarnych okien konsoli (natywna aplikacja okienkowa GUI).
* **Błyskawiczny start**: Po jednokrotnym rozpakowaniu wersjonowana pamięć podręczna, weryfikowana przez SHA-256, startuje w ułamku sekundy (< 0.2 s).

---

## 🌟 Najważniejsze funkcje

* 🚀 **Aplikacje w niezależnych oknach**: Uruchamiaj dowolne aplikacje Androida w osobnych, pływających oknach (`--new-display`).
* 🎨 **Dedykowane ikony okien w Windows**: Każde uruchomione okno aplikacji otrzymuje własną oficjalną ikonę na pasku zadań i w przełączniku zadań Windows (Alt+Tab), zamiast generycznej ikony `scrcpy`.
* 🖥️ **Nowoczesny, odświeżony interfejs**: Elegancki grafitowy motyw z delikatnie zaokrąglonymi przyciskami, wygładzaniem krawędzi (anti-aliasing) i subtelnymi efektami hover.
* ⚙️ **Profile uruchamiania per aplikacja**: Dla każdego kafelka zapisz rozdzielczość/DPI, FPS, bitrate, kodek, orientację, klawiaturę, mysz, dźwięk, pełny ekran, okno bez ramek, tryb „zawsze na wierzchu” oraz widoczność paska zadań.
* 🎛️ **Gotowe presety**: Domyślny, Praca/RDP, Terminal (wysokie DPI dla idealnej czytelności), Gra, Oszczędny Wi-Fi i Prezentacja.
* 🛡️ **Kontrola paska zadań Androida (Taskbar)**: Opcjonalne ukrywanie dolnego paska zadań (`--no-vd-system-decorations`), dzięki czemu zmaksymalizowane okna aplikacji nie są przesłaniane od dołu.
* 🎥 **Nagrywanie sesji**: Zapisuj sesje wybranych aplikacji jako pliki MP4 z datą w katalogu `Wideo\scrcpy-manager`.
* 🔎 **Wyszukiwarka pakietów w czasie rzeczywistym**: Błyskawiczne wyszukiwanie i filtrowanie pakietów zainstalowanych na telefonie.
* 📶 **Bezprzewodowe ADB (Wi‑Fi) jednym kliknięciem**: Automatycznie wykrywa adres IP telefonu w sieci domowej i przełącza połączenie z kabla USB na Wi-Fi.
* 🔊 **Sterowanie dźwiękiem**: Wygodny przełącznik przesyłania dźwięku z telefonu do głośników lub słuchawek komputera.
* ⌨️ **Klawiatura fizyczna (UHID) i polskie znaki**: Pełna natywna obsługa prawego Alt (`AltGr + a, e, c, s, l, z, x, o, n`) dzięki sprzętowej symulacji klawiatury (`-K`).
* 🖥️ **Zoptymalizowane profile RDP (Windows App)**:
  * `Full HD 1080p (Natywna 1:1)` – Idealna ostrość piksel w piksel.
  * `2K QHD (2560x1440)` – O 77% większa przestrzeń robocza.
  * `4K UHD (3840x2160)` – Maksymalna rozdzielczość dla dużych ekranów.
  * *Wysoki bitrate (16 Mbps)* gwarantujący żyletkowo ostre czcionki.
* 🔙 **Wygodna nawigacja i klawisz ESC**: Naciśnięcie klawisza `ESC` w oknie dowolnej aplikacji Androida natychmiast wywołuje `Cofnij` (to samo co strzałka `←` w aplikacji).
* 🧭 **Zadokowany pasek nawigacyjny okien**: Pływający dolny pasek z przyciskami `◀` (Cofnij), `●` (Ekran główny) i `▢` (Ostatnie) przyczepiony do okna scrcpy.
* 📱 **Przyciski nawigacji w panelu**: Szybkie sterowanie telefonem (`◀ Cofnij`, `● Home`, `▢ Ostatnie`) bezpośrednio z poziomu managera.
* 📐 **Elastyczny układ kafelków**: Przełączaj jednym kliknięciem między listą (1 kolumna), siatką 2-kolumnową i zwartą siatką 3-kolumnową.
* 🌓 **Tryb Ciemny i Jasny**: Szybkie przełączanie motywu (`☀️ Jasny` / `🌙 Ciemny`) z dedykowanymi kontrolkami.
* 🌐 **Wielojęzyczność (4 języki)**: Błyskawiczna zmiana języka (🇵🇱 Polski, 🇬🇧 Angielski, 🇩🇪 Niemiecki, 🇪🇸 Hiszpański) z zapamiętywaniem w preferencjach.
* 🔄 **Automatyczne aktualizacje w aplikacji**: Wykrywanie nowych wydań i aktualizacja wersji Portable 1 kliknięciem bez utraty ustawień.
* 🔋 **Karta stanu urządzenia**: Wskaźnik połączenia, poziom naładowania baterii ze stanem ładowania oraz status czuwania.

---

## 🚀 Szybki start (Uruchamianie ze źródeł)

Jeśli wolisz uruchamiać skrypt bezpośrednio zamiast pobierać gotowy plik `.exe`:

### Wymagania
1. **Windows 10 / 11** (PowerShell 5.1 lub PowerShell 7+)
2. Zainstalowane narzędzia **[scrcpy](https://github.com/Genymobile/scrcpy)** oraz **adb** w systemowej zmiennej `PATH`
3. Telefon z Androidem z włączonym **debugowaniem USB**

### Instrukcja
1. Sklonuj repozytorium:
   ```bash
   git clone https://github.com/tomaasz/scrcpy-manager.git
   cd scrcpy-manager
   ```
2. Kliknij dwukrotnie plik `ScrcpyApp.cmd` lub uruchom w konsoli PowerShell:
   ```powershell
   & .\ScrcpyApp.ps1
   ```

---

## ⚙️ Dostosowywanie przycisków aplikacji (`apps.json`)

Plik `apps.json` definiuje domyślny zestaw kafelków. Za pomocą przycisku **Edytuj** w programie możesz pobrać aplikacje z podłączonego telefonu, dodawać nowe, zmieniać nazwy, usuwać i przestawiać kolejność. Twoje modyfikacje są bezpiecznie zapisywane w `%APPDATA%\scrcpy-manager\apps.json`.

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

## 🙏 Podziękowania i wykorzystane oprogramowanie

Projekt powstał w oparciu o wspaniałe rozwiązania open-source, którym należą się ogromne podziękowania:

* **[scrcpy](https://github.com/Genymobile/scrcpy)** – autor: [Romain Vimont (@rom1v)](https://github.com/rom1v) & [Genymobile](https://github.com/Genymobile) (Licencja Apache 2.0)  
  *Główny silnik wyświetlania ekranu telefonu o ultraniskim opóźnieniu, tworzenia wirtualnych wyświetlaczy, sprzętowej emulacji klawiatury UHID i przesyłania audio.*
* **[Android Debug Bridge (ADB)](https://developer.android.com/tools/adb)** – autor: Google & Android Open Source Project (AOSP) (Licencja Apache 2.0)  
  *Fundament komunikacji z urządzeniami Android przez kabel USB i Wi-Fi.*
* **[Taskbar](https://github.com/farmerbb/Taskbar)** – autor: [Braden Farmer (farmerbb)](https://github.com/farmerbb) (Licencja Apache 2.0)  
  *Aplikacja paska zadań i menu Start w środowisku okienkowym Androida.*
* **[Windows Forms & User32 CueBanner](https://learn.microsoft.com/en-us/windows/win32/controls/em-setcuebanner)** – autor: Microsoft  
  *Natywna obsługa tekstów pomocniczych (placeholder) w polach tekstowych Windows.*
* **[Google Play Store API](https://play.google.com)**  
  *Źródło oficjalnych ikon w wysokiej rozdzielczości dla pakietów z wektorowymi grafikami.*

---

## 📜 Licencja

Projekt udostępniany jest na warunkach licencji **MIT**. Szczegóły w pliku [LICENSE](LICENSE).
