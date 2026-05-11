using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AiAssistantDesktopDemo.Core.Interfaces;
using AiAssistantDesktopDemo.Core.Models;

namespace AiAssistantDesktopDemo.Agents
{
    public class BasicMemoryAgent : BaseAgent
    {
        private readonly string _memoryPath;
        private bool _fasterFirstResponse;

        public BasicMemoryAgent(
            ILLMProvider llmProvider,
            string memoryPath,
            bool fasterFirstResponse = true)
            : base(llmProvider)
        {
            _memoryPath = memoryPath;
            _fasterFirstResponse = fasterFirstResponse;
            Name = "Michelle";
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            await LoadMemoryAsync();
        }

        public override async Task<AgentResponse> ProcessInputAsync(
            string userText, ConversationContext context)
        {
            if (_fasterFirstResponse && context.MessageCount <= 2)
            {
                _llmProvider.SetGenerationParams(maxTokens: 80);
            }
            else
            {
                _llmProvider.SetGenerationParams(maxTokens: 200);
            }

            return await base.ProcessInputAsync(userText, context);
        }

        public override async Task SaveMemoryAsync()
        {
            try
            {
                var directory = Path.GetDirectoryName(_memoryPath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                var memory = new
                {
                    SessionId = _context.SessionId,
                    Messages = _context.Messages.ToArray(),
                    SavedAt = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(memory, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(_memoryPath, json);
            }
            catch (Exception ex)
            {
                RaiseError($"Не удалось сохранить память: {ex.Message}");
            }
        }

        public override async Task LoadMemoryAsync()
        {
            try
            {
                if (!File.Exists(_memoryPath))
                    return;

                var json = await File.ReadAllTextAsync(_memoryPath);
                var memory = JsonSerializer.Deserialize<MemoryData>(json);

                if (memory?.Messages != null)
                {
                    foreach (var msg in memory.Messages)
                    {
                        if (msg.Role != "system")
                            _context.AddMessage(msg.Role, msg.Content);
                    }
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Не удалось загрузить память: {ex.Message}");
            }
        }

        private class MemoryData
        {
            public string? SessionId { get; set; }
            public Message[]? Messages { get; set; }
            public DateTime SavedAt { get; set; }
        }
    }
}