namespace AiAssistantDesktop.Core.Models
{
    public class AgentResponse
    {
        public string Text { get; set; } = string.Empty;
        public string? Thought { get; set; }
        public Emotion Emotion { get; set; } = Emotion.Neutral;
        public string? Animation { get; set; }
        public float Confidence { get; set; } = 1.0f;
    }
}