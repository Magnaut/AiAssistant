using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AiAssistantDesktop.Core.Interfaces;
using AiAssistantDesktop.Core.Models.Tool;

namespace AiAssistantDesktop.Core.Services
{
    public class ToolExecutor
    {
        private readonly ToolRegistry _registry;

        public ToolExecutor(ToolRegistry registry) => _registry = registry;

        public async Task<(string response, bool usedTool)> TryExecuteToolAsync(string llmOutput)
        {
            Debug.WriteLine($"🔍 Ищу инструмент в: {llmOutput}");

            // Гибкие паттерны для разных форматов
            var patterns = new[]
            {
                @"\[TOOL:([^\]]+)\](.*?)\[/TOOL\]",
                @"\[TOOL:([^\]]+)\](.*?)\[\/TOOL\]",
                @"\[([a-z_]+)\](.*?)(?:\[/\1\]|$)",
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(llmOutput, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var toolName = match.Groups[1].Value.Trim();
                    var paramsRaw = match.Groups[2].Value.Trim();

                    Debug.WriteLine($"🛠 Найден инструмент: '{toolName}', параметры: '{paramsRaw}'");

                    var tool = _registry.GetTool(toolName);
                    if (tool == null)
                    {
                        Debug.WriteLine($"❌ Инструмент '{toolName}' не найден");
                        return ($"⚠️ Инструмент '{toolName}' не найден.", true);
                    }

                    try
                    {
                        var parameters = ParseParameters(paramsRaw);
                        Debug.WriteLine($"📦 Параметры: {string.Join(", ", parameters)}");

                        var result = await tool.ExecuteAsync(parameters);
                        Debug.WriteLine($"✅ Результат: {result.Content}");

                        return result.Success ? (result.Content, true) : ($"❌ Ошибка {toolName}: {result.Error}", true);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"💥 Исключение: {ex.Message}");
                        return ($"💥 Ошибка {toolName}: {ex.Message}", true);
                    }
                }
            }

            Debug.WriteLine("🔍 Инструменты не найдены");
            return (llmOutput, false);
        }

        private Dictionary<string, string> ParseParameters(string input)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(input)) return dict;

            Debug.WriteLine($"🔧 Парсинг: '{input}'");

            // Формат: key: value, key2: value2
            var pairs = input.Split(',');
            foreach (var pair in pairs)
            {
                var parts = pair.Split(new[] { ':' }, 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();
                    if (!string.IsNullOrWhiteSpace(key))
                        dict[key] = value;
                }
            }
            return dict;
        }
    }
}