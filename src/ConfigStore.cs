using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace ScrcpyManager
{
    internal static class ConfigStore
    {
        internal static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
        };

        public static bool TryLoad<T>(string path, out T value, bool quarantineInvalid = false)
        {
            value = default(T);
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(json))
                {
                    if (quarantineInvalid) QuarantineInvalidFile(path);
                    return false;
                }
                value = JsonSerializer.Deserialize<T>(json, JsonOptions);
                if (value != null) return true;
                if (quarantineInvalid) QuarantineInvalidFile(path);
                return false;
            }
            catch
            {
                if (quarantineInvalid) QuarantineInvalidFile(path);
                return false;
            }
        }

        private static void QuarantineInvalidFile(string path)
        {
            try
            {
                if (!File.Exists(path)) return;
                string suffix = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
                string destination = path + ".corrupt-" + suffix;
                File.Move(path, destination);
            }
            catch { }
        }

        public static void SaveAtomic<T>(string path, T value)
        {
            string directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory)) throw new ArgumentException("Nieprawidłowa ścieżka konfiguracji.", "path");
            Directory.CreateDirectory(directory);

            string json = JsonSerializer.Serialize(value, JsonOptions);
            string temp = Path.Combine(directory, Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                File.WriteAllText(temp, json, new UTF8Encoding(false));
                if (File.Exists(path))
                {
                    File.Replace(temp, path, path + ".bak", true);
                }
                else
                {
                    File.Move(temp, path);
                }
            }
            finally
            {
                try { if (File.Exists(temp)) File.Delete(temp); } catch { }
            }
        }
    }
}
