namespace AiAssistantDesktop.Core.Models
{
    /// <summary>
    /// Сообщение в диалоге
    /// </summary>
    public class Message
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}