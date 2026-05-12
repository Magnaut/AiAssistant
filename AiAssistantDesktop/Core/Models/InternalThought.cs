using System;

namespace AiAssistantDesktop.Core.Models
{
    public enum ThoughtType
    {
        Background,              // Фоновое размышление (не требует ответа)
        Proactive,               // Проактивное сообщение (может озвучить)
        MemoryConsolidation,     // Консолидация/анализ памяти
        ToolReflection           // Рефлексия после использования инструмента
    }

    public class InternalThought
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Content { get; set; } = string.Empty;
        public ThoughtType Type { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool ShouldSpeak { get; set; } = false;
        public float Priority { get; set; } = 0.5f; // 0.0 - 1.0
        public string? ContextHint { get; set; }
    }
}