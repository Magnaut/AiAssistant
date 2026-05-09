using System.Collections.Generic;

namespace AiAssistantDesktop.Core.Models
{
    /// <summary>
    /// Контекст выполнения агента
    /// </summary>
    public class AgentContext
    {
        public string SessionId { get; set; } = string.Empty;
        public List<Message> History { get; } = new();
        public Dictionary<string, object> Metadata { get; } = new();

        public void AddMessage(string role, string content)
        {
            History.Add(new Message { Role = role, Content = content });
        }
    }
}