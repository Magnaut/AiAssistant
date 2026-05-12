using System;
using System.Text;
using AiAssistantDesktop.Core.Models.Memory;

namespace AiAssistantDesktop.Core.Services
{
    public class ProactivityManager
    {
        private readonly MemoryManager _memory;
        private readonly object _lockObj = new();
        private DateTime _lastInteraction;
        private int _unansweredCount;
        private bool _isDoNotDisturb;
        private readonly Random _random;

        public ProactivityManager(MemoryManager memory)
        {
            _memory = memory;
            _lastInteraction = DateTime.UtcNow;
            _unansweredCount = 0;
            _isDoNotDisturb = false;
            _random = new Random();
        }

        public bool ShouldTriggerThought()
        {
            if (_isDoNotDisturb) return false;

            lock (_lockObj)
            {
                var timeSinceLast = DateTime.UtcNow - _lastInteraction;

                // Триггер 1: Прошло больше 5 минут с последнего разговора
                if (timeSinceLast.TotalMinutes > 5)
                {
                    // Добавляем элемент случайности (70% шанс), чтобы не спамить
                    return _random.Next(0, 100) < 70;
                }

                // Триггер 2: Накопились неотвеченные запросы
                return _unansweredCount > 0;
            }
        }

        public string GetProactiveContext()
        {
            var sb = new StringBuilder();
            lock (_lockObj)
            {
                var timeSince = DateTime.UtcNow - _lastInteraction;
                sb.AppendLine($"Время с последнего разговора: {timeSince.TotalMinutes:F0} мин.");
                sb.AppendLine($"Неотвеченных запросов: {_unansweredCount}");
            }

            var memoryContext = _memory.GetContextForLLM(maxShort: 3, maxMedium: 2, maxLong: 1);
            if (!string.IsNullOrWhiteSpace(memoryContext))
                sb.AppendLine($"Память: {memoryContext}");

            return sb.ToString();
        }

        public void RecordInteraction()
        {
            lock (_lockObj)
            {
                _lastInteraction = DateTime.UtcNow;
                _unansweredCount = 0;
            }
        }

        public void RecordUnanswered()
        {
            lock (_lockObj)
            {
                _unansweredCount++;
            }
        }

        public void SetDoNotDisturb(bool enabled) => _isDoNotDisturb = enabled;
    }
}