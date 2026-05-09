using System;
using System.Collections.Generic;

namespace AiAssistantDesktopDemo.Core.Models
{
    /// <summary>
    /// Структурированный ответ агента
    /// </summary>
    public class AgentResponse
    {
        public string Text { get; set; } = "";
        public Emotion Emotion { get; set; } = Emotion.Neutral;
        public Dictionary<string, float> ExpressionParams { get; set; } = new();
        public string? Animation { get; set; }
        public string? Thought { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public float Confidence { get; set; } = 1.0f;
        public bool ShouldInterrupt { get; set; } = false;
        public bool IsFinal { get; set; } = true;
    }
}