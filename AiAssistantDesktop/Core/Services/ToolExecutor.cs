using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AiAssistantDesktop.Core.Interfaces;
using AiAssistantDesktop.Core.Models.Tool;

namespace AiAssistantDesktop.Core.Services
{
    public class ToolExecutor
    {
        private readonly ToolRegistry _registry;

        public ToolExecutor(ToolRegistry registry)
        {
            _registry = registry;
        }

        public async Task<(string response, bool usedTool)> TryExecuteToolAsync(string llmOutput)
        {
            // Ищем паттерн [TOOL:name] {params} [/TOOL]
            var match = Regex.Match(llmOutput, @"\[TOOL:(.*?)\]\s*(.*?)\s*\[/TOOL\]", RegexOptions.Singleline);
            if (!match.Success) return (llmOutput, false);

            var toolName = match.Groups[1].Value.Trim();
            var paramsJson = match.Groups[2].Value.Trim();

            var tool = _registry.GetTool(toolName);
            if (tool == null)
                return ($"⚠️ Инструмент '{toolName}' не найден.", true);

            try
            {
                // Простой парсер параметров (можно заменить на JSON парсер)
                var parameters = ParseParameters(paramsJson);
                var result = await tool.ExecuteAsync(parameters);

                return result.Success
                    ? ($" Результат инструмента {toolName}: {result.Content}", true)
                    : ($"❌ Ошибка инструмента {toolName}: {result.Error}", true);
            }
            catch (Exception ex)
            {
                return ($" Исключение при вызове {toolName}: {ex.Message}", true);
            }
        }

        private Dictionary<string, string> ParseParameters(string input)
        {
            var dict = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(input)) return dict;

            // Ожидается формат: key1: value1, key2: value2
            var pairs = input.Split(',');
            foreach (var pair in pairs)
            {
                var parts = pair.Split(':');
                if (parts.Length >= 2)
                    dict[parts[0].Trim()] = parts[1].Trim();
            }
            return dict;
        }
    }
}