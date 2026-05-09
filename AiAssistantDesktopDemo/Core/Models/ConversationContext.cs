using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AiAssistantDesktopDemo.Core.Models
{
    public class ConversationContext
    {
        private readonly List<Message> _messages = new();
        private readonly int _maxHistory;

        public string SessionId { get; } = Guid.NewGuid().ToString();
        public string? Persona { get; set; }

        public IReadOnlyList<Message> Messages => _messages.AsReadOnly();
        public int MessageCount => _messages.Count;

        public ConversationContext(int maxHistory = 20)
        {
            _maxHistory = maxHistory;
        }

        public void AddMessage(string role, string content)
        {
            _messages.Add(new Message(role, content));
            while (_messages.Count > _maxHistory)
                _messages.RemoveAt(0);
        }

        public void Clear() => _messages.Clear();

        public string ExportForPrompt(string format = "qwen")
        {
            return format.ToLower() switch
            {
                "qwen" => ExportQwenFormat(),
                "llama" => ExportLlamaFormat(),
                _ => ExportQwenFormat()
            };
        }

        private string ExportQwenFormat()
        {
            var sb = new StringBuilder();
            foreach (var msg in _messages)
            {
                sb.AppendLine($"<|{msg.Role}|>");
                sb.AppendLine(msg.Content);
            }
            sb.Append("<|assistant|>");
            return sb.ToString();
        }

        private string ExportLlamaFormat()
        {
            var sb = new StringBuilder();
            foreach (var msg in _messages)
            {
                if (msg.Role == "system")
                    sb.AppendLine($"<<SYS>>\n{msg.Content}\n<</SYS>>");
                else if (msg.Role == "user")
                    sb.AppendLine($"[INST] {msg.Content} [/INST]");
                else
                    sb.AppendLine(msg.Content);
            }
            return sb.ToString();
        }
    }
}