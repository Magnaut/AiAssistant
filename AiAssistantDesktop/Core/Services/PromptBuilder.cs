using System.Collections.Generic;
using System.Linq;
using System.Text;
using AiAssistantDesktop.Core.Models.Memory;

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

            sb.AppendLine($"Ты — {agentName}. Отвечай кратко на русском.");
            sb.AppendLine();

            var tools = _toolRegistry.GetAllTools();
            if (tools.Any())
            {
                sb.AppendLine("КОМАНДЫ (используй ТОЧНО так):");
                foreach (var tool in tools)
                {
                    sb.AppendLine($"[TOOL:{tool.Name}] {tool.Description}");
                    foreach (var p in tool.ParametersSchema)
                        sb.AppendLine($"  • {p.Key}: {p.Value}");
                }
                sb.AppendLine();
                sb.AppendLine("ПРИМЕРЫ:");
                sb.AppendLine("[TOOL:get_datetime] [/TOOL] — время");
                sb.AppendLine("[TOOL:calculate] expression: 2+2 [/TOOL] — посчитать");
                sb.AppendLine("[TOOL:clipboard] action: get [/TOOL] — буфер");
                sb.AppendLine("[TOOL:say_hello] [/TOOL] — привет");
                sb.AppendLine();
                sb.AppendLine("ПРАВИЛА:");
                sb.AppendLine("1. Если можно использовать команду — ОБЯЗАТЕЛЬНО используй.");
                sb.AppendLine("2. Формат: [TOOL:имя] параметры [/TOOL]");
                sb.AppendLine("3. После команды напиши короткий ответ.");
                sb.AppendLine();
            }

            var memory = _memoryManager.GetContextForLLM(maxShort: 3, maxMedium: 2, maxLong: 0);
            if (!string.IsNullOrWhiteSpace(memory))
            {
                sb.AppendLine("ПАМЯТЬ:");
                sb.AppendLine(memory);
            }

            return sb.ToString();
        }

        public string BuildUserPrompt(string userInput, string sessionFilesContext = "")
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(sessionFilesContext))
            {
                sb.AppendLine("ФАЙЛЫ:");
                sb.AppendLine(sessionFilesContext);
            }
            sb.AppendLine($"Пользователь: {userInput}");
            return sb.ToString();
        }
    }
}