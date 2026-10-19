# Reguły projektowe — scrcpy Manager

## 1. Obsługa Windows App (`com.microsoft.rdc.androidx`) i sesji RDP
- **Tryb myszy (`--mouse=sdk` + `scrcpy-server-mouse`)**:
  - W oryginalnym `scrcpy-server` (`com.genymobile.scrcpy.control.Controller.injectTouch` w `classes.dex`) lewy przycisk myszy (`BUTTON_PRIMARY`) jest maskowany (`& ~MotionEvent.BUTTON_PRIMARY`, instrukcja Dalvik `and-int/lit8 v9, v9, -2` / `DD 09 09 FE`) i zamieniany na dotyk ekranowy (`InputDevice.SOURCE_TOUCHSCREEN`, `TOOL_TYPE_FINGER`, `buttons = 0`). Powoduje to brak możliwości zaznaczania tekstu przeciąganiem myszy w Windows App (RDP).
  - Nie wymuszaj domyślnie `--mouse=uhid` dla wirtualnych ekranów (`--new-display`), ponieważ na Androidzie < 15 kursor UHID nie przypisuje się do wirtualnego wyświetlacza, a ponadto przechwytuje kursor hosta w trybie względnym.
  - Dla `com.microsoft.rdc.androidx` oraz profili z `--mouse-bind=++++` (`forwardAllClicks`) używaj `AdbService.EnsureDesktopMouseServerFile`, który generuje `scrcpy-server-mouse` (zamiana `0xFE` na `0xFF` we wzorcu `96 09 04 1C DD 09 09 FE 12 1C 38 09 04 00` oraz aktualizacja sum `SHA-1` i `Adler-32` w nagłówku DEX) i przekazuje go przez zmienną środowiskową `SCRCPY_SERVER_PATH`.
  - Nie nadpisuj jawnie wybranego przez użytkownika `profile.mouseMode` przy przełączaniu rozdzielczości dwumonitorowych (`3840x1080`, `5120x1440`).
- **Skróty klawiszowe i schowek**:
  - Dla sesji RDP i klawiatury `uhid` stosuj `--shortcut-mod=lalt,rctrl`, aby działały skróty `Lewy Alt` (`Alt+C`, `Alt+V`, `Alt+F`) oraz `Prawy Ctrl`, pozostawiając klawisz `Win` (`lsuper`) dla zdalnego systemu Windows.
  - Przy starcie `com.microsoft.rdc.androidx` oraz w funkcji naprawy schowka ustawiaj `adb shell settings put system clipboard_format_priority 1`.

## 2. Spójność wersji
Przy każdym podbiciu wersji aplikacji zaktualizuj jednocześnie:
1. `src/UpdateService.cs` (`public const string CurrentVersion = "X.Y.Z";`)
2. `ScrcpyManager.csproj` (`<Version>`, `<AssemblyVersion>`, `<FileVersion>`)
3. `ScrcpyApp.ps1` (`$script:appVersion = "X.Y.Z"`)
