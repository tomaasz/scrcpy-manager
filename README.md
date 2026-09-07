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

## Funkcje panelu

- **Automatyczny Taskbar**: Panel automatycznie włącza pasek zadań Taskbar i tryb okienkowy na telefonie (możliwość włączenia/wyłączenia przełącznikiem w oknie).
- **Aplikacje w 2 kolumnach (alfabetycznie)**:
  - Claude (`com.anthropic.claude`)
  - ConneckBot (`org.connectbot`)
  - Gmail (Wszystkie) (`com.google.android.gm`)
  - Messenger (`com.facebook.orca`)
  - TurboTel (`ellipi.messenger`)
  - Ustawienia (`com.android.settings`)
  - Vivaldi (`com.vivaldi.browser`)
  - WhatsApp (`com.whatsapp`)
  - Wiadomości (Google) (`com.google.android.apps.messaging`)
  - Windows App (`com.microsoft.rdc.androidx`)
- **Brak wygaszania ekranu**: telefon nie blokuje się tak długo, jak aktywny jest panel lub okna scrcpy.
- **Optymalizacja schowka dla 3 maszyn**: sprzętowy tryb klawiatury (`-K`) i pełne przekazywanie kliknięć myszy (`--mouse-bind=++++`).

---

## Przekazywanie schowka między 3 maszynami (PC lokalny <-> Telefon <-> PC zdalny)

Gdy łączysz się przez `Windows App` (Microsoft Remote Desktop) w scrcpy:

1. **Ustawienia profilu w Windows App (telefon)**:
   - W aplikacji Remote Desktop na telefonie kliknij menu `...` przy nazwie komputera zdalnego $\rightarrow$ **Edit**.
   - Przewiń do sekcji **Devices & Audio Redirection**.
   - Upewnij się, że opcja **Clipboard** (Schowek) jest włączona.

2. **Skróty klawiszowe w oknie Windows App**:
   - `Alt + V` – natychmiastowe wymuszenie przesłania schowka z komputera lokalnego do telefonu/RDP i wklejenie.
   - `Alt + C` – pobranie schowka z telefonu/RDP do komputera lokalnego.
   - Standardowe `Ctrl + C` i `Ctrl + V` działają bezpośrednio na zdalnym pulpicie dzięki emulacji sprzętowej klawiatury (`--keyboard=uhid`).
   - Prawy przycisk myszy wywołuje menu kontekstowe na zdalnym PC (dzięki `--mouse-bind=++++`).

3. **Gdy schowek zawiesi się na zdalnym komputerze Windows**:
   - Na zdalnym komputerze (w Win+R lub wierszu poleceń) zrestartuj proces schowka RDP:
     ```cmd
     taskkill /f /im rdpclip.exe & start rdpclip.exe
     ```
   - Przycisk **„📋 Napraw schowek (3 maszyny)”** w panelu automatycznie kopiuje to polecenie do Twojego schowka i odświeża uprawnienia ADB.