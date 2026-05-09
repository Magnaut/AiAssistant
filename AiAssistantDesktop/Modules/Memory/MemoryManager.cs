using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AiAssistantDesktop.Core.Models;

namespace AiAssistantDesktop.Modules.Memory
{
    public class MemoryManager
    {
        private readonly List<Message> _shortTerm = new();
        private readonly List<MemoryFact> _longTerm = new();
        private readonly string _storagePath;
        private readonly int _maxShortTerm = 10;
        private readonly int _maxLongTermInContext = 5;

        public MemoryManager()
        {
            _storagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "long_term_memory.json");
            LoadLongTerm();
        }

        public void AddShortTerm(string role, string content)
        {
            _shortTerm.Add(new Message { Role = role, Content = content, Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() });
            if (_shortTerm.Count > _maxShortTerm) _shortTerm.RemoveAt(0);

            if (role == "user" && ContainsFact(content))
            {
                _longTerm.Add(new MemoryFact
                {
                    Content = content,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Type = ExtractFactType(content)
                });
                SaveLongTerm();
            }
        }

        public List<Message> BuildContext()
        {
            var context = new List<Message>();
            var recent = _shortTerm.Skip(Math.Max(0, _shortTerm.Count - 5)).ToList();
            context.AddRange(recent);

            var facts = _longTerm
                .OrderByDescending(f => f.Timestamp)
                .Take(_maxLongTermInContext)
                .Select(f => new Message { Role = "system", Content = $"[ФАКТ: {f.Content}]" });
            context.AddRange(facts);

            return context;
        }

        public void Clear()
        {
            _shortTerm.Clear();
            _longTerm.Clear();
            if (File.Exists(_storagePath)) File.Delete(_storagePath);
        }

        private bool ContainsFact(string text)
        {
            var triggers = new[] { "люблю", "нравится", "работаю", "живу", "учусь", "помнишь", "вчера", "завтра", "имя", "зовут", "работа", "учёба", "хобби" };
            return triggers.Any(t => text.Contains(t, StringComparison.OrdinalIgnoreCase));
        }

        private string ExtractFactType(string text)
        {
            if (text.Contains("люблю") || text.Contains("нравится")) return "preference";
            if (text.Contains("работаю") || text.Contains("работа")) return "work";
            if (text.Contains("учусь") || text.Contains("учёба")) return "study";
            if (text.Contains("живу")) return "location";
            return "general";
        }

        private void SaveLongTerm()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_storagePath)!);
                File.WriteAllText(_storagePath, JsonSerializer.Serialize(_longTerm, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Memory save error: {ex.Message}"); }
        }

        private void LoadLongTerm()
        {
            try
            {
                if (File.Exists(_storagePath))
                {
                    var json = File.ReadAllText(_storagePath);
                    var loaded = JsonSerializer.Deserialize<List<MemoryFact>>(json);
                    if (loaded != null) _longTerm.AddRange(loaded);
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Memory load error: {ex.Message}"); }
        }
    }

    public class MemoryFact
    {
        public string Content { get; set; } = string.Empty;
        public string Type { get; set; } = "general";
        public long Timestamp { get; set; }
    }
}