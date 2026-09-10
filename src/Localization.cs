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
                { "LangSwitch", "EN" },
                { "LangTooltip", "Język: Polski (kliknij, aby zmienić na EN / DE / ES)" },

                { "LaunchHero", "▶  Uruchom scrcpy" },
                { "FullScreenOpt", "Pełny ekran (-f)" },

                { "SectionOptions", "OBRAZ, DŹWIĘK I STEROWANIE" },
                { "ResLabel", "Rozdzielczość wirtualnego ekranu (RDP):" },
                { "AudioPass", "Przesyłaj dźwięk do PC" },
                { "AutoTaskbar", "Uruchamiaj Taskbar" },
                { "WifiBtn", "Połącz przez Wi‑Fi" },
                { "KeyBtn", "Klawiatura fizyczna" },
                { "ClipBtn", "Naprawa schowka" },
                { "OptNavBar", "Pasek nawigacji okien" },
                { "OptNavBarTooltip", "Wyświetla dotykowy pasek nawigacyjny (Cofnij, Home, Ostatnie) pod oknem każdej aplikacji" },
                { "SectionNavigation", "NAWIGACJA ANDROIDA" },
                { "NavBack", "◀  Cofnij (Esc)" },
                { "NavHome", "●  Ekran główny" },
                { "NavRecents", "▢  Ostatnie aplikacje" },

                { "SectionApps", "APLIKACJE W OKNACH" },
                { "AppsSubtitle", "Uruchom w oknie lub kliknij [Edytuj], by dostosować listę" },
                { "AppsEdit", "Edytuj" },
                { "AppsEditTooltip", "Dostosuj listę: dodawaj aplikacje z telefonu, zmieniaj kolejność i wczytuj popularne szablony" },
                { "AppsAddPrompt", "+ Dodaj aplikacje z telefonu (kliknij tutaj lub [Edytuj])" },
                { "CustomLabel", "Inny pakiet Androida (np. com.spotify.music):" },
                { "CustomPlaceholder", "Szukaj pakietu (np. spotify, maps)..." },
                { "CustomBtn", "Uruchom" },
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
                { "MsgLoadPopular", "Czy chcesz dodać zestaw 10 popularnych aplikacji (YouTube, Spotify, Chrome, WhatsApp, Messenger, Mapy itp.) do swojej listy?" },
                { "MsgEmptyAppsPrompt", "Twoja lista aplikacji jest pusta. Czy chcesz wczytać zestaw 10 popularnych aplikacji (YouTube, Spotify, Chrome itp.)?" },

                { "SectionDevice", "OPERACJE NA URZĄDZENIU" },
                { "DesktopMode", "Włącz tryb pulpitu" },
                { "RestoreDefault", "Przywróć standardowy widok" },
                { "RebootBtn", "⚠  Uruchom telefon ponownie" },

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
                { "LangSwitch", "DE" },
                { "LangTooltip", "Language: English (click to switch to DE / ES / PL)" },

                { "LaunchHero", "▶  Launch scrcpy" },
                { "FullScreenOpt", "Fullscreen (-f)" },

                { "SectionOptions", "DISPLAY, AUDIO & CONTROL" },
                { "ResLabel", "Virtual display resolution (RDP):" },
                { "AudioPass", "Forward audio to PC" },
                { "AutoTaskbar", "Start Taskbar" },
                { "WifiBtn", "Connect via Wi‑Fi" },
                { "KeyBtn", "Hardware keyboard" },
                { "ClipBtn", "Fix clipboard" },
                { "OptNavBar", "Window navigation bar" },
                { "OptNavBarTooltip", "Shows bottom navigation bar (Back, Home, Recents) beneath each app window" },
                { "SectionNavigation", "ANDROID NAVIGATION" },
                { "NavBack", "◀  Back (Esc)" },
                { "NavHome", "●  Home" },
                { "NavRecents", "▢  Recents" },

                { "SectionApps", "WINDOWED APPLICATIONS" },
                { "AppsSubtitle", "Run in window or click [Edit] to customize your list" },
                { "AppsEdit", "Edit" },
                { "AppsEditTooltip", "Customize list: add apps from phone, reorder, or load popular presets" },
                { "AppsAddPrompt", "+ Add apps from phone (click here or [Edit])" },
                { "CustomLabel", "Custom Android package (e.g. com.spotify.music):" },
                { "CustomPlaceholder", "Search package (e.g. spotify, maps)..." },
                { "CustomBtn", "Launch" },
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

                { "SectionDevice", "DEVICE OPERATIONS" },
                { "DesktopMode", "Enable desktop mode" },
                { "RestoreDefault", "Restore default view" },
                { "RebootBtn", "⚠  Restart phone" },

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
                { "LangSwitch", "ES" },
                { "LangTooltip", "Sprache: Deutsch (Klicken für Wechsel zu ES / PL / EN)" },

                { "LaunchHero", "▶  scrcpy starten" },
                { "FullScreenOpt", "Vollbild (-f)" },

                { "SectionOptions", "ANZEIGE, AUDIO & STEUERUNG" },
                { "ResLabel", "Virtuelle Bildschirmauflösung (RDP):" },
                { "AudioPass", "Audio an PC übertragen" },
                { "AutoTaskbar", "Taskbar starten" },
                { "WifiBtn", "Über WLAN verbinden" },
                { "KeyBtn", "Physische Tastatur" },
                { "ClipBtn", "Zwischenablage reparieren" },
                { "OptNavBar", "Fenster-Navigationsleiste" },
                { "OptNavBarTooltip", "Zeigt Navigationsleiste (Zurück, Start, Apps) unter jedem Anwendungsfenster an" },
                { "SectionNavigation", "ANDROID-NAVIGATION" },
                { "NavBack", "◀  Zurück (Esc)" },
                { "NavHome", "●  Start" },
                { "NavRecents", "▢  Apps" },

                { "SectionApps", "ANWENDUNGEN IN FENSTERN" },
                { "AppsSubtitle", "Im Fenster starten oder [Bearbeiten] zum Anpassen" },
                { "AppsEdit", "Bearbeiten" },
                { "AppsEditTooltip", "Liste anpassen: Apps vom Telefon hinzufügen, sortieren oder Vorlagen laden" },
                { "AppsAddPrompt", "+ Apps vom Telefon hinzufügen (hier oder [Bearbeiten] klicken)" },
                { "CustomLabel", "Anderes Android-Paket (z. B. com.spotify.music):" },
                { "CustomPlaceholder", "Paket suchen (z. B. spotify, maps)..." },
                { "CustomBtn", "Starten" },
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

                { "SectionDevice", "GERÄTEOPERATIONEN" },
                { "DesktopMode", "Desktop-Modus aktivieren" },
                { "RestoreDefault", "Standardansicht wiederherstellen" },
                { "RebootBtn", "⚠  Telefon neu starten" },

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
                { "LangSwitch", "PL" },
                { "LangTooltip", "Idioma: Español (clic para cambiar a PL / EN / DE)" },

                { "LaunchHero", "▶  Iniciar scrcpy" },
                { "FullScreenOpt", "Pantalla completa (-f)" },

                { "SectionOptions", "PANTALLA, AUDIO Y CONTROL" },
                { "ResLabel", "Resolución de pantalla virtual (RDP):" },
                { "AudioPass", "Transmitir audio al PC" },
                { "AutoTaskbar", "Iniciar Taskbar" },
                { "WifiBtn", "Conectar por Wi‑Fi" },
                { "KeyBtn", "Teclado físico" },
                { "ClipBtn", "Reparar portapapeles" },
                { "OptNavBar", "Barra inferior en ventanas" },
                { "OptNavBarTooltip", "Muestra barra táctil de navegación (Atrás, Inicio, Recientes) debajo de cada ventana de app" },
                { "SectionNavigation", "NAVEGACIÓN ANDROID" },
                { "NavBack", "◀  Atrás (Esc)" },
                { "NavHome", "●  Inicio" },
                { "NavRecents", "▢  Recientes" },

                { "SectionApps", "APLICACIONES EN VENTANAS" },
                { "AppsSubtitle", "Abrir en ventana o pulse [Editar] para personalizar" },
                { "AppsEdit", "Editar" },
                { "AppsEditTooltip", "Personalizar lista: añadir apps del teléfono, reordenar o cargar populares" },
                { "AppsAddPrompt", "+ Añadir aplicaciones del teléfono (clic aquí o [Editar])" },
                { "CustomLabel", "Otro paquete de Android (ej. com.spotify.music):" },
                { "CustomPlaceholder", "Buscar paquete (ej. spotify, maps)..." },
                { "CustomBtn", "Iniciar" },
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

                { "SectionDevice", "OPERACIONES DEL DISPOSITIVO" },
                { "DesktopMode", "Activar modo escritorio" },
                { "RestoreDefault", "Restaurar vista estándar" },
                { "RebootBtn", "⚠  Reiniciar teléfono" },

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
            public string LangSwitch           { get { return Get("LangSwitch"); } }
            public string LangTooltip          { get { return Get("LangTooltip"); } }

            // Hero & Options
            public string LaunchHero           { get { return Get("LaunchHero"); } }
            public string FullScreenOpt        { get { return Get("FullScreenOpt"); } }
            public string SectionOptions       { get { return Get("SectionOptions"); } }
            public string ResLabel             { get { return Get("ResLabel"); } }
            public string AudioPass            { get { return Get("AudioPass"); } }
            public string AutoTaskbar          { get { return Get("AutoTaskbar"); } }
            public string WifiBtn              { get { return Get("WifiBtn"); } }
            public string KeyBtn               { get { return Get("KeyBtn"); } }
            public string ClipBtn              { get { return Get("ClipBtn"); } }
            public string OptNavBar            { get { return Get("OptNavBar"); } }
            public string OptNavBarTooltip     { get { return Get("OptNavBarTooltip"); } }
            public string SectionNavigation    { get { return Get("SectionNavigation"); } }
            public string NavBack              { get { return Get("NavBack"); } }
            public string NavHome              { get { return Get("NavHome"); } }
            public string NavRecents           { get { return Get("NavRecents"); } }

            // Apps Section
            public string SectionApps          { get { return Get("SectionApps"); } }
            public string AppsSubtitle         { get { return Get("AppsSubtitle"); } }
            public string AppsEdit             { get { return Get("AppsEdit"); } }
            public string AppsEditTooltip      { get { return Get("AppsEditTooltip"); } }
            public string AppsAddPrompt        { get { return Get("AppsAddPrompt"); } }
            public string CustomLabel          { get { return Get("CustomLabel"); } }
            public string CustomPlaceholder    { get { return Get("CustomPlaceholder"); } }
            public string CustomBtn            { get { return Get("CustomBtn"); } }
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
            public string RestoreDefault       { get { return Get("RestoreDefault"); } }
            public string RebootBtn            { get { return Get("RebootBtn"); } }
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
