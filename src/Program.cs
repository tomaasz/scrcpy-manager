using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
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
                Application.Run(new MainForm(args));
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
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string runtimeDir = Path.Combine(localAppData, "scrcpy-manager", "runtime");
            string versionFile = Path.Combine(runtimeDir, ".version");
            string scrcpyExe = Path.Combine(runtimeDir, "scrcpy.exe");
            string adbExe = Path.Combine(runtimeDir, "adb.exe");

            Assembly asm = Assembly.GetExecutingAssembly();
            using (Stream stream = asm.GetManifestResourceStream("bundle.zip"))
            {
                if (stream == null)
                {
                    // Brak osadzonego zasobu (np. uruchamianie developerskie z folderu źródłowego)
                    return runtimeDir;
                }

                string currentHash = ComputeStreamHash(stream);
                stream.Position = 0;

                bool needsExtract = true;
                if (Directory.Exists(runtimeDir) && File.Exists(versionFile) && File.Exists(scrcpyExe) && File.Exists(adbExe))
                {
                    try
                    {
                        string cachedHash = File.ReadAllText(versionFile, Encoding.UTF8).Trim();
                        if (string.Equals(cachedHash, currentHash, StringComparison.OrdinalIgnoreCase))
                        {
                            needsExtract = false;
                        }
                    }
                    catch {}
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

            return runtimeDir;
        }

        private static string ComputeStreamHash(Stream stream)
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

