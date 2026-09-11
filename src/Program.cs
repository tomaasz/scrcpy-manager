using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ScrcpyManager
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                // Obsługa parametru tworzenia skrótu na pulpicie
                if (args != null && args.Length > 0 &&
                    (args[0].Equals("--shortcut", StringComparison.OrdinalIgnoreCase) ||
                     args[0].Equals("-shortcut", StringComparison.OrdinalIgnoreCase)))
                {
                    CreateDesktopShortcut();
                    return;
                }

                // Sprawdź i wyodrębnij runtime scrcpy/adb jeśli osadzony jako zasób
                string runtimeDir = EnsureRuntimeExtracted();

                // Dodaj ścieżkę runtime do PATH procesu
                string pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
                if (!string.IsNullOrEmpty(runtimeDir) && Directory.Exists(runtimeDir))
                {
                    Environment.SetEnvironmentVariable("PATH", runtimeDir + ";" + pathVar);
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.ApplicationExit += (s, e) =>
                {
                    KeyboardHook.Stop();
                    NavBarManager.Shutdown();
                    AppWindowIconManager.Shutdown();
                };
                Application.Run(new MainForm(args, runtimeDir));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Wystąpił nieoczekiwany błąd podczas uruchamiania scrcpy Manager:\n\n" + ex.Message,
                    "scrcpy Manager",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private static string EnsureRuntimeExtracted()
        {
            string developerDir = AppDomain.CurrentDomain.BaseDirectory;
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string runtimeRoot = Path.Combine(localAppData, "scrcpy-manager", "runtime");

            Assembly asm = Assembly.GetExecutingAssembly();
            using (Stream stream = asm.GetManifestResourceStream("bundle.zip"))
            {
                if (stream == null)
                {
                    // Build developerski korzysta z zasobów leżących obok pliku wykonywalnego.
                    return developerDir;
                }

                string currentHash = ComputeStreamHash(stream);
                string runtimeDir = Path.Combine(runtimeRoot, currentHash);
                if (IsCompleteRuntime(runtimeDir))
                {
                    CleanupStaleRuntimeDirs(runtimeRoot, runtimeDir);
                    return runtimeDir;
                }

                Directory.CreateDirectory(runtimeRoot);
                using (Mutex extractionMutex = new Mutex(false, "Local\\scrcpy-manager-runtime-extraction"))
                {
                    bool lockTaken = false;
                    try
                    {
                        try { lockTaken = extractionMutex.WaitOne(TimeSpan.FromSeconds(30)); }
                        catch (AbandonedMutexException) { lockTaken = true; }
                        if (!lockTaken) throw new IOException("Przekroczono czas oczekiwania na przygotowanie runtime scrcpy.");
                        if (IsCompleteRuntime(runtimeDir)) return runtimeDir;

                        stream.Position = 0;
                        ExtractRuntimeAtomically(stream, runtimeRoot, runtimeDir, currentHash);
                    }
                    finally
                    {
                        if (lockTaken) extractionMutex.ReleaseMutex();
                    }
                }
                CleanupStaleRuntimeDirs(runtimeRoot, runtimeDir);
                return runtimeDir;
            }
        }

        // Best-effort: each new bundled scrcpy/adb version now lives in its own
        // hash-named folder (so a mid-update process keeps working off its original
        // files), but nothing else ever removed the previous version's folder. Left
        // unchecked this grows %LOCALAPPDATA%\scrcpy-manager\runtime\ without bound
        // across auto-updates. Deletion failures (e.g. an older instance still has
        // the folder open) are swallowed - the folder just survives to the next launch.
        private static void CleanupStaleRuntimeDirs(string runtimeRoot, string currentRuntimeDir)
        {
            try
            {
                foreach (string dir in Directory.GetDirectories(runtimeRoot))
                {
                    if (string.Equals(dir, currentRuntimeDir, StringComparison.OrdinalIgnoreCase)) continue;
                    string name = Path.GetFileName(dir);
                    if (string.IsNullOrEmpty(name) || name.StartsWith(".extracting-", StringComparison.OrdinalIgnoreCase)) continue;
                    try { Directory.Delete(dir, true); } catch { }
                }
            }
            catch { }
        }

        private static bool IsCompleteRuntime(string runtimeDir)
        {
            return Directory.Exists(runtimeDir) &&
                   File.Exists(Path.Combine(runtimeDir, ".complete")) &&
                   File.Exists(Path.Combine(runtimeDir, "scrcpy.exe")) &&
                   File.Exists(Path.Combine(runtimeDir, "adb.exe"));
        }

        private static void ExtractRuntimeAtomically(Stream bundle, string runtimeRoot, string runtimeDir, string hash)
        {
            string stagingDir = Path.Combine(runtimeRoot, ".extracting-" + Process.GetCurrentProcess().Id + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(stagingDir);
            try
            {
                string stagingRoot = Path.GetFullPath(stagingDir).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                using (ZipArchive archive = new ZipArchive(bundle, ZipArchiveMode.Read, true))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Name)) continue;
                        string normalizedName = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
                        string destPath = Path.GetFullPath(Path.Combine(stagingDir, normalizedName));
                        if (!destPath.StartsWith(stagingRoot, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidDataException("Niedozwolona ścieżka w bundle.zip: " + entry.FullName);
                        }
                        string destDir = Path.GetDirectoryName(destPath);
                        if (!string.IsNullOrEmpty(destDir)) Directory.CreateDirectory(destDir);
                        entry.ExtractToFile(destPath, false);
                    }
                }

                if (!File.Exists(Path.Combine(stagingDir, "scrcpy.exe")) || !File.Exists(Path.Combine(stagingDir, "adb.exe")))
                    throw new InvalidDataException("Pakiet portable nie zawiera wymaganego scrcpy.exe lub adb.exe.");

                File.WriteAllText(Path.Combine(stagingDir, ".complete"), hash, new UTF8Encoding(false));
                if (Directory.Exists(runtimeDir)) Directory.Delete(runtimeDir, true);
                Directory.Move(stagingDir, runtimeDir);
                stagingDir = null;
            }
            finally
            {
                if (!string.IsNullOrEmpty(stagingDir) && Directory.Exists(stagingDir))
                {
                    try { Directory.Delete(stagingDir, true); } catch { }
                }
            }
        }

        private static string ComputeStreamHash(Stream stream)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(stream);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private static void CreateDesktopShortcut()
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
                    "scrcpy Manager",
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
