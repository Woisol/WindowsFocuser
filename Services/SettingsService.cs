using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Storage;

namespace WindowsFocuser.Services
{
    public class SettingsService
    {
        private const string FileName = "setting.ini";
        private string _filePath;
        private Dictionary<string, string> _settings = new Dictionary<string, string>();

        public double DimOpacity { get; set; } = 0.5;
        public bool IsEnabled { get; set; } = true;
        public uint HotKeyModifiers { get; set; } = 0x0002 | 0x0001; // Ctrl + Alt
        public uint HotKeyKey { get; set; } = 0x7B; // F12
        public int OverlayWidth { get; set; } = 0; // 0 = Auto
        public int OverlayHeight { get; set; } = 0; // 0 = Auto
        public string EffectType { get; set; } = "Dim"; // Dim, Acrylic, Mica
        public string OverlayColor { get; set; } = "#000000"; // Hex color

        public bool StartOnBoot
        {
            get
            {
                try
                {
                    using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false))
                    {
                        return key?.GetValue("WindowsFocuser") != null;
                    }
                }
                catch { return false; }
            }
            set
            {
                try
                {
                    using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                    {
                        if (value)
                        {
                            string exePath = Process.GetCurrentProcess().MainModule.FileName;
                            if (exePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                            {
                                exePath = exePath.Substring(0, exePath.Length - 4) + ".exe";
                            }
                            key.SetValue("WindowsFocuser", exePath);
                        }
                        else
                        {
                            key.DeleteValue("WindowsFocuser", false);
                        }
                    }
                }
                catch { }
            }
        }

        public SettingsService()
        {
            _filePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, FileName);
            Load();
        }

        public void Load()
        {
            if (!File.Exists(_filePath))
            {
                Save();
                return;
            }

            try
            {
                var lines = File.ReadAllLines(_filePath);
                _settings.Clear();
                foreach (var line in lines)
                {
                    var parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        _settings[parts[0].Trim()] = parts[1].Trim();
                    }
                }

                if (_settings.ContainsKey("DimOpacity") && double.TryParse(_settings["DimOpacity"], out double opacity))
                {
                    DimOpacity = opacity;
                }
                if (_settings.ContainsKey("IsEnabled") && bool.TryParse(_settings["IsEnabled"], out bool enabled))
                {
                    IsEnabled = enabled;
                }
                if (_settings.ContainsKey("HotKey"))
                {
                    ParseHotKey(_settings["HotKey"]);
                }

                if (_settings.ContainsKey("OverlayWidth") && int.TryParse(_settings["OverlayWidth"], out int w)) OverlayWidth = w;
                if (_settings.ContainsKey("OverlayHeight") && int.TryParse(_settings["OverlayHeight"], out int h)) OverlayHeight = h;
                if (_settings.ContainsKey("EffectType")) EffectType = _settings["EffectType"];
                if (_settings.ContainsKey("OverlayColor")) OverlayColor = _settings["OverlayColor"];

                // Sync StartOnBoot from INI if registry fails or just to keep consistent?
                // Actually registry is source of truth, but user wants it in INI.
                // We will just write it to INI in Save(), but Load() relies on Registry for the property getter.
            }
            catch
            {
                // Ignore errors for now
            }
        }

        public void Save()
        {
            _settings["DimOpacity"] = DimOpacity.ToString("F2");
            _settings["IsEnabled"] = IsEnabled.ToString();
            _settings["HotKey"] = FormatHotKey();
            _settings["OverlayWidth"] = OverlayWidth.ToString();
            _settings["OverlayHeight"] = OverlayHeight.ToString();
            _settings["EffectType"] = EffectType;
            _settings["OverlayColor"] = OverlayColor;
            _settings["StartOnBoot"] = StartOnBoot.ToString(); // Just for display in INI

            var lines = _settings.Select(kvp => $"{kvp.Key}={kvp.Value}");
            File.WriteAllLines(_filePath, lines);
        }

        private void ParseHotKey(string hotKeyStr)
        {
            try
            {
                uint mods = 0;
                uint key = 0;
                var parts = hotKeyStr.Split('+');
                foreach (var part in parts)
                {
                    var p = part.Trim();
                    if (p.Equals("Alt", StringComparison.OrdinalIgnoreCase)) mods |= 0x0001;
                    else if (p.Equals("Ctrl", StringComparison.OrdinalIgnoreCase)) mods |= 0x0002;
                    else if (p.Equals("Shift", StringComparison.OrdinalIgnoreCase)) mods |= 0x0004;
                    else if (p.Equals("Win", StringComparison.OrdinalIgnoreCase)) mods |= 0x0008;
                    else
                    {
                        // Try parse key
                        if (Enum.TryParse(typeof(Windows.System.VirtualKey), p, true, out var vk))
                        {
                            key = (uint)vk;
                        }
                    }
                }
                if (key != 0)
                {
                    HotKeyModifiers = mods;
                    HotKeyKey = key;
                }
            }
            catch {}
        }

        private string FormatHotKey()
        {
            var parts = new List<string>();
            if ((HotKeyModifiers & 0x0002) != 0) parts.Add("Ctrl");
            if ((HotKeyModifiers & 0x0001) != 0) parts.Add("Alt");
            if ((HotKeyModifiers & 0x0004) != 0) parts.Add("Shift");
            if ((HotKeyModifiers & 0x0008) != 0) parts.Add("Win");

            parts.Add(((Windows.System.VirtualKey)HotKeyKey).ToString());
            return string.Join("+", parts);
        }

        public string GetSettingsFilePath() => _filePath;
    }
}
