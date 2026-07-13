using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace FPTester
{
    /// <summary>
    /// Template store that supports:
    ///  - A default store under %AppData%\FPTester\templates (enrollment writes here)
    ///  - An optional "active folder" that Match and 1:N read from instead
    ///    (can be pointed at any folder containing .fpt + index.json)
    /// </summary>
    internal class LocalTemplateStore
    {
        // ── Default store (enrollment writes here) ────────────────────────────
        private readonly string _defaultDir;
        private readonly string _defaultIndexPath;
        private Dictionary<string, SlotEntry> _defaultIndex = new();

        // ── Active read folder (Match / 1:N reads from here) ─────────────────
        // null = use default store
        private string? _activeFolder;
        private string  _activeIndexPath => Path.Combine(ActiveFolder, "index.json");
        private Dictionary<string, SlotEntry> _activeIndex  = new();

        public string DefaultFolder => _defaultDir;
        public string ActiveFolder  => _activeFolder ?? _defaultDir;
        public bool   IsUsingCustomFolder => _activeFolder != null;

        public LocalTemplateStore()
        {
            _defaultDir       = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FPTester", "templates");
            _defaultIndexPath = Path.Combine(_defaultDir, "index.json");
            Directory.CreateDirectory(_defaultDir);
            LoadDefaultIndex();
            _activeIndex = _defaultIndex; // start with default
        }

        // ── Active folder management ──────────────────────────────────────────

        /// <summary>
        /// Points Match/1:N reads at a different folder.
        /// The folder must contain an index.json (created automatically when
        /// you save templates there). Pass null to revert to the default.
        /// </summary>
        public void SetActiveFolder(string? folder)
        {
            if (folder == null || folder == _defaultDir)
            {
                _activeFolder = null;
                _activeIndex  = _defaultIndex;
                return;
            }

            Directory.CreateDirectory(folder);
            _activeFolder = folder;
            LoadActiveIndex();
        }

        /// <summary>Reload the active index from disk (call after external changes).</summary>
        public void ReloadActiveIndex() => LoadActiveIndex();

        // ── Read API (uses active folder) ─────────────────────────────────────

        public IReadOnlyDictionary<string, SlotEntry> All         => _activeIndex;
        public List<string>                           SlotNames() => new(_activeIndex.Keys);
        public bool Exists(string slotName) => _activeIndex.ContainsKey(slotName);

        public byte[]? Load(string slotName)
        {
            if (!_activeIndex.TryGetValue(slotName, out var entry)) return null;
            string path = Path.Combine(ActiveFolder, entry.FileName);
            if (!File.Exists(path)) return null;
            return File.ReadAllBytes(path);
        }

        // ── Write API (always writes to DEFAULT folder) ───────────────────────

        public bool Save(string slotName, byte[] template, string notes = "")
        {
            try
            {
                string filename = $"{SanitizeName(slotName)}_{DateTime.Now:yyyyMMddHHmmss}.fpt";
                string path     = Path.Combine(_defaultDir, filename);
                File.WriteAllBytes(path, template);

                _defaultIndex[slotName] = new SlotEntry
                {
                    FileName   = filename,
                    Notes      = notes,
                    EnrolledAt = DateTime.Now
                };
                SaveDefaultIndex();

                // If active folder IS the default, keep active index in sync
                if (!IsUsingCustomFolder)
                    _activeIndex = _defaultIndex;

                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Save a template into a SPECIFIC folder (not just the default).
        /// Used when the user picks a custom save folder in Scan/Enroll tabs.
        /// </summary>
        public bool SaveToFolder(string folder, string slotName, byte[] template, string notes = "")
        {
            try
            {
                Directory.CreateDirectory(folder);
                string indexPath = Path.Combine(folder, "index.json");

                // Load existing index for that folder
                var idx = new Dictionary<string, SlotEntry>();
                if (File.Exists(indexPath))
                {
                    try { idx = JsonSerializer.Deserialize<Dictionary<string, SlotEntry>>(
                        File.ReadAllText(indexPath)) ?? new(); } catch { }
                }

                string filename = $"{SanitizeName(slotName)}_{DateTime.Now:yyyyMMddHHmmss}.fpt";
                File.WriteAllBytes(Path.Combine(folder, filename), template);
                idx[slotName] = new SlotEntry { FileName = filename, Notes = notes, EnrolledAt = DateTime.Now };
                File.WriteAllText(indexPath, JsonSerializer.Serialize(idx,
                    new JsonSerializerOptions { WriteIndented = true }));

                // If active folder is this folder, keep active index in sync
                if (_activeFolder == folder) { _activeIndex = idx; }

                return true;
            }
            catch { return false; }
        }

        public bool Delete(string slotName)
        {
            // Delete from whichever index holds the slot
            var idx  = _activeIndex;
            var dir  = ActiveFolder;
            var ipath = _activeIndexPath;

            if (!idx.TryGetValue(slotName, out var entry)) return false;
            try
            {
                string path = Path.Combine(dir, entry.FileName);
                if (File.Exists(path)) File.Delete(path);
                idx.Remove(slotName);
                File.WriteAllText(ipath, JsonSerializer.Serialize(idx,
                    new JsonSerializerOptions { WriteIndented = true }));

                // If we deleted from active and it's the default, keep both in sync
                if (!IsUsingCustomFolder) _defaultIndex = idx;
                return true;
            }
            catch { return false; }
        }

        public string StorageFolder => ActiveFolder;

        // ── Default-specific read helpers (used by enroll duplicate check) ──────
        // The duplicate check always scans the DEFAULT store regardless of
        // which active folder is set for Match/1:N.

        public bool   ExistsInDefault(string slotName) =>
            _defaultIndex.ContainsKey(slotName);

        public List<string> SlotNamesFromDefault() =>
            new(_defaultIndex.Keys);

        public byte[]? LoadFromDefault(string slotName)
        {
            if (!_defaultIndex.TryGetValue(slotName, out var entry)) return null;
            string path = Path.Combine(_defaultDir, entry.FileName);
            if (!File.Exists(path)) return null;
            return File.ReadAllBytes(path);
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void LoadDefaultIndex()
        {
            if (!File.Exists(_defaultIndexPath)) return;
            try
            {
                _defaultIndex = JsonSerializer.Deserialize<Dictionary<string, SlotEntry>>(
                    File.ReadAllText(_defaultIndexPath)) ?? new();
            }
            catch { _defaultIndex = new(); }
        }

        private void LoadActiveIndex()
        {
            if (_activeFolder == null) { _activeIndex = _defaultIndex; return; }
            if (!File.Exists(_activeIndexPath)) { _activeIndex = new(); return; }
            try
            {
                _activeIndex = JsonSerializer.Deserialize<Dictionary<string, SlotEntry>>(
                    File.ReadAllText(_activeIndexPath)) ?? new();
            }
            catch { _activeIndex = new(); }
        }

        private void SaveDefaultIndex()
        {
            File.WriteAllText(_defaultIndexPath, JsonSerializer.Serialize(_defaultIndex,
                new JsonSerializerOptions { WriteIndented = true }));
        }

        private static string SanitizeName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
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
