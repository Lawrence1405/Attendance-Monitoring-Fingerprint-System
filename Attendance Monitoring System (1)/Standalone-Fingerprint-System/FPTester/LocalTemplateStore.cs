using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FPTester
{
    /// <summary>
    /// Zero-dependency local store for fingerprint templates.
    /// Saves each template as a .fpt binary file in %AppData%\FPTester\templates\.
    /// A JSON index (index.json) maps slot names to file names.
    /// No MySQL or any external dependency needed.
    /// </summary>
    internal class LocalTemplateStore
    {
        private readonly string _dir;
        private readonly string _indexPath;

        // slot name → filename (without directory)
        private Dictionary<string, SlotEntry> _index = new();

        public LocalTemplateStore()
        {
            _dir       = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FPTester", "templates");
            _indexPath = Path.Combine(_dir, "index.json");
            Directory.CreateDirectory(_dir);
            LoadIndex();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public IReadOnlyDictionary<string, SlotEntry> All => _index;

        public bool Save(string slotName, byte[] template, string notes = "")
        {
            try
            {
                string filename = $"{SanitizeName(slotName)}_{DateTime.Now:yyyyMMddHHmmss}.fpt";
                string path     = Path.Combine(_dir, filename);
                File.WriteAllBytes(path, template);

                _index[slotName] = new SlotEntry
                {
                    FileName  = filename,
                    Notes     = notes,
                    EnrolledAt = DateTime.Now
                };
                SaveIndex();
                return true;
            }
            catch { return false; }
        }

        public byte[]? Load(string slotName)
        {
            if (!_index.TryGetValue(slotName, out var entry)) return null;
            string path = Path.Combine(_dir, entry.FileName);
            if (!File.Exists(path)) return null;
            return File.ReadAllBytes(path);
        }

        public bool Delete(string slotName)
        {
            if (!_index.TryGetValue(slotName, out var entry)) return false;
            try
            {
                string path = Path.Combine(_dir, entry.FileName);
                if (File.Exists(path)) File.Delete(path);
                _index.Remove(slotName);
                SaveIndex();
                return true;
            }
            catch { return false; }
        }

        public bool Exists(string slotName) => _index.ContainsKey(slotName);

        public List<string> SlotNames() => new(_index.Keys);

        public string StorageFolder => _dir;

        // ── Private ───────────────────────────────────────────────────────────

        private void LoadIndex()
        {
            if (!File.Exists(_indexPath)) return;
            try
            {
                var json = File.ReadAllText(_indexPath);
                _index   = JsonSerializer.Deserialize<Dictionary<string, SlotEntry>>(json)
                           ?? new Dictionary<string, SlotEntry>();
            }
            catch { _index = new Dictionary<string, SlotEntry>(); }
        }

        private void SaveIndex()
        {
            var json = JsonSerializer.Serialize(_index,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_indexPath, json);
        }

        private static string SanitizeName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name.Length > 40 ? name[..40] : name;
        }
    }

    internal class SlotEntry
    {
        public string   FileName   { get; set; } = "";
        public string   Notes      { get; set; } = "";
        public DateTime EnrolledAt { get; set; }
    }
}
