using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace ScrcpyLauncher
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string scriptPath = Path.Combine(baseDir, "ScrcpyApp.ps1");

            // Jeśli podano parametr --shortcut, utwórz skrót na Pulpicie
            if (args.Length > 0 && (args[0].Equals("--shortcut", StringComparison.OrdinalIgnoreCase) || args[0].Equals("-shortcut", StringComparison.OrdinalIgnoreCase)))
            {
                CreateDesktopShortcut(baseDir);
                return;
            }

            if (!File.Exists(scriptPath))
            {
                MessageBox.Show(
                    "Nie znaleziono pliku skryptu:\n" + scriptPath + "\n\nUpewnij się, że plik ScrcpyApp.ps1 znajduje się w tym samym folderze co ScrcpyApp.exe.",
                    "Błąd scrcpy Manager",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }

            try
            {
                // Sprawdź czy dostępny jest pwsh (PowerShell 7), w przeciwnym razie użyj powershell.exe
                string psExe = "powershell.exe";
                string pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
                string[] paths = pathEnv.Split(';');
                foreach (string p in paths)
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
                psi.WorkingDirectory = baseDir;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;

                Process proc = Process.Start(psi);
                if (proc == null)
                {
                    MessageBox.Show(
                        "Nie udało się uruchomić procesu PowerShell.",
                        "Błąd scrcpy Manager",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Wystąpił błąd podczas uruchamiania scrcpy Manager:\n\n" + ex.Message,
                    "Błąd scrcpy Manager",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        static void CreateDesktopShortcut(string baseDir)
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string shortcutPath = Path.Combine(desktop, "scrcpy Manager.lnk");
                string exePath = Path.Combine(baseDir, "ScrcpyApp.exe");

                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null)
                {
                    MessageBox.Show("Nie można uzyskać dostępu do WScript.Shell.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                dynamic shell = Activator.CreateInstance(shellType);
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = exePath;
                shortcut.WorkingDirectory = baseDir;
                shortcut.Description = "scrcpy Manager";
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
