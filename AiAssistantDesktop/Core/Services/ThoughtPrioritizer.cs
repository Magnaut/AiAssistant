using System;
using System.Collections.Generic;
using System.Linq;
using AiAssistantDesktop.Core.Models;

namespace AiAssistantDesktop.Core.Services
{
    public class ThoughtPrioritizer
    {
        private readonly HashSet<string> _criticalKeywords;
        private readonly HashSet<string> _highPriorityKeywords;
        private readonly string _agentName;

        public ThoughtPrioritizer(string agentName = "Michelle")
        {
            _agentName = agentName;

            _criticalKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                agentName.ToLowerInvariant(),
                "помоги", "срочно", "важно", "экстренно",
                "стоп", "хватит", "прекрати"
            };

            _highPriorityKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "привет", "здравствуй", "добрый день", "доброе утро", "добрый вечер",
                "пока", "до свидания", "увидимся",
                "спасибо", "пожалуйста",
                "кто ты", "что ты", "как тебя зовут",
                "меня зовут", "моё имя",
                "ты слышишь", "ты меня слышишь", "ты тут",
                "запомни", "запомнила", "помнишь"
            };
        }

        public ThoughtPriority PrioritizeUserInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return ThoughtPriority.Low;

            var lower = input.ToLowerInvariant();

            // 1. Критические ключевые слова
            if (_criticalKeywords.Any(kw => lower.Contains(kw)))
                return ThoughtPriority.Critical;

            // 2. Высокий приоритет (социальные сигналы, представление, проверка связи)
            if (_highPriorityKeywords.Any(kw => lower.Contains(kw)))
                return ThoughtPriority.High;

            // 3. Вопросы — средний приоритет
            if (lower.Contains("?") ||
                lower.Contains("как") ||
                lower.Contains("что") ||
                lower.Contains("почему") ||
                lower.Contains("когда") ||
                lower.Contains("где") ||
                lower.Contains("зачем") ||
                lower.Contains("сколько"))
                return ThoughtPriority.Medium;

            // 🔥 ИЗМЕНЕНИЕ: Всё остальное — теперь MEDIUM, а не LOW
            // Чтобы агент отвечал на большинство сообщений
            return ThoughtPriority.Medium;
        }

        public ThoughtPriority PrioritizeInternalEvent(string eventType, string? content = null)
        {
            return eventType.ToLowerInvariant() switch
            {
                "error" or "exception" => ThoughtPriority.Critical,
                "user_mention" => ThoughtPriority.Critical,
                "tool_result" => ThoughtPriority.High,
                "memory_recall" => ThoughtPriority.Medium,
                "background_thought" => ThoughtPriority.Low,
                _ => ThoughtPriority.Medium
            };
        }

        public PrioritizedThought CreateThought(string content, string source = "user")
        {
            var priority = source == "user"
                ? PrioritizeUserInput(content)
                : PrioritizeInternalEvent(source, content);

            return new PrioritizedThought(content, priority, source);
        }

        public IEnumerable<PrioritizedThought> FilterResponseRequired(
            IEnumerable<PrioritizedThought> thoughts,
            ThoughtPriority minPriority = ThoughtPriority.Medium)
        {
            return thoughts.Where(t => t.Priority >= minPriority && !t.CanBeIgnored);
        }

        public IEnumerable<PrioritizedThought> SortByPriority(IEnumerable<PrioritizedThought> thoughts)
        {
            return thoughts.OrderByDescending(t => (int)t.Priority)
                          .ThenByDescending(t => t.Timestamp);
        }

        public void AddCriticalKeyword(string keyword)
        {
            if (!string.IsNullOrWhiteSpace(keyword))
                _criticalKeywords.Add(keyword.Trim().ToLowerInvariant());
        }
    }
}