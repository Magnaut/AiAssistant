using System;

namespace AiAssistantDesktopDemo.Core.Models
{
    /// <summary>
    /// Сообщение в диалоге
    /// </summary>
    public record Message(string Role, string Content, DateTime Timestamp)
    {
        public Message(string role, string content) : this(role, content, DateTime.UtcNow) { }
    }
}