using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AiAssistantDesktop.Core.Interfaces;
using AiAssistantDesktop.Core.Models.Tool;

namespace AiAssistantDesktop.Modules.Tools
{
    public class DateTimeTool : ITool
    {
        public string Name => "get_datetime";
        public string Description => "Возвращает текущую дату и время.";
        public Dictionary<string, string> ParametersSchema => new();

        public Task<ToolResult> ExecuteAsync(Dictionary<string, string> parameters)
        {
            return Task.FromResult(ToolResult.Ok(DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")));
        }
    }

    public class CalculatorTool : ITool
    {
        public string Name => "calculate";
        public string Description => "Выполняет простые математические операции. Ожидает параметр 'expression'.";
        public Dictionary<string, string> ParametersSchema => new() { { "expression", "Математическое выражение, например: 2 + 2 * 3" } };

        public Task<ToolResult> ExecuteAsync(Dictionary<string, string> parameters)
        {
            if (!parameters.TryGetValue("expression", out var expr) || string.IsNullOrWhiteSpace(expr))
                return Task.FromResult(ToolResult.Fail("Не указано выражение для вычисления."));

            try
            {
                // ⚠️ Безопасный парсер для базовых операций. В продакшене используйте NCalc или аналог.
                var result = new System.Data.DataTable().Compute(expr, null);
                return Task.FromResult(ToolResult.Ok($"{expr} = {result}"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ToolResult.Fail($"Ошибка вычисления: {ex.Message}"));
            }
        }
    }

    public class MockSearchTool : ITool
    {
        public string Name => "web_search";
        public string Description => "Имитация поиска в интернете. Ожидает параметр 'query'.";
        public Dictionary<string, string> ParametersSchema => new() { { "query", "Поисковый запрос" } };

        public Task<ToolResult> ExecuteAsync(Dictionary<string, string> parameters)
        {
            var query = parameters.GetValueOrDefault("query", "ничего");
            return Task.FromResult(ToolResult.Ok($"🔍 [Имитация] Найдено по запросу '{query}': Пример результата поиска 1, Пример результата 2."));
        }
    }
}