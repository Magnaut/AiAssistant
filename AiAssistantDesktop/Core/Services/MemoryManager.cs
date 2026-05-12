using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AiAssistantDesktop.Core.Models.Memory;

namespace AiAssistantDesktop.Core.Services
{
    public class MemoryManager
    {
        private readonly List<MemoryEntry> _shortTermMemory = new();
        private readonly List<MemoryEntry> _mediumTermMemory = new();
        private readonly List<MemoryEntry> _longTermMemory = new();
        private readonly string _storagePath;
        private readonly JsonSerializerOptions _jsonOptions;

        public MemoryManager(string? storagePath = null)
        {
            _storagePath = storagePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "memory");
            _jsonOptions = new JsonSerializerOptions { WriteIndented = true };

            Directory.CreateDirectory(_storagePath);
            LoadFromDisk();
        }

        public void Add(MemoryEntry entry)
        {
            if (entry.IsExpired) return;

            switch (entry.Level)
            {
                case MemoryLevel.ShortTerm:
                    _shortTermMemory.Add(entry);
                    if (_shortTermMemory.Count > 15) _shortTermMemory.RemoveAt(0); // FIFO
                    break;
                case MemoryLevel.MediumTerm:
                    _mediumTermMemory.Add(entry);
                    break;
                case MemoryLevel.LongTerm:
                    _longTermMemory.Add(entry);
                    break;
            }
            SaveToDisk();
        }

        public string GetContextForLLM(int maxShort = 10, int maxMedium = 5, int maxLong = 3)
        {
            var context = new System.Text.StringBuilder();

            // Краткосрочная (актуальный диалог)
            var shortMem = _shortTermMemory.Where(m => !m.IsExpired).TakeLast(maxShort).ToList();
            if (shortMem.Any())
            {
                context.AppendLine("💬 Краткосрочная память (последние реплики):");
                foreach (var m in shortMem)
                    context.AppendLine($"- [{m.Source}] {m.Content}");
                context.AppendLine();
            }

            // Среднесрочная (факты о пользователе)
            var mediumMem = _mediumTermMemory.Where(m => !m.IsExpired).TakeLast(maxMedium).ToList();
            if (mediumMem.Any())
            {
                context.AppendLine("📌 Факты о пользователе:");
                foreach (var m in mediumMem)
                    context.AppendLine($"- {m.Content}");
                context.AppendLine();
            }

            // Долгосрочная (глубокие выводы)
            var longMem = _longTermMemory.Where(m => !m.IsExpired).TakeLast(maxLong).ToList();
            if (longMem.Any())
            {
                context.AppendLine("🧠 Долгосрочная память (выводы):");
                foreach (var m in longMem)
                    context.AppendLine($"- {m.Content}");
                context.AppendLine();
            }

            return context.ToString();
        }

        public void ExtractFacts(string userInput, string agentResponse)
        {
            // Простая эвристика для извлечения фактов (можно заменить на LLM-экстрактор позже)
            if (userInput.Contains("меня зовут") || userInput.Contains("мое имя"))
            {
                var name = userInput.Split(new[] { "меня зовут", "мое имя" }, StringSplitOptions.RemoveEmptyEntries).Last().Trim();
                Add(new MemoryEntry($"Пользователя зовут {name}", MemoryLevel.MediumTerm, "user", TimeSpan.FromDays(30)));
            }
            if (userInput.Contains("я люблю") || userInput.Contains("предпочитаю"))
            {
                var pref = userInput.Split(new[] { "я люблю", "предпочитаю" }, StringSplitOptions.RemoveEmptyEntries).Last().Trim();
                Add(new MemoryEntry($"Пользователь любит/предпочитает: {pref}", MemoryLevel.MediumTerm, "user", TimeSpan.FromDays(60)));
            }
        }

        private void SaveToDisk()
        {
            try
            {
                var data = new { Short = _shortTermMemory, Medium = _mediumTermMemory, Long = _longTermMemory };
                File.WriteAllText(Path.Combine(_storagePath, "memory.json"), JsonSerializer.Serialize(data, _jsonOptions));
            }
            catch { /* Игнорируем ошибки записи */ }
        }

        private void LoadFromDisk()
        {
            try
            {
                var path = Path.Combine(_storagePath, "memory.json");
                if (!File.Exists(path)) return;

                var json = File.ReadAllText(path);
                var data = JsonSerializer.Deserialize<MemoryData>(json);
                if (data == null) return;

                _shortTermMemory.Clear();
                _shortTermMemory.AddRange(data.Short.Where(m => !m.IsExpired));
                _mediumTermMemory.Clear();
                _mediumTermMemory.AddRange(data.Medium.Where(m => !m.IsExpired));
                _longTermMemory.Clear();
                _longTermMemory.AddRange(data.Long.Where(m => !m.IsExpired));
            }
            catch { /* Игнорируем ошибки чтения */ }
        }

        private class MemoryData
        {
            public List<MemoryEntry> Short { get; set; } = new();
            public List<MemoryEntry> Medium { get; set; } = new();
            public List<MemoryEntry> Long { get; set; } = new();
        }
    }
}