---
name: release-portable
description: >-
  Buduje przenośny plik wykonywalny ScrcpyManager-Portable.exe ze zintegrowanym
  scrcpy, generuje sumę kontrolną SHA-256, tworzy tag Git i publikuje nowe
  wydanie na GitHub Releases. Używaj, gdy użytkownik prosi o przygotowanie
  nowego wydania (release) lub zbudowanie nowej wersji portable.
---

# Publikacja wydania Portable (`ScrcpyManager-Portable.exe`)

## Kroki

1. **Aktualizacja numeru wersji**:
   Upewnij się, że nowa wersja `X.Y.Z` jest spójna w:
   - `src/UpdateService.cs` (`CurrentVersion`)
   - `ScrcpyManager.csproj` (`Version`, `AssemblyVersion`, `FileVersion`)
   - `ScrcpyApp.ps1` (`$script:appVersion`)

2. **Budowanie `ScrcpyManager-Portable.exe`**:
   Uruchom skrypt budujący:
   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File .\build-portable.ps1
   ```
   - Zweryfikuj w logu wyjściowym linię `Katalog źródłowy scrcpy: ...`, upewniając się, że wybrano najwyższą dostępną wersję `scrcpy` (np. `v5.0.1`).
   - Skrypt automatycznie obsługuje przypadek, gdy poprzedni `ScrcpyManager-Portable.exe` jest uruchomiony (przez zmianę nazwy zablokowanego pliku w `$env:TEMP`).

3. **Wygenerowanie pliku sumy kontrolnej SHA-256**:
   Oblicz hash SHA-256 (małymi literami) i zapisz plik `ScrcpyManager-Portable.exe.sha256` w formacie:
   ```text
   <sha256_lowercase>  ScrcpyManager-Portable.exe
   ```

4. **Commit, tag i publikacja na GitHubie**:
   - Zatwierdź zmodyfikowane pliki źródłowe i utwórz tag `vX.Y.Z`.
   - Wypchnij gałąź `main` oraz tag `vX.Y.Z` do `origin`.
   - Utwórz wydanie GitHub Release z dwoma załącznikami (`ScrcpyManager-Portable.exe` oraz `ScrcpyManager-Portable.exe.sha256`) oraz dwujęzycznym opisem zmian (PL/EN):
     ```powershell
     gh release create vX.Y.Z ScrcpyManager-Portable.exe ScrcpyManager-Portable.exe.sha256 --title "scrcpy Manager vX.Y.Z - Portable Edition (scrcpy v5.0.1)" --notes-file release_notes.md
     ```
