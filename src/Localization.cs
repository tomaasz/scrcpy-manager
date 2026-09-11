using System;
using System.Collections.Generic;

namespace ScrcpyManager
{
    public static class Localization
    {
        private static readonly Dictionary<string, Dictionary<string, string>> _table = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        static Localization()
        {
            // POLISH (PL)
            var pl = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "StatusConnectedUsb", "połączono przez USB" },
                { "StatusConnectedWifi", "połączono przez Wi‑Fi" },
                { "StatusNoPhone", "Brak połączenia z telefonem" },
                { "StatusCheckConn", "Podłącz kabel USB lub sprawdź sieć Wi‑Fi" },
                { "BatteryLabel", "Bateria:" },
                { "ChargingStr", " (ładowanie)" },
                { "StatusActive", "Czuwanie aktywne" },
                { "ThemeDark", "☀ Jasny" },
                { "ThemeLight", "☾ Ciemny" },
                { "ThemeTooltip", "Zmień motyw programu\nPrzełącza pomiędzy motywem ciemnym a jasnym." },
                { "LangSwitch", "EN" },
                { "LangTooltip", "Język: Polski (kliknij, aby zmienić na EN / DE / ES)" },
                { "DeviceStatusTooltip", "Status podłączonego telefonu\nWyświetla model urządzenia, stan naładowania baterii\noraz aktywny tryb połączenia (USB lub Wi‑Fi)." },

                { "LaunchHero", "▶  Uruchom scrcpy" },
                { "LaunchHeroTooltip", "Uruchom pełny ekran scrcpy\nOtwiera pełny pulpit Androida lub ekran telefonu w oknie na PC.\n\n💡 Wskazówka: Aby otworzyć pojedynczą aplikację, kliknij bezpośrednio\nw jej kafelek poniżej – nie musisz najpierw wciskać tego przycisku!" },
                { "FullScreenOpt", "Pełny ekran (-f)" },
                { "FullScreenTooltip", "Pełny ekran (-f)\nUruchamia okno scrcpy w trybie pełnoekranowym na monitorze komputera." },

                { "SectionOptions", "OBRAZ, DŹWIĘK I STEROWANIE" },
                { "ResLabel", "Rozdzielczość wirtualnego ekranu (RDP):" },
                { "ResTooltip", "Rozdzielczość wirtualnego ekranu (RDP)\nOkreśla wymiary i gęstość DPI dla okien aplikacji.\n• 2K QHD (zalecane): wysoka ostrość tekstu i wygoda pracy." },
                { "AudioPass", "Przesyłaj dźwięk do PC" },
                { "AudioTooltip", "Przesyłaj dźwięk do PC\nPrzekierowuje strumień audio z telefonu\ndo głośników komputera w czasie rzeczywistym." },
                { "AutoTaskbar", "Uruchamiaj Taskbar" },
                { "AutoTaskbarTooltip", "Dolny pasek zadań Androida (Taskbar)\n• Włączone: wyświetla dolny pasek zadań na wirtualnym ekranie.\n• Wyłączone (zalecane): ukrywa pasek (--no-vd-system-decorations),\ndzięki czemu zmaksymalizowane okna nie są przesłaniane od dołu." },
                { "InstallTaskbar", "⬇ Zainstaluj Taskbar" },
                { "InstallTaskbarTooltip", "Aplikacja Taskbar nie jest zainstalowana na telefonie.\nKliknij, aby pobrać oficjalną wersję (farmerbb)\ni zainstalować na urządzeniu przez ADB." },
                { "InstallingTaskbar", "Instalowanie Taskbar..." },
                { "MsgTaskbarInstalled", "Aplikacja Taskbar została pomyślnie zainstalowana i skonfigurowana na telefonie!" },
                { "MsgTaskbarInstallFailed", "Nie udało się automatycznie zainstalować aplikacji Taskbar: {0}\n\nCzy chcesz otworzyć stronę aplikacji w Google Play na telefonie?" },
                { "WifiBtn", "Połącz przez Wi‑Fi" },
                { "WifiTooltip", "Połącz bezprzewodowo przez Wi‑Fi\nPrzełącza ADB na połączenie sieciowe (port 5555).\nPozwala sterować telefonem bez kabla USB w tej samej sieci Wi‑Fi." },
                { "KeyBtn", "Klawiatura fizyczna" },
                { "KeyFixTooltip", "Konfiguracja klawiatury fizycznej\nOtwiera ustawienia klawiatury fizycznej w telefonie.\nUpewnij się, że wybrano układ 'Polski (programisty)'." },
                { "ClipBtn", "Naprawa schowka" },
                { "ClipFixTooltip", "Naprawa synchronizacji schowka\nOptymalizuje priorytety schowka i kopiuje skrypt naprawczy\ndla sesji pulpitu zdalnego (RDP).\n• Alt + V : wklej schowek PC do sesji\n• Alt + C : pobierz schowek do PC" },
                { "OptNavBar", "Pasek nawigacji okien" },
                { "OptNavBarTooltip", "Pasek nawigacji okien\nWyświetla dotykowy pasek nawigacyjny (Cofnij, Home, Ostatnie)\npod oknem każdej uruchomionej aplikacji." },
                { "SectionNavigation", "NAWIGACJA ANDROIDA" },
                { "NavBack", "◀  Cofnij (Esc)" },
                { "NavBackTooltip", "Cofnij (Esc / Keycode 4)\nSymuluje wciśnięcie przycisku 'Wstecz' na telefonie." },
                { "NavHome", "●  Ekran główny" },
                { "NavHomeTooltip", "Ekran główny (Home / Keycode 3)\nMinimalizuje aplikacje i przechodzi do ekranu głównego." },
                { "NavRecents", "▢  Ostatnie" },
                { "NavRecentsTooltip", "Ostatnie aplikacje (Recents / Keycode 187)\nOtwiera listę ostatnio uruchamianych aplikacji." },

                { "SectionApps", "APLIKACJE W OKNACH" },
                { "AppsSubtitle", "Uruchom w oknie lub kliknij [Edytuj], by dostosować listę" },
                { "AppsSectionTooltip", "Każda aplikacja uruchamia się bezpośrednio we własnym, niezależnym oknie scrcpy.\n\n💡 Nie musisz wcześniej klikać 'Uruchom scrcpy' ani 'Włącz tryb pulpitu'!" },
                { "AppsStandaloneHint", "💡 Bezpośredni start: nie wymaga \"Uruchom scrcpy\" ani trybu pulpitu." },
                { "AppsEdit", "Edytuj" },
                { "AppsEditTooltip", "Dostosuj listę: dodawaj aplikacje z telefonu, zmieniaj kolejność i wczytuj popularne szablony" },
                { "AppsAddPrompt", "+ Dodaj aplikacje z telefonu (kliknij tutaj lub [Edytuj])" },
                { "CustomLabel", "Inny pakiet Androida (np. com.spotify.music):" },
                { "CustomPlaceholder", "Szukaj pakietu (np. spotify, maps)..." },
                { "CustomSearchTooltip", "Szukaj lub wpisz nazwę pakietu Androida\nNp. com.spotify.music, maps itp.\nMożesz uruchomić pakiet jednorazowo lub dodać stały kafelek." },
                { "CustomBtn", "Uruchom" },
                { "CustomLaunchTooltip", "Uruchom aplikację w oknie\nUruchamia podany wyżej pakiet w niezależnym wirtualnym oknie scrcpy." },
                { "CustomAddBtn", "+ Dodaj" },
                { "CustomAddTooltip", "Dodaj wpisany/wybrany pakiet jako stały kafelek na liście aplikacji" },
                { "AppsRemoveTooltip", "Usuń kafelek '{0}' z listy" },
                { "AppsRenameTooltip", "Zmień nazwę kafelka '{0}'" },
                { "MsgConfirmRemoveApp", "Czy na pewno chcesz usunąć kafelek '{0}' z listy aplikacji?" },
                { "AddAppDialogTitle", "Dodaj kafelek aplikacji" },
                { "AddAppDialogLabel", "Podaj nazwę dla nowego kafelka:" },
                { "AddAppDialogAdd", "+ Dodaj kafelek" },
                { "RenameAppDialogTitle", "Zmień nazwę kafelka" },
                { "RenameAppDialogLabel", "Podaj nową nazwę dla kafelka:" },
                { "RenameAppDialogSave", "Zapisz" },
                { "AppsRenameItem", "✎ Zmień nazwę..." },
                { "AppsDeleteItem", "✕ Usuń kafelek" },
                { "Layout1Tooltip", "Układ: Lista (1 kolumna)" },
                { "Layout2Tooltip", "Układ: Dwukolumnowy (standard)" },
                { "Layout3Tooltip", "Układ: Trzykolumnowy (zwarty)" },
                { "MsgAppAlreadyExists", "Pakiet '{0}' znajduje się już na liście pod nazwą '{1}'!" },

                { "AppsEditorTitle", "Edytuj aplikacje w oknach" },
                { "AppsEditorIntro", "Twój układ przycisków jest zapisywany osobno dla użytkownika Windows." },
                { "AppsEditorPhoneList", "Wybierz aplikację pobraną z telefonu" },
                { "AppsEditorSearchPlaceholder", "Szukaj pakietu (np. spotify, vivaldi, messenger)..." },
                { "AppsEditorNoMatches", "Brak pasujących pakietów" },
                { "AppsEditorRefresh", "Odśwież" },
                { "AppsEditorName", "Nazwa" },
                { "AppsEditorPackage", "Pakiet Androida" },
                { "AppsEditorUhid", "Klawiatura" },
                { "AppsEditorClicks", "Wszystkie kliknięcia" },
                { "AppsEditorAdd", "+ Dodaj" },
                { "AppsEditorRemove", "Usuń" },
                { "AppsEditorUp", "W górę" },
                { "AppsEditorDown", "W dół" },
                { "AppsEditorPopular", "★ Popularne aplikacje" },
                { "AppsEditorSave", "Zapisz" },
                { "AppsEditorCancel", "Anuluj" },
                { "MsgAppsInvalid", "Każdy wpis musi mieć nazwę i poprawny pakiet Androida (np. com.spotify.music)." },
                { "MsgAppsEmpty", "Lista musi zawierać co najmniej jedną aplikację." },
                { "MsgAppsSaveError", "Nie udało się zapisać układu użytkownika:\n{0}" },
                { "MsgAppsPhoneError", "Nie udało się pobrać aplikacji z telefonu. Sprawdź połączenie ADB." },
                { "MsgLoadPopular", "Czy chcesz dodać zestaw popularnych aplikacji (YouTube, Spotify, Chrome, WhatsApp, Messenger itp.) do swojej listy?" },
                { "MsgEmptyAppsPrompt", "Twoja lista aplikacji jest pusta. Czy chcesz wczytać zestaw popularnych aplikacji?" },
                { "DiscoverTitle", "Wykrywanie aplikacji" },
                { "MsgDiscoverPrompt", "Wykryto podłączony telefon ({0}).\n\nCzy chcesz, aby scrcpy Manager sprawdził aplikacje zainstalowane na Twoim telefonie i automatycznie dodał kafelki dla popularnych aplikacji (np. YouTube, WhatsApp, Chrome, Ustawienia itp.)?" },

                { "SectionDevice", "OPERACJE NA URZĄDZENIU" },
                { "DesktopMode", "Włącz tryb pulpitu" },
                { "DesktopModeTooltip", "Włącz tryb pulpitu Androida\nWymusza systemowy tryb pulpitu (ustawienie deweloperskie).\nZmienia DPI na 250 i przyspiesza animacje systemowe.\n\n💡 Wskazówka: Kafelki aplikacji powyżej tworzą pływające okna\nautomatycznie – nie wymagają włączania trybu pulpitu!" },
                { "RestoreDefault", "Przywróć standardowy widok" },
                { "RestoreDefaultTooltip", "Przywróć standardowy widok telefonu\nPrzywraca domyślne DPI telefonu, resetuje skalę animacji,\nwłącza automatyczny obrót ekranu i zamyka aplikację Taskbar." },
                { "RebootBtn", "⚠  Uruchom telefon ponownie" },
                { "RebootTooltip", "Uruchom telefon ponownie (adb reboot)\nWysyła bezpieczne polecenie restartu do telefonu.\nWymaga wcześniejszego potwierdzenia." },

                { "TitleRestartConfirm", "Potwierdzenie restartu" },
                { "MsgRestartConfirm", "Czy na pewno chcesz uruchomić telefon ponownie?" },
                { "MsgRestartSent", "Polecenie restartu zostało wysłane do telefonu." },
                { "MsgNoDevice", "Nie wykryto podłączonego telefonu.\nSprawdź kabel USB, debugowanie USB w telefonie lub połączenie Wi‑Fi." },
                { "MsgDesktopOn", "Zastosowano niski DPI (250), przyspieszono animacje i włączono tryb okienkowy na telefonie." },
                { "MsgResetDone", "Przywrócono domyślne DPI telefonu, zresetowano animacje i zamknięto Taskbar." },
                { "MsgClipDone", "Zastosowano optymalizacje schowka dla telefonu.\nPolecenie naprawcze dla zdalnego PC skopiowano do schowka:\n{0}\n\nSkróty w oknie:\n• Alt + V : Wklej schowek PC do sesji\n• Alt + C : Pobierz schowek sesji do PC" },
                { "MsgKeyDone", "Otwarto ustawienia klawiatury fizycznej w telefonie.\nUpewnij się, że układ klawiatury fizycznej 'scrcpy' ma zaznaczone 'Polski (programisty)'." },
                { "MsgWifiNoIp", "Nie udało się automatycznie wykryć adresu IP telefonu w sieci Wi‑Fi.\nUpewnij się, że telefon jest połączony z tą samą siecią Wi‑Fi co komputer." },
                { "MsgWifiDone", "Połączono bezprzewodowo z telefonem:\n{0}:5555\n\nMożesz teraz odłączyć kabel USB!" },
                { "MsgPkgEmpty", "Wpisz poprawną nazwę pakietu Androida." },

                { "UpdateBadge", "⬆ Dostępna aktualizacja" },
                { "UpdateDialogTitle", "Aktualizacja scrcpy Manager" },
                { "UpdateDialogHeader", "Dostępna jest nowa wersja programu!" },
                { "UpdateDialogCurrent", "Twoja wersja: {0}" },
                { "UpdateDialogLatest", "Najnowsza wersja: {0}" },
                { "UpdateDialogNotes", "Lista zmian:" },
                { "UpdateBtnInstall", "⚡ Zaktualizuj teraz" },
                { "UpdateBtnDownload", "🌐 Strona wydania" },
                { "UpdateBtnLater", "Później" },
                { "UpdateDownloading", "Pobieranie..." },
                { "UpdateInstallSuccess", "Pobrano aktualizację. Aplikacja zostanie zrestartowana." },
                { "UpdateFailed", "Nie udało się zaktualizować automatycznie: {0}\nCzy chcesz otworzyć stronę wydania w przeglądarce?" },

                { "ResName0", "Full HD 1080p (Natywna 1:1)" },
                { "ResName1", "2K QHD (2560x1440)" },
                { "ResName2", "2K QHD Kompakt (DPI 140)" },
                { "ResName3", "4K UHD (3840x2160)" },
                { "ResName4", "Domyślna telefonu" }
            };
            _table["PL"] = pl;

            // ENGLISH (EN)
            var en = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "StatusConnectedUsb", "connected via USB" },
                { "StatusConnectedWifi", "connected via Wi‑Fi" },
                { "StatusNoPhone", "No phone connected" },
                { "StatusCheckConn", "Connect USB cable or check Wi‑Fi network" },
                { "BatteryLabel", "Battery:" },
                { "ChargingStr", " (charging)" },
                { "StatusActive", "Keep-awake active" },
                { "ThemeDark", "☀ Light" },
                { "ThemeLight", "☾ Dark" },
                { "ThemeTooltip", "Toggle application theme\nSwitches between dark and light appearance." },
                { "LangSwitch", "DE" },
                { "LangTooltip", "Language: English (click to switch to DE / ES / PL)" },
                { "DeviceStatusTooltip", "Connected device status\nDisplays phone model, battery charge level,\nand active connection type (USB or Wi‑Fi)." },

                { "LaunchHero", "▶  Launch scrcpy" },
                { "LaunchHeroTooltip", "Launch full scrcpy screen\nOpens full Android desktop or phone screen in a PC window.\n\n💡 Tip: To launch a single app, click its tile below directly\nwithout needing to click this button first!" },
                { "FullScreenOpt", "Fullscreen (-f)" },
                { "FullScreenTooltip", "Fullscreen mode (-f)\nLaunches scrcpy in fullscreen mode on your PC monitor." },

                { "SectionOptions", "DISPLAY, AUDIO & CONTROL" },
                { "ResLabel", "Virtual display resolution (RDP):" },
                { "ResTooltip", "Virtual display resolution (RDP)\nSets display dimensions and DPI for windowed apps.\n• 2K QHD (recommended): crisp text and comfortable workspace." },
                { "AudioPass", "Forward audio to PC" },
                { "AudioTooltip", "Forward audio to PC\nRoutes phone audio stream to PC speakers in real time." },
                { "AutoTaskbar", "Start Taskbar" },
                { "AutoTaskbarTooltip", "Android bottom taskbar\n• Enabled: displays Android system taskbar on the virtual screen.\n• Disabled (recommended): hides taskbar (--no-vd-system-decorations)\nso maximized application windows are not obscured from below." },
                { "InstallTaskbar", "⬇ Install Taskbar" },
                { "InstallTaskbarTooltip", "Taskbar app is not installed on your phone.\nClick to download official APK (farmerbb)\nand install on device via ADB." },
                { "InstallingTaskbar", "Installing Taskbar..." },
                { "MsgTaskbarInstalled", "Taskbar app has been successfully installed and configured on your phone!" },
                { "MsgTaskbarInstallFailed", "Failed to automatically install Taskbar app: {0}\n\nWould you like to open Taskbar in Google Play on your phone?" },
                { "WifiBtn", "Connect via Wi‑Fi" },
                { "WifiTooltip", "Connect wirelessly via Wi‑Fi\nSwitches ADB debugging to network mode (port 5555).\nAllows controlling phone without USB cable on the same Wi‑Fi network." },
                { "KeyBtn", "Hardware keyboard" },
                { "KeyFixTooltip", "Hardware keyboard settings\nOpens physical keyboard settings on the phone.\nEnsure physical keyboard layout is configured correctly for your language." },
                { "ClipBtn", "Fix clipboard" },
                { "ClipFixTooltip", "Fix clipboard synchronization\nOptimizes Android clipboard priority and copies PC helper script\nfor Remote Desktop (RDP) sessions.\n• Alt + V : paste PC clipboard into session\n• Alt + C : copy session clipboard to PC" },
                { "OptNavBar", "Window navigation bar" },
                { "OptNavBarTooltip", "Window navigation bar\nShows bottom navigation bar (Back, Home, Recents)\nbeneath each running application window." },
                { "SectionNavigation", "ANDROID NAVIGATION" },
                { "NavBack", "◀  Back (Esc)" },
                { "NavBackTooltip", "Back (Esc / Keycode 4)\nSimulates pressing the Back button on the phone." },
                { "NavHome", "●  Home" },
                { "NavHomeTooltip", "Home screen (Home / Keycode 3)\nMinimizes apps and returns to the phone home screen." },
                { "NavRecents", "▢  Recents" },
                { "NavRecentsTooltip", "Recent applications (Recents / Keycode 187)\nOpens the Android recent tasks overview." },

                { "SectionApps", "WINDOWED APPLICATIONS" },
                { "AppsSubtitle", "Run in window or click [Edit] to customize your list" },
                { "AppsSectionTooltip", "Each application launches directly in its own independent scrcpy window.\n\n💡 You do not need to click 'Launch scrcpy' or 'Enable desktop mode' first!" },
                { "AppsStandaloneHint", "💡 Direct launch: does not require 'Launch scrcpy' or desktop mode." },
                { "AppsEdit", "Edit" },
                { "AppsEditTooltip", "Customize list: add apps from phone, reorder, or load popular presets" },
                { "AppsAddPrompt", "+ Add apps from phone (click here or [Edit])" },
                { "CustomLabel", "Custom Android package (e.g. com.spotify.music):" },
                { "CustomPlaceholder", "Search package (e.g. spotify, maps)..." },
                { "CustomSearchTooltip", "Search or enter Android package name\nE.g. com.spotify.music, maps, etc.\nYou can launch it once or add it as a permanent tile." },
                { "CustomBtn", "Launch" },
                { "CustomLaunchTooltip", "Launch application in window\nRuns the specified package in an independent virtual scrcpy window." },
                { "CustomAddBtn", "+ Add" },
                { "CustomAddTooltip", "Add the typed/selected package as a permanent tile in the app list" },
                { "AppsRemoveTooltip", "Remove '{0}' tile from the list" },
                { "AppsRenameTooltip", "Rename '{0}' tile" },
                { "MsgConfirmRemoveApp", "Are you sure you want to remove the '{0}' tile from the application list?" },
                { "AddAppDialogTitle", "Add Application Tile" },
                { "AddAppDialogLabel", "Enter a name for the new tile:" },
                { "AddAppDialogAdd", "+ Add Tile" },
                { "RenameAppDialogTitle", "Rename Application Tile" },
                { "RenameAppDialogLabel", "Enter a new name for the tile:" },
                { "RenameAppDialogSave", "Save" },
                { "AppsRenameItem", "✎ Rename..." },
                { "AppsDeleteItem", "✕ Delete tile" },
                { "Layout1Tooltip", "Layout: List (1 column)" },
                { "Layout2Tooltip", "Layout: Two columns (standard)" },
                { "Layout3Tooltip", "Layout: Three columns (compact)" },
                { "MsgAppAlreadyExists", "Package '{0}' is already in the list as '{1}'!" },

                { "AppsEditorTitle", "Edit windowed applications" },
                { "AppsEditorIntro", "Your button layout is stored separately for each Windows user." },
                { "AppsEditorPhoneList", "Select an application loaded from the phone" },
                { "AppsEditorSearchPlaceholder", "Search package (e.g. spotify, vivaldi, messenger)..." },
                { "AppsEditorNoMatches", "No matching packages" },
                { "AppsEditorRefresh", "Refresh" },
                { "AppsEditorName", "Name" },
                { "AppsEditorPackage", "Android package" },
                { "AppsEditorUhid", "Keyboard" },
                { "AppsEditorClicks", "All clicks" },
                { "AppsEditorAdd", "+ Add" },
                { "AppsEditorRemove", "Remove" },
                { "AppsEditorUp", "Move up" },
                { "AppsEditorDown", "Move down" },
                { "AppsEditorPopular", "★ Popular Apps" },
                { "AppsEditorSave", "Save" },
                { "AppsEditorCancel", "Cancel" },
                { "MsgAppsInvalid", "Every entry needs a name and a valid Android package (e.g. com.spotify.music)." },
                { "MsgAppsEmpty", "The list must contain at least one application." },
                { "MsgAppsSaveError", "Could not save the user layout:\n{0}" },
                { "MsgAppsPhoneError", "Could not load applications from the phone. Check the ADB connection." },
                { "MsgLoadPopular", "Do you want to add a set of 10 popular apps (YouTube, Spotify, Chrome, WhatsApp, Messenger, Maps, etc.) to your list?" },
                { "MsgEmptyAppsPrompt", "Your app list is empty. Would you like to load a set of 10 popular apps (YouTube, Spotify, Chrome, etc.)?" },
                { "DiscoverTitle", "App Discovery" },
                { "MsgDiscoverPrompt", "Connected phone detected ({0}).\n\nWould you like scrcpy Manager to check the apps installed on your phone and automatically add tiles for popular apps (e.g. YouTube, WhatsApp, Chrome, Settings, etc.)?" },

                { "SectionDevice", "DEVICE OPERATIONS" },
                { "DesktopMode", "Enable desktop mode" },
                { "DesktopModeTooltip", "Enable Android desktop mode\nForces system desktop mode on phone (developer setting).\nSets display density to 250 DPI and accelerates animations.\n\n💡 Tip: App tiles above launch floating windows automatically\nwithout needing desktop mode!" },
                { "RestoreDefault", "Restore default view" },
                { "RestoreDefaultTooltip", "Restore standard phone view\nRestores default phone DPI, resets system animation scales,\nenables automatic screen rotation, and closes Taskbar." },
                { "RebootBtn", "⚠  Restart phone" },
                { "RebootTooltip", "Reboot phone (adb reboot)\nSends safe reboot command to connected Android device.\nRequires confirmation before restarting." },

                { "TitleRestartConfirm", "Confirm Restart" },
                { "MsgRestartConfirm", "Are you sure you want to restart the phone?" },
                { "MsgRestartSent", "Restart command has been sent to the phone." },
                { "MsgNoDevice", "No Android device detected.\nPlease check USB cable, USB debugging, or Wi‑Fi connection." },
                { "MsgDesktopOn", "Low DPI (250) applied, animations accelerated, and freeform mode enabled on phone." },
                { "MsgResetDone", "Default DPI restored, animations reset, and Taskbar closed." },
                { "MsgClipDone", "Clipboard optimizations applied.\nRemote PC repair command copied to clipboard:\n{0}\n\nWindow Shortcuts:\n• Alt + V : Paste PC clipboard to session\n• Alt + C : Copy session clipboard to PC" },
                { "MsgKeyDone", "Physical keyboard settings opened on phone.\nEnsure 'scrcpy' hardware keyboard layout is set to your preferred layout." },
                { "MsgWifiNoIp", "Could not automatically detect phone IP on Wi‑Fi.\nEnsure your phone is connected to the same Wi‑Fi network as this PC." },
                { "MsgWifiDone", "Connected wirelessly to phone:\n{0}:5555\n\nYou can now disconnect the USB cable!" },
                { "MsgPkgEmpty", "Please enter a valid Android package name." },

                { "UpdateBadge", "⬆ Update available" },
                { "UpdateDialogTitle", "scrcpy Manager Update" },
                { "UpdateDialogHeader", "A new version of scrcpy Manager is available!" },
                { "UpdateDialogCurrent", "Current version: {0}" },
                { "UpdateDialogLatest", "Latest version: {0}" },
                { "UpdateDialogNotes", "Release notes:" },
                { "UpdateBtnInstall", "⚡ Update Now" },
                { "UpdateBtnDownload", "🌐 Release Page" },
                { "UpdateBtnLater", "Later" },
                { "UpdateDownloading", "Downloading..." },
                { "UpdateInstallSuccess", "Update downloaded. The application will restart." },
                { "UpdateFailed", "Automatic update failed: {0}\nWould you like to open the release page in your browser?" },

                { "ResName0", "Full HD 1080p (Native 1:1)" },
                { "ResName1", "2K QHD (2560x1440)" },
                { "ResName2", "2K QHD Compact (DPI 140)" },
                { "ResName3", "4K UHD (3840x2160)" },
                { "ResName4", "Phone Default" }
            };
            _table["EN"] = en;

            // GERMAN (DE)
            var de = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "StatusConnectedUsb", "über USB verbunden" },
                { "StatusConnectedWifi", "über WLAN verbunden" },
                { "StatusNoPhone", "Kein Telefon verbunden" },
                { "StatusCheckConn", "USB-Kabel anschließen oder WLAN prüfen" },
                { "BatteryLabel", "Akku:" },
                { "ChargingStr", " (lädt)" },
                { "StatusActive", "Wachmodus aktiv" },
                { "ThemeDark", "☀ Hell" },
                { "ThemeLight", "☾ Dunkel" },
                { "ThemeTooltip", "Design wechseln\nSchaltet zwischen dunklem und hellem Modus um." },
                { "LangSwitch", "ES" },
                { "LangTooltip", "Sprache: Deutsch (Klicken für Wechsel zu ES / PL / EN)" },
                { "DeviceStatusTooltip", "Status des verbundenen Geräts\nZeigt Telefonmodell, Akkustand und\naktiven Verbindungstyp (USB oder WLAN) an." },

                { "LaunchHero", "▶  scrcpy starten" },
                { "LaunchHeroTooltip", "Vollständigen Bildschirm starten\nÖffnet den Android-Desktop oder Telefonbildschirm im PC-Fenster.\n\n💡 Tipp: Um eine einzelne App zu starten, klicken Sie direkt\nauf ihre Kachel unten – dieser Button ist dafür nicht nötig!" },
                { "FullScreenOpt", "Vollbild (-f)" },
                { "FullScreenTooltip", "Vollbildmodus (-f)\nStartet scrcpy im Vollbildmodus auf dem PC-Monitor." },

                { "SectionOptions", "ANZEIGE, AUDIO & STEUERUNG" },
                { "ResLabel", "Virtuelle Bildschirmauflösung (RDP):" },
                { "ResTooltip", "Virtuelle Bildschirmauflösung (RDP)\nLegt Auflösung und DPI für Anwendungsfenster fest.\n• 2K QHD (empfohlen): scharfer Text und optimale Arbeitsfläche." },
                { "AudioPass", "Audio an PC übertragen" },
                { "AudioTooltip", "Audio an PC übertragen\nLeitet den Audiostream des Telefons in Echtzeit\nan die PC-Lautsprecher weiter." },
                { "AutoTaskbar", "Taskbar starten" },
                { "AutoTaskbarTooltip", "Android-Taskleiste\n• Aktiviert: zeigt die System-Taskleiste auf dem virtuellen Bildschirm an.\n• Deaktiviert (empfohlen): blendet die Leiste aus (--no-vd-system-decorations),\ndamit maximierte Fenster unten nicht verdeckt werden." },
                { "InstallTaskbar", "⬇ Taskbar installieren" },
                { "InstallTaskbarTooltip", "Die Taskbar-App ist nicht auf dem Telefon installiert.\nKlicken Sie hier, um das offizielle APK (farmerbb)\nherunterzuladen und über ADB zu installieren." },
                { "InstallingTaskbar", "Taskbar wird installiert..." },
                { "MsgTaskbarInstalled", "Die Taskbar-App wurde erfolgreich auf Ihrem Telefon installiert und eingerichtet!" },
                { "MsgTaskbarInstallFailed", "Taskbar-App konnte nicht automatisch installiert werden: {0}\n\nMöchten Sie Taskbar im Google Play Store auf Ihrem Telefon öffnen?" },
                { "WifiBtn", "Über WLAN verbinden" },
                { "WifiTooltip", "Drahtlos über WLAN verbinden\nSchaltet ADB-Debugging in den Netzwerkmodus (Port 5555).\nErmöglicht die Steuerung ohne USB-Kabel im selben WLAN." },
                { "KeyBtn", "Physische Tastatur" },
                { "KeyFixTooltip", "Physische Tastatur einrichten\nÖffnet die Einstellungen für die externe Tastatur auf dem Telefon.\nStellen Sie sicher, dass das passende Tastaturlayout gewählt ist." },
                { "ClipBtn", "Zwischenablage reparieren" },
                { "ClipFixTooltip", "Zwischenablage synchronisieren\nOptimiert die Android-Zwischenablage und kopiert PC-Reparaturskript\nfür Remote-Desktop-Sitzungen (RDP).\n• Alt + V : PC-Zwischenablage einfügen\n• Alt + C : Sitzungszwischenablage kopieren" },
                { "OptNavBar", "Fenster-Navigationsleiste" },
                { "OptNavBarTooltip", "Fenster-Navigationsleiste\nZeigt Navigationsleiste (Zurück, Start, Apps)\nunter jedem laufenden Anwendungsfenster an." },
                { "SectionNavigation", "ANDROID-NAVIGATION" },
                { "NavBack", "◀  Zurück (Esc)" },
                { "NavBackTooltip", "Zurück (Esc / Keycode 4)\nSimuliert das Drücken der Zurück-Taste auf dem Telefon." },
                { "NavHome", "●  Start" },
                { "NavHomeTooltip", "Startbildschirm (Home / Keycode 3)\nMinimiert Apps und kehrt zum Startbildschirm zurück." },
                { "NavRecents", "▢  Apps" },
                { "NavRecentsTooltip", "Letzte Apps (Recents / Keycode 187)\nÖffnet die Android-App-Übersicht." },

                { "SectionApps", "ANWENDUNGEN IN FENSTERN" },
                { "AppsSubtitle", "Im Fenster starten oder [Bearbeiten] zum Anpassen" },
                { "AppsSectionTooltip", "Jede App startet direkt in einem eigenen, unabhängigen scrcpy-Fenster.\n\n💡 Sie müssen vorher nicht auf 'scrcpy starten' oder 'Desktop-Modus' klicken!" },
                { "AppsStandaloneHint", "💡 Direktstart: erfordert weder 'scrcpy starten' noch den Desktop-Modus." },
                { "AppsEdit", "Bearbeiten" },
                { "AppsEditTooltip", "Liste anpassen: Apps vom Telefon hinzufügen, sortieren oder Vorlagen laden" },
                { "AppsAddPrompt", "+ Apps vom Telefon hinzufügen (hier oder [Bearbeiten] klicken)" },
                { "CustomLabel", "Anderes Android-Paket (z. B. com.spotify.music):" },
                { "CustomPlaceholder", "Paket suchen (z. B. spotify, maps)..." },
                { "CustomSearchTooltip", "Android-Paket suchen oder eingeben\nZ. B. com.spotify.music, maps usw.\nDirekt starten oder als dauerhafte Kachel hinzufügen." },
                { "CustomBtn", "Starten" },
                { "CustomLaunchTooltip", "App im Fenster starten\nStartet das angegebene Paket in einem separaten scrcpy-Fenster." },
                { "CustomAddBtn", "+ Hinzufügen" },
                { "CustomAddTooltip", "Eingegebenes/ausgewähltes Paket als Kachel zur App-Liste hinzufügen" },
                { "AppsRemoveTooltip", "Kachel '{0}' aus der Liste entfernen" },
                { "AppsRenameTooltip", "Kachel '{0}' umbenennen" },
                { "MsgConfirmRemoveApp", "Möchten Sie die Kachel '{0}' wirklich aus der App-Liste entfernen?" },
                { "AddAppDialogTitle", "App-Kachel hinzufügen" },
                { "AddAppDialogLabel", "Geben Sie einen Namen für die neue Kachel ein:" },
                { "AddAppDialogAdd", "+ Kachel hinzufügen" },
                { "RenameAppDialogTitle", "App-Kachel umbenennen" },
                { "RenameAppDialogLabel", "Geben Sie einen neuen Namen für die Kachel ein:" },
                { "RenameAppDialogSave", "Speichern" },
                { "AppsRenameItem", "✎ Umbenennen..." },
                { "AppsDeleteItem", "✕ Kachel löschen" },
                { "Layout1Tooltip", "Layout: Liste (1 Spalte)" },
                { "Layout2Tooltip", "Layout: Zweispaltig (Standard)" },
                { "Layout3Tooltip", "Layout: Dreispaltig (Kompakt)" },
                { "MsgAppAlreadyExists", "Das Paket '{0}' ist bereits in der Liste als '{1}' vorhanden!" },

                { "AppsEditorTitle", "Fenster-Apps bearbeiten" },
                { "AppsEditorIntro", "Ihr Tastenlayout wird separat für jeden Windows-Benutzer gespeichert." },
                { "AppsEditorPhoneList", "Vom Telefon geladene App auswählen" },
                { "AppsEditorSearchPlaceholder", "Paket suchen (z. B. spotify, vivaldi, messenger)..." },
                { "AppsEditorNoMatches", "Keine passenden Pakete gefunden" },
                { "AppsEditorRefresh", "Aktualisieren" },
                { "AppsEditorName", "Name" },
                { "AppsEditorPackage", "Android-Paket" },
                { "AppsEditorUhid", "Tastatur" },
                { "AppsEditorClicks", "Alle Klicks" },
                { "AppsEditorAdd", "+ Hinzufügen" },
                { "AppsEditorRemove", "Entfernen" },
                { "AppsEditorUp", "Nach oben" },
                { "AppsEditorDown", "Nach unten" },
                { "AppsEditorPopular", "★ Beliebte Apps" },
                { "AppsEditorSave", "Speichern" },
                { "AppsEditorCancel", "Abbrechen" },
                { "MsgAppsInvalid", "Jeder Eintrag benötigt einen Namen und ein gültiges Android-Paket (z. B. com.spotify.music)." },
                { "MsgAppsEmpty", "Die Liste muss mindestens eine Anwendung enthalten." },
                { "MsgAppsSaveError", "Benutzerlayout konnte nicht gespeichert werden:\n{0}" },
                { "MsgAppsPhoneError", "Apps konnten nicht vom Telefon geladen werden. ADB-Verbindung prüfen." },
                { "MsgLoadPopular", "Möchten Sie eine Auswahl von 10 beliebten Apps (YouTube, Spotify, Chrome, WhatsApp usw.) hinzufügen?" },
                { "MsgEmptyAppsPrompt", "Ihre App-Liste ist leer. Möchten Sie 10 beliebte Apps (YouTube, Spotify, Chrome usw.) laden?" },
                { "DiscoverTitle", "Apps erkennen" },
                { "MsgDiscoverPrompt", "Verbundenes Telefon erkannt ({0}).\n\nMöchten Sie, dass scrcpy Manager die auf Ihrem Telefon installierten Apps überprüft und automatisch Kacheln für beliebte Apps hinzufügt (z. B. YouTube, WhatsApp, Chrome, Einstellungen usw.)?" },

                { "SectionDevice", "GERÄTEOPERATIONEN" },
                { "DesktopMode", "Desktop-Modus aktivieren" },
                { "DesktopModeTooltip", "Android Desktop-Modus aktivieren\nErzwingt den Desktop-Modus auf dem Telefon (Entwickleroption).\nStellt die Pixeldichte auf 250 DPI und beschleunigt Animationen.\n\n💡 Tipp: Die App-Kacheln oben öffnen Fenster automatisch,\nohne dass der Desktop-Modus aktiviert werden muss!" },
                { "RestoreDefault", "Standardansicht wiederherstellen" },
                { "RestoreDefaultTooltip", "Standard-Telefonansicht wiederherstellen\nStellt Standard-DPI wieder her, setzt Animationen zurück,\naktiviert automatische Bildschirmdrehung und schließt Taskbar." },
                { "RebootBtn", "⚠  Telefon neu starten" },
                { "RebootTooltip", "Telefon neu starten (adb reboot)\nSendet sicheren Neustartbefehl an das verbundene Telefon.\nErfordert vorherige Bestätigung." },

                { "TitleRestartConfirm", "Neustart bestätigen" },
                { "MsgRestartConfirm", "Möchten Sie das Telefon wirklich neu starten?" },
                { "MsgRestartSent", "Neustartbefehl wurde an das Telefon gesendet." },
                { "MsgNoDevice", "Kein Android-Gerät erkannt.\nBitte USB-Kabel, USB-Debugging oder WLAN-Verbindung prüfen." },
                { "MsgDesktopOn", "Niedrige DPI (250) angewendet, Animationen beschleunigt und Fenstermodus aktiviert." },
                { "MsgResetDone", "Standard-DPI wiederhergestellt, Animationen zurückgesetzt und Taskbar geschlossen." },
                { "MsgClipDone", "Zwischenablage-Optimierungen angewendet.\nReparaturbefehl für Remote-PC in Zwischenablage kopiert:\n{0}\n\nFenster-Tastenkombinationen:\n• Alt + V : PC-Zwischenablage in Sitzung einfügen\n• Alt + C : Sitzungs-Zwischenablage auf PC kopieren" },
                { "MsgKeyDone", "Tastatureinstellungen auf dem Telefon geöffnet.\nStellen Sie sicher, dass das Layout der physischen Tastatur 'scrcpy' passend eingestellt ist." },
                { "MsgWifiNoIp", "Telefon-IP konnte im WLAN nicht automatisch ermittelt werden.\nStellen Sie sicher, dass das Telefon im selben WLAN ist." },
                { "MsgWifiDone", "Drahtlos mit Telefon verbunden:\n{0}:5555\n\nSie können das USB-Kabel jetzt trennen!" },
                { "MsgPkgEmpty", "Bitte geben Sie einen gültigen Android-Paketnamen ein." },

                { "UpdateBadge", "⬆ Update verfügbar" },
                { "UpdateDialogTitle", "scrcpy Manager Aktualisierung" },
                { "UpdateDialogHeader", "Eine neue Version von scrcpy Manager ist verfügbar!" },
                { "UpdateDialogCurrent", "Aktuelle Version: {0}" },
                { "UpdateDialogLatest", "Neueste Version: {0}" },
                { "UpdateDialogNotes", "Änderungsprotokoll:" },
                { "UpdateBtnInstall", "⚡ Jetzt aktualisieren" },
                { "UpdateBtnDownload", "🌐 Release-Seite" },
                { "UpdateBtnLater", "Später" },
                { "UpdateDownloading", "Wird heruntergeladen..." },
                { "UpdateInstallSuccess", "Update heruntergeladen. Die Anwendung wird neu gestartet." },
                { "UpdateFailed", "Automatisches Update fehlgeschlagen: {0}\nMöchten Sie die Release-Seite im Browser öffnen?" },

                { "ResName0", "Full HD 1080p (Nativ 1:1)" },
                { "ResName1", "2K QHD (2560x1440)" },
                { "ResName2", "2K QHD Kompakt (DPI 140)" },
                { "ResName3", "4K UHD (3840x2160)" },
                { "ResName4", "Telefon-Standard" }
            };
            _table["DE"] = de;

            // SPANISH (ES)
            var es = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "StatusConnectedUsb", "conectado por USB" },
                { "StatusConnectedWifi", "conectado por Wi‑Fi" },
                { "StatusNoPhone", "Sin conexión con el teléfono" },
                { "StatusCheckConn", "Conecte el cable USB o compruebe la red Wi‑Fi" },
                { "BatteryLabel", "Batería:" },
                { "ChargingStr", " (cargando)" },
                { "StatusActive", "Modo activo" },
                { "ThemeDark", "☀ Claro" },
                { "ThemeLight", "☾ Oscuro" },
                { "ThemeTooltip", "Cambiar tema del programa\nAlterna entre la apariencia oscura y clara." },
                { "LangSwitch", "PL" },
                { "LangTooltip", "Idioma: Español (clic para cambiar a PL / EN / DE)" },
                { "DeviceStatusTooltip", "Estado del teléfono conectado\nMuestra el modelo del dispositivo, nivel de batería\ny modo de conexión activo (USB o Wi‑Fi)." },

                { "LaunchHero", "▶  Iniciar scrcpy" },
                { "LaunchHeroTooltip", "Iniciar pantalla completa de scrcpy\nAbre el escritorio Android o la pantalla del teléfono en PC.\n\n💡 Consejo: Para abrir una sola app, haz clic directamente\nen su mosaico abajo – ¡no necesitas pulsar este botón primero!" },
                { "FullScreenOpt", "Pantalla completa (-f)" },
                { "FullScreenTooltip", "Modo de pantalla completa (-f)\nInicia scrcpy a pantalla completa en el monitor del PC." },

                { "SectionOptions", "PANTALLA, AUDIO Y CONTROL" },
                { "ResLabel", "Resolución de pantalla virtual (RDP):" },
                { "ResTooltip", "Resolución de pantalla virtual (RDP)\nDefine dimensiones y DPI para ventanas de aplicaciones.\n• 2K QHD (recomendado): texto nítido y espacio óptimo." },
                { "AudioPass", "Transmitir audio al PC" },
                { "AudioTooltip", "Transmitir audio al PC\nEnvía el sonido del teléfono a los altavoces\ndel PC en tiempo real." },
                { "AutoTaskbar", "Iniciar Taskbar" },
                { "AutoTaskbarTooltip", "Barra de tareas inferior de Android (Taskbar)\n• Activado: muestra la barra de tareas en la pantalla virtual.\n• Desactivado (recomendado): oculta la barra (--no-vd-system-decorations)\npara no tapar las ventanas maximizadas por abajo." },
                { "InstallTaskbar", "⬇ Instalar Taskbar" },
                { "InstallTaskbarTooltip", "La aplicación Taskbar no está instalada en el teléfono.\nHaz clic para descargar e instalar la versión oficial (farmerbb) mediante ADB." },
                { "InstallingTaskbar", "Instalando Taskbar..." },
                { "MsgTaskbarInstalled", "¡La aplicación Taskbar se ha instalado y configurado con éxito en tu teléfono!" },
                { "MsgTaskbarInstallFailed", "No se pudo instalar automáticamente la aplicación Taskbar: {0}\n\n¿Deseas abrir Taskbar en Google Play en tu teléfono?" },
                { "WifiBtn", "Conectar por Wi‑Fi" },
                { "WifiTooltip", "Conectar de forma inalámbrica por Wi‑Fi\nCambia la depuración ADB a modo red (puerto 5555).\nPermite controlar el teléfono sin cable en la misma red Wi‑Fi." },
                { "KeyBtn", "Teclado físico" },
                { "KeyFixTooltip", "Configuración del teclado físico\nAbre los ajustes de teclado físico en el teléfono.\nAsegúrate de seleccionar la distribución correcta de teclado." },
                { "ClipBtn", "Reparar portapapeles" },
                { "ClipFixTooltip", "Reparar sincronización del portapapeles\nOptimiza el portapapeles y copia el script de reparación\npara sesiones de escritorio remoto (RDP).\n• Alt + V : pegar portapapeles del PC en la sesión\n• Alt + C : copiar portapapeles de la sesión al PC" },
                { "OptNavBar", "Barra inferior en ventanas" },
                { "OptNavBarTooltip", "Barra de navegación en ventanas\nMuestra barra táctil (Atrás, Inicio, Recientes)\ndebajo de cada ventana de aplicación abierta." },
                { "SectionNavigation", "NAVEGACIÓN ANDROID" },
                { "NavBack", "◀  Atrás (Esc)" },
                { "NavBackTooltip", "Atrás (Esc / Keycode 4)\nSimula pulsar el botón Atrás en el teléfono." },
                { "NavHome", "●  Inicio" },
                { "NavHomeTooltip", "Pantalla de inicio (Home / Keycode 3)\nMinimiza las aplicaciones y vuelve al inicio." },
                { "NavRecents", "▢  Recientes" },
                { "NavRecentsTooltip", "Aplicaciones recientes (Recents / Keycode 187)\nAbre la vista de aplicaciones recientes." },

                { "SectionApps", "APLICACIONES EN VENTANAS" },
                { "AppsSubtitle", "Abrir en ventana o pulse [Editar] para personalizar" },
                { "AppsSectionTooltip", "Cada aplicación se abre directamente en su propia ventana independiente.\n\n💡 ¡No necesitas pulsar 'Iniciar scrcpy' ni 'Modo escritorio' previamente!" },
                { "AppsStandaloneHint", "💡 Inicio directo: no requiere 'Iniciar scrcpy' ni modo escritorio." },
                { "AppsEdit", "Editar" },
                { "AppsEditTooltip", "Personalizar lista: añadir apps del teléfono, reordenar o cargar populares" },
                { "AppsAddPrompt", "+ Añadir aplicaciones del teléfono (clic aquí o [Editar])" },
                { "CustomLabel", "Otro paquete de Android (ej. com.spotify.music):" },
                { "CustomPlaceholder", "Buscar paquete (ej. spotify, maps)..." },
                { "CustomSearchTooltip", "Buscar o introducir nombre del paquete Android\nEj. com.spotify.music, com.google.android.apps.maps, etc.\nPuedes iniciarlo al instante o añadirlo como botón permanente." },
                { "CustomBtn", "Iniciar" },
                { "CustomLaunchTooltip", "Iniciar aplicación en ventana\nEjecuta el paquete especificado en una ventana virtual independiente." },
                { "CustomAddBtn", "+ Añadir" },
                { "CustomAddTooltip", "Añadir paquete escrito/seleccionado como botón fijo a la lista" },
                { "AppsRemoveTooltip", "Eliminar botón '{0}' de la lista" },
                { "AppsRenameTooltip", "Cambiar nombre del botón '{0}'" },
                { "MsgConfirmRemoveApp", "¿Seguro que desea eliminar '{0}' de la lista de aplicaciones?" },
                { "AddAppDialogTitle", "Añadir botón de aplicación" },
                { "AddAppDialogLabel", "Introduzca el nombre para el nuevo botón:" },
                { "AddAppDialogAdd", "+ Añadir botón" },
                { "RenameAppDialogTitle", "Cambiar nombre del botón" },
                { "RenameAppDialogLabel", "Introduzca un nuevo nombre para el botón:" },
                { "RenameAppDialogSave", "Guardar" },
                { "AppsRenameItem", "✎ Cambiar nombre..." },
                { "AppsDeleteItem", "✕ Eliminar botón" },
                { "Layout1Tooltip", "Diseño: Lista (1 columna)" },
                { "Layout2Tooltip", "Diseño: Dos columnas (estándar)" },
                { "Layout3Tooltip", "Diseño: Tres columnas (compacto)" },
                { "MsgAppAlreadyExists", "¡El paquete '{0}' ya existe en la lista con el nombre '{1}'!" },

                { "AppsEditorTitle", "Editar aplicaciones en ventanas" },
                { "AppsEditorIntro", "El diseño de botones se guarda por separado para cada usuario de Windows." },
                { "AppsEditorPhoneList", "Seleccionar aplicación obtenida del teléfono" },
                { "AppsEditorSearchPlaceholder", "Buscar paquete (ej. spotify, vivaldi, messenger)..." },
                { "AppsEditorNoMatches", "No hay paquetes coincidentes" },
                { "AppsEditorRefresh", "Actualizar" },
                { "AppsEditorName", "Nombre" },
                { "AppsEditorPackage", "Paquete Android" },
                { "AppsEditorUhid", "Teclado" },
                { "AppsEditorClicks", "Todos los clics" },
                { "AppsEditorAdd", "+ Añadir" },
                { "AppsEditorRemove", "Eliminar" },
                { "AppsEditorUp", "Subir" },
                { "AppsEditorDown", "Bajar" },
                { "AppsEditorPopular", "★ Aplicaciones populares" },
                { "AppsEditorSave", "Guardar" },
                { "AppsEditorCancel", "Cancelar" },
                { "MsgAppsInvalid", "Cada entrada necesita un nombre y un paquete válido de Android (ej. com.spotify.music)." },
                { "MsgAppsEmpty", "La lista debe contener al menos una aplicación." },
                { "MsgAppsSaveError", "No se pudo guardar el diseño del usuario:\n{0}" },
                { "MsgAppsPhoneError", "No se pudieron obtener las aplicaciones del teléfono. Compruebe la conexión ADB." },
                { "MsgLoadPopular", "¿Desea añadir una selección de 10 aplicaciones populares (YouTube, Spotify, Chrome, WhatsApp, etc.)?" },
                { "MsgEmptyAppsPrompt", "La lista de aplicaciones está vacía. ¿Desea cargar 10 aplicaciones populares (YouTube, Spotify, Chrome, etc.)?" },
                { "DiscoverTitle", "Detección de aplicaciones" },
                { "MsgDiscoverPrompt", "Teléfono conectado detectado ({0}).\n\n¿Desea que scrcpy Manager compruebe las aplicaciones instaladas en su teléfono y añada automáticamente botones para aplicaciones populares (ej. YouTube, WhatsApp, Chrome, Ajustes, etc.)?" },

                { "SectionDevice", "OPERACIONES DEL DISPOSITIVO" },
                { "DesktopMode", "Activar modo escritorio" },
                { "DesktopModeTooltip", "Activar modo escritorio de Android\nFuerza el modo de escritorio del sistema en el teléfono (ajuste desarrollador).\nAjusta densidad a 250 DPI y acelera animaciones del sistema.\n\n💡 Consejo: Los mosaicos de apps arriba abren ventanas automáticamente,\n¡no requieren activar el modo escritorio!" },
                { "RestoreDefault", "Restaurar vista estándar" },
                { "RestoreDefaultTooltip", "Restaurar vista estándar del teléfono\nRestaura el DPI original, restablece las animaciones,\nactiva la rotación automática y cierra la aplicación Taskbar." },
                { "RebootBtn", "⚠  Reiniciar teléfono" },
                { "RebootTooltip", "Reiniciar teléfono (adb reboot)\nEnvía un comando de reinicio seguro al teléfono Android.\nRequiere confirmación previa." },

                { "TitleRestartConfirm", "Confirmar reinicio" },
                { "MsgRestartConfirm", "¿Está seguro de que desea reiniciar el teléfono?" },
                { "MsgRestartSent", "Se ha enviado el comando de reinicio al teléfono." },
                { "MsgNoDevice", "No se ha detectado ningún teléfono Android.\nCompruebe el cable USB, depuración USB o conexión Wi‑Fi." },
                { "MsgDesktopOn", "DPI bajo (250) aplicado, animaciones aceleradas y modo ventana activado en el teléfono." },
                { "MsgResetDone", "DPI original restaurado, animaciones restablecidas y Taskbar cerrado." },
                { "MsgClipDone", "Optimizaciones de portapapeles aplicadas.\nComando de reparación para PC remoto copiado al portapapeles:\n{0}\n\nAtajos de teclado en ventana:\n• Alt + V : Pegar portapapeles de PC a la sesión\n• Alt + C : Copiar portapapeles de sesión al PC" },
                { "MsgKeyDone", "Ajustes de teclado físico abiertos en el teléfono.\nAsegúrese de que la distribución del teclado 'scrcpy' esté configurada correctamente." },
                { "MsgWifiNoIp", "No se pudo detectar automáticamente la IP del teléfono en Wi‑Fi.\nAsegúrese de que el teléfono esté en la misma red Wi‑Fi que este PC." },
                { "MsgWifiDone", "Conectado de forma inalámbrica al teléfono:\n{0}:5555\n\n¡Ya puede desconectar el cable USB!" },
                { "MsgPkgEmpty", "Por favor, introduzca un nombre de paquete Android válido." },

                { "UpdateBadge", "⬆ Actualización disponible" },
                { "UpdateDialogTitle", "Actualización de scrcpy Manager" },
                { "UpdateDialogHeader", "¡Hay una nueva versión de scrcpy Manager disponible!" },
                { "UpdateDialogCurrent", "Versión actual: {0}" },
                { "UpdateDialogLatest", "Última versión: {0}" },
                { "UpdateDialogNotes", "Notas de la versión:" },
                { "UpdateBtnInstall", "⚡ Actualizar ahora" },
                { "UpdateBtnDownload", "🌐 Página de la versión" },
                { "UpdateBtnLater", "Más tarde" },
                { "UpdateDownloading", "Descargando..." },
                { "UpdateInstallSuccess", "Actualización descargada. La aplicación se reiniciará." },
                { "UpdateFailed", "Error en la actualización automática: {0}\n¿Desea abrir la página de la versión en su navegador?" },

                { "ResName0", "Full HD 1080p (Nativa 1:1)" },
                { "ResName1", "2K QHD (2560x1440)" },
                { "ResName2", "2K QHD Compacta (DPI 140)" },
                { "ResName3", "4K UHD (3840x2160)" },
                { "ResName4", "Predeterminada del teléfono" }
            };
            _table["ES"] = es;
        }

        public static readonly Dictionary<string, string> SupportedLanguages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "PL", "Polski (PL)" },
            { "EN", "English (EN)" },
            { "DE", "Deutsch (DE)" },
            { "ES", "Español (ES)" }
        };

        public static Strings Get(string lang)
        {
            return new Strings(lang);
        }

        public static string GetRaw(string lang, string key, params object[] args)
        {
            if (string.IsNullOrEmpty(lang) || !_table.ContainsKey(lang))
            {
                lang = "PL";
            }

            Dictionary<string, string> dict = _table[lang];
            string val;
            if (!dict.TryGetValue(key, out val))
            {
                if (_table["PL"].TryGetValue(key, out val))
                {
                    // found in PL fallback
                }
                else
                {
                    val = key;
                }
            }

            if (args != null && args.Length > 0)
            {
                try
                {
                    return string.Format(val, args);
                }
                catch
                {
                    return val;
                }
            }

            return val;
        }

        public class Strings
        {
            private readonly string _lang;

            public Strings(string lang)
            {
                _lang = lang;
            }

            public string Get(string key, params object[] args)
            {
                return Localization.GetRaw(_lang, key, args);
            }

            // Status Card
            public string StatusConnectedUsb   { get { return Get("StatusConnectedUsb"); } }
            public string StatusConnectedWifi  { get { return Get("StatusConnectedWifi"); } }
            public string StatusNoPhone        { get { return Get("StatusNoPhone"); } }
            public string StatusCheckConn      { get { return Get("StatusCheckConn"); } }
            public string BatteryLabel         { get { return Get("BatteryLabel"); } }
            public string ChargingStr          { get { return Get("ChargingStr"); } }
            public string StatusActive         { get { return Get("StatusActive"); } }
            public string ThemeDark            { get { return Get("ThemeDark"); } }
            public string ThemeLight           { get { return Get("ThemeLight"); } }
            public string ThemeTooltip         { get { return Get("ThemeTooltip"); } }
            public string LangSwitch           { get { return Get("LangSwitch"); } }
            public string LangTooltip          { get { return Get("LangTooltip"); } }
            public string DeviceStatusTooltip  { get { return Get("DeviceStatusTooltip"); } }

            // Hero & Options
            public string LaunchHero           { get { return Get("LaunchHero"); } }
            public string LaunchHeroTooltip    { get { return Get("LaunchHeroTooltip"); } }
            public string FullScreenOpt        { get { return Get("FullScreenOpt"); } }
            public string FullScreenTooltip    { get { return Get("FullScreenTooltip"); } }
            public string SectionOptions       { get { return Get("SectionOptions"); } }
            public string ResLabel             { get { return Get("ResLabel"); } }
            public string ResTooltip           { get { return Get("ResTooltip"); } }
            public string AudioPass            { get { return Get("AudioPass"); } }
            public string AudioTooltip         { get { return Get("AudioTooltip"); } }
            public string AutoTaskbar          { get { return Get("AutoTaskbar"); } }
            public string AutoTaskbarTooltip   { get { return Get("AutoTaskbarTooltip"); } }
            public string InstallTaskbar       { get { return Get("InstallTaskbar"); } }
            public string InstallTaskbarTooltip { get { return Get("InstallTaskbarTooltip"); } }
            public string InstallingTaskbar    { get { return Get("InstallingTaskbar"); } }
            public string MsgTaskbarInstalled  { get { return Get("MsgTaskbarInstalled"); } }
            public string MsgTaskbarInstallFailed { get { return Get("MsgTaskbarInstallFailed"); } }
            public string WifiBtn              { get { return Get("WifiBtn"); } }
            public string WifiTooltip          { get { return Get("WifiTooltip"); } }
            public string KeyBtn               { get { return Get("KeyBtn"); } }
            public string KeyFixTooltip        { get { return Get("KeyFixTooltip"); } }
            public string ClipBtn              { get { return Get("ClipBtn"); } }
            public string ClipFixTooltip       { get { return Get("ClipFixTooltip"); } }
            public string OptNavBar            { get { return Get("OptNavBar"); } }
            public string OptNavBarTooltip     { get { return Get("OptNavBarTooltip"); } }
            public string SectionNavigation    { get { return Get("SectionNavigation"); } }
            public string NavBack              { get { return Get("NavBack"); } }
            public string NavBackTooltip       { get { return Get("NavBackTooltip"); } }
            public string NavHome              { get { return Get("NavHome"); } }
            public string NavHomeTooltip       { get { return Get("NavHomeTooltip"); } }
            public string NavRecents           { get { return Get("NavRecents"); } }
            public string NavRecentsTooltip    { get { return Get("NavRecentsTooltip"); } }

            // Apps Section
            public string SectionApps          { get { return Get("SectionApps"); } }
            public string AppsSubtitle         { get { return Get("AppsSubtitle"); } }
            public string AppsSectionTooltip   { get { return Get("AppsSectionTooltip"); } }
            public string AppsStandaloneHint   { get { return Get("AppsStandaloneHint"); } }
            public string AppsEdit             { get { return Get("AppsEdit"); } }
            public string AppsEditTooltip      { get { return Get("AppsEditTooltip"); } }
            public string AppsAddPrompt        { get { return Get("AppsAddPrompt"); } }
            public string CustomLabel          { get { return Get("CustomLabel"); } }
            public string CustomPlaceholder    { get { return Get("CustomPlaceholder"); } }
            public string CustomSearchTooltip  { get { return Get("CustomSearchTooltip"); } }
            public string CustomBtn            { get { return Get("CustomBtn"); } }
            public string CustomLaunchTooltip  { get { return Get("CustomLaunchTooltip"); } }
            public string CustomAddBtn         { get { return Get("CustomAddBtn"); } }
            public string CustomAddTooltip     { get { return Get("CustomAddTooltip"); } }
            public string AppsRemoveTooltip    { get { return Get("AppsRemoveTooltip"); } }
            public string AppsRenameTooltip    { get { return Get("AppsRenameTooltip"); } }
            public string MsgConfirmRemoveApp  { get { return Get("MsgConfirmRemoveApp"); } }
            public string AddAppDialogTitle    { get { return Get("AddAppDialogTitle"); } }
            public string AddAppDialogLabel    { get { return Get("AddAppDialogLabel"); } }
            public string AddAppDialogAdd      { get { return Get("AddAppDialogAdd"); } }
            public string RenameAppDialogTitle { get { return Get("RenameAppDialogTitle"); } }
            public string RenameAppDialogLabel { get { return Get("RenameAppDialogLabel"); } }
            public string RenameAppDialogSave  { get { return Get("RenameAppDialogSave"); } }
            public string AppsRenameItem       { get { return Get("AppsRenameItem"); } }
            public string AppsDeleteItem       { get { return Get("AppsDeleteItem"); } }
            public string Layout1Tooltip       { get { return Get("Layout1Tooltip"); } }
            public string Layout2Tooltip       { get { return Get("Layout2Tooltip"); } }
            public string Layout3Tooltip       { get { return Get("Layout3Tooltip"); } }

            // Apps Editor
            public string AppsEditorTitle      { get { return Get("AppsEditorTitle"); } }
            public string AppsEditorIntro      { get { return Get("AppsEditorIntro"); } }
            public string AppsEditorSearchPlaceholder { get { return Get("AppsEditorSearchPlaceholder"); } }
            public string AppsEditorPhoneList  { get { return Get("AppsEditorPhoneList"); } }
            public string AppsEditorNoMatches  { get { return Get("AppsEditorNoMatches"); } }
            public string AppsEditorRefresh    { get { return Get("AppsEditorRefresh"); } }
            public string AppsEditorAdd        { get { return Get("AppsEditorAdd"); } }
            public string AppsEditorName       { get { return Get("AppsEditorName"); } }
            public string AppsEditorPackage    { get { return Get("AppsEditorPackage"); } }
            public string AppsEditorUhid       { get { return Get("AppsEditorUhid"); } }
            public string AppsEditorClicks     { get { return Get("AppsEditorClicks"); } }
            public string AppsEditorRemove     { get { return Get("AppsEditorRemove"); } }
            public string AppsEditorUp         { get { return Get("AppsEditorUp"); } }
            public string AppsEditorDown       { get { return Get("AppsEditorDown"); } }
            public string AppsEditorPopular    { get { return Get("AppsEditorPopular"); } }
            public string AppsEditorCancel     { get { return Get("AppsEditorCancel"); } }
            public string AppsEditorSave       { get { return Get("AppsEditorSave"); } }

            // Device Operations
            public string SectionDevice        { get { return Get("SectionDevice"); } }
            public string DesktopMode          { get { return Get("DesktopMode"); } }
            public string DesktopModeTooltip   { get { return Get("DesktopModeTooltip"); } }
            public string RestoreDefault       { get { return Get("RestoreDefault"); } }
            public string RestoreDefaultTooltip { get { return Get("RestoreDefaultTooltip"); } }
            public string RebootBtn            { get { return Get("RebootBtn"); } }
            public string RebootTooltip        { get { return Get("RebootTooltip"); } }
            public string RebootTitle          { get { return Get("RebootTitle"); } }

            // Messages
            public string MsgNoDevice          { get { return Get("MsgNoDevice"); } }
            public string NoDevice             { get { return Get("NoDevice"); } }
            public string MsgPkgEmpty          { get { return Get("MsgPkgEmpty"); } }
            public string MsgAppAlreadyExists  { get { return Get("MsgAppAlreadyExists"); } }
            public string MsgDesktopOn         { get { return Get("MsgDesktopOn"); } }
            public string MsgResetDone         { get { return Get("MsgResetDone"); } }
            public string MsgConfirmReboot     { get { return Get("MsgConfirmReboot"); } }
            public string MsgKeyDone           { get { return Get("MsgKeyDone"); } }
            public string MsgWifiNoIp          { get { return Get("MsgWifiNoIp"); } }
            public string MsgWifiDone          { get { return Get("MsgWifiDone"); } }
            public string MsgClipDone          { get { return Get("MsgClipDone"); } }
            public string MsgLoadPopular       { get { return Get("MsgLoadPopular"); } }
            public string MsgAppsEmpty         { get { return Get("MsgAppsEmpty"); } }
            public string MsgAppsInvalid       { get { return Get("MsgAppsInvalid"); } }
            public string MsgAppsPhoneError    { get { return Get("MsgAppsPhoneError"); } }
            public string MsgAppsSaveError     { get { return Get("MsgAppsSaveError"); } }
            public string DiscoverTitle        { get { return Get("DiscoverTitle"); } }
            public string MsgDiscoverPrompt    { get { return Get("MsgDiscoverPrompt"); } }

            // Auto-update
            public string UpdateBadge          { get { return Get("UpdateBadge"); } }
            public string UpdateDialogTitle    { get { return Get("UpdateDialogTitle"); } }
            public string UpdateDialogHeader   { get { return Get("UpdateDialogHeader"); } }
            public string UpdateDialogCurrent  { get { return Get("UpdateDialogCurrent"); } }
            public string UpdateDialogLatest   { get { return Get("UpdateDialogLatest"); } }
            public string UpdateDialogNotes    { get { return Get("UpdateDialogNotes"); } }
            public string UpdateBtnInstall     { get { return Get("UpdateBtnInstall"); } }
            public string UpdateBtnDownload    { get { return Get("UpdateBtnDownload"); } }
            public string UpdateBtnLater       { get { return Get("UpdateBtnLater"); } }
            public string UpdateDownloading    { get { return Get("UpdateDownloading"); } }
            public string UpdateInstallSuccess { get { return Get("UpdateInstallSuccess"); } }
            public string UpdateFailed         { get { return Get("UpdateFailed"); } }

            // Resolutions
            public string[] ResNames
            {
                get
                {
                    return new[]
                    {
                        Get("ResName0"),
                        Get("ResName1"),
                        Get("ResName2"),
                        Get("ResName3"),
                        Get("ResName4")
                    };
                }
            }
        }
    }
}
