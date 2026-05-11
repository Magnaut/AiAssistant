namespace AiAssistantDesktop.Core.Models
{
    /// <summary>
    /// Уровень приоритета мысли/события
    /// </summary>
    public enum ThoughtPriority
    {
        /// <summary>
        /// Фоновые размышления, не требующие ответа
        /// </summary>
        Low = 0,

        /// <summary>
        /// Обычные события, можно ответить при возможности
        /// </summary>
        Medium = 1,

        /// <summary>
        /// Важные события, стоит ответить
        /// </summary>
        High = 2,

        /// <summary>
        /// Критические события (упоминание имени, экстренные запросы) — ответить немедленно
        /// </summary>
        Critical = 3
    }

    /// <summary>
    /// Обёртка для мысли с приоритетом
    /// </summary>
    public class PrioritizedThought
    {
        public string Content { get; set; } = string.Empty;
        public ThoughtPriority Priority { get; set; } = ThoughtPriority.Medium;
        public string? Source { get; set; } // "user", "internal", "tool", etc.
        public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public PrioritizedThought() { }

        public PrioritizedThought(string content, ThoughtPriority priority, string? source = null)
        {
            Content = content;
            Priority = priority;
            Source = source;
        }

        /// <summary>
        /// Определяет, требует ли мысль немедленного ответа
        /// </summary>
        public bool RequiresImmediateResponse => Priority >= ThoughtPriority.High;

        /// <summary>
        /// Определяет, можно ли проигнорировать мысль
        /// </summary>
        public bool CanBeIgnored => Priority == ThoughtPriority.Low;
    }
}