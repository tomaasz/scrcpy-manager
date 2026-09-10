using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace ScrcpyPortable
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                // Obsługa parametru tworzenia skrótu
                if (args.Length > 0 && (args[0].Equals("--shortcut", StringComparison.OrdinalIgnoreCase) || args[0].Equals("-shortcut", StringComparison.OrdinalIgnoreCase)))
                {
                    CreateDesktopShortcut();
                    return;
                }

                // Ścieżka docelowa dla wyodrębnionego środowiska wykonawczego
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string runtimeDir = Path.Combine(localAppData, "scrcpy-manager", "runtime");
                string versionFile = Path.Combine(runtimeDir, ".version");
                string scriptPath = Path.Combine(runtimeDir, "ScrcpyApp.ps1");

                // Pobierz osadzony zasób ZIP
                Assembly asm = Assembly.GetExecutingAssembly();
                string resourceName = "bundle.zip";

                using (Stream stream = asm.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        MessageBox.Show(
                            "Błąd integralności pliku: brak osadzonego pakietu zasobów (bundle.zip).",
                            "scrcpy Manager Portable - Błąd",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                        return;
                    }

                    // Oblicz hash zasobu, aby sprawdzić czy wersja w cache jest aktualna
                    string currentHash = ComputeStreamHash(stream);
                    stream.Position = 0;

                    bool needsExtract = true;
                    if (Directory.Exists(runtimeDir) && File.Exists(versionFile) && File.Exists(scriptPath))
                    {
                        try
                        {
                            string cachedHash = File.ReadAllText(versionFile, Encoding.UTF8).Trim();
                            if (string.Equals(cachedHash, currentHash, StringComparison.OrdinalIgnoreCase))
                            {
                                needsExtract = false;
                            }
                        }
                        catch { }
                    }

                    if (needsExtract)
                    {
                        if (!Directory.Exists(runtimeDir))
                        {
                            Directory.CreateDirectory(runtimeDir);
                        }

                        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
                        {
                            foreach (ZipArchiveEntry entry in archive.Entries)
                            {
                                string destPath = Path.Combine(runtimeDir, entry.FullName);
                                string destDir = Path.GetDirectoryName(destPath);
                                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                                {
                                    Directory.CreateDirectory(destDir);
                                }

                                if (!string.IsNullOrEmpty(entry.Name))
                                {
                                    entry.ExtractToFile(destPath, true);
                                }
                            }
                        }

                        File.WriteAllText(versionFile, currentHash, Encoding.UTF8);
                    }
                }

                // Przygotuj zmienną środowiskową PATH (dodaj runtimeDir na sam początek)
                string currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
                string newPath = runtimeDir + ";" + currentPath;

                // Sprawdź czy dostępny jest pwsh (PowerShell 7), w przeciwnym razie użyj powershell.exe
                string psExe = "powershell.exe";
                foreach (string p in currentPath.Split(';'))
                {
                    try
                    {
                        string trimmed = p.Trim();
                        if (trimmed.Length > 0)
                        {
                            string candidate = Path.Combine(trimmed, "pwsh.exe");
                            if (File.Exists(candidate))
                            {
                                psExe = candidate;
                                break;
                            }
                        }
                    }
                    catch { }
                }

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = psExe;
                psi.Arguments = "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"" + scriptPath + "\"";
                psi.WorkingDirectory = runtimeDir;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;

                // Ustaw zaktualizowany PATH ze scrcpy i adb w procesie potomnym
                psi.EnvironmentVariables["PATH"] = newPath;
                try
                {
                    string currentExePath = Process.GetCurrentProcess().MainModule.FileName;
                    if (!string.IsNullOrEmpty(currentExePath))
                    {
                        psi.EnvironmentVariables["SCRCPY_MANAGER_EXE"] = currentExePath;
                    }
                }
                catch { }

                Process proc = Process.Start(psi);
                if (proc == null)
                {
                    MessageBox.Show(
                        "Nie udało się uruchomić procesu PowerShell.",
                        "scrcpy Manager Portable",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Wystąpił nieoczekiwany błąd podczas uruchamiania scrcpy Manager Portable:\n\n" + ex.Message,
                    "scrcpy Manager Portable",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        static string ComputeStreamHash(Stream stream)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(stream);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        static void CreateDesktopShortcut()
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string shortcutPath = Path.Combine(desktop, "scrcpy Manager.lnk");
                string currentExe = Process.GetCurrentProcess().MainModule.FileName;
                string currentDir = Path.GetDirectoryName(currentExe);

                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null)
                {
                    MessageBox.Show("Nie można uzyskać dostępu do WScript.Shell.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                dynamic shell = Activator.CreateInstance(shellType);
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = currentExe;
                shortcut.WorkingDirectory = currentDir;
                shortcut.Description = "scrcpy Manager (Portable)";
                shortcut.Save();

                MessageBox.Show(
                    "Pomyślnie utworzono skrót na Pulpicie:\n" + shortcutPath,
                    "scrcpy Manager Portable",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Nie udało się utworzyć skrótu:\n" + ex.Message,
                    "Błąd",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}
