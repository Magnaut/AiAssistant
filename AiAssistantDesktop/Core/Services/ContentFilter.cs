using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AiAssistantDesktop.Core.Services
{
    /// <summary>
    /// Централизованный фильтр контента для ввода/вывода
    /// </summary>
    public class ContentFilter
    {
        private readonly HashSet<string> _blockedWords;
        private readonly HashSet<Regex> _blockedPatterns;
        private readonly string _replacementText;

        public ContentFilter()
        {
            _replacementText = "[FILTERED]";
            _blockedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // Примеры блокируемых слов (добавляй по необходимости)
                "спам", "вирус", "взлом", "кража", "мошенник", "тест123"
            };

            _blockedPatterns = new HashSet<Regex>
            {
                // Паттерны для блокировки
                new Regex(@"http[s]?://[^\s]+", RegexOptions.IgnoreCase), // Ссылки (опционально)
                new Regex(@"\b\d{10,}\b"), // Длинные числа (номера карт и т.п.)
            };
        }

        /// <summary>
        /// Фильтрует входящий текст от пользователя
        /// </summary>
        public string FilterInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            return ApplyFilters(input.Trim());
        }

        /// <summary>
        /// Фильтрует исходящий текст от агента
        /// </summary>
        public string FilterOutput(string output)
        {
            if (string.IsNullOrWhiteSpace(output)) return string.Empty;
            return ApplyFilters(output.Trim());
        }

        /// <summary>
        /// Проверяет, был ли текст изменён фильтром
        /// </summary>
        public bool WasFiltered(string original, string filtered)
        {
            return !string.Equals(original, filtered, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Добавляет слово в чёрный список
        /// </summary>
        public void AddBlockedWord(string word)
        {
            if (!string.IsNullOrWhiteSpace(word))
                _blockedWords.Add(word.Trim().ToLowerInvariant());
        }

        /// <summary>
        /// Удаляет слово из чёрного списка
        /// </summary>
        public void RemoveBlockedWord(string word)
        {
            if (!string.IsNullOrWhiteSpace(word))
                _blockedWords.Remove(word.Trim().ToLowerInvariant());
        }

        /// <summary>
        /// Применяет все фильтры к тексту
        /// </summary>
        private string ApplyFilters(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            string result = text;

            // 1. Фильтрация по словам
            foreach (var word in _blockedWords)
            {
                result = Regex.Replace(result, $@"\b{Regex.Escape(word)}\b", _replacementText, RegexOptions.IgnoreCase);
            }

            // 2. Фильтрация по паттернам
            foreach (var pattern in _blockedPatterns)
            {
                result = pattern.Replace(result, _replacementText);
            }

            // 3. Базовая санитизация (защита от инъекций)
            result = result.Replace("<script>", "&lt;script&gt;")
                          .Replace("</script>", "&lt;/script&gt;")
                          .Replace("javascript:", "[BLOCKED]");

            return result;
        }
    }
}