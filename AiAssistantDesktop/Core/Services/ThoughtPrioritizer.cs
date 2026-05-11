using System;
using System.Collections.Generic;
using System.Linq;
using AiAssistantDesktop.Core.Models;

namespace AiAssistantDesktop.Core.Services
{
    /// <summary>
    /// Сервис приоритезации мыслей и событий
    /// </summary>
    public class ThoughtPrioritizer
    {
        private readonly HashSet<string> _criticalKeywords;
        private readonly string _agentName;

        public ThoughtPrioritizer(string agentName = "Michelle")
        {
            _agentName = agentName;
            _criticalKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // Ключевые слова для критического приоритета
                agentName.ToLowerInvariant(),
                "помоги", "срочно", "важно", "экстренно",
                "ошибка", "проблема", "не работает",
                "стоп", "хватит", "прекрати"
            };
        }

        /// <summary>
        /// Определяет приоритет для входящего сообщения от пользователя
        /// </summary>
        public ThoughtPriority PrioritizeUserInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return ThoughtPriority.Low;

            var lower = input.ToLowerInvariant();

            // 1. Проверка на критические ключевые слова
            if (_criticalKeywords.Any(kw => lower.Contains(kw)))
                return ThoughtPriority.Critical;

            // 2. Проверка на вопросы (средний приоритет)
            if (lower.Contains("?") ||
                lower.Contains("как") ||
                lower.Contains("что") ||
                lower.Contains("почему") ||
                lower.Contains("когда") ||
                lower.Contains("где"))
                return ThoughtPriority.Medium;

            // 3. Приветствия и прощания — высокий приоритет (социальные сигналы)
            if (lower.Contains("привет") || lower.Contains("здравствуй") ||
                lower.Contains("пока") || lower.Contains("до свидания"))
                return ThoughtPriority.High;

            // 4. Всё остальное — низкий приоритет
            return ThoughtPriority.Low;
        }

        /// <summary>
        /// Определяет приоритет для внутреннего события/мысли
        /// </summary>
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

        /// <summary>
        /// Создаёт приоритезированную мысль из текста
        /// </summary>
        public PrioritizedThought CreateThought(string content, string source = "user")
        {
            var priority = source == "user"
                ? PrioritizeUserInput(content)
                : PrioritizeInternalEvent(source, content);

            return new PrioritizedThought(content, priority, source);
        }

        /// <summary>
        /// Фильтрует список мыслей, оставляя только те, что требуют ответа
        /// </summary>
        public IEnumerable<PrioritizedThought> FilterResponseRequired(
            IEnumerable<PrioritizedThought> thoughts,
            ThoughtPriority minPriority = ThoughtPriority.Medium)
        {
            return thoughts.Where(t => t.Priority >= minPriority && !t.CanBeIgnored);
        }

        /// <summary>
        /// Сортирует мысли по приоритету (критические первыми)
        /// </summary>
        public IEnumerable<PrioritizedThought> SortByPriority(IEnumerable<PrioritizedThought> thoughts)
        {
            return thoughts.OrderByDescending(t => (int)t.Priority)
                          .ThenByDescending(t => t.Timestamp);
        }

        /// <summary>
        /// Добавляет ключевое слово для критического приоритета
        /// </summary>
        public void AddCriticalKeyword(string keyword)
        {
            if (!string.IsNullOrWhiteSpace(keyword))
                _criticalKeywords.Add(keyword.Trim().ToLowerInvariant());
        }
    }
}