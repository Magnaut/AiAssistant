using System.Text;
using AiAssistantDesktop.Core.Models.Memory;
using AiAssistantDesktop.Core.Services;

namespace AiAssistantDesktop.Core.Services
{
    public class PromptBuilder
    {
        private readonly MemoryManager _memoryManager;
        private readonly ToolRegistry _toolRegistry;

        public PromptBuilder(MemoryManager memoryManager, ToolRegistry toolRegistry)
        {
            _memoryManager = memoryManager;
            _toolRegistry = toolRegistry;
        }

        public string BuildSystemPrompt(string agentName = "Michelle")
        {
            var sb = new StringBuilder();

            // 🔥 МАКСИМАЛЬНО УПРОЩЕНО
            sb.AppendLine($"Ты {agentName}. Отвечай кратко.");

            // 🔥 Только критическая память (последние 3 факта)
            var memoryContext = _memoryManager.GetContextForLLM(maxShort: 3, maxMedium: 2, maxLong: 0);
            if (!string.IsNullOrWhiteSpace(memoryContext))
            {
                sb.AppendLine($"Память: {memoryContext}");
            }

            return sb.ToString();
        }

        public string BuildUserPrompt(string userInput, string sessionFilesContext = "")
        {
            return $"Пользователь: {userInput}";
        }
    }
}