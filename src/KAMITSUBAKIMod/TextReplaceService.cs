using System;
using System.IO;

namespace KAMITSUBAKIMod
{
    public static class TextReplaceService
    {
        private static FileSystemWatcher _watcher;
        public static bool Enabled = true;
        public static bool ApplyToUGUIText = true;
        public static bool ApplyToTMP = true;

        public static void Init()
        {
            TextReplaceRules.EnsureFile();
            TextReplaceRules.Load();
            SetupWatcher();
        }

        public static string Apply(string input)
        {
            if (!Enabled) return input;
            return TextReplaceRules.ApplyAll(input);
        }

        private static void SetupWatcher()
        {
            try
            {
                var dir = Path.GetDirectoryName(TextReplaceRules.RulesFilePath);
                if (string.IsNullOrEmpty(dir)) return;

                _watcher = new FileSystemWatcher(dir, Path.GetFileName(TextReplaceRules.RulesFilePath));
                _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName;
                _watcher.Changed += (_, __) => DebouncedReload();
                _watcher.Created += (_, __) => DebouncedReload();
                _watcher.Renamed += (_, __) => DebouncedReload();
                _watcher.EnableRaisingEvents = true;
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning($"File watcher failed: {e.Message}");
            }
        }

        static DateTime _last;
        private static void DebouncedReload()
        {
            var now = DateTime.UtcNow;
            if ((now - _last).TotalMilliseconds < 300) return;
            _last = now;
            try
            {
                if (TextReplaceRules.Load())
                    Plugin.Log?.LogInfo("Reloaded text replace rules (file change).");
            }
            catch { /* ignore */ }
        }
    }
}
